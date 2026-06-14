Public Class ClSetDriverCommand

    Public Sub New(gCrewId As Guid, gDriverId As Guid)
        _DriverId = gDriverId
        _CrewId = gCrewId
    End Sub

    Public ReadOnly Property DriverId As Guid
    Public ReadOnly Property CrewId As Guid


End Class
