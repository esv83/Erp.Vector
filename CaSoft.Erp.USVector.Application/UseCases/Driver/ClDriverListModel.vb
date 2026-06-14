Public Class ClDriverListModel
    Inherits List(Of ClDriverModel)

    Public Sub New(lstEmployee As List(Of ClEmployee))
        For Each employee In lstEmployee
            Me.Add(New ClDriverModel(employee.Id, employee.DisplayName))
        Next
    End Sub
End Class
