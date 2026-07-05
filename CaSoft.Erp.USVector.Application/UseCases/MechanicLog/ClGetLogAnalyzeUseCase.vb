' Lecture de l'analyse d'un log mécanique — Result pattern. (Analyse absente → Ok(Nothing) → 404.)
Public Class ClGetLogAnalyzeUseCase
    Implements IResultUseCase(Of ClGetLogAnalyzeModel)

    Private ReadOnly _logId As Integer
    Private ReadOnly _repository As ILogAnalyzeRepository

    Public Sub New(intLogId As Integer, repository As ILogAnalyzeRepository)
        _logId = intLogId
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClGetLogAnalyzeModel) Implements IResultUseCase(Of ClGetLogAnalyzeModel).Handle

        Try
            Dim logAnalyze = _repository.GetAnalyze(_logId)

            Dim analyzeModel As ClGetLogAnalyzeModel = Nothing
            If logAnalyze IsNot Nothing Then
                analyzeModel = logAnalyze.ToLogAnalyzeModel()
            End If

            Return ClResult(Of ClGetLogAnalyzeModel).Ok(analyzeModel)

        Catch ex As Exception
            Return ClResult(Of ClGetLogAnalyzeModel).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
