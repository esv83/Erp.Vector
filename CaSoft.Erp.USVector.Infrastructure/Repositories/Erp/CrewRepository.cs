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

        // Fenêtre du jour. Orders.Api liste les missions affectées ; on filtre ensuite
        // sur les équipages du personnel (pas de filtre crew natif).
        var today = DateTime.Today;

        // Pont sync/async : le contrat legacy ICrewRepository est synchrone.
        var missions = _erp.ListMissionsAsync(today, today.AddDays(1).AddTicks(-1), 500, CancellationToken.None)
            .GetAwaiter().GetResult();

        // Union des missions des crews du personnel (dédupliquée par mission).
        var crewMissions = missions
            .Where(m => m.AssignedCrewId.HasValue && crewSet.Contains(m.AssignedCrewId.Value))
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
                IsAck = state?.MST_ACK_AT is not null,
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
