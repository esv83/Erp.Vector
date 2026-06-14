Public Class ClJobIdQueryParameter
    Inherits ClQueryId(Of Guid)

    Public Sub New(id As Guid)
        MyBase.New(id)

        If id = Nothing Then
            _IsIdValid = False
            _IdError = "Le JobId est egal à nothing"
        Else
            If id = Guid.Empty Then
                _IdError = "Le JobId est vide {0000-0000-0000...}"
            End If
        End If

    End Sub

    Public ReadOnly Property IsIdValid As Boolean
    Public ReadOnly Property IdError As String

End Class
