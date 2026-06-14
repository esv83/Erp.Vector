
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

                jobTime.GoTime = New ClTimeFormatAdapter(_command.JobTime.GoTime).ToDateTime
                jobTime.OnSiteTime = New ClTimeFormatAdapter(_command.JobTime.OnSiteTime).ToDateTime
                jobTime.TerminateTime = New ClTimeFormatAdapter(_command.JobTime.TerminatedTime).ToDateTime

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
