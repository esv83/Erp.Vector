Imports System.Threading

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
    ''' <para>Asynchrone de bout en bout depuis le 04/10 : chaque requête du terrain passe par ici
    ''' (garde-fou d'équipage), et le pont <c>.GetAwaiter().GetResult()</c> y bloquait un thread
    ''' pendant chaque appel à Orders.</para>
    ''' </summary>
    Public Interface IMobileIdentityResolver

        ''' <summary>
        ''' Résout le <c>sub</c> Keycloak en PER_ID. <c>Nothing</c> si le compte
        ''' n'est rattaché à aucun personnel (→ 403 « compte non rattaché »).
        ''' </summary>
        Function ResolvePersonnelIdAsync(keyCloakSub As Guid, ct As CancellationToken) As Task(Of Guid?)

        ''' <summary>
        ''' Crews dont le personnel est membre et dont la vacation couvre la date.
        ''' Peut en renvoyer plusieurs (changement d'équipage en cours de journée).
        ''' Vide si aucun crew actif ce jour-là. Chemin mis en cache (garde-fou des endpoints).
        ''' </summary>
        Function ResolveActiveCrewIdsAsync(personnelId As Guid, onDate As DateOnly, ct As CancellationToken) As Task(Of IReadOnlyList(Of Guid))

        ''' <summary>
        ''' Idem, mais garantit une lecture <b>fraîche</b> (contourne tout cache) — pour la
        ''' (re)sélection d'équipage (<c>GET /api/crew/mine</c>) : un crew créé le jour même doit
        ''' y apparaître immédiatement. Rafraîchit le cache au passage.
        ''' </summary>
        Function ResolveActiveCrewIdsFreshAsync(personnelId As Guid, onDate As DateOnly, ct As CancellationToken) As Task(Of IReadOnlyList(Of Guid))

        ''' <summary>
        ''' Le personnel peut-il consulter le détail de cette mission ? Vrai si la
        ''' mission est affectée à l'un de ses crews actifs à la date de la mission.
        ''' Garde-fou d'accès du détail (un personnel mappé ne voit pas toutes les
        ''' missions). Faux si mission inconnue, non affectée, ou hors de ses crews.
        ''' </summary>
        Function IsMissionAccessibleAsync(personnelId As Guid, missionId As Guid, ct As CancellationToken) As Task(Of Boolean)

    End Interface

End Namespace
