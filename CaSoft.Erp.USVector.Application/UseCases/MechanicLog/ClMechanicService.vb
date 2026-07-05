Public Class ClMechanicService

    Private ReadOnly _repository As ILogAnalyzeRepository

    Public Sub New(repository As ILogAnalyzeRepository)
        _repository = repository
    End Sub

    Public Function GetLogs(gCrewId As Guid) As ClResult(Of List(Of ClLogEntryModel))
        Return New ClGetMechanicLogUseCase(gCrewId, _repository).Handle()
    End Function

    Public Function GetAnalyze(intLogId As Integer) As ClResult(Of ClGetLogAnalyzeModel)
        Return New ClGetLogAnalyzeUseCase(intLogId, _repository).Handle()
    End Function

    Public Function InsertAnalyze(analyze As ClEditLogAnalyzeModel) As ClResult(Of Boolean)
        Return New ClInsertLogAnalyzeUseCase(analyze, _repository).Handle()
    End Function

    Public Function UpdateAnalyze(analyze As ClEditLogAnalyzeModel) As ClResult(Of Boolean)
        Return New ClUpdateLogAnalyzeUseCase(analyze, _repository).Handle()
    End Function

    Public Function DeleteAnalyze(intId As Integer) As ClResult(Of Boolean)
        Return New ClDeleteLogAnalyzeUseCase(intId, _repository).Handle()
    End Function

End Class
