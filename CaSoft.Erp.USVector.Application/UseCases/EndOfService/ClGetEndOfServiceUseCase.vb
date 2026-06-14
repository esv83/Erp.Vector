Public Class ClGetEndOfServiceUseCase
    Inherits ClUseCaseBase

    Private _repository As ICrewRepository
    Private _query As Guid
    Public Sub New(query As Guid, Repository As ICrewRepository)
        _query = query
        _repository = Repository
    End Sub

    Public Overrides sub execute(presenter as IResponseHandler)

        If CanExecute() Then
            Try
                Dim crew As ClCrew = _repository.GetCrew(_query)
                Response.SetResult(New ClReliableDateModel With {.Date = crew.ServiceEndDateR, .Fiability = crew.ServiceEndDateR.Level})

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

