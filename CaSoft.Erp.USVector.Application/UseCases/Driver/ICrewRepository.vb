Imports CaSoft.Erp.USVector.Application.Dto

Namespace Port

    Public Interface ICrewRepository
        Function GetCrew(gCrewID As Guid) As ClCrew
        Function IsEmployeeInCrew(ByVal gCrewID As Guid, ByVal gEmployeeId As Guid) As Boolean
        Function GetCrewDriver(ByVal gVehicleID As Guid) As ClLogDriverModel
        'Function FetchJobList(gCrewToken As Guid) As List(Of ClJobListItemModel)
        Function FetchJobList(gCrewId As Guid) As List(Of ClJobListItemModel)
        ''' <summary>
        ''' MOB-4a — Union des missions du jour pour plusieurs crews (un personnel
        ''' peut être membre de plusieurs crews actifs le même jour). Dédupliquée.
        ''' </summary>
        Function FetchJobList(gCrewIds As IReadOnlyCollection(Of Guid)) As List(Of ClJobListItemModel)
        Function FetchInstructionList(gCrewId As Guid) As List(Of ClInstructionListItemModel)
        Sub Update(crew As ClCrew)
        Sub AckInstruction(instructionId As Integer)
        Function GetCrewIdList(id As DateOnly) As List(Of Guid)

    End Interface

End Namespace

