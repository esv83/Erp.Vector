Namespace Port

    ''' <summary>
    ''' Persistance des documents/photos terrain (TRF-10, BD Mobile). Rattachés à la mission ;
    ''' historisés. Transférés dans le paquet field-data (binaire servi par <c>imageUrl</c>).
    ''' </summary>
    Public Interface IDocumentRepository

        ''' <summary>Enregistre un document (l'Id est porté par <paramref name="document"/>).</summary>
        Sub Save(document As ClDocument)

        ''' <summary>Documents d'une mission (métadonnées), du plus récent au plus ancien.</summary>
        Function ListByMission(missionId As Guid) As IReadOnlyList(Of ClDocument)

        ''' <summary>Document par identifiant (pour servir le binaire), ou Nothing.</summary>
        Function GetById(documentId As Guid) As ClDocument

    End Interface

End Namespace
