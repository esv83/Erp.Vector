''' <summary>Nature de l'anomalie terrain (spec §17.1).</summary>
Public Enum EnAnomalyType
    ''' <summary>Erreur de téléphone.</summary>
    Phone = 1
    ''' <summary>Erreur d'adresse.</summary>
    Address = 2
    ''' <summary>Erreur patient.</summary>
    Patient = 3
    ''' <summary>Problème administratif.</summary>
    Administrative = 4
    ''' <summary>Impossibilité de prise en charge.</summary>
    Impossibility = 5
End Enum

''' <summary>
''' Anomalie terrain signalée par l'équipage sur une mission (TRF-8, spec §17). Non bloquante :
''' transférée dans le paquet field-data et arbitrée par la facturation. Historisée.
''' </summary>
Public Class ClAnomaly
    Public Property Id As Guid
    Public Property MissionId As Guid
    Public Property Type As EnAnomalyType
    Public Property Text As String
    Public Property ReportedAt As DateTime
    Public Property ReportedCrewId As Guid?
End Class
