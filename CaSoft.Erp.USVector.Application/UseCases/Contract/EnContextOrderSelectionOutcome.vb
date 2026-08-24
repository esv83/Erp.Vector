''' <summary>
''' OC-4 — Issue d'une sélection de context par l'ambulancier. Les refus sont des <b>cas métier
''' attendus</b> et non des pannes : le contrôleur les traduit en codes HTTP, personne ne lève.
''' </summary>
Public Enum EnContextOrderSelectionOutcome

    ''' <summary>Choix enregistré côté ERP (204 → 200 mobile).</summary>
    Applied

    ''' <summary>
    ''' Le régulateur a imposé le type : lecture seule côté terrain (→ 409).
    ''' </summary>
    LockedByRegulator

    ''' <summary>
    ''' Type non proposé pour cette commande (agence/mode), inactif, ou <b>sans correspondance dans
    ''' le catalogue Order</b> (→ 400). Ce dernier cas est propre à la transition : voir
    ''' <c>ContextOrderSelectionService</c>.
    ''' </summary>
    NotApplicable

    ''' <summary>Mission introuvable côté ERP (→ 404).</summary>
    MissionNotFound

End Enum
