Public Interface IAttributsRepository
    ' Sub Save(gJobID As Guid, attributsCollection As ClAttributCollection)
    Sub Delete(gJobId As Guid, attribut As ClContractAttribut)
    Sub Update(gJobId As Guid, attribut As ClContractAttribut)
    Sub Insert(gJobId As Guid, attribut As ClContractAttribut)
    Function GetAttributsByContract(gJobId As Guid, intContractId As Integer) As ClAttributCollection

End Interface
