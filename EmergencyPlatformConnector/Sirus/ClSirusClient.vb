

Imports System.Net
Imports CaSoft.Erp.USVector.Application

''' <summary>
''' Un client Sirus peut avoir plusieurs compte GpsGate
''' </summary>
Public Class ClSirusClient
    Implements IEmergencyPlatform

#Region "fields"
    'serveur sirus ="217.109.38.57"
    Private _lastTransmit As DateTime
    Private _TramesBuffer As New List(Of ClGeolocTrameSirus)
    Protected Friend _server As ClServerAddress


#End Region
    Private Sub New()

        _lastTransmit = Date.Now.AddDays(-1)

    End Sub

#Region "Methodes"

    ''' <summary>
    ''' Transmet les positions au serveur Sirus
    ''' </summary>
    Public Sub UpdatePosition(locationList As List(Of ClLocationModel)) Implements IEmergencyPlatform.UpdatePosition

        For Each user In locationList
            UpdatePosition(user)
        Next

    End Sub
    Public Sub UpdatePosition(location As ClLocationModel) Implements IEmergencyPlatform.UpdatePosition

        Dim geolocTrame = New ClGeolocTrameSirus(location)
        SendPositionToServer(geolocTrame)

    End Sub
    Private Function SendPositionToServer(trame As ClGeolocTrameSirus) As ClAcknoledgeTrameSirus


        Dim cmd As New ClSendUdpQuery(New Sockets.UdpClient, _server, trame.ContentAsByte)
        Dim sendUdpUseCase As New ClSendUdpUseCase(cmd)
        'NLog.LogManager.GetCurrentClassLogger.Info("Sending:" + trame.ContentAsText)
        sendUdpUseCase.Execute()

        Dim result As New ClAcknoledgeTrameSirus(sendUdpUseCase.UdpResponse)
        'NLog.LogManager.GetCurrentClassLogger.Info("Received:" + result.ContentAsText)

        Return result

    End Function


#End Region

#Region "Builder"
    Public Shared Function GetBuilder() As ClSirusClientBuilder
        Return ClSirusClientBuilder.GetBuilder
    End Function
    Public Class ClSirusClientBuilder


        Private _client As ClSirusClient
        Private Sub New()
            _client = New ClSirusClient
        End Sub
        Public Shared Function GetBuilder() As ClSirusClientBuilder
            Return New ClSirusClientBuilder
        End Function
        Public Function WithServer(srv As ClServerAddress) As ClSirusClientBuilder
            _client._server = srv
            Return Me
        End Function
        Public Function Build() As ClSirusClient
            Return _client
        End Function
    End Class

#End Region

End Class
