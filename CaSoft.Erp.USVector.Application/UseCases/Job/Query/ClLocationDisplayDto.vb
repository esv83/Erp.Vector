''' <summary>
''' DET-2 — Graphe d'affichage d'un lieu (pickup/dropoff) piloté par le serveur. Les sections
''' (<see cref="Blocks"/>) portent QUOI afficher et COMMENT ; l'UI n'est qu'un moteur de rendu.
''' Les coordonnées sont un sous-objet à part (carto), Nothing si le lieu n'est pas géocodé.
''' </summary>
Public Class ClLocationDisplayDto
    ''' <summary>Sections de lignes : chaque section (sous-liste) est rendue comme un bloc, dans l'ordre.</summary>
    Public Property Blocks As IReadOnlyList(Of IReadOnlyList(Of ClLocationLineDto)) =
        New List(Of IReadOnlyList(Of ClLocationLineDto))
    ''' <summary>Coordonnées WGS84 (hors des lignes) ; Nothing si non géocodé.</summary>
    Public Property Coordinates As ClJobDetailModel.ClJobCoordinatesDto
End Class
