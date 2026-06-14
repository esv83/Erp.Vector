Namespace Port

    ''' <summary>
    ''' MOB-13 — Overlay des attributs de mission, persisté en BD Mobile (aucune écriture ERP).
    ''' Le catalogue (types de contrat + attributs) et les valeurs saisies vivent côté Mobile ;
    ''' les coordonnées patient déjà présentes côté ERP servent de baseline verrouillée pour les
    ''' attributs liste (PHONES/MAILS) : on peut AJOUTER, jamais modifier celles de l'ERP.
    ''' </summary>
    Public Interface IJobAttributeOverlay

        ''' <summary>
        ''' Construit le <see cref="ClContractType"/> de la mission : attributs « core » +
        ''' attributs du contrat sélectionné (contrat actif par défaut sinon), valeurs overlay
        ''' fusionnées. Pour les attributs liste, la valeur affichée = baseline ERP ∪ items
        ''' overlay (dédoublonnés). <paramref name="erpBaselines"/> est indexé par nom d'attribut.
        ''' </summary>
        Function BuildContractType(missionId As Guid,
                                   erpBaselines As IDictionary(Of String, IEnumerable(Of String))) As ClContractType

        ''' <summary>
        ''' Persiste l'overlay à partir du contrat édité. Pour les attributs liste, seuls les
        ''' items hors baseline ERP sont conservés (doublons écartés) ; les scalaires sont upsertés
        ''' tels quels.
        ''' </summary>
        Sub Save(missionId As Guid,
                 contractType As ClContractType,
                 erpBaselines As IDictionary(Of String, IEnumerable(Of String)))

        ''' <summary>Types de contrat actifs sélectionnables (Id + Display ; attributs non chargés).</summary>
        Function GetContracts() As IReadOnlyList(Of ClContractType)

        ''' <summary>Contrat explicitement choisi pour la mission, ou Nothing si aucun (défaut appliqué).</summary>
        Function GetSelectedContractId(missionId As Guid) As Integer?

        ''' <summary>Enregistre le contrat choisi pour la mission (upsert MOB_JOB_CONTRACT).</summary>
        Sub SelectContract(missionId As Guid, contractId As Integer)

    End Interface

End Namespace
