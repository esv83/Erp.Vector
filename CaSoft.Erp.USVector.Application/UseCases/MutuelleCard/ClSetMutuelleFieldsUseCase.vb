Imports System.Threading

''' <summary>
''' P2 — Renseigne manuellement les champs mutuelle (nom/AMC/concentrateur/télétransmission)
''' d'une carte. Saisie humaine = donnée fiable → statut <c>validated</c>. L'OCR (P3) ne fera
''' que pré-remplir ces mêmes champs avant validation. Result pattern.
''' </summary>
Public Class ClSetMutuelleFieldsUseCase
    Implements IResultUseCaseAsync(Of ClMutuelleCardDtoOut)

    ''' <summary>Sans état : partagé entre les appels.</summary>
    Private Shared ReadOnly Validateur As New ClMutuelleFieldsDtoInValidator()

    Private ReadOnly _command As ClSetMutuelleFieldsCommand
    Private ReadOnly _repository As IMutuelleCardRepository

    Public Sub New(command As ClSetMutuelleFieldsCommand, repository As IMutuelleCardRepository)
        _command = command
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClMutuelleCardDtoOut)) Implements IResultUseCaseAsync(Of ClMutuelleCardDtoOut).HandleAsync
        Dim f = _command.Fields
        If f Is Nothing Then
            Return ClResult(Of ClMutuelleCardDtoOut).Fail(ClError.Application("Corps de la saisie manquant."))
        End If

        ' Refus métier, en 400 avec leur motif — rien n'est écrit (04/10).
        Dim verdict = Validateur.Validate(f)
        If Not verdict.IsValid Then
            Return ClResult(Of ClMutuelleCardDtoOut).Fail(
                ClError.Application(String.Join(" ", verdict.Errors.Select(Function(e) e.ErrorMessage))))
        End If

        Dim patch As New ClMutuelleCard With {
            .Id = _command.CardId,
            .MutuelleName = f.MutuelleName,
            .AmcCode = f.AmcCode,
            .Concentrateur = f.Concentrateur,
            .Teletransmission = f.Teletransmission,
            .OcrStatus = "validated",
            .OcrValidatedAt = DateTime.UtcNow
        }

        Dim updated = Await _repository.UpdateAsync(patch, ct)
        If updated Is Nothing Then
            Return ClResult(Of ClMutuelleCardDtoOut).Fail(ClError.Application("Carte mutuelle introuvable."))
        End If

        Return ClResult(Of ClMutuelleCardDtoOut).Ok(updated.ToDtoOut())
    End Function

End Class
