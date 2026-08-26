''' <summary>
''' OC-5 — Issue d'une écriture de valeurs d'attributs, <b>et le motif qu'Order en a donné</b>.
''' </summary>
''' <remarks>
''' <para>
''' <b>Pourquoi le motif voyage avec l'issue.</b> L'issue seule ne dit que la famille du refus :
''' « champ verrouillé », « valeur invalide ». Elle ne dit pas <i>lequel</i>, ni <i>pourquoi</i> —
''' or c'est tout ce dont l'ambulancier a besoin pour corriger. Order, lui, le sait et l'écrit :
''' « clé de contrôle du numéro de sécurité sociale incorrecte », « la fiche bénéficiaire est
''' maîtrisée par le référentiel AidesNSoft ». Ce message était jusqu'ici lu, journalisé, puis
''' jeté ; le mobile recevait une phrase générique et l'ambulancier concluait que sa saisie
''' n'avait pas été enregistrée — sans jamais savoir qu'elle avait été <b>refusée</b>.
''' </para>
''' <para>
''' <b>Le motif reste facultatif.</b> Order peut répondre sans corps lisible, et une panne de
''' formulation ne doit pas faire perdre l'issue : <see cref="Reason"/> vaut alors Nothing, et
''' l'appelant retombe sur son libellé par défaut.
''' </para>
''' </remarks>
Public Class ClContextOrderValuesResult

    Private ReadOnly _outcome As EnContextOrderValuesOutcome
    Private ReadOnly _reason As String

    Private Sub New(outcome As EnContextOrderValuesOutcome, reason As String)
        _outcome = outcome
        _reason = reason
    End Sub

    ''' <summary>Famille de l'issue, telle que le contrôleur la traduit en code HTTP.</summary>
    Public ReadOnly Property Outcome As EnContextOrderValuesOutcome
        Get
            Return _outcome
        End Get
    End Property

    ''' <summary>
    ''' Motif exact rendu par Order, destiné à être <b>affiché</b>. Nothing si Order n'en a pas
    ''' fourni — l'appelant doit alors se rabattre sur un libellé générique.
    ''' </summary>
    Public ReadOnly Property Reason As String
        Get
            Return _reason
        End Get
    End Property

    ''' <summary>Vrai si un motif affichable est disponible.</summary>
    Public ReadOnly Property HasReason As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(_reason)
        End Get
    End Property

    ' ── Fabriques ────────────────────────────────────────────────────────────

    ''' <summary>Le lot est enregistré, en entier. Un succès n'a pas de motif.</summary>
    Public Shared Function Applied() As ClContextOrderValuesResult
        Return New ClContextOrderValuesResult(EnContextOrderValuesOutcome.Applied, Nothing)
    End Function

    ''' <summary>
    ''' Refus métier, avec le motif d'Order s'il en a donné un. Un motif blanc est ramené à
    ''' Nothing : une chaîne vide affichée telle quelle serait pire qu'un libellé générique.
    ''' </summary>
    Public Shared Function Refused(outcome As EnContextOrderValuesOutcome, reason As String) As ClContextOrderValuesResult
        Dim cleaned = If(String.IsNullOrWhiteSpace(reason), Nothing, reason.Trim())
        Return New ClContextOrderValuesResult(outcome, cleaned)
    End Function

End Class
