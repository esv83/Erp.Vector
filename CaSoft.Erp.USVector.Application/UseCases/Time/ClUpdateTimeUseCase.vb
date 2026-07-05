
Public Class ClUpdateTimeUseCase
    Inherits ClUseCaseBase

    Private _repository As IJobRepository
    Private _command As ClJobTimeCommand

    Public Sub New(command As ClJobTimeCommand, repository As IJobRepository)
        _command = command

        _repository = repository

    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler)

        If CanExecute() Then
            Try
                Dim jobTime As ClJobTimeData = _repository.GetJobTime(_command.JobId)

                ' Cumulatif : on ne pose QUE les jalons réellement fournis (non-null) ; les autres
                ' conservent leur valeur existante. Le client peut ainsi n'envoyer que le jalon franchi
                ' sans effacer les précédents (aligné sur la projection ERP cumulative).
                Dim goTime = New ClTimeFormatAdapter(_command.JobTime.GoTime).ToDateTime
                If goTime.HasValue Then jobTime.GoTime = goTime

                Dim onSiteTime = New ClTimeFormatAdapter(_command.JobTime.OnSiteTime).ToDateTime
                If onSiteTime.HasValue Then jobTime.OnSiteTime = onSiteTime

                Dim terminateTime = New ClTimeFormatAdapter(_command.JobTime.TerminatedTime).ToDateTime
                If terminateTime.HasValue Then jobTime.TerminateTime = terminateTime

                _repository.SaveJobTime(jobTime)
                ' _repository.Save(job)

                Response.SetResult(True)
            Catch ex As Exception
                Response.AddError(ex.Message)
            Finally
                presenter.Handle(Response)
            End Try


        End If

    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
