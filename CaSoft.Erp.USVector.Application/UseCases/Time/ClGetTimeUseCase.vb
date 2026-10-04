Imports System.Threading

' Lecture des jalons opérationnels d'une mission (En route / Sur place / Terminé) — Result pattern.
Public Class ClGetTimeUseCase
    Implements IResultUseCaseAsync(Of ClJobTimeModel)

    Private ReadOnly _jobId As Guid
    Private ReadOnly _repository As IJobRepository

    Public Sub New(gJobId As Guid, repository As IJobRepository)
        _jobId = gJobId
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClJobTimeModel)) Implements IResultUseCaseAsync(Of ClJobTimeModel).HandleAsync

        Dim jobTime As ClJobTimeData = Await _repository.GetJobTimeAsync(_jobId, ct)
        If jobTime Is Nothing Then
            jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).Build
        End If

        Return ClResult(Of ClJobTimeModel).Ok(jobTime.ToJobTimeModel)

    End Function

End Class
