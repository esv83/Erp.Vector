Namespace Port

    ''' <summary>
    ''' MOB-4a — Résolution de l'identité mobile : du compte Keycloak (claim
    ''' <c>sub</c>) vers le personnel ERP, puis vers ses crews actifs.
    '''
    ''' <para>Indirection volontaire (cf. mobile_devplan.md §7) : la 1ʳᵉ
    ''' implémentation s'appuie sur la liaison <c>PER_KEYCLOAK_MAP</c> côté Orders ;
    ''' elle pourra être re-pointée vers l'identité société unifiée (Track B) sans
    ''' toucher au code missions/crew/joblist.</para>
    '''
    ''' <para>Signatures synchrones : cohérence avec le contrat legacy
    ''' (controllers/use cases synchrones). Le pont async est interne à l'impl.</para>
    ''' </summary>
    Public Interface IMobileIdentityResolver

        ''' <summary>
        ''' Résout le <c>sub</c> Keycloak en PER_ID. <c>Nothing</c> si le compte
        ''' n'est rattaché à aucun personnel (→ 403 « compte non rattaché »).
        ''' </summary>
        Function ResolvePersonnelId(keyCloakSub As Guid) As Guid?

        ''' <summary>
        ''' Crews dont le personnel est membre et dont la vacation couvre la date.
        ''' Peut en renvoyer plusieurs (changement d'équipage en cours de journée) :
        ''' le joblist en fait l'union. Vide si aucun crew actif ce jour-là.
        ''' </summary>
        Function ResolveActiveCrewIds(personnelId As Guid, onDate As DateOnly) As IReadOnlyList(Of Guid)

        ''' <summary>
        ''' Le personnel peut-il consulter le détail de cette mission ? Vrai si la
        ''' mission est affectée à l'un de ses crews actifs à la date de la mission.
        ''' Garde-fou d'accès du détail (un personnel mappé ne voit pas toutes les
        ''' missions). Faux si mission inconnue, non affectée, ou hors de ses crews.
        ''' </summary>
        Function IsMissionAccessible(personnelId As Guid, missionId As Guid) As Boolean

    End Interface

End Namespace
