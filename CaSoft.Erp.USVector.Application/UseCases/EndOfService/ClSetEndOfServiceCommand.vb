Public Class ClSetEndOfServiceCommand

    Public Sub New(gCrewId As Guid, dteEndofService As DateTime, strSource As String)

        _EndOfServiceDate = dteEndofService
        _Source = strSource
        _CrewId = CrewId
        ' Valid(_EndOfServiceDate)

    End Sub

    Public ReadOnly Property EndOfServiceDate As DateTime
    Public ReadOnly Property Source As String
    Public Property CrewId As Guid
End Class
