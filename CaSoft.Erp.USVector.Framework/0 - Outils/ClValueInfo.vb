Public Class ClValueInfo(Of T)

    Public Sub New(value As T, attribut As String)
        _Value = value
        _Attribut = attribut
    End Sub

    Public ReadOnly Property Value As T

    Public ReadOnly Property Attribut As String

End Class
