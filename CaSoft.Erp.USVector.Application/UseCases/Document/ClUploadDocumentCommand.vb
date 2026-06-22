''' <summary>Commande de dépôt d'un document/photo terrain sur une mission (TRF-10).</summary>
Public Class ClUploadDocumentCommand
    Public ReadOnly Property MissionId As Guid
    Public ReadOnly Property Content As Byte()
    Public ReadOnly Property ContentType As String
    Public ReadOnly Property FileName As String
    Public ReadOnly Property Category As Integer
    Public ReadOnly Property CrewId As Guid?

    Public Sub New(missionId As Guid, content As Byte(), contentType As String,
                   fileName As String, category As Integer, crewId As Guid?)
        Me.MissionId = missionId
        Me.Content = content
        Me.ContentType = contentType
        Me.FileName = fileName
        Me.Category = category
        Me.CrewId = crewId
    End Sub
End Class
