
' Retour arrière : efface un jalon opérationnel (Mission vue / En route / Sur place / Terminé) — Result pattern.
' L'effacement est appliqué en BD Mobile (via SaveJobTime) qui inscrit l'Outbox → le worker projette
' le snapshot consolidé (jalon à null) vers Orders.Api (propagé à la régulation dès qu'Orders.Api
' traite « null = effacé », cf. endPoint.md §3).
Public Class ClClearJobTimeUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As IJobRepository
    Private ReadOnly _jobId As Guid
    Private ReadOnly _jalon As String

    Public Sub New(jobId As Guid, jalon As String, repository As IJobRepository)
        _jobId = jobId
        _jalon = jalon
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            Dim jobTime As ClJobTimeData = _repository.GetJobTime(_jobId)

            Select Case _jalon?.Trim().ToLowerInvariant()
                Case "seen", "read"
                    jobTime.ReadTime = Nothing
                Case "go", "enroute"
                    jobTime.GoTime = Nothing
                Case "onsite", "surplace"
                    jobTime.OnSiteTime = Nothing
                Case "terminate", "terminated", "termine"
                    jobTime.TerminateTime = Nothing
                Case Else
                    Return ClResult(Of Boolean).Fail(
                        ClError.Application($"Jalon inconnu : « {_jalon} ». Attendu : seen | go | onsite | terminate."))
            End Select

            ' Upsert BD Mobile (jalon effacé) + enqueue Outbox → projection consolidée (retour arrière).
            _repository.SaveJobTime(jobTime)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
