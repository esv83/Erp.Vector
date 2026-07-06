Imports System.Text.Json.Serialization

''' <summary>
''' DtoOut des 3 jalons opérationnels d'une mission renvoyés à l'app (GET /api/time/{id}).
''' ATTENTION : les heures arrivent/repartent en String et en UTC (Zulu).
'''
''' L'ordre des propriétés EST significatif : l'UI construit sa timeline dans l'ordre des clés
''' du payload JSON. Les jalons sont donc déclarés — et sérialisés via <see cref="JsonPropertyOrder"/> —
''' dans l'ordre chronologique réel : En route (GoTime) → Sur place (OnSiteTime) → Disponible
''' (TerminatedTime). Ne pas réordonner sans corriger l'UnknownOrder côté clients.
''' (Correctif rapide « Option B » — cf. note dev UI. Une refonte « Option A » remplacera à terme
'''  ces 3 champs plats par une liste ordonnée portant label + rang explicites.)
''' </summary>
Public Class ClJobTimeModel

    <JsonPropertyOrder(1)>
    Public Property GoTime As String          ' Jalon 1 — « En route »

    <JsonPropertyOrder(2)>
    Public Property OnSiteTime As String      ' Jalon 2 — « Sur place »

    <JsonPropertyOrder(3)>
    Public Property TerminatedTime As String  ' Jalon 3 — « Disponible »

End Class
