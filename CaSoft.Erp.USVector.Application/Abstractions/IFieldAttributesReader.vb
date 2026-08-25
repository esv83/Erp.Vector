Namespace Port

    ''' <summary>
    ''' OC-7 — Le bloc <c>attributes</c> du paquet terrain, lu <b>uniquement dans le magasin Vector</b>.
    '''
    ''' <para>
    ''' <b>Pourquoi un port à part plutôt que l'overlay complet.</b> Le paquet passait par
    ''' <c>IJobAttributeOverlay.BuildContractType</c>, qui depuis OC-3b résout le type de la mission —
    ''' et donc interroge Orders.Api. Le paquet payait ainsi un appel réseau par mission pour une
    ''' information dont il ne fait rien : sur un traitement mesuré à <b>14,7 s pour 284 missions</b>,
    ''' déclenché par un clic, ce n'est pas un détail. Ce port coupe cette dépendance : il ne résout
    ''' rien, il lit.
    ''' </para>
    '''
    ''' <para>
    ''' <b>Pourquoi on ne va pas chercher les attributs chez Order.</b> La facturation les lit
    ''' <b>déjà</b> à la source, et son fusionneur donne la priorité à Order à nom égal. Les faire
    ''' transiter par ce paquet créerait un troisième chemin vers la même donnée — deux appels de plus
    ''' par mission pour une valeur que le consommateur possède avant même de nous appeler.
    ''' </para>
    '''
    ''' <para>
    ''' Ce que ce bloc apporte encore, et qui justifie qu'il survive : les valeurs des missions
    ''' <b>saisies avant la bascule</b>, qui n'existent que côté Vector. Elles comblent les trous là où
    ''' Order n'a rien. Le bloc disparaîtra avec la décision OC-8 sur le sort de ces lignes.
    ''' </para>
    ''' </summary>
    Public Interface IFieldAttributesReader

        ''' <summary>
        ''' Bloc <c>attributes</c> de la mission : type retenu par le magasin Vector et valeurs
        ''' terrain non vides. <b>Aucun appel réseau</b>, quel que soit l'état des drapeaux de bascule.
        ''' </summary>
        Function Read(missionId As Guid) As ClFieldAttributesDto

    End Interface

End Namespace
