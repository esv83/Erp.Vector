''' <summary>
''' P2 — Renseigne manuellement les champs mutuelle (nom/AMC/concentrateur/télétransmission)
''' d'une carte. Saisie humaine = donnée fiable → statut <c>validated</c>. L'OCR (P3) ne fera
''' que pré-remplir ces mêmes champs avant validation. Result pattern.
''' </summary>
Public Class ClSetMutuelleFieldsUseCase
    Implements IResultUseCase(Of ClMutuelleCardDtoOut)

    Private ReadOnly _command As ClSetMutuelleFieldsCommand
    Private ReadOnly _repository As IMutuelleCardRepository

    Public Sub New(command As ClSetMutuelleFieldsCommand, repository As IMutuelleCardRepository)
        _command = command
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClMutuelleCardDtoOut) Implements IResultUseCase(Of ClMutuelleCardDtoOut).Handle
        Try
            Dim f = _command.Fields
            Dim patch As New ClMutuelleCard With {
                .Id = _command.CardId,
                .MutuelleName = f.MutuelleName,
                .AmcCode = f.AmcCode,
                .Concentrateur = f.Concentrateur,
                .Teletransmission = f.Teletransmission,
                .OcrStatus = "validated",
                .OcrValidatedAt = DateTime.UtcNow
            }

            Dim updated = _repository.Update(patch)
            If updated Is Nothing Then
                Return ClResult(Of ClMutuelleCardDtoOut).Fail(ClError.Application("Carte mutuelle introuvable."))
            End If

            Return ClResult(Of ClMutuelleCardDtoOut).Ok(updated.ToDtoOut())
        Catch ex As Exception
            Return ClResult(Of ClMutuelleCardDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
