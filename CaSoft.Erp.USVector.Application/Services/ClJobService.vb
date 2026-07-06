
Public Class ClJobService
    Implements IJobService

    Private ReadOnly _jobRepository As IJobRepository
    Private ReadOnly _jobTimeRepository As IJobTimeRepository

    Public Sub New(jobRepository As IJobRepository, jobTimeRepository As IJobTimeRepository)
        _jobRepository = jobRepository
        _jobTimeRepository = jobTimeRepository
    End Sub

    Public Function MarkMissionSeen(gJobId As Guid) As ClResult(Of Boolean) Implements IJobService.MarkMissionSeen
        Return New ClMarkMissionSeenUseCase(gJobId, _jobTimeRepository).Handle()
    End Function

    Public Function GetJobTime(gJobId As Guid) As ClResult(Of ClJobTimeModel) Implements IJobService.GetJobTime
        Return New ClGetTimeUseCase(gJobId, _jobRepository).Handle()
    End Function

    Public Function GetJobTimeline(gJobId As Guid) As ClResult(Of ClJobTimelineDtoOut) Implements IJobService.GetJobTimeline
        Return New ClGetJobTimelineUseCase(gJobId, _jobRepository).Handle()
    End Function

    Public Function SetJobTime(gJobId As Guid, jobTimeModel As ClJobTimeModel) As ClResult(Of Boolean) Implements IJobService.SetJobTime
        Dim jobTimeCommand = New ClJobTimeCommand(gJobId, jobTimeModel)
        Return New ClUpdateTimeUseCase(jobTimeCommand, _jobRepository).Handle()
    End Function

    Public Function ClearJobTime(gJobId As Guid, jalon As String) As ClResult(Of Boolean) Implements IJobService.ClearJobTime
        Return New ClClearJobTimeUseCase(gJobId, jalon, _jobRepository).Handle()
    End Function

End Class
