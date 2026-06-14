Public Class ClInsertMechanicLogCommand

    Public Sub New(gCrewId As Guid, strConStat As String)
        Me.CrewId = gCrewId
        Me.constat = strConStat
    End Sub


    Public Property CrewId As Guid
    Public Property Constat As String

End Class
