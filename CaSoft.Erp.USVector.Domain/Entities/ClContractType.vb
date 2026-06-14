Public Class ClContractType
    Public Sub New()

    End Sub
    Public Sub New(intId As Integer, strDisplay As String, contractAttributs As ClAttributCollection)

        _Id = intId
        _Display = strDisplay
        _Attributs = contractAttributs

    End Sub
    Public ReadOnly Property Id As Integer
    Public ReadOnly Property Display As String
    Public ReadOnly Property Attributs As ClAttributCollection

End Class
