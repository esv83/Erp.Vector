Public Class ClSetKilometersUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _command As ClSetKilometersCommand
    Private _repository As ICrewRepository
    Private _crewCache As ICrewCache
    Public Sub New(Command As ClSetKilometersCommand, cache As ICrewCache, Repository As ICrewRepository)
        _command = Command
        _repository = Repository
        _crewCache = cache
    End Sub

    Public Overrides sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Try
            Dim crew = _crewCache.GetCrew(_command.CrewId)
            Dim result As Boolean = crew.Vehicle.SetKilometers(Date.Now, _command.Kilometers, _command.InputBy)

            _repository.Update(crew)

            Response.SetResult(True)
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try


    End Sub

    Public Overrides Sub Before()

    End Sub
End Class
