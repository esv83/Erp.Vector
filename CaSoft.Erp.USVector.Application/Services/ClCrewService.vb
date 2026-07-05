Imports CaSoft.Erp.USVector.Application.Dto

Public Class ClCrewService
    Implements ICrewService

    Private _repository As ICrewRepository
    Private _crewList As ClCrewListCache '(Of ClCrew)
    Private _lastLocationRefresh As Date
    Public Sub New(repository As ICrewRepository)
        _repository = repository
        _crewList = New ClCrewListCache(repository)
        _lastLocationRefresh = DateTime.Now.AddHours(-1)
    End Sub

#Region "Public members"
    Public Sub GetDriver(gCrewId As Guid, handler As IResponseHandler) Implements ICrewService.GetDriver

        Dim useCase = New ClGetDriverUseCase(gCrewId, _repository)
        Dim adapter As New ClResultUseCaseAdapter(Of ClLogDriverModel)(useCase)
        adapter.Execute(handler)

    End Sub
    Public Sub ChangeDriver(gCrewId As Guid, gEmployeeId As Guid, handler As IResponseHandler) Implements ICrewService.ChangeDriver

        Dim command = New ClSetDriverCommand(gCrewId, gEmployeeId)
        Dim UseCase = New ClSetDriverUseCase(command, _repository)

        UseCase.execute(handler)



    End Sub
    <Obsolete> Public Function GetEndOfService(gCrewId As Guid) As IUseCaseResponse(Of ClReliableDateModel) Implements ICrewService.GetEndOfService
        Dim Query = New ClQueryId(Of Guid)(gCrewId)
        Dim useCase = New ClGetEndOfServiceUseCase(gCrewId, _repository)

        Dim presenter As New ClDefaultPresenter
        Dim adapter As New ClResultUseCaseAdapter(Of ClReliableDateModel)(useCase)
        adapter.Execute(presenter)

        Return presenter.Response

    End Function
    <Obsolete> Public Function SetEndOfService(gCrewId As Guid, dteDate As Date, source As String) As IUseCaseResponse(Of Boolean) Implements ICrewService.SendEndOfService

        Dim crew As ClCrew = GetCrew(gCrewId)

        Dim query As New ClSetEndOfServiceCommand(gCrewId, dteDate, source)
        Dim useCase As New ClSetEndOfServiceUseCase(query, Nothing, _repository)
        Dim presenter As New ClDefaultPresenter

        Dim adapter As New ClResultUseCaseAdapter(Of Boolean)(useCase)
        adapter.Execute(presenter)

        Return presenter.Response

    End Function
    <Obsolete> Public Function GetKilometers(gCrewId As Guid) As IUseCaseResponse(Of ClKmModel) Implements ICrewService.GetKilometers
        'Dim useCase = New ClGetKilometersUseCase(gCrewId, _repository)

        'Dim presenter As New ClDefaultPresenter
        'useCase.Execute(presenter)

        'Return presenter.Response

    End Function
    <Obsolete> Public Function SetKilometers(gCrewId As Guid, intKm As Integer) As IUseCaseResponse(Of Boolean) Implements ICrewService.SetKilometers
        'Dim Query = New ClSetKilometersCommand(GetCrew(gCrewId), intKm, "ByCrew")
        'Dim Response = New ClUseCaseResponse(Of Boolean)
        'Dim useCase = New ClSetKilometersUseCase(Query, _repository)

        'Dim presenter As New ClDefaultPresenter
        'useCase.Execute(presenter)

        ' Return presenter.Response

    End Function
    Public Sub AddStartOfServiceInfo(gCrewId As Guid, dteTime As DateTime, infoSource As ValueInfoSource)
        Dim crew = GetCrew(gCrewId)

    End Sub
    Public Sub AddEndOfServiceInfo(gCrewId As Guid, dteTime As DateTime, source As ValueInfoSource)
        Dim valueInfo As New ClEndOfServiceInfo(dteTime, source)

        Dim crew = GetCrew(gCrewId)
        crew.ServiceEndDateR.AddValueInfo(valueInfo)

    End Sub
    Public Sub GetJobList(gCrewId As Guid, handler As IResponseHandler) Implements ICrewService.GetJobList

        Dim useCase = New ClGetJobListUseCase(New List(Of Guid) From {gCrewId}, _repository)

        Dim adapter As New ClResultUseCaseAdapter(Of ClJobListModel)(useCase)
        adapter.Execute(handler)


    End Sub
    Public Function ReadInstruction(gJobId As Integer) As Boolean Implements ICrewService.ReadInstruction

        Dim command As New ClQueryId(Of Integer)(gJobId)
        Dim UseCase = New ClAckInstructionUseCase(gJobId, _repository)

        Dim presenter As New ClDefaultPresenter
        Dim adapter As New ClResultUseCaseAdapter(Of Boolean)(UseCase)
        adapter.Execute(presenter)

        Return False
        'Return presenter.Response

    End Function

#End Region
    Private Function GetCrew(gCrewId As Guid) As ClCrew
        Dim result As ClCrew

        result = _crewList.SingleOrDefault(Function(f) f.CrewId = gCrewId)
        If result Is Nothing Then
            result = _repository.GetCrew(gCrewId)
            _crewList.Add(result)
        End If

        Return result
    End Function
    Public Function GetCrewList(dteDate As DateOnly) As IUseCaseResponse(Of List(Of ClCrewListModel)) Implements ICrewService.GetCrewList
        Dim result As New List(Of ClCrewListModel)


        Dim UseCase = New ClGetCrewIdListUseCase(dteDate, _repository)
        Dim presenter As New ClDefaultPresenter
        Dim adapter As New ClResultUseCaseAdapter(Of List(Of Guid))(UseCase)
        adapter.Execute(presenter)



        'TODO Ecrire un Use Case

        Dim finalResponse As New ClUseCaseResponse(Of List(Of ClCrewListModel))

        If presenter.Response.IsSuccess Then
            For Each gCrewId In presenter.Response.Data
                Dim crew = GetCrew(gCrewId)
                result.Add(New ClCrewListModel(crew))
            Next
            finalResponse.SetResult(result)
        Else
            ' response.AddError("Pas d'equipage à cette date")
        End If

        Return finalResponse

    End Function
    Friend Class ClCrewListCache
        Inherits List(Of ClCrew)

        Private _repository As ICrewRepository


        Public Sub New(repository As ICrewRepository)
            _repository = repository
        End Sub

        Private Function GetCrewFromCache(gCrewId As Guid) As ClCrew
            Dim result As ClCrew

            result = Me.SingleOrDefault(Function(f) f.CrewId = gCrewId)

            Return result

        End Function
        Private Function GetCrewFromDatabase(gCrewId As Guid) As IUseCaseResponse(Of ClCrew)

            Dim useCase As New ClGetCrewUseCase(gCrewId, _repository)
            Dim presenter As New ClDefaultPresenter
            Dim adapter As New ClResultUseCaseAdapter(Of ClCrew)(useCase)
            adapter.Execute(presenter)

            Return presenter.Response

        End Function
        Public Function GetCrew(gCrewId As Guid) As ClCrew
            Dim result As ClCrew

            result = GetCrewFromCache(gCrewId)

            If result Is Nothing Then
                Dim response = GetCrewFromDatabase(gCrewId)
                If response.IsSuccess Then
                    result = response.Result
                    Me.Add(result)
                End If
            End If

            Return result

        End Function

    End Class

End Class
