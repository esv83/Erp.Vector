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
    {
        var candidates = _erp.ListCrewIdsAsync(personnelId, onDate, 500, CancellationToken.None)
            .GetAwaiter().GetResult();

        // Garde-fou défensif : Orders.Api peut remonter des équipages qui ne partagent que le
        // VÉHICULE (bug de jointure côté ERP) et non l'appartenance réelle du personnel. On revérifie
        // donc que le personnel est bien MEMBRE de chaque équipage (ErpCrewFullDto.Members, Id = PER_ID).
        // Même philosophie « le filtre client garantit le résultat » que la joblist ; corrige à la fois
        // l'affichage de faux crews ET le trou d'autorisation (le garde-fou valide crewId ∈ ce résultat).
        return candidates
            .Select(id => _erp.GetCrewFullAsync(id, CancellationToken.None).GetAwaiter().GetResult())
            .OfType<ErpCrewFullDto>()
            .Where(crew => crew.Members.Any(m => m.Id == personnelId))
            .Select(crew => crew.Id)
            .ToList();
    }

    // Source HTTP : « frais » et « normal » sont identiques (le cache est ajouté par le décorateur).
    public IReadOnlyList<Guid> ResolveActiveCrewIdsFresh(Guid personnelId, DateOnly onDate)
        => ResolveActiveCrewIds(personnelId, onDate);

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
