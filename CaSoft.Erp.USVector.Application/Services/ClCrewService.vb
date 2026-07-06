Imports CaSoft.Erp.USVector.Application.Dto

Public Class ClCrewService
    Implements ICrewService

    Private ReadOnly _repository As ICrewRepository

    Public Sub New(repository As ICrewRepository)
        _repository = repository
    End Sub

    Public Function GetDriver(gCrewId As Guid) As ClResult(Of ClLogDriverModel) Implements ICrewService.GetDriver
        Return New ClGetDriverUseCase(gCrewId, _repository).Handle()
    End Function

    Public Function ChangeDriver(gCrewId As Guid, gEmployeeId As Guid) As ClResult(Of Boolean) Implements ICrewService.ChangeDriver
        Dim command = New ClSetDriverCommand(gCrewId, gEmployeeId)
        Return New ClSetDriverUseCase(command, _repository).Handle()
    End Function

    Public Function GetMyActiveCrews(crewIds As IReadOnlyList(Of Guid), at As DateTime) As ClResult(Of ClActiveCrewSelectionDtoOut) Implements ICrewService.GetMyActiveCrews
        Return New ClGetMyActiveCrewsUseCase(crewIds, at, _repository).Handle()
    End Function

End Class
