Public Class ClGpsGateCredential

    Public Sub New(strServerName As String, strUserName As String, strPassword As String, intApplicationId As Integer, intSirusTagId As Integer)
        _ServerName = strServerName
        _UserName = strUserName
        _Password = strPassword
        _ApplicationID = intApplicationId
        _SirusTagId = intSirusTagId

    End Sub
    Public Property ServerName As String
    Public Property UserName As String
    Public Property Password As String
    Public Property ApplicationID As Integer
    Public Property SirusTagId As Integer

End Class
