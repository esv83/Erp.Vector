''' <summary>
''' Corps de <c>PUT api/missions/{id}/beneficiary/identity</c> (J-144). Relayé à Order sans
''' interprétation : c'est lui qui valide (nom obligatoire, date jamais future).
''' </summary>
Public Class ClBeneficiaryIdentityDtoIn
    ''' <summary>Obligatoire.</summary>
    Public Property LastName As String
    Public Property FirstName As String
    ''' <summary>Facultatif, <c>AAAA-MM-JJ</c>.</summary>
    Public Property BirthDate As String
    ''' <summary>0 inconnu, 1 homme, 2 femme.</summary>
    Public Property Gender As Integer?
End Class

''' <summary>
''' Corps de <c>PUT api/missions/{id}/beneficiary/contact</c>. L'un, l'autre ou les deux ; vide ou
''' déjà connu ne change rien (204 quand même).
''' </summary>
Public Class ClBeneficiaryContactDtoIn
    Public Property Phone As String
    Public Property Email As String
End Class

''' <summary>Issue d'une saisie sur le patient, telle que le contrôleur la traduit en code HTTP.</summary>
Public Enum EnBeneficiaryEntryOutcome
    ''' <summary>204 — enregistré, ou rien à enregistrer.</summary>
    Applied
    ''' <summary>400 — valeur mal formée ; rien n'est écrit.</summary>
    Invalid
    ''' <summary>403 — hors de l'équipage affecté, ambulancier inconnu d'Order, mission de plus de deux jours. Définitif.</summary>
    Forbidden
    ''' <summary>404 — mission ou patient introuvable (identité : commande qui n'est pas née anonyme).</summary>
    NotFound
    ''' <summary>409 — identité déjà tranchée par la régulation.</summary>
    Locked
End Enum

''' <summary>Issue d'une saisie sur le patient, <b>et le motif qu'Order en a donné</b> (son <c>detail</c>).</summary>
Public NotInheritable Class ClBeneficiaryEntryResult

    Private Sub New(outcome As EnBeneficiaryEntryOutcome, reason As String)
        Me.Outcome = outcome
        Me.Reason = reason
    End Sub

    Public ReadOnly Property Outcome As EnBeneficiaryEntryOutcome

    ''' <summary>Motif d'Order, à afficher tel quel. Nothing s'il n'en a pas donné.</summary>
    Public ReadOnly Property Reason As String

    Public Shared Function Applied() As ClBeneficiaryEntryResult
        Return New ClBeneficiaryEntryResult(EnBeneficiaryEntryOutcome.Applied, Nothing)
    End Function

    ''' <summary>Refus métier ; un motif blanc est ramené à Nothing.</summary>
    Public Shared Function Refused(outcome As EnBeneficiaryEntryOutcome, reason As String) As ClBeneficiaryEntryResult
        Return New ClBeneficiaryEntryResult(outcome, If(String.IsNullOrWhiteSpace(reason), Nothing, reason.Trim()))
    End Function

End Class
