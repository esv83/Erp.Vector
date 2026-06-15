Namespace Port

    ''' <summary>
    ''' Persistance des cartes mutuelle (P1, BD Mobile). La carte est rattachée au bénéficiaire ;
    ''' la plus récemment capturée fait foi.
    ''' </summary>
    Public Interface IMutuelleCardRepository

        ''' <summary>Enregistre une carte (l'Id est porté par <paramref name="card"/>).</summary>
        Sub Save(card As ClMutuelleCard)

        ''' <summary>Carte courante (la plus récente) d'un bénéficiaire, ou Nothing.</summary>
        Function GetCurrent(beneficiaryId As Guid) As ClMutuelleCard

        ''' <summary>Carte par identifiant (pour servir l'image), ou Nothing.</summary>
        Function GetById(cardId As Guid) As ClMutuelleCard

    End Interface

End Namespace
