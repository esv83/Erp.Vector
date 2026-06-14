
Public Class ClValidGuid
    Private _token As Guid
    Public Sub New(gToken As Guid)

        SetToken(gToken)


    End Sub

    Public Sub New(strToken As String)
        Dim token As Guid

        Try
            token = New Guid(strToken)
        Catch ex As Exception
            Throw New ArgumentOutOfRangeException("Impossible de transformer la chaine en token valide")
        End Try

        SetToken(token)

    End Sub
    Private Function IsFormatTokenValid(gToken As Guid) As Boolean
        Dim result As Boolean = False
        If gToken = Nothing Then
            Throw New ArgumentNullException("Le Guid est null")
        ElseIf gToken = Guid.Empty Then
            Throw New ArgumentNullException("Le Guid est vide {000-000-000-000}")
        Else
            result = True
        End If

        Return result
    End Function
    Private Sub SetToken(gToken As Guid)
        If IsFormatTokenValid(gToken) Then

            _token = gToken

        End If
    End Sub
    Public ReadOnly Property Value As Guid
        Get
            Return _token
        End Get

    End Property
End Class
