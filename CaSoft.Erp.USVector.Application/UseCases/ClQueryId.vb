Public Class ClQueryId(Of T)
    Inherits ClQueryBase

    Public Sub New(id As T)
        _Id = id
    End Sub
    Public ReadOnly Property Id As T

    Public Overrides Sub Valid(dte As Object)
        If _Id Is Nothing Then
            MyBase._IsValid = False
            MyBase._ErrorText = False = "L'Id est null (nothing)"
        End If

    End Sub
End Class


