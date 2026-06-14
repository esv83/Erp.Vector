Public Class ClJobEditDto
    Public Sub New()
        Emails = New List(Of String)()
        Phones = New List(Of String)()
        ' SelectedContractType = New ClContractTypeSelectedModel()
    End Sub

    Public Property CrewComments As String
    Public Property SelectedContractType As Integer  '
    Public Property Emails As List(Of String)
    Public Property Phones As List(Of String)
    Public Property DDN As DateTime?

End Class
