namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// Accès en lecture aux données de référence ERP via <c>Orders.Api</c> (HTTP).
/// Remplace la consommation in-process des query services Orders : Vector ne référence
/// plus le module Orders, il l'appelle en REST (comme Address.Api). Découplage du build.
/// </summary>
public interface IErpReadApiClient
{
    Task<ErpMissionFullDto?> GetMissionFullAsync(Guid missionId, CancellationToken ct = default);

    Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(
        DateTime from, DateTime to, int take, CancellationToken ct = default);

    Task<ErpOrderEditDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid beneficiaryId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ListCrewIdsAsync(
        Guid personnelId, DateOnly onDate, int take, CancellationToken ct = default);

    /// <summary>Résolution identité Keycloak (sub) → PER_ID. Null si non mappé ou endpoint absent.</summary>
    Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid keycloakSub, CancellationToken ct = default);
}
