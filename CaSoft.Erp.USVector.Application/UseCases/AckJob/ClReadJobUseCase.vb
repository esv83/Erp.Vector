
Public Class ClReadJobUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _repository As IJobTimeRepository
    Private _jobId As Guid
    Private _jobTime As ClJobTimeData

    Public Sub New(command As Guid, repository As IJobTimeRepository)
        _jobId = command
        _repository = repository

    End Sub
    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute


        Try
            If CanExecute() Then

                If _jobTime Is Nothing Then
                    _jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).WithReadTime(DateTime.Now).Build
                Else
                    _jobTime.ReadTime = DateTime.Now
                End If

            End If

            _repository.Save(_jobId, _jobTime)

            Response.SetResult(True)

        Catch ex As Exception

            Response.AddError(ex.Message)

        Finally
            presenter.Handle(Response)
        End Try




    End Sub

    Public Overrides Sub Before()

        _jobTime = _repository.GetJobTimeData(_jobId)
        If _jobTime IsNot Nothing Then

            If _jobTime.ReadTime.HasValue Then
                Response.AddError("La mission à déja été acquittée")
            End If
        End If

    End Sub


End Class
