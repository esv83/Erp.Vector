Public Interface IInvoicingRepository
    'Function GetAttributsCollection(intContractId As Integer, gJobID As Guid) As ClAttributCollection
    'Sub UpdateAttributs(attributs As List(Of ClContractAttribut))
    ' Sub UpdateAttributsValues(gJobId As Guid, attributList As ClAttributCollection)
    Function GetContract(gJobId As Guid) As ClContractType
    Function GetContractList(jobId As Guid) As List(Of ClContractType)
    'Function GetAttributsByContract(intContractId As Integer) As List(Of ClContractAttribut)
    'Function GetOptionsByAttribut(id As Integer) As Dictionary(Of Integer, String)
    ReadOnly Property AttributValuesRepository As IAttributsRepository

End Interface
