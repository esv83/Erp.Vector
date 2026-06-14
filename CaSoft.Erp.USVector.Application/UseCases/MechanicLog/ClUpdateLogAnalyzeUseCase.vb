Public Class ClUpdateLogAnalyzeUseCase
    Inherits ClUseCaseBase

    Private _model As ClEditLogAnalyzeModel
    Private _repository As ILogAnalyzeRepository



    Public Sub New(model As ClEditLogAnalyzeModel, repository As ILogAnalyzeRepository)
        _model = model
        _repository = repository

    End Sub

    Public Overrides Sub Execute(Handler As IResponseHandler)
        Try
            If CanExecute() Then

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



                '  f
                _repository.SaveAnalyze(oldAnalyze)

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


