''' <summary>
''' MOB-11 — Issue d'une désignation de conducteur, <b>et le motif qu'Orders en a donné</b>.
''' </summary>
''' <remarks>
''' <para>
''' <b>Pourquoi le motif voyage avec l'issue.</b> Orders refuse de désigner un conducteur après la
''' fin de la vacation, et le dit : « La vacation s'est terminée le 13/09/2026 à 18:00 : on ne peut
''' pas y désigner un conducteur après. » Ce texte était journalisé en erreur puis perdu ; le mobile
''' recevait « Orders.Api PUT crews/…/driver → 400. » et l'ambulancier réessayait — cinq fois en
''' 35 s le 13/09 —, sans jamais savoir que c'était un refus et pas une panne.
''' </para>
''' <para>
''' <b>Le motif reste facultatif</b> : Orders peut répondre sans corps lisible. <see cref="Reason"/>
''' vaut alors Nothing, et le cas d'usage retombe sur un libellé générique.
''' </para>
''' </remarks>
Public Class ClCrewDriverWriteResult

    Private ReadOnly _isApplied As Boolean
    Private ReadOnly _reason As String

    Private Sub New(isApplied As Boolean, reason As String)
        _isApplied = isApplied
        _reason = reason
    End Sub

    ''' <summary>Vrai si Orders a enregistré le conducteur.</summary>
    Public ReadOnly Property IsApplied As Boolean
        Get
            Return _isApplied
        End Get
    End Property

    ''' <summary>Motif du refus rendu par Orders, à afficher tel quel. Nothing s'il n'en a pas donné.</summary>
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

    ''' <summary>Conducteur enregistré. Un succès n'a pas de motif.</summary>
    Public Shared Function Applied() As ClCrewDriverWriteResult
        Return New ClCrewDriverWriteResult(True, Nothing)
    End Function

    ''' <summary>
    ''' Refus d'Orders, avec son motif s'il en a donné un. Un motif blanc est ramené à Nothing : une
    ''' chaîne vide affichée telle quelle serait pire qu'un libellé générique.
    ''' </summary>
    Public Shared Function Refused(reason As String) As ClCrewDriverWriteResult
        Dim cleaned = If(String.IsNullOrWhiteSpace(reason), Nothing, reason.Trim())
        Return New ClCrewDriverWriteResult(False, cleaned)
    End Function

End Class
