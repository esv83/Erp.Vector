

Public Class ClDriverModel

    Public Sub New(employee As ClEmployee)
        DriverId = employee.Id
        DriverName = employee.DisplayName
    End Sub

    Public Sub New(ByVal gId As Guid, ByVal Name As String)
        DriverId = gId
        DriverName = Name
    End Sub

    Public Property DriverId As Guid
    Public Property DriverName As String = String.Empty
End Class

