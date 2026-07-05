
' MOB — « Bien reçu » : acquittement d'une mission par l'équipage depuis la JobList.
' Pose l'horodatage d'ack (MST_ACK_AT via ClJobTimeData.AckTime). Le Save projette aussi
' l'ack vers Orders.Api (régulation) — cf. JobTimeRepository.ProjectToErp.
Public Class ClAckJobUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _repository As IJobTimeRepository
    Private _jobId As Guid
    Private _jobTime As ClJobTimeData

    Public Sub New(jobId As Guid, repository As IJobTimeRepository)
        _jobId = jobId
        _repository = repository
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute

        Try
            If CanExecute() Then

                ' Idempotent : si déjà acquitté, on conserve l'horodatage d'origine (no-op).
                If _jobTime IsNot Nothing AndAlso _jobTime.AckTime.HasValue Then
                    Response.SetResult(True)
                Else
                    If _jobTime Is Nothing Then
                        _jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).WithAckTime(DateTime.Now).Build
                    Else
                        _jobTime.AckTime = DateTime.Now
                    End If

                    ' Upsert BD Mobile (MST_ACK_AT) + projection best-effort vers Orders.Api (ackAt).
                    _repository.Save(_jobId, _jobTime)
                    Response.SetResult(True)
                End If

            End If

        Catch ex As Exception

            Response.AddError(ex.Message)

        Finally
            presenter.Handle(Response)
        End Try

    End Sub

    Public Overrides Sub Before()
        _jobTime = _repository.GetJobTimeData(_jobId)
    End Sub

End Class
