Public Class ClConstraintModel
    Public Property Id As Integer
    Public Property Label As String
    Public Property RequiresDate As Boolean

    Public Sub New(id As Integer, label As String, Optional requiresDate As Boolean = False)
        Me.Id = id
        Me.Label = label
        Me.RequiresDate = requiresDate
    End Sub
End Class
