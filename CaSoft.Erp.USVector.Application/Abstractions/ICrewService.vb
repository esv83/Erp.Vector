Imports CaSoft.Erp.USVector.Application.Dto

Public Interface ICrewService
    Function GetDriver(gCrewId As Guid) As ClResult(Of ClLogDriverModel)
    Function ChangeDriver(gCrewId As Guid, gEmployeeId As Guid) As ClResult(Of Boolean)
    ''' <summary>Sélecteur d'équipage actif (login + changement mid-day) : réponse décision-complète pour l'UI.</summary>
    Function GetMyActiveCrews(crewIds As IReadOnlyList(Of Guid), at As DateTime) As ClResult(Of ClActiveCrewSelectionDtoOut)
End Interface
