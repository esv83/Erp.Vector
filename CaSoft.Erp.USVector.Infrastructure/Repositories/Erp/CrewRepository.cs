using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Dto;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
// ICrewRepository existe côté ERP et côté mobile : on désambiguïse explicitement.
using IMobileCrewRepository = CaSoft.Erp.USVector.Application.Port.ICrewRepository;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// MOB-5 — Implémentation ERP-backed de la partie liste de <see cref="ICrewRepository"/>.
/// La liste des missions vient d'Orders.Api (HTTP, découplage 4a), les flags opérationnels
/// (ack/terminé) de la BD Mobile (<c>MOB_MISSION_STATE</c>).
///
/// Les autres membres (résolution équipage, conducteur…) restent à implémenter en MOB-4/MOB-11.
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

    public List<ClJobListItemModel> FetchJobList(Guid gCrewId)
        => FetchJobList(new[] { gCrewId });

    public List<ClJobListItemModel> FetchJobList(IReadOnlyCollection<Guid> gCrewIds)
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
        // porte pas l'état d'engagement — tant qu'Orders ne l'honore pas, les missions affectées non
        // engagées fuient.
        var crewMissions = crewSet
            .SelectMany(id => _erp.ListMissionsByCrewAsync(id, CancellationToken.None).GetAwaiter().GetResult())
            .Where(m => m.Status < ClosedMissionStatus)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .OrderBy(m => m.MissionDate)
            .ThenBy(m => m.SchedulingTime)
            .ToList();

        // Overlay des flags opérationnels depuis MOB_MISSION_STATE en une seule requête.
        var ids = crewMissions.Select(m => m.Id).ToList();
        var states = _mobileDb.MissionStates
            .Where(s => ids.Contains(s.MST_MISSION_ID))
            .ToDictionary(s => s.MST_MISSION_ID);

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
                // TransportType / TransportSens / IsSerial absents du DTO liste léger ERP
                // (portés par le détail Order) → renseignés en MOB-6.
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
    public ClCrew GetCrew(Guid gCrewID)
    {
        var dto = _erp.GetCrewFullAsync(gCrewID, CancellationToken.None).GetAwaiter().GetResult();
        if (dto is null)
            throw new InvalidOperationException($"Équipage {gCrewID} introuvable côté ERP.");
        return dto.ToDomain();
    }

    public bool IsEmployeeInCrew(Guid gCrewID, Guid gEmployeeId)
    {
        var dto = _erp.GetCrewFullAsync(gCrewID, CancellationToken.None).GetAwaiter().GetResult();
        return dto is not null && dto.Members.Any(m => m.Id == gEmployeeId);
    }

    // ── MOB-11 : désignation du conducteur (écriture ERP) ────────────────────────────────────
    public void Update(ClCrew crew)
    {
        var driver = crew.LastDriver;
        if (driver is null)
            throw new InvalidOperationException($"Aucun conducteur à enregistrer pour l'équipage {crew.CrewId}.");

        _erpWrite.SetCrewDriverAsync(crew.CrewId, driver.Employee.Id, driver.From, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    // ── À implémenter ultérieurement ────────────────────────────────────────
    // GetCrewDriver(vehicleId) : lookup par véhicule non exposé par Orders.Api (le chemin canonique
    // passe par GetCrew(crewId)). GetCrewIdList(date) : nécessite un endpoint « crews du jour » global.
    public ClLogDriverModel GetCrewDriver(Guid gVehicleID) => throw new NotImplementedException("MOB-11 (lookup par véhicule)");
    public void AckInstruction(int instructionId) => throw new NotImplementedException("MOB-5 (instructions post-MVP)");
    public List<Guid> GetCrewIdList(DateOnly id) => throw new NotImplementedException("MOB-4 (liste crews du jour)");
}
