''' <summary>
''' P1 — Dépose une photo de carte mutuelle (validation type/poids), la persiste, et renvoie l'Id.
''' </summary>
Public Class ClUploadMutuelleCardUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private Const MaxBytes As Integer = 8 * 1024 * 1024   ' 8 Mo

    Private _command As ClUploadMutuelleCardCommand
    Private _repository As IMutuelleCardRepository

    Public Sub New(command As ClUploadMutuelleCardCommand, repository As IMutuelleCardRepository)
        _command = command
        _repository = repository
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Try
            If _command.Image Is Nothing OrElse _command.Image.Length = 0 Then
                Response.AddError("Image manquante.")
            ElseIf String.IsNullOrWhiteSpace(_command.ContentType) _
                   OrElse Not _command.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) Then
                Response.AddError("Le fichier doit être une image.")
            ElseIf _command.Image.Length > MaxBytes Then
                Response.AddError("Image trop volumineuse (max 8 Mo).")
            Else
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
                Response.SetResult(New ClMutuelleCardCreatedDtoOut With {.Id = card.Id})
            End If
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try
    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
