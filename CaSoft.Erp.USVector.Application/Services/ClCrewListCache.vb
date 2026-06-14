Public Class ClCrewListCache
    Inherits List(Of ClCrew)
    Implements ICrewCache

    Private _repository As ICrewRepository
    Public Sub New(repository As ICrewRepository)
        _repository = repository
    End Sub
    Public Function GetCrew(gCrewId As Guid) As ClCrew Implements ICrewCache.GetCrew
        Dim result As ClCrew = Nothing

        result = Me.SingleOrDefault(Function(f) f.CrewId = gCrewId)
        If result Is Nothing Then
            result = _repository.GetCrew(gCrewId)
            Me.Add(result)
        End If

        Return result

    End Function

End Class

