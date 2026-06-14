Friend Class ClGetCrewUseCase
    Inherits ClUseCaseBase

    Private _query As Guid
    Private _repository As ICrewRepository
    Public Sub New(query As Guid, repository As ICrewRepository)
        _query = query
        _repository = repository

    End Sub
    Public Overrides Sub execute(presenter As IResponseHandler)
        Try
            Dim crew = _repository.GetCrew(_query)
            Response.SetResult(crew)
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try

    End Sub

    Public Overrides Sub Before()
        If IsNull(_query) Then

            Response.AddError("Le CrewId est null")

        End If
    End Sub



End Class
