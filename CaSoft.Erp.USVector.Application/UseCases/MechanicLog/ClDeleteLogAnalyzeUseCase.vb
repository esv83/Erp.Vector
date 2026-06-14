Public Class ClDeleteLogAnalyzeUseCase
    Inherits ClUseCaseBase

    Private _logId As Integer
    Private _repository As ILogRepository



    Public Sub New(intLogId As Integer, repository As ILogAnalyzeRepository)
        _logId = intLogId
        _repository = repository
    End Sub

    Public Overrides Sub Execute(Handler As IResponseHandler)
        Try
            If CanExecute() Then
                _repository.DeleteLog(_logId)
            End If

        Catch ex As Exception
            Response.AddError(ex)
        Finally
            Handler.Handle(Response)
        End Try

    End Sub

    Public Overrides Sub Before()
    End Sub

End Class


