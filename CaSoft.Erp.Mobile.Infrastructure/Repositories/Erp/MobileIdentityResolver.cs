using CaSoft.Erp.Mobile.Application.Port;
using CaSoft.Orders.Application;

namespace CaSoft.Erp.Mobile.Infrastructure.Repositories.Erp;

/// <summary>
/// MOB-4a — Implémentation ERP-backed de <see cref="IMobileIdentityResolver"/>.
/// <para><c>sub</c> Keycloak → PER_ID via <see cref="IPersonnelQueryService"/>
/// (table de liaison PER_KEYCLOAK_MAP).</para>
/// <para>PER_ID + date → crews actifs via <see cref="ICrewQueryService"/>
/// (filtres PersonnelId + VacationDate déjà natifs).</para>
/// <para>Pont sync/async : le contrat mobile est synchrone. Sûr hors
/// SynchronizationContext (ASP.NET Core), cohérent avec Crew/JobRepository.</para>
/// </summary>
public class MobileIdentityResolver : IMobileIdentityResolver
{
    private readonly IPersonnelQueryService _personnel;
    private readonly ICrewQueryService _crews;
    private readonly IMissionDetailQueryService _missionDetail;

    public MobileIdentityResolver(
        IPersonnelQueryService personnel,
        ICrewQueryService crews,
        IMissionDetailQueryService missionDetail)
    {
        _personnel = personnel;
        _crews = crews;
        _missionDetail = missionDetail;
    }

    public Guid? ResolvePersonnelId(Guid keyCloakSub)
        => _personnel.GetPersonnelIdByKeyCloakIdAsync(keyCloakSub, CancellationToken.None)
            .GetAwaiter().GetResult();

    public IReadOnlyList<Guid> ResolveActiveCrewIds(Guid personnelId, DateOnly onDate)
    {
        var query = new ClListCrewsQuery
        {
            PersonnelId = personnelId,
            VacationDate = onDate,
            Take = 500
        };

        var crews = _crews.ListAsync(query, CancellationToken.None).GetAwaiter().GetResult();
        return crews.Select(c => c.Id).ToList();
    }

    public bool IsMissionAccessible(Guid personnelId, Guid missionId)
    {
        var mission = _missionDetail.GetFullAsync(missionId, CancellationToken.None)
            .GetAwaiter().GetResult();

        if (mission is null || !mission.AssignedCrewId.HasValue)
            return false;

        // Crews du personnel actifs à la date de la mission (pas « aujourd'hui »).
        var crewIds = ResolveActiveCrewIds(personnelId, mission.MissionDate);
        return crewIds.Contains(mission.AssignedCrewId.Value);
    }
}
