using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// MOB-4a — Implémentation de <see cref="IMobileIdentityResolver"/> via Orders.Api (HTTP, découplage 4a).
/// <para><c>sub</c> Keycloak → PER_ID (endpoint additif <c>GET /personnel/by-keycloak/{sub}</c>).</para>
/// <para>PER_ID + date → crews actifs (<c>GET /crews?personnelId=&amp;date=</c>).</para>
/// <para>Pont sync/async : le contrat mobile est synchrone. Sûr hors SynchronizationContext (ASP.NET Core).</para>
/// </summary>
public class MobileIdentityResolver : IMobileIdentityResolver
{
    private readonly IErpReadApiClient _erp;

    public MobileIdentityResolver(IErpReadApiClient erp) => _erp = erp;

    public Guid? ResolvePersonnelId(Guid keyCloakSub)
        => _erp.ResolvePersonnelIdByKeycloakAsync(keyCloakSub, CancellationToken.None)
            .GetAwaiter().GetResult();

    public IReadOnlyList<Guid> ResolveActiveCrewIds(Guid personnelId, DateOnly onDate)
        => _erp.ListCrewIdsAsync(personnelId, onDate, 500, CancellationToken.None)
            .GetAwaiter().GetResult();

    public bool IsMissionAccessible(Guid personnelId, Guid missionId)
    {
        var mission = _erp.GetMissionFullAsync(missionId, CancellationToken.None)
            .GetAwaiter().GetResult();

        if (mission is null || !mission.AssignedCrewId.HasValue)
            return false;

        // Crews du personnel actifs à la date de la mission (pas « aujourd'hui »).
        var crewIds = ResolveActiveCrewIds(personnelId, mission.MissionDate);
        return crewIds.Contains(mission.AssignedCrewId.Value);
    }
}
