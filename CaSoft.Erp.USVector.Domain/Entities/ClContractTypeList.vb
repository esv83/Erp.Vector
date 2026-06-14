

Public Class ClContractTypeList
    Inherits Dictionary(Of Integer, ClContractType)

    Public Overloads Sub Add(ContractType As ClContractType)
        MyBase.Add(ContractType.Id, ContractType)
    End Sub

End Class

