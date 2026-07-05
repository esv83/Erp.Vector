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

    public Guid? ResolveActiveCrewId(Guid personnelId, DateTime at)
    {
        var ids = ResolveActiveCrewIds(personnelId, DateOnly.FromDateTime(at));
        if (ids.Count == 0) return null;
        if (ids.Count == 1) return ids[0];

        // Plusieurs crews le même jour : on départage sur la fenêtre de service pour ne pas retomber
        // sur un crew clôturé (Orders.Api interdit toute modif dessus → 400 « crew clôturé »).
        var fulls = ids
            .Select(id => _erp.GetCrewFullAsync(id, CancellationToken.None).GetAwaiter().GetResult())
            .OfType<ErpCrewFullDto>()
            .ToList();
        if (fulls.Count == 0) return ids[0];

        // 1. Crew dont la fenêtre couvre l'instant (vacation en cours).
        var covering = fulls.FirstOrDefault(f => f.ServiceStart <= at && (f.ServiceEnd is null || at <= f.ServiceEnd));
        if (covering is not null) return covering.Id;

        // 2. Sinon, crew ouvert (pas encore clôturé), le plus proche à démarrer (prochaine vacation).
        var open = fulls
            .Where(f => f.ServiceEnd is null || f.ServiceEnd >= at)
            .OrderBy(f => f.ServiceStart)
            .FirstOrDefault();
        if (open is not null) return open.Id;

        // 3. Tout est clôturé → le plus récemment terminé (affichage en lecture, modif refusée côté ERP).
        return fulls.OrderByDescending(f => f.ServiceEnd ?? DateTime.MinValue).First().Id;
    }

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
