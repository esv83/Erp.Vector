''' <summary>
''' Présence de la carte mutuelle pour une mission — ce dont les écrans de régulation et de
''' certification ont besoin pour activer leur entrée de menu « Carte mutuelle ». Une ligne par
''' mission demandée, carte ou pas.
''' </summary>
''' <remarks>
''' Ni nom de mutuelle ni code AMC : une liste n'en a pas l'usage, et la route de métadonnées les sert
''' au clic. <see cref="ImageUrl"/> vise la route <b>par mission</b> — l'écran ne connaît que la
''' mission, et l'URL reste valable après une nouvelle photo. Chemin relatif, comme partout.
''' </remarks>
Public Class ClMissionMutuelleCardPresenceDtoOut

    Public Property MissionId As Guid

    ''' <summary>Nothing si la mission est inconnue d'Orders ou n'a pas de patient.</summary>
    Public Property BeneficiaryId As Guid?

    Public Property HasCard As Boolean

    ''' <summary>Date de la dernière photo ; Nothing sans carte.</summary>
    Public Property CapturedAt As DateTime?

    ''' <summary>Nothing sans carte.</summary>
    Public Property ImageUrl As String

End Class
