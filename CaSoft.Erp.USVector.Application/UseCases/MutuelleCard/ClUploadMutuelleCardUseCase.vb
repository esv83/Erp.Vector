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
        Try
            If _command.Image Is Nothing OrElse _command.Image.Length = 0 Then
                Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application("Image manquante."))
            ElseIf String.IsNullOrWhiteSpace(_command.ContentType) _
                   OrElse Not _command.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) Then
                Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application("Le fichier doit être une image."))
            ElseIf _command.Image.Length > MaxBytes Then
                Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application("Image trop volumineuse (max 8 Mo)."))
            End If

            Dim card As New ClMutuelleCard With {
                .Id = Guid.NewGuid(),
                .BeneficiaryId = _command.BeneficiaryId,
                .Image = _command.Image,
                .ContentType = _command.ContentType,
                .ByteSize = _command.Image.Length,
                .CapturedAt = DateTime.UtcNow,
                .CapturedCrewId = _command.CrewId,
                .MissionId = _command.MissionId,
                .OcrStatus = "none"
            }

            _repository.Save(card)
            Return ClResult(Of ClMutuelleCardCreatedDtoOut).Ok(New ClMutuelleCardCreatedDtoOut With {.Id = card.Id})
        Catch ex As Exception
            Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
