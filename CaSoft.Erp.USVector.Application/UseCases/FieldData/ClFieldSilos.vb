''' <summary>
''' Silos terrain de plusieurs missions, lus en une requête par silo et <b>sans aucun binaire</b> —
''' la matière du paquet d'enrichissement (field-data), à l'unité comme en lot.
''' </summary>
''' <remarks>
''' Une mission absente d'un dictionnaire n'a simplement rien dans ce silo : pas de jalon, pas de
''' signature, pas de document. Ce n'est jamais une erreur.
''' </remarks>
Public Class ClFieldSilos

    ''' <summary>Jalons opérationnels, par mission.</summary>
    Public Property Timelines As IReadOnlyDictionary(Of Guid, ClJobTimeData)

    ''' <summary>Horodatage de la signature, par mission — la présence d'une clé vaut « signée ».</summary>
    Public Property SignedAt As IReadOnlyDictionary(Of Guid, DateTime)

    ''' <summary>Carte mutuelle courante (métadonnées), par <b>bénéficiaire</b>.</summary>
    Public Property CurrentMutuelles As IReadOnlyDictionary(Of Guid, ClMutuelleCard)

    ''' <summary>Documents (métadonnées, contenu à Nothing), par mission, du plus récent au plus ancien.</summary>
    Public Property Documents As ILookup(Of Guid, ClDocument)

    ''' <summary>Anomalies, par mission, de la plus récente à la plus ancienne.</summary>
    Public Property Anomalies As ILookup(Of Guid, ClAnomaly)

End Class
