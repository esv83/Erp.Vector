using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Dto;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
// ICrewRepository existe côté ERP et côté mobile : on désambiguïse explicitement.
using IMobileCrewRepository = CaSoft.Erp.USVector.Application.Port.ICrewRepository;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// MOB-4/5/11 — Équipage et missions du terrain. La liste des missions et l'équipage viennent
/// d'Orders.Api (HTTP, découplage 4a), les flags opérationnels (ack/terminé) de la BD Mobile
/// (<c>MOB_MISSION_STATE</c>). Asynchrone de bout en bout depuis le 04/10.
/// </summary>
public class CrewRepository : IMobileCrewRepository
{
    // Statut mission Orders.Api à partir duquel la mission n'apparaît plus au terrain (clôturée). Cf. spec §14.
    private const int ClosedMissionStatus = 4;

    private readonly IErpReadApiClient _erp;
    private readonly IErpWriteApiClient _erpWrite;
    private readonly MobileDbContext _mobileDb;
    private readonly ISignatureRepository _signatures;

    public CrewRepository(IErpReadApiClient erp, IErpWriteApiClient erpWrite,
        MobileDbContext mobileDb, ISignatureRepository signatures)
    {
        _erp = erp;
        _erpWrite = erpWrite;
        _mobileDb = mobileDb;
        _signatures = signatures;
    }

    public async Task<List<ClJobListItemModel>> FetchJobListAsync(IReadOnlyCollection<Guid> gCrewIds, CancellationToken ct)
    {
        if (gCrewIds is null || gCrewIds.Count == 0)
            return new List<ClJobListItemModel>();

        var crewSet = gCrewIds.ToHashSet();

        // Périmètre = LE CREW (cycle de vie ≤ 18h), JAMAIS la date. Orders.Api renvoie directement
        // toutes les missions affectées à l'équipage via GET /crews/{crewId}/missions (aucune borne
        // de date) → la joblist se filtre par crewId uniquement (missions de tous les jours du crew).
        // Union dédupliquée si plusieurs crews. Affichées jusqu'à « Terminé » (status 3) ; les missions
        // clôturées (status ≥ 4) disparaissent du terrain (spec §14 : accès autorisé jusqu'au clôturé).
        // Le filtre « engagée » (axe distinct de la progression, cf. endPoint.md §5) est délégué à Orders
        // via engagedOnly=true sur cette route crew-missions : pas de repli client ici, le DTO liste ne
        // porte pas l'état d'engagement. Désormais honoré côté Orders.Api (ListByCrewAsync filtre
        // MIS_IS_ENGAGED) → les missions affectées mais non engagées ne remontent plus au terrain.
        var parEquipage = new List<ErpMissionListItemDto>();
        foreach (var id in crewSet)
            parEquipage.AddRange(await _erp.ListMissionsByCrewAsync(id, ct));

        var crewMissions = parEquipage
            .Where(m => m.Status < ClosedMissionStatus)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .OrderBy(m => m.MissionDate)
            .ThenBy(m => m.SchedulingTime)
            .ToList();

        // Overlay des flags opérationnels depuis MOB_MISSION_STATE en une seule requête.
        var ids = crewMissions.Select(m => m.Id).ToList();
        var states = await _mobileDb.MissionStates
            .Where(s => ids.Contains(s.MST_MISSION_ID))
            .ToDictionaryAsync(s => s.MST_MISSION_ID, ct);

        // MOB-8 : overlay « signature existe » (MOB_SIGNATURE) — 1 requête clé seule.
        var signed = _signatures.ExistingFor(ids);

        var result = new List<ClJobListItemModel>();
        var index = 1;
        foreach (var m in crewMissions)
        {
            states.TryGetValue(m.Id, out var state);

            result.Add(new ClJobListItemModel
            {
                Index = index++,
                JobId = m.Id,
                Patient = m.BeneficiaryDisplayName ?? string.Empty,
                TransportMode = m.TransportModeId,
                // Sens de transport : l'UI attend 1=Aller / 2=Retour (MIS_KIND, exposé par Orders sur la liste crew).
                TransportSens = m.Kind,
                // TransportType / IsSerial absents du DTO liste léger ERP (portés par le détail Order) → MOB-6.
                schedule = m.SchedulingTime.ToString("HH:mm"),
                Appointment = m.AppointmentTime.HasValue
                    ? m.MissionDate.ToDateTime(m.AppointmentTime.Value)
                    : null,
                Departure = m.PickupLabel ?? string.Empty,
                Arrival = m.DropoffLabel ?? string.Empty,
                IsSeen = state?.MST_READ_AT is not null,   // « Mission vue » (spec §10)
                IsTerminated = state?.MST_TERMINATED_AT is not null,
                SignatureExists = signed.Contains(m.Id)
            });
        }

        return result;
    }

    // Pas d'équivalent ERP des instructions régulation → liste vide (cf. devplan, post-MVP).
    public List<ClInstructionListItemModel> FetchInstructionList(Guid gCrewId)
        => new();

    // ── MOB-4 : détail d'un équipage (membres + conducteur + véhicule) via Orders.Api ────────
    // Inconnu d'Orders → null : c'est au cas d'usage de dire ce que ça signifie pour l'ambulancier.
    public async Task<ClCrew?> GetCrewAsync(Guid gCrewID, CancellationToken ct)
        => (await _erp.GetCrewFullAsync(gCrewID, ct))?.ToDomain();

    // ── MOB-11 : désignation du conducteur (écriture ERP) ────────────────────────────────────
    public async Task<ClCrewDriverWriteResult> UpdateAsync(ClCrew crew, CancellationToken ct)
    {
        var driver = crew.LastDriver;
        if (driver is null)
            throw new InvalidOperationException($"Aucun conducteur à enregistrer pour l'équipage {crew.CrewId}.");

        var write = await _erpWrite.SetCrewDriverAsync(crew.CrewId, driver.Employee.Id, driver.From, ct);

        // Le motif d'Orders traverse tel quel : il nomme la règle (vacation terminée…), et c'est lui
        // que l'ambulancier doit lire. Refus et équipage inconnu se lisent pareil sur le terrain.
        return write.Outcome == EnCrewDriverWriteOutcome.Applied
            ? ClCrewDriverWriteResult.Applied()
            : ClCrewDriverWriteResult.Refused(write.Reason);
    }
}
