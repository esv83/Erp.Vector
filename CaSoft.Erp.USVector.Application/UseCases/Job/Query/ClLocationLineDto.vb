''' <summary>
''' DET-2 — Ligne d'affichage pilotée par le serveur : contenu (label/valeur) + style (gras, couleur).
''' L'UI la rend telle quelle, sans logique de mise en forme.
''' </summary>
Public Class ClLocationLineDto
    ''' <summary>Ordre de la ligne dans sa section (1-based).</summary>
    Public Property Index As Integer
    ''' <summary>Libellé optionnel (vide → valeur seule).</summary>
    Public Property Label As String
    Public Property Value As String
    Public Property IsBold As Boolean
    ''' <summary>Couleur hexa "#RRGGBB" ; Nothing = couleur par défaut de l'UI.</summary>
    Public Property Color As String
End Class
