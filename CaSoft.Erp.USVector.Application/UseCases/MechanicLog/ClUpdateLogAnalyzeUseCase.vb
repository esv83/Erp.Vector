' Mise à jour d'une analyse de log mécanique (fusion des actions) — Result pattern.
Public Class ClUpdateLogAnalyzeUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _model As ClEditLogAnalyzeModel
    Private ReadOnly _repository As ILogAnalyzeRepository

    Public Sub New(model As ClEditLogAnalyzeModel, repository As ILogAnalyzeRepository)
        _model = model
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            Dim oldAnalyze = _repository.GetAnalyze(_model.LogId)
            Dim newAnalyze = _model.ToLogAnalyze

            oldAnalyze.Analyze = newAnalyze.Analyze
            oldAnalyze.Concerning = newAnalyze.Concerning
            oldAnalyze.Detail = newAnalyze.Detail
            oldAnalyze.ImmobilizeVehicle = newAnalyze.ImmobilizeVehicle
            oldAnalyze.Nature = newAnalyze.Nature

            For Each oldAction In oldAnalyze.ActionsList
                Dim newAction = newAnalyze.ActionsList.FirstOrDefault(Function(f) f.Id = oldAction.Id)
                If newAction Is Nothing Then
                    oldAction.MarkAsDeleted()
                Else
                    oldAction.Merge(newAction)
                End If
            Next

            For Each newAction In newAnalyze.ActionsList
                Dim oldAction = oldAnalyze.ActionsList.FirstOrDefault(Function(f) f.Id = newAction.Id)
                If oldAction Is Nothing Then
                    oldAnalyze.ActionsList.Add(newAction)
                End If
            Next

            _repository.SaveAnalyze(oldAnalyze)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
