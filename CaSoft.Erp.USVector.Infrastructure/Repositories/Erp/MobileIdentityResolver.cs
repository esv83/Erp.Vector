using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// MOB-4a — Implémentation de <see cref="IMobileIdentityResolver"/> via Orders.Api (HTTP, découplage 4a).
/// <para><c>sub</c> Keycloak → PER_ID (endpoint additif <c>GET /personnel/by-keycloak/{sub}</c>).</para>
/// <para>PER_ID + date → crews actifs (<c>GET /crews?personnelId=&amp;date=</c>).</para>
/// </summary>
public class MobileIdentityResolver : IMobileIdentityResolver
{
    private readonly IErpReadApiClient _erp;

    public MobileIdentityResolver(IErpReadApiClient erp) => _erp = erp;

    public Task<Guid?> ResolvePersonnelIdAsync(Guid keyCloakSub, CancellationToken ct)
        => _erp.ResolvePersonnelIdByKeycloakAsync(keyCloakSub, ct);

    public async Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
    {
        var candidates = await _erp.ListCrewIdsAsync(personnelId, onDate, 500, ct);

        // Garde-fou défensif : Orders.Api peut remonter des équipages qui ne partagent que le
        // VÉHICULE (bug de jointure côté ERP) et non l'appartenance réelle du personnel. On revérifie
        // donc que le personnel est bien MEMBRE de chaque équipage (ErpCrewFullDto.Members, Id = PER_ID).
        // Même philosophie « le filtre client garantit le résultat » que la joblist ; corrige à la fois
        // l'affichage de faux crews ET le trou d'autorisation (le garde-fou valide crewId ∈ ce résultat).
        // Un à un, comme avant : un personnel n'a que quelques équipages candidats, et Orders est
        // protégé par son disjoncteur — pas de rafale à lui imposer pour gagner quelques ms.
        var actifs = new List<Guid>();
        foreach (var id in candidates)
        {
            var crew = await _erp.GetCrewFullAsync(id, ct);
            if (crew is not null && crew.Members.Any(m => m.Id == personnelId))
                actifs.Add(crew.Id);
        }
        return actifs;
    }

    // Source HTTP : « frais » et « normal » sont identiques (le cache est ajouté par le décorateur).
    public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsFreshAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
        => ResolveActiveCrewIdsAsync(personnelId, onDate, ct);

    public async Task<bool> IsMissionAccessibleAsync(Guid personnelId, Guid missionId, CancellationToken ct)
    {
        var mission = await _erp.GetMissionFullAsync(missionId, ct);

        if (mission is null || !mission.AssignedCrewId.HasValue)
            return false;

        // Crews du personnel actifs à la date de la mission (pas « aujourd'hui »).
        var crewIds = await ResolveActiveCrewIdsAsync(personnelId, mission.MissionDate, ct);
        return crewIds.Contains(mission.AssignedCrewId.Value);
    }
}
