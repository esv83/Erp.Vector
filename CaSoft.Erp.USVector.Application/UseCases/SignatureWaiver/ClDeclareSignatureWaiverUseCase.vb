Imports System.Threading
Imports CaSoft.Erp.USVector.Application.Port

''' <summary>
''' L'équipage déclare que le patient ne peut pas signer, et pourquoi. Pose ou remplace le motif de la
''' mission ; la projection vers Order part avec l'écriture (PRJ-1). Refus métier en <c>Fail</c>, rien
''' n'est écrit. Result pattern.
''' </summary>
Public Class ClDeclareSignatureWaiverUseCase
    Implements IResultUseCaseAsync(Of ClSignatureWaiverDtoOut)

    ''' <summary>Sans état : partagé entre les appels.</summary>
    Private Shared ReadOnly Validateur As New ClDeclareSignatureWaiverDtoInValidator()

    Private ReadOnly _missionId As Guid
    Private ReadOnly _input As ClDeclareSignatureWaiverDtoIn
    Private ReadOnly _repository As ISignatureWaiverRepository

    Public Sub New(missionId As Guid, input As ClDeclareSignatureWaiverDtoIn, repository As ISignatureWaiverRepository)
        _missionId = missionId
        _input = input
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClSignatureWaiverDtoOut)) Implements IResultUseCaseAsync(Of ClSignatureWaiverDtoOut).HandleAsync
        If _missionId = Guid.Empty Then
            Return Refus("Mission obligatoire.")
        End If
        If _input Is Nothing Then
            Return Refus("Corps de la déclaration manquant.")
        End If

        Dim verdict = Validateur.Validate(_input)
        If Not verdict.IsValid Then
            Return Refus(String.Join(" ", verdict.Errors.Select(Function(e) e.ErrorMessage)))
        End If

        Dim code = _input.ReasonCode.Trim().ToUpperInvariant()
        Dim reason = Await _repository.GetReasonAsync(code, ct)
        If reason Is Nothing OrElse Not reason.IsActive Then
            Return Refus($"Motif inconnu ou retiré : {code}.")
        End If

        Dim comment = If(String.IsNullOrWhiteSpace(_input.Comment), Nothing, _input.Comment.Trim())
        If reason.IsCommentRequired AndAlso comment Is Nothing Then
            Return Refus($"Le motif « {reason.Label} » demande un commentaire.")
        End If

        Dim waiver As New ClSignatureWaiver With {
            .MissionId = _missionId,
            .ReasonCode = reason.Code,
            .Comment = comment,
            .DeclaredAt = ClHorloge.MaintenantUtc(),
            .DeclaredCrewId = _input.CrewId
        }

        Await _repository.SaveAsync(waiver, ct)
        Return ClResult(Of ClSignatureWaiverDtoOut).Ok(waiver.ToDtoOut(reason.Label))
    End Function

    Private Shared Function Refus(message As String) As ClResult(Of ClSignatureWaiverDtoOut)
        Return ClResult(Of ClSignatureWaiverDtoOut).Fail(ClError.Application(message))
    End Function

End Class
