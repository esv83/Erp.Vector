Public Interface IContactRepository
    Sub UpdateContact(Contact As ClJobBeneficiary)
    Function GetContact(gId As Guid) As ClJobBeneficiary
    Function GetContactList(strName As String, strFirstName As String) As List(Of ClJobBeneficiary)
    Function GetContactList(FullSearchName As String) As List(Of ClJobBeneficiary)

End Interface
