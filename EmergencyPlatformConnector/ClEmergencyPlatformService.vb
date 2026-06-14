Imports System.Timers
Imports CaSoft.Erp.USVector.Application


Public Class ClEmergencyPlatformService
    Implements IEmergencyConnector


    Private _trackingList As Dictionary(Of String, ClLocationModel)
    Private _statutList As Dictionary(Of String, Integer)
    Private _geolocServer As IGeolocServer
    Private _platform As IEmergencyPlatform
    Private _lastRefresh As Date
    Private _timer As Timer

    Public Sub New(emergencyPlatform As IEmergencyPlatform, geolocServer As IGeolocServer)
        _trackingList = New Dictionary(Of String, ClLocationModel)
        _statutList = New Dictionary(Of String, Integer)

        _lastRefresh = DateTime.Now.AddHours(-4)
        _platform = emergencyPlatform
        _geolocServer = geolocServer

        InitTimer
    End Sub

    Private Sub InitTimer()
        _timer = New Timer(60 * 1000)
        _timer.AutoReset = True

        AddHandler _timer.Elapsed, AddressOf OnTime

        UpdatePositionFromGeolocToPlatform()

        _timer.Start()


    End Sub

    Private Sub OnTime(sender As Object, e As ElapsedEventArgs)
        UpdatePositionFromGeolocToPlatform()
    End Sub

    Public Sub AddVehicleToTrackList(strImmatriculation As String) Implements IEmergencyConnector.AddVehicleToTrackList
        If _geolocServer.AddToView(strImmatriculation) Then
            ' _trackingList.Add(strImmatriculation, Nothing)
            '  _statutList.Add(strImmatriculation, 0)
        Else
            Throw New Exception("impossible d'ajouter le véhicle a la plateforme de géolocalisation")
        End If
    End Sub
    Public Sub RemoveVehicleFromTrackList(strImmatriculation As String) Implements IEmergencyConnector.RemoveVehicleFromTrackList
        If _geolocServer.RemoveFromAmuView(strImmatriculation) Then
            ' _trackingList.Remove(strImmatriculation, Nothing)
            ' _statutList.Remove(strImmatriculation, 0)
        Else
            Throw New Exception("impossible de retirer le véhicle a la plateforme de géolocalisation")
        End If
    End Sub
    Public Function UpdatePositionFromGeolocToPlatform() As Integer Implements IEmergencyConnector.UpdatePositionFromGeolocToPlatform

        Dim updatedGeolocUsersList = _geolocServer.GetUpdatedUsers(_lastRefresh)

        For Each geolocUser In updatedGeolocUsersList

            Dim updatedlocation As ClLocationModel = GetLocation(geolocUser)

            _trackingList(geolocUser.Immatriculation) = updatedlocation
            _platform.UpdatePosition(updatedlocation)

        Next

        Return updatedGeolocUsersList.Count

    End Function
    Public Sub UpdateVehicleStatut(strImmat As String, intStatut As Integer) Implements IEmergencyConnector.UpdateVehicleStatut
        If _statutList.ContainsKey(strImmat) Then
            _statutList(strImmat) = intStatut
            'TODO plublier fichier XML dans le repoertoire SIRUS
        End If
    End Sub
    Private Function GetLocation(geolocUser As ClUserModel) As ClLocationModel
        Dim result As New ClLocationModel

        With result
            .Immatriculation = geolocUser.Immatriculation
            .IMEI = geolocUser.Imei
            .Heading = geolocUser.Heading
            .Latitude = geolocUser.Latitude
            .Longitude = geolocUser.Longitude
            .Speed = geolocUser.Speed
            ' .State = _statutList(geolocUser.Immatriculation)
            .LocationUtcDate = geolocUser.LastUtcPosition
        End With

        Return result

    End Function

    Public Property Enabled As Boolean Implements IEmergencyConnector.TimerEnabled
        Get
            Return _timer.Enabled
        End Get
        Set(value As Boolean)
            If value <> _timer.Enabled Then
                If value Then
                    _timer.Start()
                Else
                    _timer.Stop()
                End If
            End If

        End Set
    End Property
End Class
