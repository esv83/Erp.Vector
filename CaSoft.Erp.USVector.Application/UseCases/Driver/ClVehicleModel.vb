


Public Class ClVehicleModel


    Public Sub New(objVehicle As ClVehicle)
        _VehicleID = objVehicle.Id
        _Immatriculation = objVehicle.Immatriculation
    End Sub

    Public Sub New(ByVal gVehicleID As Guid, ByVal strImmatriculation As String)
        _VehicleID = gVehicleID
        _Immatriculation = strImmatriculation
    End Sub

    Public ReadOnly Property VehicleID As Guid
    Public ReadOnly Property Immatriculation As String

End Class

