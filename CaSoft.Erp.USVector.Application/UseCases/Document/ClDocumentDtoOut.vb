''' <summary>DTO de sortie d'un document terrain (métadonnées + url du binaire). TRF-10.</summary>
Public Class ClDocumentDtoOut
    Public Property Id As Guid
    Public Property MissionId As Guid
    ''' <summary>Catégorie (cf. EnDocumentCategory : 1=bon transport, 2=prescription, 3=admin, 4=autre).</summary>
    Public Property Category As Integer
    Public Property ContentType As String
    Public Property ByteSize As Integer
    Public Property FileName As String
    Public Property CapturedAt As DateTime
    ''' <summary>URL relative pour récupérer le binaire (servi par Vector.Api).</summary>
    Public Property FileUrl As String
End Class
