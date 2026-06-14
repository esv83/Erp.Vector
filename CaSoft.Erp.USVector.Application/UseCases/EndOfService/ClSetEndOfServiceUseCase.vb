Public Class ClSetEndOfServiceUseCase
    Inherits ClUseCaseBase
    Implements IUseCase


    Private _repository As ICrewRepository
    Private _cache As ICrewCache
    Private _query As ClSetEndOfServiceCommand

    Public Sub New(query As ClSetEndOfServiceCommand, cache As ICrewCache, repository As ICrewRepository)

        _query = query
        _repository = repository
        _cache = cache

    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        If CanExecute() Then

            Try
                Dim crew = _cache.GetCrew(_query.CrewId)
                Dim valuInfo As New ClValueInfo(Of DateTime)(_query.EndOfServiceDate, _query.Source)
                crew.ServiceEndDateR.AddValueInfo(valuInfo)
                'TODO Generer une exception quand on affecte une fin de service a un crew qui n'a pas pris son service
                _repository.Update(crew)

                Response.SetResult(True)

            Catch ex As Exception
                Response.AddError(ex.Message)
            Finally
                presenter.Handle(Response)
            End Try

        End If

    End Sub

    Public Overrides Sub Before()
        Dim crew = _cache.GetCrew(_query.CrewId)
        If crew Is Nothing Then
            Response.AddError("Le Crew est null (nothing)")
        End If
    End Sub

End Class
