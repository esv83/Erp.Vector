Public Class ClCrewListModel
    Private _crew As ClCrew
    Public Sub New(crew As ClCrew)
        _crew = crew
    End Sub

    Public ReadOnly Property Id As Guid
        Get
            Return _crew.CrewId
        End Get
    End Property
    Public ReadOnly Property Immatriculation As String
        Get
            Return _crew.Vehicle.Immatriculation
        End Get
    End Property

End Class
