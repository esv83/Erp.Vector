Public Class ClSetKilometersCommand
    Public Sub New(gCrewId As Guid, intKilometers As Integer, strInputBy As String)
        _Kilometers = intKilometers
        _InputBy = strInputBy
        _CrewId = gCrewId
    End Sub

    Public ReadOnly Property Kilometers As Integer
    Public ReadOnly Property InputBy As String
    Public ReadOnly Property CrewId As Guid

End Class
