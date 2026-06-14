
Public Class ClAttributCollection
    Inherits Dictionary(Of String, ClContractAttribut)

    Public Sub UpdateAttributs(NewContractAttributs As IEnumerable(Of ClContractAttribut))

        '1 Gerer les attributs qui n'existe plus (supprimé)
        For i = 0 To Me.Count - 1
            Dim oldAttribut = Me(i)
            If Not NewContractAttributs.Contains(oldAttribut) Then
                Remove(oldAttribut.Name)
            End If

        Next

        '2 Ajoute ou modifie les nouveaux attributs
        For Each contractAttribut In NewContractAttributs
            Me.Add(contractAttribut.Name, contractAttribut)
        Next

    End Sub

End Class
