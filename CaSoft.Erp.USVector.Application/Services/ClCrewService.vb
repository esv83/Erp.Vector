Imports System.Threading
Imports CaSoft.Erp.USVector.Application.Dto

Public Class ClCrewService
    Implements ICrewService

    Private ReadOnly _repository As ICrewRepository

    Public Sub New(repository As ICrewRepository)
        _repository = repository
    End Sub

    Public Function GetDriverAsync(gCrewId As Guid, ct As CancellationToken) As Task(Of ClResult(Of ClLogDriverModel)) Implements ICrewService.GetDriverAsync
        Return New ClGetDriverUseCase(gCrewId, _repository).HandleAsync(ct)
    End Function

    Public Function ChangeDriverAsync(gCrewId As Guid, gEmployeeId As Guid, ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements ICrewService.ChangeDriverAsync
        Dim command = New ClSetDriverCommand(gCrewId, gEmployeeId)
        Return New ClSetDriverUseCase(command, _repository).HandleAsync(ct)
    End Function

    Public Function GetMyActiveCrewsAsync(crewIds As IReadOnlyList(Of Guid), at As DateTime, ct As CancellationToken) As Task(Of ClResult(Of ClActiveCrewSelectionDtoOut)) Implements ICrewService.GetMyActiveCrewsAsync
        Return New ClGetMyActiveCrewsUseCase(crewIds, at, _repository).HandleAsync(ct)
    End Function

End Class
