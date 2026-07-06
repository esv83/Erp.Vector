
' Lecture des jalons opérationnels d'une mission sous forme de timeline ordonnée + labellisée
' (Option A — contrat riche auto-suffisant pour l'UI). Result pattern.
Public Class ClGetJobTimelineUseCase
    Implements IResultUseCase(Of ClJobTimelineDtoOut)

    Private ReadOnly _jobId As Guid
    Private ReadOnly _repository As IJobRepository

    Public Sub New(gJobId As Guid, repository As IJobRepository)
        _jobId = gJobId
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClJobTimelineDtoOut) Implements IResultUseCase(Of ClJobTimelineDtoOut).Handle

        Try
            Dim jobTime As ClJobTimeData = _repository.GetJobTime(_jobId)
            If jobTime Is Nothing Then
                jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).Build
            End If

            Return ClResult(Of ClJobTimelineDtoOut).Ok(jobTime.ToJobTimelineDtoOut)

        Catch ex As Exception
            Return ClResult(Of ClJobTimelineDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
