
' MOB — « Mission vue » (spec_architecture_vector_mission_dmz §10 : le marqueur terrain retenu est
' « vue », pas un acquittement). L'ambulancier signale à la régulation qu'il a reçu/vu la mission
' depuis la JobList (icône « bien reçu »). Pose l'horodatage « vue » (MST_READ_AT via
' ClJobTimeData.ReadTime). Le Save projette aussi l'info vers Orders.Api (régulation, MissionSeen).
'
' PILOTE de migration vers le Result pattern : renvoie ClResult(Of Boolean) via Handle() (au lieu de
' l'ancien Execute(presenter)). Se branche dans la plomberie existante via ClResultUseCaseAdapter.
Public Class ClMarkMissionSeenUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As IJobTimeRepository
    Private ReadOnly _jobId As Guid

    Public Sub New(jobId As Guid, repository As IJobTimeRepository)
        _jobId = jobId
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            Dim jobTime = _repository.GetJobTimeData(_jobId)

            ' Idempotent : si déjà vue, on conserve l'horodatage d'origine (no-op).
            If jobTime IsNot Nothing AndAlso jobTime.ReadTime.HasValue Then
                Return ClResult(Of Boolean).Ok(True)
            End If

            If jobTime Is Nothing Then
                jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).WithReadTime(DateTime.Now).Build
            Else
                jobTime.ReadTime = DateTime.Now
            End If

            ' Upsert BD Mobile (MST_READ_AT) + projection (Outbox) vers Orders.Api (readAt).
            _repository.Save(_jobId, jobTime)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
