' Suppression d'un log mécanique — Result pattern.
Public Class ClDeleteLogAnalyzeUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _logId As Integer
    Private ReadOnly _repository As ILogRepository

    Public Sub New(intLogId As Integer, repository As ILogAnalyzeRepository)
        _logId = intLogId
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            _repository.DeleteLog(_logId)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
