Imports System.Threading

''' <summary>
''' Carte mutuelle — présence pour une liste de missions (écrans de régulation et de certification,
''' 04/10). Les écrans ne connaissent que la mission : le patient est résolu ici, en un appel à Orders,
''' puis la présence est lue en une requête. Result pattern.
''' </summary>
Public Class ClGetMissionsMutuelleCardPresenceUseCase
    Implements IResultUseCaseAsync(Of IReadOnlyList(Of ClMissionMutuelleCardPresenceDtoOut))

    ''' <summary>Plafond d'un appel — celui de <c>POST /missions/batch-refs</c> chez Orders.</summary>
    Public Const MaxMissions As Integer = 200

    Private ReadOnly _missionIds As IReadOnlyCollection(Of Guid)
    Private ReadOnly _beneficiaries As IMissionBeneficiaryQueryService
    Private ReadOnly _repository As IMutuelleCardRepository

    Public Sub New(missionIds As IReadOnlyCollection(Of Guid),
                   beneficiaries As IMissionBeneficiaryQueryService,
                   repository As IMutuelleCardRepository)
        _missionIds = missionIds
        _beneficiaries = beneficiaries
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of IReadOnlyList(Of ClMissionMutuelleCardPresenceDtoOut))) Implements IResultUseCaseAsync(Of IReadOnlyList(Of ClMissionMutuelleCardPresenceDtoOut)).HandleAsync

        Dim ids = If(_missionIds, Array.Empty(Of Guid)()).Where(Function(id) id <> Guid.Empty).Distinct().ToList()

        If ids.Count > MaxMissions Then
            Return ClResult(Of IReadOnlyList(Of ClMissionMutuelleCardPresenceDtoOut)).Fail(
                ClError.Application($"Trop de missions demandées ({ids.Count}) : maximum {MaxMissions} par appel."))
        End If

        ' Un écran sans ligne n'est pas une erreur d'appelant : une liste vide, sans solliciter Orders.
        If ids.Count = 0 Then
            Return ClResult(Of IReadOnlyList(Of ClMissionMutuelleCardPresenceDtoOut)).Ok(New List(Of ClMissionMutuelleCardPresenceDtoOut)())
        End If

        Dim patients = Await _beneficiaries.GetBeneficiaryIdsAsync(ids, ct)

        Dim connus = patients.Values.Where(Function(b) b.HasValue).Select(Function(b) b.Value).Distinct().ToList()
        Dim presences = If(connus.Count = 0,
                           New Dictionary(Of Guid, DateTime)(),
                           (Await _repository.ListPresenceAsync(connus, ct)).ToDictionary(Function(p) p.BeneficiaryId, Function(p) p.CapturedAt))

        ' Une ligne par mission demandée, dans l'ordre de la demande.
        Dim lignes As New List(Of ClMissionMutuelleCardPresenceDtoOut)
        For Each missionId In ids
            Dim patient As Guid? = Nothing
            patients.TryGetValue(missionId, patient)

            Dim capturedAt As DateTime
            Dim hasCard = patient.HasValue AndAlso presences.TryGetValue(patient.Value, capturedAt)

            lignes.Add(New ClMissionMutuelleCardPresenceDtoOut With {
                .MissionId = missionId,
                .BeneficiaryId = patient,
                .HasCard = hasCard,
                .CapturedAt = If(hasCard, capturedAt, CType(Nothing, DateTime?)),
                .ImageUrl = If(hasCard, $"api/missions/{missionId}/mutuelle-card/image", Nothing)
            })
        Next

        Return ClResult(Of IReadOnlyList(Of ClMissionMutuelleCardPresenceDtoOut)).Ok(lignes)

    End Function

End Class
