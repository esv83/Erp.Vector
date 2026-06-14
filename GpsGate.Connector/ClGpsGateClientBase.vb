Imports System.Globalization
Imports Newtonsoft.Json
Imports RestSharp
Public Class ClGpsGateClientBase

    Private _client As RestClient

    Private _token As String
    Public Property Login As String
    Public Property Password As String

    Protected Function Connect(intAppId As Integer) As Boolean
        ' ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls

        Dim dico As New Dictionary(Of String, String) From {
            {"username", Login},
            {"password", Password}
        }

        Dim strJson = JsonConvert.SerializeObject(dico, Formatting.Indented)

        Dim strUrl As String = String.Format("/applications/{0}/tokens", intAppId)
        Dim request = New RestRequest(strUrl, DataFormat.Json)
        request.AddJsonBody(strJson)

        Dim response = _client.Post(request)
        If response.IsSuccessful Then
            Dim strJsonResult = response.Content
            Dim JsonResult = JsonConvert.DeserializeObject(Of TokenResponse)(strJsonResult)
            _token = JsonResult.token

        Else
            NLog.LogManager.GetCurrentClassLogger.Error(String.Format("impossible de se connecter au serveur : {0}", response.ErrorMessage))
        End If

        Return Not String.IsNullOrWhiteSpace(_token)

    End Function
    Private Function TryConnect(intAppId As Integer) As Boolean
        Dim result As Boolean = True

        If Not IsConnected Then
            result = Connect(intAppId)

        End If

        Return result

    End Function
    Protected Function GetView(intAppId As Integer) As List(Of View)
        Dim result As New List(Of View)
        Dim strUrl As String = String.Format("/applications/{0}/views", intAppId)

        Dim response = GetRequest(strUrl)


        If response.IsSuccessful Then
            Dim strJsonResult = response.Content
            Dim JsonResult = JsonConvert.DeserializeObject(Of List(Of View))(strJsonResult)
            result.AddRange(JsonResult)
        Else
            NLog.LogManager.GetCurrentClassLogger.Warn(String.Format("impossible de deserialiser les vues de l'appli numéro {0}", intAppId))
        End If

        Return result


    End Function
    Protected Function GetUsers(intApplicationId As Integer, intViewId As Integer) As List(Of ClGgsUserModel)
        Dim result As New List(Of ClGgsUserModel)
        Dim strUrl As String = String.Format("/applications/{0}/users?&ViewId={1}", intApplicationId, intViewId)

        Dim response = GetRequest(strUrl)
        If response.IsSuccessful Then
            Dim JsonREsult = JsonConvert.DeserializeObject(Of List(Of ClGgsUserModel))(response.Content)
            result.AddRange(JsonREsult)
        End If
        Return result
    End Function
    Protected Function GetUsers(intApplicationId As Integer, intViewId As Integer, intFromIndex As Integer, intPageSize As Integer) As List(Of ClGgsUserModel)
        Dim result As New List(Of ClGgsUserModel)
        Dim strUrl As String = String.Format("/applications/{0}/users?FromIndex={1}&PageSize={2}&ViewId={3}", intApplicationId, intFromIndex, intPageSize, intViewId)

        Dim response = GetRequest(strUrl)
        If response.IsSuccessful Then
            Dim JsonREsult = JsonConvert.DeserializeObject(Of List(Of ClGgsUserModel))(response.Content)
            result.AddRange(JsonREsult)
        End If
        Return result
    End Function
    Protected Function GetUsers(intApplicationId As Integer, intViewId As Integer, intFromIndex As Integer, intPageSize As Integer, dteFromDate As DateTime) As List(Of ClGgsUserModel)
        Dim result As New List(Of ClGgsUserModel)
        'TODO Checker la gestion de l'heure UTC par GpsGAte
        '1996-12-19T16:39:57-08:00
        Dim strDte = dteFromDate.ToString("yyyy-MM-dd'T'HH:mm:ss", DateTimeFormatInfo.InvariantInfo)
        Dim strUrl As String = String.Format("/applications/{0}/users?FromIndex={1}&PageSize={2}&ViewId={3}&UpdatesFrom={4}", intApplicationId, intFromIndex, intPageSize, intViewId, strDte)

        Dim response = GetRequest(strUrl)
        If response.IsSuccessful Then
            Dim JsonREsult = JsonConvert.DeserializeObject(Of List(Of ClGgsUserModel))(response.Content)
            result.AddRange(JsonREsult)
        End If
        Return result
    End Function
    Protected Function GetUsersInTag(intApplicationId As Integer, intTag As Integer) As List(Of ClGgsUserModel)
        Dim result As New List(Of ClGgsUserModel)

        If Not TryConnect(intApplicationId) Then
            Return result
            NLog.LogManager.GetCurrentClassLogger.Error("impossible de se logguer au serveur GpsGate")
        End If

        Dim strUrl As String = String.Format("/applications/{0}/tags/{1}/users", intApplicationId, intTag)
        Dim response = GetRequest(strUrl)
        If response.IsSuccessful Then
            Dim JsonREsult = JsonConvert.DeserializeObject(Of List(Of ClGgsUserModel))(response.Content)
            result.AddRange(JsonREsult)
        End If

        Return result

    End Function
    Protected Function GetUserByName(intApplicationId As Integer, strUserName As String) As ClGgsUserModel
        Dim result As ClGgsUserModel = Nothing
        Dim strUrl As String = String.Format("/applications/{0}/users/{1}?Identifier=Username", intApplicationId, strUserName)

        Dim response = GetRequest(strUrl)
        If response.IsSuccessful Then
            result = JsonConvert.DeserializeObject(Of ClGgsUserModel)(response.Content)

        End If

        Return result
    End Function
    Protected Function GetTags(intApplicationId As Integer) As List(Of Tag)
        Dim result As New List(Of Tag)
        Dim strUrl As String = String.Format("/applications/{0}/tags/", intApplicationId)

        Dim response = GetRequest(strUrl)
        If response.IsSuccessful Then
            Dim JsonREsult = JsonConvert.DeserializeObject(Of List(Of Tag))(response.Content)
            result.AddRange(JsonREsult)
        End If

        Return result

    End Function
    Private Function GetRequest(strUrl As String) As RestResponse
        Dim result As RestResponse

        Dim request = New RestRequest(strUrl, DataFormat.Json)
        request.AddHeader("Authorization", _token)

        result = _client.Get(request)
        If Not result.IsSuccessful Then

            NLog.LogManager.GetCurrentClassLogger.Warn(String.Format("erreur sur la requete {0} : {0}", strUrl, result.ErrorMessage))
        End If

        Return result

    End Function

    Protected Function GetStatus(appId As Integer, userId As Integer) As ClGgsUserStatutModel
        Throw New NotImplementedException()
    End Function

    Protected Function AddUserInTag(intAppId As Integer, intTagId As Integer, intUserId As Integer) As ClGgsUserModel
        Dim result As ClGgsUserModel = Nothing


        Dim idObject = New With {.Id = intUserId}

        Dim jsonBody = JsonConvert.SerializeObject(idObject, Formatting.Indented)

        ' Dim strJson = JsonConvert.SerializeObject(dico, Formatting.Indented)

        Dim strUrl As String = String.Format("/applications/{0}/tags/{1}/users", intAppId, intTagId)
        Dim request = New RestRequest(strUrl, DataFormat.Json)
        request.AddHeader("Authorization", _token)
        request.AddJsonBody(jsonBody)

        Dim response = _client.Post(request)
        If response.IsSuccessful Then
            Dim strJsonResult = response.Content
            result = JsonConvert.DeserializeObject(Of ClGgsUserModel)(strJsonResult)
        Else
            NLog.LogManager.GetCurrentClassLogger.Error(String.Format("impossible de se connecter au serveur : {0}", response.ErrorMessage))
        End If

        Return result
    End Function

    Protected Function RemoveUserInTag(intAppId As Integer, intTagId As Integer, intUserId As Integer) As Object
        'http://ambuloc.net/comGpsGate/api/v.1/applications/8/tags/264/users/666

        Dim result As Boolean = False

        Dim strUrl As String = String.Format("/applications/{0}/tags/{1}/users/{2}", intAppId, intTagId, intUserId)
        Dim request = New RestRequest(strUrl, DataFormat.Json)
        request.AddHeader("Authorization", _token)

        Dim response = _client.Delete(request)
        result = response.IsSuccessful
        If Not response.IsSuccessful Then
            NLog.LogManager.GetCurrentClassLogger.Error(String.Format("impossible de se connecter au serveur : {0}", response.ErrorMessage))
        End If

        Return result

    End Function

    Public ReadOnly Property IsConnected As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(_token)
        End Get
    End Property
    Public ReadOnly Property Token As String
        Get
            Return _token
        End Get
    End Property

    Private _serverName As String
    Public Property ServerName As String
        Get
            Return _serverName
        End Get
        Protected Friend Set(value As String)
            _serverName = value
            Dim strBaseUrl As String = String.Format("http://{0}/comGpsGate/api/v.1", _serverName)  'TODO rendre parametrable
            _client = New RestClient(strBaseUrl)
        End Set
    End Property

End Class