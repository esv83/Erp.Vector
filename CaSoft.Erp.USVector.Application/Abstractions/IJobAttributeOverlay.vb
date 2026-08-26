Namespace Port

    ''' <summary>
    ''' Ce qui reste du magasin Vector des attributs de mission : une <b>lecture</b>, et une seule.
    ''' <para>
    ''' Order sert et enregistre désormais le contexte de la mission comme ses attributs. Ce magasin
    ''' n'est plus alimenté par personne — il ne conserve que la saisie <b>antérieure à la bascule
    ''' du 2026-08-25</b>, que le paquet terrain transporte vers la facturation pour combler les
    ''' trous de l'historique.
    ''' </para>
    ''' <para>
    ''' Son unique appelant est <c>IFieldAttributesReader</c>. Il disparaît avec la suppression des
    ''' tables <c>MOB_*</c> du contrat, une fois tranché jusqu'à quand cet historique doit rester
    ''' servi.
    ''' </para>
    ''' </summary>
    Public Interface IJobAttributeOverlay

        ''' <summary>
        ''' Reconstitue le <see cref="ClContractType"/> de la mission tel que le magasin Vector l'a
        ''' connu : attributs « core » + attributs du contrat sélectionné (premier type actif à
        ''' défaut), valeurs saisies fusionnées. Pour les attributs liste, la valeur rendue =
        ''' baseline ERP ∪ items du magasin (dédoublonnés). <paramref name="erpBaselines"/> est
        ''' indexé par nom d'attribut.
        ''' </summary>
        Function BuildContractType(missionId As Guid,
                                   erpBaselines As IDictionary(Of String, IEnumerable(Of String))) As ClContractType

    End Interface

End Namespace
