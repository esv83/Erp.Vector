Imports NLog
Imports System.Net
Imports System.Text

Public Class ClSendUdpUseCase

    Private _command As ClSendUdpQuery

    Public Sub New(command As ClSendUdpQuery)
        _command = command
    End Sub
    Public Sub Execute()
        _command.UdpClient.Connect(_command.ServerUri.Address, _command.ServerUri.Port)
        _command.UdpClient.Send(_command.Data, _command.Data.Length)
        Console.WriteLine(ASCIIEncoding.ASCII.GetString(_command.Data))

        Dim remoteIpEndPoint As New IPEndPoint(IPAddress.Any, 0)
        Dim receiveBytes = _command.UdpClient.Receive(remoteIpEndPoint)
        Console.WriteLine(ASCIIEncoding.ASCII.GetString(receiveBytes))

        _UdpResponse = receiveBytes

    End Sub

    Public ReadOnly Property UdpResponse As Byte()

End Class
