' Journal mécanique d'un équipage (ou 8 derniers jours si pas de crew) — Result pattern.
Public Class ClGetMechanicLogUseCase
    Implements IResultUseCase(Of List(Of ClLogEntryModel))

    Private ReadOnly _crewId As Guid?
    Private ReadOnly _repository As ILogRepository

    Public Sub New(repository As ILogRepository)
        _crewId = Nothing
        _repository = repository
    End Sub

    Public Sub New(gCrewId As Guid?, repository As ILogRepository)
        _crewId = gCrewId
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of List(Of ClLogEntryModel)) Implements IResultUseCase(Of List(Of ClLogEntryModel)).Handle

        Try
            Dim result As List(Of ClLogEntry)
            If _crewId.HasValue Then
                result = _repository.GetLogsByCrew(_crewId)
            Else
                result = _repository.GetLogsByDate(DateOnly.FromDateTime(Date.Now).AddDays(-8), DateOnly.FromDateTime(Date.Now))
            End If

            Dim modelList As New List(Of ClLogEntryModel)
            For Each log In result
                modelList.Add(log.ToLogEntryModel)
            Next

            Return ClResult(Of List(Of ClLogEntryModel)).Ok(modelList)

        Catch ex As Exception
            Return ClResult(Of List(Of ClLogEntryModel)).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
