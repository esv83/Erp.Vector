Imports CaSoft.Framework.Application
Imports System.Threading

Public Class ClJobService
    Implements IJobService

    Private ReadOnly _jobRepository As IJobRepository
    Private ReadOnly _jobTimeRepository As IJobTimeRepository
    Private ReadOnly _unitOfWork As IUnitOfWork

    Public Sub New(jobRepository As IJobRepository, jobTimeRepository As IJobTimeRepository, unitOfWork As IUnitOfWork)
        _jobRepository = jobRepository
        _jobTimeRepository = jobTimeRepository
        _unitOfWork = unitOfWork
    End Sub

    Public Function MarkMissionSeenAsync(gJobId As Guid, seenAt As DateTimeOffset?, ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IJobService.MarkMissionSeenAsync
        Return New ClMarkMissionSeenUseCase(gJobId, _jobTimeRepository, _unitOfWork, seenAt).HandleAsync(ct)
    End Function

    Public Function GetJobTimeAsync(gJobId As Guid, ct As CancellationToken) As Task(Of ClResult(Of ClJobTimeModel)) Implements IJobService.GetJobTimeAsync
        Return New ClGetTimeUseCase(gJobId, _jobRepository).HandleAsync(ct)
    End Function

    Public Function GetJobTimelineAsync(gJobId As Guid, ct As CancellationToken) As Task(Of ClResult(Of ClJobTimelineDtoOut)) Implements IJobService.GetJobTimelineAsync
        Return New ClGetJobTimelineUseCase(gJobId, _jobRepository).HandleAsync(ct)
    End Function

    Public Function SetJobTimeAsync(gJobId As Guid, jobTimeModel As ClJobTimeModel, ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IJobService.SetJobTimeAsync
        Dim jobTimeCommand = New ClJobTimeCommand(gJobId, jobTimeModel)
        Return New ClUpdateTimeUseCase(jobTimeCommand, _jobRepository, _unitOfWork).HandleAsync(ct)
    End Function

    Public Function ClearJobTimeAsync(gJobId As Guid, jalon As String, ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IJobService.ClearJobTimeAsync
        Return New ClClearJobTimeUseCase(gJobId, jalon, _jobRepository, _unitOfWork).HandleAsync(ct)
    End Function

End Class
