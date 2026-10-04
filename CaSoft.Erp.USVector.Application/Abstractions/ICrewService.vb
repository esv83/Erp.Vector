Imports System.Threading
Imports CaSoft.Erp.USVector.Application.Dto

Public Interface ICrewService
    Function GetDriverAsync(gCrewId As Guid, ct As CancellationToken) As Task(Of ClResult(Of ClLogDriverModel))
    Function ChangeDriverAsync(gCrewId As Guid, gEmployeeId As Guid, ct As CancellationToken) As Task(Of ClResult(Of Boolean))
    ''' <summary>Sélecteur d'équipage actif (login + changement mid-day) : réponse décision-complète pour l'UI.</summary>
    Function GetMyActiveCrewsAsync(crewIds As IReadOnlyList(Of Guid), at As DateTime, ct As CancellationToken) As Task(Of ClResult(Of ClActiveCrewSelectionDtoOut))
End Interface
