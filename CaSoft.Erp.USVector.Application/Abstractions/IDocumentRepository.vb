Imports System.Threading

Namespace Port

    ''' <summary>
    ''' Persistance des documents/photos terrain (TRF-10, BD Mobile). Rattachés à la mission ;
    ''' historisés. Transférés dans le paquet field-data (binaire servi par <c>imageUrl</c>).
    ''' </summary>
    Public Interface IDocumentRepository

        ''' <summary>Enregistre un document (l'Id est porté par <paramref name="document"/>).</summary>
        Function SaveAsync(document As ClDocument, ct As CancellationToken) As Task

        ''' <summary>
        ''' Documents d'une mission, du plus récent au plus ancien — <b>métadonnées seules</b> :
        ''' <c>Content</c> vaut Nothing. Les octets se servent un par un, par <see cref="GetByIdAsync"/>.
        ''' </summary>
        Function ListByMissionAsync(missionId As Guid, ct As CancellationToken) As Task(Of IReadOnlyList(Of ClDocument))

        ''' <summary>Document par identifiant (pour servir le binaire), ou Nothing.</summary>
        Function GetByIdAsync(documentId As Guid, ct As CancellationToken) As Task(Of ClDocument)

    End Interface

End Namespace
