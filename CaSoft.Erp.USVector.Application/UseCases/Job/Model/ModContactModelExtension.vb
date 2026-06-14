Imports System.Runtime.CompilerServices

' Mapping ClContactModel (DTO contrat mobile) -> ClJobBeneficiary (domaine).
' Reprend la correspondance du ClContactModelAdapter legacy (MobApp.Data).
Public Module ModContactModelExtension

    <Extension>
    Public Function ToJobBeneficiary(model As ClContactModel) As ClJobBeneficiary
        Dim result As New ClJobBeneficiary

        If model.ContactId.HasValue Then result.Id = model.ContactId.Value
        result.Name = model.Name
        result.LastName = model.FirstName
        If model.DDN.HasValue Then result.DDN = model.DDN.Value

        Return result

    End Function

End Module
