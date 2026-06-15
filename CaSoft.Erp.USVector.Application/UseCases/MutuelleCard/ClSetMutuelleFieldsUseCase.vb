''' <summary>
''' P2 — Renseigne manuellement les champs mutuelle (nom/AMC/concentrateur/télétransmission)
''' d'une carte. Saisie humaine = donnée fiable → statut <c>validated</c>. L'OCR (P3) ne fera
''' que pré-remplir ces mêmes champs avant validation.
''' </summary>
Public Class ClSetMutuelleFieldsUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _command As ClSetMutuelleFieldsCommand
    Private _repository As IMutuelleCardRepository

    Public Sub New(command As ClSetMutuelleFieldsCommand, repository As IMutuelleCardRepository)
        _command = command
        _repository = repository
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
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
                Response.AddError("Carte mutuelle introuvable.")
            Else
                Response.SetResult(updated.ToDtoOut())
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
