
Public Interface ICrewService
    Sub GetJobList(gCrewId As Guid, handler As IResponseHandler)
    Sub GetDriver(gCrewId As Guid, handler As IResponseHandler)
    Sub ChangeDriver(gCrewId As Guid, gEmployeeId As Guid, handler As IResponseHandler)
    Function GetEndOfService(gCrewId As Guid) As IUseCaseResponse(Of ClReliableDateModel)
    Function SendEndOfService(gCrewId As Guid, dteDate As Date, source As String) As IUseCaseResponse(Of Boolean)
    Function GetKilometers(gCrewId As Guid) As IUseCaseResponse(Of ClKmModel)
    Function SetKilometers(gCrewId As Guid, intKm As Integer) As IUseCaseResponse(Of Boolean)
    Function ReadInstruction(intInstructionId As Integer) As Boolean
    Function GetCrewList(dteDate As DateOnly) As IUseCaseResponse(Of List(Of ClCrewListModel))

End Interface
