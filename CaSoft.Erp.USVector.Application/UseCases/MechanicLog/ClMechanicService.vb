Public Class ClMechanicService

    Private _repository As ILogAnalyzeRepository

    Public Sub New(repository As ILogAnalyzeRepository)
        _repository = repository
    End Sub
    Public Function GetLogs(gCrewId As Guid) As ClResponseHandler(Of List(Of ClLogEntryModel))

        Dim useCase = New ClGetMechanicLogUseCase(gCrewId, _repository)
        Dim handler As New ClResponseHandler(Of List(Of cllogentrymodel))
        useCase.Execute(handler)

        Return handler

    End Function
    Public Function GetAnalyze(intLogId As Integer) As ClResponseHandler(Of ClGetLogAnalyzeModel)


        Dim useCase = New ClGetLogAnalyzeUseCase(intLogId, _repository)
        Dim handler As New ClResponseHandler(Of ClGetLogAnalyzeModel)
        useCase.Execute(handler)

        Return handler

    End Function
    Public Function InsertAnalyze(analyze As ClEditLogAnalyzeModel) As ClNoResponseHandler

        Dim useCase = New ClInsertLogAnalyzeUseCase(analyze, _repository)
        Dim handler As New ClNoResponseHandler
        useCase.Execute(handler)

        Return handler

    End Function
    Public Function UpdateAnalyze(analyze As ClEditLogAnalyzeModel) As ClNoResponseHandler
        Dim useCase = New ClUpdateLogAnalyzeUseCase(analyze, _repository)
        Dim handler As New ClNoResponseHandler
        useCase.Execute(handler)

        Return handler
    End Function
    Public Function DeleteAnalyze(intId As Integer) As ClNoResponseHandler
        Dim useCase = New ClDeleteLogAnalyzeUseCase(intId, _repository)
        Dim handler As New ClNoResponseHandler
        useCase.Execute(handler)

        Return handler

    End Function

End Class


Public Class ClResponseHandler(Of T)
    Implements IResponseHandler

    Private _response As ClUseCaseResponseBase
    Public Sub Handle(response As ClUseCaseResponseBase) Implements IResponseHandler.Handle
        _response = response.Data

    End Sub
    Public ReadOnly Property Result As T
        Get
            Return _response.Data
        End Get
    End Property
    Public ReadOnly Property IsSuccess As Boolean
        Get
            Return _response.IsSuccess
        End Get
    End Property
    Public ReadOnly Property ErrorText As String
        Get
            Return _response.ErrorText
        End Get
    End Property

End Class

Public Class ClNoResponseHandler
    Implements IResponseHandler

    Private _response As ClUseCaseResponseBase
    Public Sub Handle(response As ClUseCaseResponseBase) Implements IResponseHandler.Handle
        _response = response

    End Sub
    Public ReadOnly Property IsSuccess As Boolean
        Get
            Return _response.IsSuccess
        End Get
    End Property
    Public ReadOnly Property ErrorText As String
        Get
            Return _response.ErrorText
        End Get
    End Property

End Class
