' Création d'une analyse de log mécanique — Result pattern.
Public Class ClInsertLogAnalyzeUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As ILogAnalyzeRepository
    Private ReadOnly _analyzeModel As ClEditLogAnalyzeModel

    Public Sub New(analyzeModel As ClEditLogAnalyzeModel, repository As ILogAnalyzeRepository)
        _analyzeModel = analyzeModel
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            Dim analyze = _analyzeModel.ToLogAnalyze
            analyze.MarkAsNew()
            _repository.SaveAnalyze(analyze)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
