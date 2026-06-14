Public Class ClGetMechanicLogUseCase
    Inherits ClUseCaseBase

    Private _crewId As Guid?
    Private _repository As ILogRepository

    Public Sub New(repository As ILogRepository)

        _crewId = Nothing
        _repository = repository

    End Sub
    Public Sub New(gCrewId As Guid?, repository As ILogRepository)
        _crewId = gCrewId
        _repository = repository

    End Sub
    Public Overrides Sub Execute(presenter As IResponseHandler)
        Try

            If CanExecute() Then
                Dim result As List(Of ClLogEntry)
                If _crewId.HasValue Then
                    result = _repository.GetLogsByCrew(_crewId)
                Else
                    result = _repository.GetLogsByDate(DateOnly.FromDateTime(Date.Now).AddDays(-8), DateOnly.FromDateTime(Date.Now))
                End If

                Dim modelList As New List(Of ClLogEntryModel)

                For Each log In result
                    Dim logModel = log.ToLogEntryModel
                    modelList.Add(logModel)
                Next

                SetResult(modelList)

            End If

        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try

    End Sub

    Public Overrides Sub Before()

    End Sub
End Class
