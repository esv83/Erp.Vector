Public Class ClContractTypeDto

    Public Sub New(intId As Integer, strName As String)
        _Id = intId
        _DisplayName = strName
        _Attributs = New List(Of ClContractAttributDto)
    End Sub
    Public Property Id As Integer
    Public Property DisplayName As String

    Public Property Attributs As List(Of ClContractAttributDto)

    Public Class ClContractAttributDto

        Public Sub New(strAttributName As String, strLabel As String, strValue As String)
            _AttributId = strAttributName
            _Label = strLabel
            _Value = strValue
        End Sub

        Public Property AttributId As String
        Public Property Label As String
        Public Property Value As String

    End Class

End Class
