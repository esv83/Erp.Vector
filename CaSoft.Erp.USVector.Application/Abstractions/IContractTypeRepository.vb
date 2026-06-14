
Public Interface IContractTypeRepository
    Function GetContract(intContractTypeId As Integer) As ClContractType
    Function GetContractList(jobId As Guid) As List(Of ClContractType)
    Function GetAttributsByContract(intContractId As Integer) As List(Of ClContractAttribut)
    Function GetOptionsByAttribut(id As Integer) As Dictionary(Of Integer, String)
    Function GetAttributsValuesByJob(gJobId As Guid) As List(Of ClAttributValue)
End Interface

