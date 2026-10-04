
' Lecture des jalons opérationnels d'une mission (En route / Sur place / Terminé) — Result pattern.
Public Class ClGetTimeUseCase
    Implements IResultUseCase(Of ClJobTimeModel)

    Private ReadOnly _jobId As Guid
    Private ReadOnly _repository As IJobRepository

    Public Sub New(gJobId As Guid, repository As IJobRepository)
        _jobId = gJobId
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClJobTimeModel) Implements IResultUseCase(Of ClJobTimeModel).Handle

        Dim jobTime As ClJobTimeData = _repository.GetJobTime(_jobId)
        If jobTime Is Nothing Then
            jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).Build
        End If

        Return ClResult(Of ClJobTimeModel).Ok(jobTime.ToJobTimeModel)

    End Function

End Class
