''' <summary>
''' DET-2 — Compose le graphe d'affichage d'un lieu (sections de lignes) à partir des données
''' structurées du domaine. C'est ICI que le serveur décide QUOI afficher et COMMENT (ordre, gras,
''' couleur) : modifier l'affichage pickup/dropoff = modifier ce builder, sans toucher l'UI.
''' </summary>
Public Class ClLocationDisplayBuilder

    Public Shared Function Build(loc As ClJobLocation) As ClLocationDisplayDto
        Dim display As New ClLocationDisplayDto
        Dim blocks As New List(Of IReadOnlyList(Of ClLocationLineDto))

        If loc IsNot Nothing Then
            ' Section 1 — identité : nom (en gras) puis service.
            Dim identity As New List(Of ClLocationLineDto)
            AddLine(identity, Nothing, loc.Nom, bold:=True)
            AddLine(identity, "Service", loc.Service)
            If identity.Count > 0 Then blocks.Add(identity)

            ' Section 2 — adresse : lignes non vides, dans l'ordre.
            Dim address As New List(Of ClLocationLineDto)
            AddLine(address, Nothing, loc.Adresse)
            AddLine(address, Nothing, loc.Residence)
            AddLine(address, Nothing, loc.BatEtage)
            AddLine(address, Nothing, loc.Commune)
            AddLine(address, Nothing, loc.Complement)
            If address.Count > 0 Then blocks.Add(address)

            ' Coordonnées : sous-objet présent seulement si le lieu est réellement géocodé.
            If loc.Latitude.HasValue AndAlso loc.Longitude.HasValue Then
                display.Coordinates = New ClJobDetailModel.ClJobCoordinatesDto With {
                    .Latitude = loc.Latitude.Value,
                    .Longitude = loc.Longitude.Value
                }
            End If
        End If

        display.Blocks = blocks
        Return display
    End Function

    ''' <summary>Ajoute une ligne si la valeur est non vide ; Index = position dans la section.</summary>
    Private Shared Sub AddLine(section As List(Of ClLocationLineDto), label As String, value As String,
                               Optional bold As Boolean = False, Optional color As String = Nothing)
        If String.IsNullOrWhiteSpace(value) Then Return
        section.Add(New ClLocationLineDto With {
            .Index = section.Count + 1,
            .Label = label,
            .Value = value,
            .IsBold = bold,
            .Color = color
        })
    End Sub

End Class
