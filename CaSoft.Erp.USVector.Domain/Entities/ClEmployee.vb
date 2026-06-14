Public Class ClEmployee

    Public Sub New(gId As Guid, strName As String, strLastName As String)
        _Id = gId
        _name = strName
        _lastName = strLastName
    End Sub

    Public ReadOnly Property Id As Guid
    Public ReadOnly Property Name As String
    Public ReadOnly Property LastName As String

    Public Function DisplayName() As String
        Return (LastName + " " + Name.ToUpper)
    End Function
End Class
