
' Retour arrière : efface un jalon opérationnel (Mission vue / En route / Sur place / Terminé).
' L'effacement est appliqué en BD Mobile (via SaveJobTime) qui inscrit l'Outbox → le worker projette
' le snapshot consolidé (jalon à null) vers Orders.Api (propagé à la régulation dès qu'Orders.Api
' traite « null = effacé », cf. endPoint.md §3).
Public Class ClClearJobTimeUseCase
    Inherits ClUseCaseBase

    Private _repository As IJobRepository
    Private _jobId As Guid
    Private _jalon As String

    Public Sub New(jobId As Guid, jalon As String, repository As IJobRepository)
        _jobId = jobId
        _jalon = jalon
        _repository = repository
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler)

        Try
            If CanExecute() Then

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
                        Response.AddError($"Jalon inconnu : « {_jalon} ». Attendu : seen | go | onsite | terminate.")
                        Return
                End Select

                ' Upsert BD Mobile (jalon effacé) + enqueue Outbox → projection consolidée (retour arrière).
                _repository.SaveJobTime(jobTime)
                Response.SetResult(True)
            End If

        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try

    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
