Imports System.Threading

''' <summary>
''' Carte mutuelle — capture depuis une mission. Résout le patient de la mission côté ERP, puis
''' délègue validation et stockage à <see cref="ClUploadMutuelleCardUseCase"/> ; la mission est
''' tracée d'office. Result pattern.
''' </summary>
''' <remarks>
''' Le terrain n'a jamais reçu l'identifiant du patient (<c>ClPatientDto</c> ne le porte pas) : la
''' route par bénéficiaire lui était inatteignable. Résoudre ici évite aussi de rattacher une carte
''' au patient que désignerait le client.
''' </remarks>
Public Class ClUploadMissionMutuelleCardUseCase

    Private ReadOnly _command As ClUploadMissionMutuelleCardCommand
    Private ReadOnly _beneficiaries As IMissionBeneficiaryQueryService
    Private ReadOnly _repository As IMutuelleCardRepository

    Public Sub New(command As ClUploadMissionMutuelleCardCommand,
                   beneficiaries As IMissionBeneficiaryQueryService,
                   repository As IMutuelleCardRepository)
        _command = command
        _beneficiaries = beneficiaries
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClMutuelleCardCreatedDtoOut))
        Dim beneficiaryId As Guid?
        Try
            beneficiaryId = Await _beneficiaries.GetBeneficiaryIdAsync(_command.MissionId, ct)
        Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
            Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try

        If Not beneficiaryId.HasValue Then
            Return ClResult(Of ClMutuelleCardCreatedDtoOut).Fail(ClError.NotFound("Patient introuvable pour cette mission."))
        End If

        Dim command As New ClUploadMutuelleCardCommand(
            beneficiaryId.Value, _command.Image, _command.ContentType, _command.CrewId, _command.MissionId)
        Return New ClUploadMutuelleCardUseCase(command, _repository).Handle()
    End Function

End Class
