''' <summary>
''' DtoOut « timeline » d'une mission (Option A) : contrat riche et auto-suffisant destiné à l'UI.
''' Contrairement à <see cref="ClJobTimeModel"/> (3 champs plats), il porte une <b>liste ordonnée</b>
''' de jalons avec rang, code stable et label prêts à afficher. L'UI n'a donc plus à inférer ni
''' l'ordre (via la position des clés JSON) ni le libellé (via le nom de propriété).
'''
''' Les 3 jalons opérationnels sont TOUJOURS présents et ordonnés (En route → Sur place → Disponible) ;
''' <see cref="ClJobStatusDtoOut.At"/> vaut Nothing tant que le jalon n'est pas franchi (l'UI peut
''' ainsi afficher les étapes à venir en attente).
''' </summary>
Public Class ClJobTimelineDtoOut
    Public Property JobId As Guid
    Public Property Statuses As IReadOnlyList(Of ClJobStatusDtoOut)
End Class

''' <summary>Un jalon de la timeline. <see cref="At"/> = horodatage UTC (Zulu) String, Nothing si non franchi.</summary>
Public Class ClJobStatusDtoOut
    ''' <summary>Rang d'affichage (1..n), chronologique. Redondant avec l'ordre de la liste, mais explicite.</summary>
    Public Property Order As Integer
    ''' <summary>Code technique stable (ne pas traduire) : EnRoute | SurPlace | Disponible.</summary>
    Public Property Code As String
    ''' <summary>Libellé prêt à afficher.</summary>
    Public Property Label As String
    ''' <summary>Horodatage UTC (Zulu) au format String, ou Nothing si le jalon n'est pas encore franchi.</summary>
    Public Property At As String
End Class
