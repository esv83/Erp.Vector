Public Class ClGetCrewIdListUseCase
    Inherits ClUseCaseBase

    Private _query As DateOnly
    Private _repository As ICrewRepository

    Public Sub New(query As DateOnly, repository As ICrewRepository)
        _query = query
        _repository = repository

    End Sub
    Public Overrides Sub execute(presenter As IResponseHandler)

        If CanExecute() Then

            Try
                Dim crewIdList As List(Of Guid) = _repository.GetCrewIdList(_query)
                Response.SetResult(crewIdList)
            Catch ex As Exception
                Response.AddError(ex.Message)
            Finally
                presenter.Handle(Response)
            End Try

        End If

    End Sub

    Public Overrides Sub Before()

    End Sub
End Class
