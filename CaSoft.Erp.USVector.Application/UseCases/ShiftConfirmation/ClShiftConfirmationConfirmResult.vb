''' <summary>
''' Issue d'une confirmation de prise de service depuis l'application. Les refus sont des <b>cas
''' métier attendus</b> : le contrôleur les traduit en codes HTTP, personne ne lève.
''' </summary>
Public Enum EnShiftConfirmationConfirmOutcome

    ''' <summary>Confirmation enregistrée (→ 200).</summary>
    Confirmed

    ''' <summary>Elle l'était déjà — double appui, ou lien du courriel utilisé avant (→ 200).</summary>
    AlreadyConfirmed

    ''' <summary>
    ''' Aucune demande de ce numéro pour cet ambulancier sur cet équipage (→ 404). Order ne distingue
    ''' pas « n'existe pas » de « existe pour quelqu'un d'autre », et c'est voulu.
    ''' </summary>
    NotFound

    ''' <summary>Demande remplacée, ou déjà validée par le régulateur (→ 400, motif d'Order).</summary>
    Refused

End Enum

''' <summary>Issue d'une confirmation, et le motif qu'Order a donné d'un refus.</summary>
Public Class ClShiftConfirmationConfirmResult

    Public Property Outcome As EnShiftConfirmationConfirmOutcome

    ''' <summary>Motif affichable d'un refus. <c>Nothing</c> hors refus, ou si Order n'en a pas donné.</summary>
    Public Property Reason As String

    ''' <summary>Heure confirmée — <b>locale</b>. <c>Nothing</c> hors succès.</summary>
    Public Property ProposedLocalTime As DateTime?

End Class

''' <summary>Réponse mobile d'une confirmation réussie.</summary>
Public Class ClShiftConfirmedDtoOut

    Public Property Confirmed As Boolean

    ''' <summary>Vrai quand la confirmation était déjà acquise : afficher le même succès, pas une erreur.</summary>
    Public Property AlreadyConfirmed As Boolean

    ''' <summary>Heure confirmée — <b>locale</b>.</summary>
    Public Property ProposedLocalTime As DateTime?

End Class
