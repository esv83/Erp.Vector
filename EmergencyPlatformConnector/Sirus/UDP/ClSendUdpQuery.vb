Imports System.Net.Sockets

Public Class ClSendUdpQuery

    Public Sub New(pUdpClient As UdpClient, pUri As ClServerAddress, messageData As Byte())
        _UdpClient = pUdpClient
        _ServerUri = pUri
        _Data = messageData
    End Sub

    Public ReadOnly Property UdpClient As UdpClient
    Public ReadOnly Property ServerUri As ClServerAddress
    Public ReadOnly Property Data As Byte()

End Class
