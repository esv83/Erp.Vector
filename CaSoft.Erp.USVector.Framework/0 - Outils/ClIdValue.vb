Public Class ClIdValue

    Public Sub New(Id As Object, strDisplay As String)
        _Id = Id
        _Display = strDisplay
    End Sub

    Public ReadOnly Property Id As Object

    Public ReadOnly Property Display As String

End Class
