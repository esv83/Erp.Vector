
Public Class ClFieldManager
    Inherits Dictionary(Of String, ClPropertyInfo)


    Public Function FieldExists(propertyInfo As ClPropertyInfo) As Boolean
        Return Me.ContainsKey(propertyInfo.Name)
    End Function
    Public Function FieldExists(propertyName As String) As Boolean
        Return Me.ContainsKey(propertyName)
    End Function


End Class

