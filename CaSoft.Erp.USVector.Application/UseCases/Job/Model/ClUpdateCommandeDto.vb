Public Class ClUpdateCommandeDto

    Public Property IsPmtPresent As Boolean
    Public Property Comments As String
    Public Property Reference As String
    Public Property FacturationAttributs As List(Of ClFacturationAttributDto)
    Public Class ClFacturationAttributDto
        Public Sub New(intAttributId As Integer, strValue As String)
            AttributId = intAttributId
            _Value = strValue
        End Sub
        Public Property AttributId As Integer
        Public Property Value As String
    End Class
End Class
