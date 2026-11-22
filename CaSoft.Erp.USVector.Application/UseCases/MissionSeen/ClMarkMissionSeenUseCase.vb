Imports CaSoft.Framework.Application
Imports System.Threading

' MOB — « Mission vue » (spec_architecture_vector_mission_dmz §10 : le marqueur terrain retenu est
' « vue », pas un acquittement). L'ambulancier signale à la régulation qu'il a reçu/vu la mission
' depuis la JobList (icône « bien reçu »). Pose l'horodatage « vue » (MST_READ_AT via
' ClJobTimeData.ReadTime). Le Save projette aussi l'info vers Orders.Api (régulation, MissionSeen).
' ⚖️ En heure LOCALE, par convention partagée avec Orders (MOP_READ_AT) — voir ClHorloge.
Public Class ClMarkMissionSeenUseCase
    Implements IResultUseCaseAsync(Of Boolean)

    Private ReadOnly _repository As IJobTimeRepository
    Private ReadOnly _unitOfWork As IUnitOfWork
    Private ReadOnly _jobId As Guid
    Private ReadOnly _seenAt As DateTimeOffset?

    ''' <summary>Tolérance sur l'horloge du téléphone : une heure un peu en avance est ramenée à maintenant.</summary>
    Public Shared ReadOnly AvanceToleree As TimeSpan = TimeSpan.FromMinutes(5)

    ''' <summary>Au-delà, un « Vu » rejoué ne dit plus rien d'utile à la régulation : il est refusé.</summary>
    Public Shared ReadOnly RetardMaximal As TimeSpan = TimeSpan.FromHours(24)

    ''' <param name="seenAt">
    ''' Heure du clic donnée par l'app (22/11, J-172 : « Vu » hors réseau, envoyé au retour du réseau).
    ''' <c>Nothing</c> : l'heure de réception, comme avant.
    ''' </param>
    Public Sub New(jobId As Guid, repository As IJobTimeRepository, unitOfWork As IUnitOfWork,
                   Optional seenAt As DateTimeOffset? = Nothing)
        _jobId = jobId
        _repository = repository
        _unitOfWork = unitOfWork
        _seenAt = seenAt
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IResultUseCaseAsync(Of Boolean).HandleAsync

        Dim lecture = HeureDuVu(ClHorloge.MaintenantLocal())
        If lecture.IsFail Then Return ClResult(Of Boolean).Fail(lecture.InnerError)

        Dim jobTime = Await _repository.GetJobTimeDataAsync(_jobId, ct)

        ' Idempotent : si déjà vue, on conserve l'horodatage d'origine (no-op).
        If jobTime IsNot Nothing AndAlso jobTime.ReadTime.HasValue Then
            Return ClResult(Of Boolean).Ok(True)
        End If

        If jobTime Is Nothing Then
            jobTime = ClJobTimeData.GetBuilder.WithId(_jobId).WithReadTime(lecture.Value).Build
        Else
            jobTime.ReadTime = lecture.Value
        End If

        ' Upsert BD Mobile (MST_READ_AT) + projection (Outbox) vers Orders.Api (readAt).
        Await _repository.SaveAsync(_jobId, jobTime, ct)
        Await _unitOfWork.CommitAsync(ct)
        Return ClResult(Of Boolean).Ok(True)

    End Function

    ''' <summary>
    ''' L'heure à poser, en heure <b>locale</b> du serveur (convention partagée, voir <see cref="ClHorloge"/>).
    ''' </summary>
    ''' <remarks>
    ''' Sans heure du clic : maintenant. Avec : convertie en heure locale ; jusqu'à 5 minutes d'avance
    ''' (horloge du téléphone), ramenée à maintenant ; au-delà, ou plus de 24 heures en arrière, refusée.
    ''' </remarks>
    Private Function HeureDuVu(maintenant As DateTime) As ClResult(Of DateTime)
        If Not _seenAt.HasValue Then Return ClResult(Of DateTime).Ok(maintenant)

        Dim clic = _seenAt.Value.ToLocalTime().DateTime
        If clic > maintenant + AvanceToleree Then
            Return ClResult(Of DateTime).Fail(ClError.Application(
                "Heure du « Vu » dans le futur : vérifiez l'heure du téléphone."))
        End If
        If clic < maintenant - RetardMaximal Then
            Return ClResult(Of DateTime).Fail(ClError.Application(
                "Heure du « Vu » de plus de 24 heures : elle n'est plus acceptée."))
        End If
        Return ClResult(Of DateTime).Ok(If(clic > maintenant, maintenant, clic))
    End Function

End Class
