Public Class ClInsertMechanicLogUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _repository As ILogRepository
    Private _command As ClInsertLogModel
    Public Sub New(cmd As ClInsertLogModel, repository As ILogRepository)
        _repository = repository
        _command = cmd
    End Sub
    Public Overrides Sub Execute(presenter As IResponseHandler)
        Try
            If CanExecute() Then

                _repository.InsertLog(_command.CrewId, _command.Constat, DateTime.Now)
                Response.SetResult(True)
            End If
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try

    End Sub
    Public Overrides Sub Before()

        If IsNothing(_command.CrewId) Then
            Response.AddError("L'id de l'equipage est null")
        End If

        If String.IsNullOrWhiteSpace(_command.Constat) Then
            Response.AddError("Le constat ne peut etre vide")
        End If


    End Sub

End Class
