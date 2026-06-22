''' <summary>Commande de signalement d'une anomalie terrain sur une mission (TRF-8).</summary>
Public Class ClReportAnomalyCommand
    Public ReadOnly Property MissionId As Guid
    Public ReadOnly Property Input As ClReportAnomalyDtoIn

    Public Sub New(missionId As Guid, input As ClReportAnomalyDtoIn)
        Me.MissionId = missionId
        Me.Input = input
    End Sub
End Class
