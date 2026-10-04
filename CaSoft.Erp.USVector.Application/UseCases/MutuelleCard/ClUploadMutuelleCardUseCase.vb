''' <summary>
''' P1 — Dépose une photo de carte mutuelle (validation type/poids), la persiste, et renvoie l'Id. Result pattern.
''' </summary>
Public Class ClUploadMutuelleCardUseCase
    Implements IResultUseCase(Of ClMutuelleCardCreatedDtoOut)

    Private Const MaxBytes As Integer = 8 * 1024 * 1024   ' 8 Mo

    Private ReadOnly _command As ClUploadMutuelleCardCommand
    Private ReadOnly _repository As IMutuelleCardRepository

    Public Sub New(command As ClUploadMutuelleCardCommand, repository As IMutuelleCardRepository)
        _command = command
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClMutuelleCardCreatedDtoOut) Implements IResultUseCase(Of ClMutuelleCardCreatedDtoOut).Handle
        If _command.Image Is Nothing OrElse _command.Image.Length = 0 Then
            Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application("Image manquante."))
        ElseIf String.IsNullOrWhiteSpace(_command.ContentType) _
               OrElse Not _command.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) Then
            Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application("Le fichier doit être une image."))
        ElseIf _command.Image.Length > MaxBytes Then
            Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application("Image trop volumineuse (max 8 Mo)."))
        End If

        ' P3 — la carte part EN FILE de lecture automatique (« pending »). Sans worker
        ' configuré, ce statut ne fait rien : personne ne dépile, et la saisie manuelle reste
        ' le seul chemin, exactement comme avant.
        Dim card As New ClMutuelleCard With {
            .Id = Guid.NewGuid(),
            .BeneficiaryId = _command.BeneficiaryId,
            .Image = _command.Image,
            .ContentType = _command.ContentType,
            .ByteSize = _command.Image.Length,
            .CapturedAt = DateTime.UtcNow,
            .CapturedCrewId = _command.CrewId,
            .MissionId = _command.MissionId,
            .OcrStatus = "pending"
        }
        HeriterDesChampsValides(card, _repository.GetCurrentMetadata(_command.BeneficiaryId))

        _repository.Save(card)
        Return ClResult(Of ClMutuelleCardCreatedDtoOut).Ok(New ClMutuelleCardCreatedDtoOut With {.Id = card.Id})
    End Function

    ''' <summary>
    ''' 04/10 — Une nouvelle photo d'un patient connu reprend les champs <b>validés</b> de la précédente
    ''' (ou repris eux-mêmes), en disant de quelle photo ils viennent. Sans cela, la mutuelle disparaissait
    ''' de la carte courante à chaque photo, alors que la carte n'a le plus souvent pas changé.
    ''' <para>
    ''' Ce n'est pas une écriture aveugle (M5) : rien n'est lu par une machine, on reprend ce qu'un humain
    ''' a validé, marqué « à revérifier ». La lecture automatique, si elle tourne, propose à côté.
    ''' </para>
    ''' </summary>
    Private Shared Sub HeriterDesChampsValides(card As ClMutuelleCard, precedente As ClMutuelleCard)
        If precedente Is Nothing Then Return

        Dim valides = precedente.FieldsInheritedFrom.HasValue OrElse
                      String.Equals(precedente.OcrStatus, "validated", StringComparison.Ordinal)
        If Not valides Then Return

        card.MutuelleName = precedente.MutuelleName
        card.AmcCode = precedente.AmcCode
        card.Concentrateur = precedente.Concentrateur
        card.Teletransmission = precedente.Teletransmission
        ' La date d'ORIGINE : reprise deux fois, c'est toujours sur la même photo qu'elle a été validée.
        card.FieldsInheritedFrom = If(precedente.FieldsInheritedFrom, precedente.CapturedAt)
    End Sub

End Class
