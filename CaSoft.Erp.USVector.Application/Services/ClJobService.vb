
Public Class ClJobService
    Implements IJobService

    Private _jobRepository As IJobRepository
    Private _jobTimeRepository As IJobTimeRepository
    ' Private _responseHandler As IResponseHandler
    Private _jobCache As ClJobListCache

    Public Sub New(jobRepository As IJobRepository, jobTimeRepository As IJobTimeRepository)
        _jobRepository = jobRepository
        _jobTimeRepository = jobTimeRepository
        ' _responseHandler = presenter
        ' _jobCache = New ClJobListCache(_repository)
    End Sub
    Public Sub SignJob()

    End Sub

#Region "Public properties"
    Public Sub GetJobEditFormStructure(gJobId As Guid, handler As IResponseHandler) Implements IJobService.GetJobEditFormStructure


        Dim useCase = New ClGetJobEditFormStructureUseCase(gJobId, _jobRepository)

        useCase.execute(handler)

    End Sub
    Public Function GetJobValue(gJobId As Guid) As Object Implements IJobService.GetJobValue


        Dim UseCase = New ClGetJobUseCase(gJobId, _jobCache)
        Dim presenter As New ClDefaultPresenter
        UseCase.execute(presenter)

        Return presenter

    End Function
    <Obsolete> Public Function UpdateAttributValues(gJobId As Guid, attributsValues As List(Of ClAttributValueModel)) As Boolean Implements IJobService.UpdateAttributValues

        Dim Cmd = New ClUpdateJobEditCommand(gJobId, attributsValues)
        Dim presenter As New ClDefaultPresenter()
        Dim UseCase = New ClUpdateJobEditUseCase(Cmd, _jobCache, _jobRepository)

        UseCase.execute(presenter)

        Return presenter.Response.Data

    End Function
    '<Obsolete> Public Function ReadJob(gJobId As Guid) As IUseCaseResponse(Of Boolean) Implements IJobService.ReadJob

    '    Dim UseCase = New ClReadJobUseCase(gJobId, _jobCache, _repository)

    '    Dim presenter As New ClDefaultPresenter
    '    UseCase.execute(presenter)

    '    Return presenter.Response.Data

    'End Function


#End Region

    Public Sub SetJobTime(gJobId As Guid, jobTimeModel As ClJobTimeModel, handler As IResponseHandler) Implements IJobService.SetJobTime

        Dim jobTimeCommand = New ClJobTimeCommand(gJobId, jobTimeModel)
        Dim useCase = New ClUpdateTimeUseCase(jobTimeCommand, _jobRepository)

        useCase.execute(handler)

    End Sub
    Public Sub EditMission()

    End Sub

    Public Sub GetJobTime(gJobId As Guid, handler As IResponseHandler) Implements IJobService.GetJobTime

        Dim useCase As New ClGetTimeUseCase(gJobId, _jobRepository)

        useCase.execute(handler)



    End Sub
    <Obsolete> Public Function GetJobDetail(gJobId As Guid) As Object Implements IJobService.GetJobDetail

        'Dim UseCase = New ClGetJobUseCase(gJobId)
        'Dim presenter As New ClDefaultPresenter
        'UseCase.Execute(presenter)

        'Return presenter.Response.Data

    End Function

    Public Sub ReadJob(gJobId As Guid, handler As IResponseHandler) Implements IJobService.ReadJob
        Dim useCase As New ClReadJobUseCase(gJobId, _jobTimeRepository)
        useCase.execute(handler)

    End Sub

End Class
