namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// Accès en lecture aux données de référence ERP via <c>Orders.Api</c> (HTTP).
/// Remplace la consommation in-process des query services Orders : Vector ne référence
/// plus le module Orders, il l'appelle en REST (comme Address.Api). Découplage du build.
/// </summary>
public interface IErpReadApiClient
{
    Task<ErpMissionFullDto?> GetMissionFullAsync(Guid missionId, CancellationToken ct = default);

    /// <summary>
    /// Missions sur une fenêtre. <paramref name="assignedCrewIds"/> (optionnel) demande à Orders.Api
    /// de ne renvoyer que les missions affectées à ces équipages (param répétable
    /// <c>assignedCrewId</c>) : évite de rapatrier toute la journée pour n'en garder qu'une poignée,
    /// et surtout évite que le plafond <paramref name="take"/> (global) tronque les missions d'un
    /// équipage un jour chargé. Ignoré par Orders.Api s'il ne le gère pas encore (rétro-compatible).
    /// </summary>
    Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(
        DateTime from, DateTime to, int take,
        IReadOnlyCollection<Guid>? assignedCrewIds = null, CancellationToken ct = default);

    /// <summary>
    /// Toutes les missions affectées à un équipage, <b>sans borne de date</b>
    /// (<c>GET /crews/{crewId}/missions</c>). Le crew (cycle de vie ≤ 18h) EST le périmètre : la liste
    /// terrain se filtre par équipage uniquement, plus par jour. Liste vide si l'équipage n'a aucune mission.
    /// </summary>
    Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default);

    Task<ErpOrderEditDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid beneficiaryId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ListCrewIdsAsync(
        Guid personnelId, DateOnly onDate, int take, CancellationToken ct = default);

    /// <summary>
    /// Détail complet d'un équipage (membres avec Id, conducteur actif, véhicule) — MOB-4/MOB-11.
    /// Null si l'équipage est introuvable côté ERP.
    /// </summary>
    Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default);

    /// <summary>Résolution identité Keycloak (sub) → PER_ID. Null si non mappé ou endpoint absent.</summary>
    Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid keycloakSub, CancellationToken ct = default);

    /// <summary>
    /// Statut de transfert en facturation d'une mission (gel terrain, TRF-7). Null si mission introuvable.
    /// Valeurs : 0=NonTransférable, 1=Transférable, 2=Transféré, 3=Facturé.
    /// </summary>
    Task<int?> GetMissionTransferStatusAsync(Guid missionId, CancellationToken ct = default);
}
