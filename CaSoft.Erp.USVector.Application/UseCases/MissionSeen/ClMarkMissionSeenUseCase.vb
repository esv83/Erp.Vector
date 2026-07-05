
' MOB — « Mission vue » (spec_architecture_vector_mission_dmz §10 : le marqueur terrain retenu est
' « vue », pas un acquittement). L'ambulancier signale à la régulation qu'il a reçu/vu la mission
' depuis la JobList (icône « bien reçu »). Pose l'horodatage « vue » (MST_READ_AT via
' ClJobTimeData.ReadTime). Le Save projette aussi l'info vers Orders.Api (régulation, MissionSeen).
Public Class ClMarkMissionSeenUseCase
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

                ' Idempotent : si déjà vue, on conserve l'horodatage d'origine (no-op).
                If _jobTime IsNot Nothing AndAlso _jobTime.ReadTime.HasValue Then
                    Response.SetResult(True)
                Else
                    If _jobTime Is Nothing Then
                        _jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).WithReadTime(DateTime.Now).Build
                    Else
                        _jobTime.ReadTime = DateTime.Now
                    End If

                    ' Upsert BD Mobile (MST_READ_AT) + projection best-effort vers Orders.Api (readAt).
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
