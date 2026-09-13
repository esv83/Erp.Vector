Imports System.Threading

''' <summary>
''' Carte mutuelle — carte courante du patient d'une mission, pour que l'app sache à l'ouverture
''' s'il faut la photographier. <c>NotFound</c> si la mission n'a pas de patient ou si le patient
''' n'a aucune carte. Result pattern.
''' </summary>
Public Class ClGetMissionMutuelleCardUseCase

    Private ReadOnly _missionId As Guid
    Private ReadOnly _beneficiaries As IMissionBeneficiaryQueryService
    Private ReadOnly _repository As IMutuelleCardRepository

    Public Sub New(missionId As Guid,
                   beneficiaries As IMissionBeneficiaryQueryService,
                   repository As IMutuelleCardRepository)
        _missionId = missionId
        _beneficiaries = beneficiaries
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClMutuelleCardDtoOut))
        Try
            Dim beneficiaryId = Await _beneficiaries.GetBeneficiaryIdAsync(_missionId, ct)
            If Not beneficiaryId.HasValue Then
                Return ClResult(Of ClMutuelleCardDtoOut).Fail(ClError.NotFound("Patient introuvable pour cette mission."))
            End If

            Dim card = _repository.GetCurrent(beneficiaryId.Value)
            If card Is Nothing Then
                Return ClResult(Of ClMutuelleCardDtoOut).Fail(ClError.NotFound("Aucune carte mutuelle pour ce patient."))
            End If

            Return ClResult(Of ClMutuelleCardDtoOut).Ok(card.ToDtoOut())
        Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
            Return ClResult(Of ClMutuelleCardDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
