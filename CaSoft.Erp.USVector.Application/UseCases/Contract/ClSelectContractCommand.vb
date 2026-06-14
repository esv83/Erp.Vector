''' <summary>MOB-13.8 — Choix d'un type de contrat pour une mission.</summary>
Public Class ClSelectContractCommand
    Public Sub New(missionId As Guid, contractId As Integer)
        _MissionId = missionId
        _ContractId = contractId
    End Sub

    Public ReadOnly Property MissionId As Guid
    Public ReadOnly Property ContractId As Integer
End Class
