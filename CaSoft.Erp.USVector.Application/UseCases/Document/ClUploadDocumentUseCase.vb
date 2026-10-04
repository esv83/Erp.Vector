Imports System.Threading

''' <summary>
''' TRF-10 — Dépose un document/photo terrain sur une mission. Historisé ; transféré ensuite
''' dans le paquet field-data (binaire servi par <c>imageUrl</c>). Result pattern.
''' </summary>
Public Class ClUploadDocumentUseCase
    Implements IResultUseCaseAsync(Of ClDocumentDtoOut)

    Private ReadOnly _command As ClUploadDocumentCommand
    Private ReadOnly _repository As IDocumentRepository

    Public Sub New(command As ClUploadDocumentCommand, repository As IDocumentRepository)
        _command = command
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClDocumentDtoOut)) Implements IResultUseCaseAsync(Of ClDocumentDtoOut).HandleAsync
        If _command.MissionId = Guid.Empty Then
            Return ClResult(Of ClDocumentDtoOut).Fail(ClError.Application("Mission obligatoire."))
        End If
        If _command.Content Is Nothing OrElse _command.Content.Length = 0 Then
            Return ClResult(Of ClDocumentDtoOut).Fail(ClError.Application("Fichier manquant."))
        End If
        If Not [Enum].IsDefined(GetType(EnDocumentCategory), _command.Category) Then
            Return ClResult(Of ClDocumentDtoOut).Fail(ClError.Application("Catégorie de document invalide."))
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

        Await _repository.SaveAsync(document, ct)
        Return ClResult(Of ClDocumentDtoOut).Ok(document.ToDtoOut())
    End Function

End Class
