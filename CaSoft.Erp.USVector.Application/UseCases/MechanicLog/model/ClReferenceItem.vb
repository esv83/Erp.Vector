Public Class ClReferenceItem


    Public Sub New(Id As Integer, Value As String)
        Me.Id = Id
        Me.Value = Value
    End Sub

    Public ReadOnly Property Id As Integer
    Public ReadOnly Property Value As String

End Class
