Public Class ClServerAddress

    Public Sub New(strAddress As String, intPort As Integer)
        _Address = strAddress
        _Port = intPort
    End Sub
    Public ReadOnly Property Address As String
    Public ReadOnly Property Port As Integer

End Class
