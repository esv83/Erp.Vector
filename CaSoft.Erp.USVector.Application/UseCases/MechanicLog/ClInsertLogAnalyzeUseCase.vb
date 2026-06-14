Public Class ClInsertLogAnalyzeUseCase
    Inherits ClUseCaseBase

    Private _repository As ILogAnalyzeRepository
    Private _analyzeModel As ClEditLogAnalyzeModel

    Public Sub New(analyzeModel As ClEditLogAnalyzeModel, repository As ILogAnalyzeRepository)
        _analyzeModel = analyzeModel
        _repository = repository

    End Sub

    Public Overrides Sub Execute(Handler As IResponseHandler)
        Try
            Before()

            If CanExecute() Then

                Dim analyze = _analyzeModel.ToLogAnalyze
                analyze.MarkAsNew()

                _repository.SaveAnalyze(analyze)

                SetResult(True)
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
