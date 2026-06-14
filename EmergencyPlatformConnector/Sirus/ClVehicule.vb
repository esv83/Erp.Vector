Public Class ClLomacoVehicule

    Public Property IMEI As String
    Public Property Immatriculation As String
    Public Property Latitude As Double
    Public Property Longitude As Double
    Public Property Etat As Integer
    Public Property Km As Integer

    ''' <summary>
    ''' Date de la position en UTC
    ''' </summary>
    ''' <returns></returns>
    Public Property [PositionUtcDate] As DateTime?
    Public Property [TransmitDate] As DateTime?
    Public Property Vitesse As Integer
    Public Property Cap As Integer
    Public Property HasChange As Boolean
    'Public Sub LoadFromGpsGateUser(ggsUser As ClUser)

    '    If ggsUser.devices.Count > 0 Then

    '        With Me
    '            .Cap = Integer.Parse(Math.Floor(ggsUser.trackPoint.velocity.heading))
    '            .Etat = 99
    '            .IMEI = ggsUser.devices(0).imei
    '            .Immatriculation = ggsUser.username
    '            .Km = 0
    '            .Latitude = ggsUser.trackPoint.position.latitude
    '            .Longitude = ggsUser.trackPoint.position.longitude
    '            .PositionUtcDate = ggsUser.trackPoint.utc
    '            .TransmitDate = Nothing
    '            .Vitesse = ggsUser.trackPoint.velocity.groundSpeed
    '            .HasChange = True
    '        End With
    '        NLog.LogManager.GetCurrentClassLogger.Info(String.Format("{0} ({1}) mis à jour avec position du {2} à {3}", Me.Immatriculation, Me.IMEI, Me.PositionUtcDate.Value.ToShortDateString, Me.PositionUtcDate.Value.ToShortTimeString))

    '    Else
    '        NLog.LogManager.GetCurrentClassLogger.Warn(String.Format("{0} ({1}) n'à pas de devices", Me.Immatriculation, Me.IMEI))

    '    End If

    'End Sub

End Class
