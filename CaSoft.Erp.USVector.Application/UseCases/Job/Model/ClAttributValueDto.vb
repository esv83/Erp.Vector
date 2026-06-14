Public Class ClAttributValueDto

    Public Sub New(strName As String, strType As String)
        Name = strName
        Type = strType
        Value = Nothing

    End Sub
    Public Sub New(strName As String, strType As String, objValue As Object)
        Name = strName
        Type = strType
        Value = objValue

    End Sub

    Public Property Name As String
    Public Property Value As Object
    Public Property Type As String

End Class
