Public Class ClUpdateJobValuesCommand
    Inherits ClJobQueryWithCache

    Public Sub New(gJobId As Guid, cache As ClJobListCache, intContractID As Integer, lstAttributs As List(Of ClAttributValueDto))
        MyBase.New(gJobId, cache)
        _AttributList = lstAttributs
        _ContractID = intContractID
    End Sub
    Public ReadOnly Property AttributList As List(Of ClAttributValueDto)

    Public ReadOnly Property ContractID As Integer

    'Public ReadOnly Property ContractId As Integer
    '    Get
    '        Dim ContractAttribute = AttributList.Single(Function(f) f.Name = "CONTRACT")
    '        Return ContractAttribute.Value
    '    End Get
    'End Property

End Class
