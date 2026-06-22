''' <summary>
''' TRF-10 — Dépose un document/photo terrain sur une mission. Historisé ; transféré ensuite
''' dans le paquet field-data (binaire servi par <c>imageUrl</c>).
''' </summary>
Public Class ClUploadDocumentUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _command As ClUploadDocumentCommand
    Private _repository As IDocumentRepository

    Public Sub New(command As ClUploadDocumentCommand, repository As IDocumentRepository)
        _command = command
        _repository = repository
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Try
            If _command.MissionId = Guid.Empty Then
                Response.AddError("Mission obligatoire.")
                Return
            End If
            If _command.Content Is Nothing OrElse _command.Content.Length = 0 Then
                Response.AddError("Fichier manquant.")
                Return
            End If
            If Not [Enum].IsDefined(GetType(EnDocumentCategory), _command.Category) Then
                Response.AddError("Catégorie de document invalide.")
                Return
            End If

            Dim document As New ClDocument With {
                .Id = Guid.NewGuid(),
                .MissionId = _command.MissionId,
                .Category = CType(_command.Category, EnDocumentCategory),
                .Content = _command.Content,
                .ContentType = _command.ContentType,
                .ByteSize = _command.Content.Length,
                .FileName = _command.FileName,
                .CapturedAt = DateTime.UtcNow,
                .CapturedCrewId = _command.CrewId
            }

            _repository.Save(document)
            Response.SetResult(document.ToDtoOut())
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try
    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
