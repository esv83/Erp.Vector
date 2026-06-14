Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports CaSoft.Erp.USVector.Application
Imports NLog

Public Class ClTransmitter

    Private _TramesBuffer As New List(Of ClGeolocTrameSirus)
    Private _AckBuffer As New List(Of ClAcknoledgeTrameSirus)

    ' Private _udpClient As UdpClient
    Private _ip As String
    Private _Port As Integer

    Sub New(IP As String, Port As Integer)

        _ip = IP
        _Port = Port

        ' _udpClient = New UdpClient

    End Sub


    Public Async Function SendTrameToSirusAsync(Trame As ClGeolocTrameSirus) As Task(Of Boolean)
        Dim Result As Boolean = True


        Await SendDataViaUdpAsync(Trame.ContentAsByte)

        Trame.TransmitDate = Date.Now
        Debug.WriteLine("envoie de trame :" + Trame.ContentAsString)


        Return Result



    End Function
    'Public Function SendTrameToSirus(Trame As ClTrameSirus) As Boolean
    '    Dim Result As Boolean = True

    '    Try
    '        _udpClient.Connect(_ip, _Port)
    '        Dim x = _udpClient.Send(Trame.ContentAsByte, Trame.ContentAsByte.Length)
    '        Trame.TransmitDate = Date.Now
    '        Debug.WriteLine("envoie de trame :" + Encoding.UTF8.GetString(Trame.ContentAsByte))

    '    Catch ex As Exception
    '        Result = False
    '        Debug.WriteLine(ex, "Impossible de transmettre la trame :" + ex.Message)
    '    End Try

    '    Return Result



    'End Function
    Public Async Function TransmitAsync(UpdatedPositions As List(Of ClLocationModel)) As Task

        For Each user In UpdatedPositions

            _TramesBuffer.Add(New ClGeolocTrameSirus(user))

            'On stock les trames dans un tampon qui sert a:
            '1 Envoyer les nouvelles Trames
            '2 Envoyer les trames qui n'ont pas encore été validée par le serveur Thelis
            '3 Garder les trames en attente de validation par retour du serveur Thelis

        Next

        Dim TransmitList = _TramesBuffer.Where(Function(f) f.TrameState = TrameState.Pending Or (f.TransmitDate.HasValue AndAlso f.TransmitDate.Value.Subtract(Date.Now).TotalSeconds = 90))

        For Each trame In TransmitList
            Dim debug = trame.ContentAsString
            Await SendDataViaUdpAsync(trame.ContentAsByte)

            Await StartListeningAsync(20019)

            Dim ack = ListenAckFromSirusAsync()

            TreatAck(Await ack)


            trame.TransmitDate = Date.Now

        Next

    End Function

    Public Function ListenAckFromSirusAsync()
        Dim RemoteIpEndPoint As New IPEndPoint(IPAddress.Any, 20019)

        ' Try
        Using udpClient = New UdpClient
            udpClient.Client.Bind(New IPEndPoint(IPAddress.Any, 0))
            Dim receiveBytes = udpClient.ReceiveAsync()

            ' Dim AckTrame = New ClAcknoledgeTrameThelis(receiveBytes.Buffer)

            ' Return AckTrame

        End Using
        ' Catch ex As SocketException
        '  Debug.WriteLine(ex.Message)
        'End Try


    End Function
    Public Sub TreatAck(ack As ClAcknoledgeTrameSirus)



        Dim trame = _TramesBuffer.FirstOrDefault(Function(i) i.Id = ack.Id)
        If trame IsNot Nothing Then
            trame.TrameState = TrameState.Received
        End If


    End Sub

    Private Async Function SendDataViaUdpAsync(dataBytes As Byte()) As Task

        Using udpClient = New UdpClient

            Await udpClient.SendAsync(dataBytes, dataBytes.Length, _ip, _Port)

        End Using

    End Function
    Public Async Function StartListeningAsync(port As Integer) As Task
        Dim receiveUdpClient As UdpClient = Nothing
        Try
            ' Initialisez le client UDP et liez-le au port spécifié
            receiveUdpClient = New UdpClient()
            Console.WriteLine($"En écoute sur le port {port} pour les messages UDP...")

            While True
                ' Attendez un message UDP
                Dim result = Await receiveUdpClient.ReceiveAsync()
                Dim receivedMessage = Encoding.UTF8.GetString(result.Buffer)

                ' Affichez les données reçues et l'expéditeur
                Console.WriteLine($"Message reçu : {receivedMessage}")
                Console.WriteLine($"De : {result.RemoteEndPoint.Address}:{result.RemoteEndPoint.Port}")
            End While
        Catch ex As SocketException
            Console.WriteLine($"Erreur de socket : {ex.Message}")

        Catch ex As Exception
            Console.WriteLine($"Erreur : {ex.Message}")

        Finally
            ' Fermez le client UDP
            If receiveUdpClient IsNot Nothing Then
                receiveUdpClient.Close()
                Console.WriteLine("Récepteur UDP fermé.")
            End If

        End Try

    End Function

End Class
