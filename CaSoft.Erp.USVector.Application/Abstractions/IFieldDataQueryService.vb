Namespace Port

    ''' <summary>
    ''' Lecture groupée des silos terrain (base Vector) pour le paquet d'enrichissement.
    ''' </summary>
    ''' <remarks>
    ''' <para>
    ''' <b>Une requête par silo, quelle que soit la taille du lot</b> — et non six par mission, comme
    ''' l'assemblage à l'unité le faisait avant le 19/09 en passant par les dépôts.
    ''' </para>
    ''' <para>
    ''' <b>Aucun binaire ne sort de la base ici.</b> Le paquet annonce les octets par leur URL (D8) :
    ''' l'image de signature (~42 Ko), le contenu des documents et l'image de la carte n'ont rien à y
    ''' faire. Les dépôts d'avant les chargeaient pour n'en lire qu'une date.
    ''' </para>
    ''' </remarks>
    Public Interface IFieldDataQueryService

        ''' <param name="missionIds">Missions dont on lit jalons, signature, documents et anomalies.</param>
        ''' <param name="beneficiaryIds">Bénéficiaires dont on lit la carte mutuelle courante.</param>
        Function ReadSilos(missionIds As IReadOnlyCollection(Of Guid),
                           beneficiaryIds As IReadOnlyCollection(Of Guid)) As ClFieldSilos

    End Interface

End Namespace
