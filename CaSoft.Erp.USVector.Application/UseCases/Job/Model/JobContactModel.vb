Public Class ContactInfoModel

    Private _phones As List(Of String)
    Private _mails As List(Of String)

    Public Sub New()
        _phones = New List(Of String)()
        _mails = New List(Of String)()

    End Sub

    Public Property NIR As String
    Public Property DDN As DateTime?

    Public ReadOnly Property Phones As List(Of String)
        Get
            Return _phones
        End Get
    End Property

    Public ReadOnly Property Emails As List(Of String)
        Get
            Return _mails
        End Get
    End Property

    Public Property Nom As String
    Public Property Prenom As String
End Class
