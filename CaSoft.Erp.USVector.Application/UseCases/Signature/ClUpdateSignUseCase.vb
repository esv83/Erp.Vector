Public Class ClUpdateSignUseCase
    Inherits ClUseCaseBase

    Private _repository As ISignatureRepository
    Private _command As ClUpdateSignCommand
    Public Sub New(command As ClUpdateSignCommand, Repository As ISignatureRepository)
        _command = command
        _repository = Repository
    End Sub

    Public Overrides sub execute(presenter As IResponseHandler)
        Try
            _repository.Insert(_command.JobId, _command.Data)
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
