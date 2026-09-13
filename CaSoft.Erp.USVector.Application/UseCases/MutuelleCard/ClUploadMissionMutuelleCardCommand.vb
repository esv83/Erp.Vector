''' <summary>Dépôt d'une photo de carte mutuelle depuis une mission : le patient est résolu côté serveur.</summary>
Public Class ClUploadMissionMutuelleCardCommand

    Public Sub New(missionId As Guid, image As Byte(), contentType As String, crewId As Guid?)
        _MissionId = missionId
        _Image = image
        _ContentType = contentType
        _CrewId = crewId
    End Sub

    Public ReadOnly Property MissionId As Guid
    Public ReadOnly Property Image As Byte()
    Public ReadOnly Property ContentType As String
    Public ReadOnly Property CrewId As Guid?

End Class
