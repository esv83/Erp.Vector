Namespace Port

    ''' <summary>
    ''' Persistance des cartes mutuelle (P1, BD Mobile). La carte est rattachée au bénéficiaire ;
    ''' la plus récemment capturée fait foi.
    ''' </summary>
    ''' <remarks>
    ''' <para>
    ''' <b>Le binaire ne se charge que sur les chemins qui le servent.</b> L'image pèse jusqu'à 8 Mo,
    ''' et les accès aux métadonnées sont bien plus fréquents qu'elle : le paquet terrain en lit un
    ''' par mission, et les écrans des modules amont interrogent par lot. D'où la séparation stricte
    ''' ci-dessous — les métadonnées, la présence, et l'image sont trois lectures distinctes, et une
    ''' seule descend jusqu'aux octets.
    ''' </para>
    ''' </remarks>
    Public Interface IMutuelleCardRepository

        ''' <summary>Enregistre une carte (l'Id est porté par <paramref name="card"/>).</summary>
        Sub Save(card As ClMutuelleCard)

        ''' <summary>
        ''' Métadonnées de la carte courante (la plus récente) d'un bénéficiaire, ou Nothing.
        ''' </summary>
        ''' <remarks>
        ''' ⚠️ <b><see cref="ClMutuelleCard.Image"/> n'est pas chargée</b> — la projection SQL ne la
        ''' lit pas. Qui a besoin des octets appelle <see cref="GetCurrentImage"/> ou
        ''' <see cref="GetImage"/>. Charger le blob ici revenait à sortir jusqu'à 8 Mo de la base pour
        ''' rendre un nom de mutuelle, <b>une fois par mission</b> dans la construction du paquet
        ''' terrain — sur un traitement déjà mesuré à 14,7 s pour 284 missions.
        ''' </remarks>
        Function GetCurrentMetadata(beneficiaryId As Guid) As ClMutuelleCard

        ''' <summary>Octets de la carte désignée, ou Nothing si elle n'existe pas.</summary>
        Function GetImage(cardId As Guid) As ClMutuelleCardImage

        ''' <summary>
        ''' Octets de la carte <b>courante</b> d'un bénéficiaire, ou Nothing s'il n'en a aucune.
        ''' </summary>
        ''' <remarks>
        ''' Sert les écrans des modules amont, qui connaissent le bénéficiaire et non la carte : une
        ''' URL stable, qui suit les nouvelles captures sans que l'appelant ait à relire un
        ''' identifiant.
        ''' </remarks>
        Function GetCurrentImage(beneficiaryId As Guid) As ClMutuelleCardImage

        ''' <summary>
        ''' Pour chacun des bénéficiaires demandés qui porte au moins une carte : son identifiant et
        ''' la date de la plus récente. Les bénéficiaires <b>sans</b> carte sont simplement absents du
        ''' résultat.
        ''' </summary>
        ''' <remarks>
        ''' <b>Une seule requête, quel que soit le nombre demandé.</b> C'est tout l'objet de cette
        ''' méthode : un écran qui affiche une liste de bénéficiaires doit savoir lesquels ont une
        ''' photo <b>sans</b> interroger ligne par ligne. Le parc a déjà payé cette leçon — l'appel
        ''' unitaire du paquet terrain coûte 14,7 s pour 284 missions, sur un traitement déclenché
        ''' par un clic.
        ''' </remarks>
        Function ListPresence(beneficiaryIds As IReadOnlyCollection(Of Guid)) As IReadOnlyList(Of ClMutuelleCardPresence)

        ''' <summary>
        ''' Met à jour les champs mutuelle modifiables (nom/AMC/concentrateur/télétransmission +
        ''' statut/validation) de la carte <c>card.Id</c>. Renvoie la carte à jour, ou Nothing si introuvable.
        ''' </summary>
        Function Update(card As ClMutuelleCard) As ClMutuelleCard

    End Interface

End Namespace
