Public Class ClGetJobListCommand

    Public Sub New(gCrewToken As Guid, gCrewId As Guid)
        _CrewId = gCrewId
        _CrewToken = gCrewToken
    End Sub
    Public ReadOnly Property CrewId As Guid
    Public ReadOnly Property CrewToken As Guid

End Class
