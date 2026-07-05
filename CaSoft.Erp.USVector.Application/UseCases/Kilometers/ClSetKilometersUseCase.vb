
' Enregistrement du kilométrage véhicule — Result pattern. (Sans consommateur actif.)
Public Class ClSetKilometersUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _command As ClSetKilometersCommand
    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _crewCache As ICrewCache

    Public Sub New(Command As ClSetKilometersCommand, cache As ICrewCache, Repository As ICrewRepository)
        _command = Command
        _repository = Repository
        _crewCache = cache
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            Dim crew = _crewCache.GetCrew(_command.CrewId)
            Dim result As Boolean = crew.Vehicle.SetKilometers(Date.Now, _command.Kilometers, _command.InputBy)

            _repository.Update(crew)

            Return ClResult(Of Boolean).Ok(True)
        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
