Imports System.Threading

' Retour arrière : efface un jalon opérationnel (Mission vue / En route / Sur place / Terminé) — Result pattern.
' L'effacement est appliqué en BD Mobile (via SaveJobTime) qui inscrit l'Outbox → le worker projette
' le snapshot consolidé (jalon à null) vers Orders.Api (propagé à la régulation dès qu'Orders.Api
' traite « null = effacé », cf. endPoint.md §3).
Public Class ClClearJobTimeUseCase
    Implements IResultUseCaseAsync(Of Boolean)

    Private ReadOnly _repository As IJobRepository
    Private ReadOnly _jobId As Guid
    Private ReadOnly _jalon As String

    Public Sub New(jobId As Guid, jalon As String, repository As IJobRepository)
        _jobId = jobId
        _jalon = jalon
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IResultUseCaseAsync(Of Boolean).HandleAsync

        Dim jobTime As ClJobTimeData = Await _repository.GetJobTimeAsync(_jobId, ct)

        'TODO remplacer par enum quand VB.NET le supportera.
        Select Case _jalon?.Trim().ToLowerInvariant()
            Case "seen", "read"
                jobTime.ReadTime = Nothing
            Case "go", "enroute"
                jobTime.GoTime = Nothing
            Case "onsite", "surplace"
                jobTime.OnSiteTime = Nothing
            Case "terminate", "terminated", "termine", "disponible"
                jobTime.TerminateTime = Nothing
            Case Else
                Return ClResult(Of Boolean).Fail(
                    ClError.Application($"Jalon inconnu : « {_jalon} ». Attendu : seen | go | onsite | terminate | disponible."))
        End Select

        ' Upsert BD Mobile (jalon effacé) + enqueue Outbox → projection consolidée (retour arrière).
        Await _repository.SaveJobTimeAsync(jobTime, ct)
        Return ClResult(Of Boolean).Ok(True)

    End Function

End Class
