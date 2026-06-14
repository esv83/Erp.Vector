Public Class ClIdValueModel

    Public Sub New(id As Object, value As String)
        _Id = id
        _Value = value
    End Sub
    Public ReadOnly Property Id As Object
    Public ReadOnly Property Value As String

End Class
