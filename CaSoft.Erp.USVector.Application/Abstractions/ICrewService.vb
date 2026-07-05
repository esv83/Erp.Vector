Imports CaSoft.Erp.USVector.Application.Dto

Public Interface ICrewService
    Function GetDriver(gCrewId As Guid) As ClResult(Of ClLogDriverModel)
    Function ChangeDriver(gCrewId As Guid, gEmployeeId As Guid) As ClResult(Of Boolean)
End Interface
