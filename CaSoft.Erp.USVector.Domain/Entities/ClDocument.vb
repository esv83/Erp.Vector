''' <summary>Catégorie de document terrain (spec §14.3).</summary>
Public Enum EnDocumentCategory
    ''' <summary>Bon de transport.</summary>
    TransportOrder = 1
    ''' <summary>Prescription.</summary>
    Prescription = 2
    ''' <summary>Document administratif.</summary>
    Administrative = 3
    ''' <summary>Autre.</summary>
    Other = 4
End Enum

''' <summary>
''' Document/photo terrain rattaché à une mission (TRF-10, spec §14). Conteneur : binaire +
''' catégorie + traçabilité de capture. Transféré dans le paquet field-data (servi par imageUrl).
''' </summary>
Public Class ClDocument
    Public Property Id As Guid
    Public Property MissionId As Guid
    Public Property Category As EnDocumentCategory
    Public Property Content As Byte()
    Public Property ContentType As String
    Public Property ByteSize As Integer
    Public Property FileName As String
    Public Property CapturedAt As DateTime
    Public Property CapturedCrewId As Guid?
End Class
