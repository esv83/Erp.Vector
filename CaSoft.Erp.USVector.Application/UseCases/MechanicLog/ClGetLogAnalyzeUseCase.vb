
Public Class ClGetLogAnalyzeUseCase
    Inherits ClUseCaseBase

    Private _logId As Integer
    Private _repository As ILogAnalyzeRepository
    Public Sub New(intLogId As Integer, repository As ILogAnalyzeRepository)
        _logId = intLogId
        _repository = repository
    End Sub

    Public Overrides Sub Execute(presenter As IResponseHandler)
        Try
            Before()

            Dim logAnalyze = _repository.GetAnalyze(_logId)

            Dim analyzeModel As ClGetLogAnalyzeModel = Nothing
            If logAnalyze IsNot Nothing Then
                analyzeModel = logAnalyze.ToLogAnalyzeModel()
            End If


            Response.SetResult(analyzeModel)

        Catch ex As Exception
            Response.AddError(ex.Message)

        Finally
            presenter.Handle(Response)

        End Try

    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
