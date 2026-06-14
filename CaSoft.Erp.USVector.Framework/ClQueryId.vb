Public Class ClQueryId(Of T)
    Public Sub New(id As T)
        _Id = id
    End Sub

    Public ReadOnly Property Id As T

End Class
