''' <summary>
''' OC-5 — Issue d'une écriture de valeurs d'attributs par l'ambulancier. Comme pour le choix du
''' type, les refus sont des <b>cas métier attendus</b> et non des pannes : le contrôleur les traduit
''' en codes HTTP, personne ne lève.
''' </summary>
Public Enum EnContextOrderValuesOutcome

    ''' <summary>Le lot est enregistré, en entier (204 → 200 mobile).</summary>
    Applied

    ''' <summary>
    ''' Une valeur <b>modifie</b> un champ verrouillé — DDN/NIR déjà connus, PMT/BT déjà cochés
    ''' (→ 409). Rien n'est enregistré. Reposer une valeur inchangée ne produit pas ce refus.
    ''' </summary>
    FieldLocked

    ''' <summary>
    ''' Au moins une valeur est invalide : NIR à clé de contrôle fausse, date de naissance dans le
    ''' futur, ou champ absent du formulaire de la mission (→ 400). <b>Rien n'est enregistré</b> —
    ''' l'écriture est tout ou rien.
    ''' </summary>
    Invalid

    ''' <summary>Mission introuvable côté ERP (→ 404).</summary>
    MissionNotFound

End Enum
