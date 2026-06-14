
Public Class ClGetTimeUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _jobId As Guid
    Private _repository As IJobRepository

    Public Sub New(gJobId As Guid, repository As IJobRepository)

        _jobId = gJobId
        _repository = repository

    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        If CanExecute() Then
            Try
                'Dim job = _cache.GetJob(_query)
                Dim jobTime As ClJobTimeData = _repository.GetJobTime(_jobId)
                If jobTime Is Nothing Then
                    jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).Build
                End If

                Response.SetResult(jobTime.ToJobTimeModel)

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