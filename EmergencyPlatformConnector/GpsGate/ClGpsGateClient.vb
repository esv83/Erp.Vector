Imports CaSoft.Connectors.GpsGate

Public Class ClGpsGateClient
    Inherits ClGpsGateClientBase
    Implements IGeolocServer


    Private _AppId As Integer = 8
    Private _ViewId As Integer
    Const AMU_TAG = 264

    Private Sub New()
    End Sub
    Public Overloads Function Connect() As Boolean

        If Not MyBase.IsConnected Then
            MyBase.Connect(_AppId)
        End If

        Return MyBase.IsConnected

    End Function
    Public Overloads Function GetView() As List(Of View)

        Return MyBase.GetView(_AppId)

    End Function
    Public Overloads Function GetUsers() As List(Of ClUserModel) Implements IGeolocServer.GetUsers

        Dim result As New List(Of ClUserModel)

        If Connect() Then

            Dim ggsUsersList = MyBase.GetUsers(_AppId, _ViewId)
            For Each ggsUser In ggsUsersList
                result.Add(GetUserModelFromGgs(ggsUser))
            Next
        Else
            Debug.WriteLine("impossible de se connecter au serveur GpsGate")
        End If

        Return result

    End Function

    Public Function GetUpdatedUsers(fromDate As DateTime) As List(Of ClUserModel) Implements IGeolocServer.GetUpdatedUsers
        Dim result As New List(Of ClUserModel)

        Dim updatedUsers = MyBase.GetUsers(_AppId, _ViewId, 0, 1000, fromDate)
        If updatedUsers IsNot Nothing Then
            For Each user In updatedUsers
                result.Add(GetUserModelFromGgs(user))
            Next

        End If

        Return result

    End Function
    Private Function GetUserModelFromGgs(ggsUser As ClGgsUserModel) As ClUserModel
        Dim result As New ClUserModel
        If ggsUser.devices.Count > 0 Then

            With result
                .Heading = Integer.Parse(Math.Floor(ggsUser.trackPoint.velocity.heading))
                .Imei = ggsUser.devices(0).imei
                .Immatriculation = ggsUser.username
                .Latitude = ggsUser.trackPoint.position.latitude
                .Longitude = ggsUser.trackPoint.position.longitude
                .LastUtcPosition = ggsUser.trackPoint.utc
                .TransmitDate = Nothing
                .Speed = ggsUser.trackPoint.velocity.groundSpeed
                .HasChange = True
            End With
            NLog.LogManager.GetCurrentClassLogger.Info(String.Format("{0} ({1}) mis à jour avec position du {2} à {3}", result.Immatriculation, result.Imei, result.LastUtcPosition.ToShortDateString, result.LastUtcPosition.ToShortTimeString))

        Else
            NLog.LogManager.GetCurrentClassLogger.Warn(String.Format("{0} ({1}) n'à pas de devices", result.Immatriculation, result.Imei))

        End If

        Return result

    End Function
    Public Property AppId As Integer
        Get
            Return _AppId
        End Get
        Protected Friend Set(value As Integer)
            _AppId = value
        End Set
    End Property
    Public Property ViewID As Integer
        Get
            Return _ViewId
        End Get
        Protected Friend Set(value As Integer)
            _ViewId = value
        End Set
    End Property

    Private _userListCache As List(Of ClGgsUserModel)
    Public ReadOnly Property UserList As List(Of ClGgsUserModel)
        Get
            If _userListCache Is Nothing Then
                _userListCache = MyBase.GetUsers(_AppId, _ViewId)
            End If

            Return _userListCache

        End Get
    End Property
    Public Shared Function GetBuilder() As ClGpsGateServiceBuilder
        Return ClGpsGateServiceBuilder.GetBuilder
    End Function

    Public Overloads Function GetUser(strImmat As String) As ClUserModel Implements IGeolocServer.GetUser
        Dim user = MyBase.GetUserByName(_AppId, strImmat)
        Dim result = GetUserModelFromGgs(user)

        Return result

    End Function
    Public Overloads Function GetStatus(strImmat As String) As ClUserModel
        Dim result As ClUserModel = Nothing

        Dim user = UserList.SingleOrDefault(Function(f) f.username = strImmat)
        If user IsNot Nothing Then
            Dim status = MyBase.GetStatus(_AppId, user.id)
            If status IsNot Nothing Then
                user.trackPoint.position = status.position
                user.deviceActivity = status.deviceActivity
                user.trackPoint.velocity = status.velocity
                user.trackPoint.utc = status.uTC

            End If
            result = GetUserModelFromGgs(user)

        End If

        Return result

    End Function

    Public Function AddToView(strImmatriculation As String) As Boolean Implements IGeolocServer.AddToView
        Dim result As Boolean = False

        Dim user = MyBase.GetUserByName(_AppId, strImmatriculation)
        If user IsNot Nothing Then
            Dim serverResult = MyBase.AddUserInTag(_AppId, AMU_TAG, user.id)
            result = (serverResult IsNot Nothing)
        End If

        Return result



    End Function

    Public Function RemoveFromAmuView(strImmatriculation As String) As Boolean Implements IGeolocServer.RemoveFromAmuView

        Dim user = MyBase.GetUserByName(_AppId, strImmatriculation)
        Dim serverResult = MyBase.RemoveUserInTag(_AppId, AMU_TAG, user.id)

        Return (serverResult IsNot Nothing)

    End Function

    Public Class ClGpsGateServiceBuilder

        Private _ggsSce As ClGpsGateClient

        Private Sub New()
            _ggsSce = New ClGpsGateClient
        End Sub

        Public Shared Function GetBuilder() As ClGpsGateServiceBuilder
            Return New ClGpsGateServiceBuilder
        End Function
        Public Function WithServer(strServerName As String) As ClGpsGateServiceBuilder
            _ggsSce.ServerName = strServerName
            Return Me
        End Function
        Public Function WithAppId(intAppId As Integer) As ClGpsGateServiceBuilder

            _ggsSce.AppId = intAppId
            Return Me

        End Function
        Public Function WithViewId(intViewId As Integer) As ClGpsGateServiceBuilder

            _ggsSce.ViewID = intViewId
            Return Me

        End Function
        Public Function WithCredential(login As String, password As String) As ClGpsGateServiceBuilder

            _ggsSce.Login = login
            _ggsSce.Password = password

            Return Me

        End Function
        Public Function WithCredential(Credential As ClGpsGateCredential) As ClGpsGateServiceBuilder

            _ggsSce.ServerName = Credential.ServerName
            _ggsSce.AppId = Credential.ApplicationID
            _ggsSce.Login = Credential.UserName
            _ggsSce.Password = Credential.Password

            Return Me

        End Function
        Public Function Build() As ClGpsGateClient
            _ggsSce.Connect()
            Return _ggsSce
        End Function


    End Class

End Class