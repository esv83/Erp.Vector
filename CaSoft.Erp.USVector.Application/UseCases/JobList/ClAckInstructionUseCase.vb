Friend Class ClAckInstructionUseCase
    Inherits ClUseCaseBase

    Private _query As Integer
    Private _repository As ICrewRepository
    Public Sub New(query As Integer, repository As ICrewRepository)
        _query = query
        _repository = repository
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler)
        If CanExecute() Then
            Try
                _repository.AckInstruction(_query)
                Response.SetResult(True)
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
