Imports System.Threading

' Lecture des jalons opérationnels d'une mission sous forme de timeline ordonnée + labellisée
' (Option A — contrat riche auto-suffisant pour l'UI). Result pattern.
Public Class ClGetJobTimelineUseCase
    Implements IResultUseCaseAsync(Of ClJobTimelineDtoOut)

    Private ReadOnly _jobId As Guid
    Private ReadOnly _repository As IJobRepository

    Public Sub New(gJobId As Guid, repository As IJobRepository)
        _jobId = gJobId
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClJobTimelineDtoOut)) Implements IResultUseCaseAsync(Of ClJobTimelineDtoOut).HandleAsync

        Dim jobTime As ClJobTimeData = Await _repository.GetJobTimeAsync(_jobId, ct)
        If jobTime Is Nothing Then
            jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).Build
        End If

        Return ClResult(Of ClJobTimelineDtoOut).Ok(jobTime.ToJobTimelineDtoOut)

    End Function

End Class
