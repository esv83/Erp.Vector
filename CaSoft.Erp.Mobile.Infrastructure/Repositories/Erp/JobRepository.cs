using CaSoft.Beneficiary.Application;
using CaSoft.Erp.Mobile.Application;
using CaSoft.Erp.Mobile.Application.Dto;
using CaSoft.Erp.Mobile.Application.Port;
using CaSoft.Erp.Mobile.Domain;
using CaSoft.Orders.Application;

namespace CaSoft.Erp.Mobile.Infrastructure.Repositories.Erp;

/// <summary>
/// MOB-6 — Implémentation ERP-backed de <see cref="IJobRepository"/> pour le détail mission.
///
/// <para><b>GetJob</b> compose le détail à partir de l'ERP (in-process, lecture seule) :
/// <see cref="IMissionDetailQueryService"/> (mission + adresses pickup/dropoff résolues, MIS-2),
/// <see cref="IOrderQueryService"/> (mode/sens/fréquence/rdv de la commande) et
/// <see cref="IBeneficiaryQueryService"/> (identité patient). Le flag de présence de signature
/// (MOB-8) vient de la BD Mobile via <see cref="ISignatureRepository"/>.</para>
///
/// <para>La timeline opérationnelle (GetJobTime/SaveJobTime) reste purement BD Mobile : déléguée
/// au <see cref="IJobTimeRepository"/> réel (MOB-2/MOB-7), avec création paresseuse de la ligne.</para>
///
/// <para>Édition mission + facturation dynamique (Save/UpdateCommande/Invoicing) : différées MOB-13.</para>
/// </summary>
public class JobRepository : IJobRepository
{
    private readonly IMissionDetailQueryService _missionDetail;
    private readonly IOrderQueryService _orderQuery;
    private readonly IBeneficiaryQueryService _beneficiaryQuery;
    private readonly IJobTimeRepository _jobTimeRepository;
    private readonly ISignatureRepository _signatures;

    public JobRepository(
        IMissionDetailQueryService missionDetail,
        IOrderQueryService orderQuery,
        IBeneficiaryQueryService beneficiaryQuery,
        IJobTimeRepository jobTimeRepository,
        ISignatureRepository signatures)
    {
        _missionDetail = missionDetail;
        _orderQuery = orderQuery;
        _beneficiaryQuery = beneficiaryQuery;
        _jobTimeRepository = jobTimeRepository;
        _signatures = signatures;
    }

    public ClJob GetJob(Guid gJobId)
    {
        // Pont sync/async : le contrat legacy IJobRepository est synchrone.
        // Sûr hors SynchronizationContext (ASP.NET Core). Cohérent avec CrewRepository (MOB-5).
        var mission = _missionDetail.GetFullAsync(gJobId, CancellationToken.None).GetAwaiter().GetResult();
        if (mission is null)
            throw new InvalidOperationException($"Mission {gJobId} introuvable côté ERP.");

        // La commande porte mode de transport, aller/retour, fréquence (itératif), rdv.
        var order = _orderQuery.GetByIdAsync(mission.OrderId, CancellationToken.None).GetAwaiter().GetResult();

        // Identité patient (le bénéficiaire est rattaché à la commande).
        ClBeneficiaryDetailDtoOut? beneficiary = null;
        if (order?.Order is not null && order.Order.BeneficiaryId != Guid.Empty)
            beneficiary = _beneficiaryQuery.GetByIdAsync(order.Order.BeneficiaryId, CancellationToken.None)
                .GetAwaiter().GetResult();

        var domainMission = BuildMission(gJobId, mission, order);
        var domainBeneficiary = BuildBeneficiary(beneficiary);
        var timeData = GetJobTime(gJobId);

        return ClJob.GetBuilder()
            .WithId(gJobId)
            .WithMission(domainMission)
            .WithBeneficiary(domainBeneficiary)
            .WithTimeData(timeData)
            // Facturation dynamique différée (MOB-13) : contrat minimal, attributs vides.
            .WithContractType(new ClContractType())
            .WithPersistentSource()
            .Build();
    }

    public bool IsExist(Guid jobId)
        => _missionDetail.GetFullAsync(jobId, CancellationToken.None).GetAwaiter().GetResult() is not null;

    // ── Timeline opérationnelle : déléguée à la BD Mobile (MOB-2/MOB-7) ──────────
    // Création paresseuse : la ligne naît au premier geste de l'ambulancier
    // (ClUpdateTimeUseCase ne gère pas l'absence).
    public ClJobTimeData GetJobTime(Guid jobId)
        => _jobTimeRepository.GetJobTimeData(jobId)
           ?? ClJobTimeData.GetBuilder().WithId(jobId).Build();

    public void SaveJobTime(ClJobTimeData jobTime)
        => _jobTimeRepository.Save(jobTime.JobId, jobTime);

    // ── Édition mission + facturation dynamique : différées MOB-13 ───────────────
    public void Save(ClJob Job) => throw new NotImplementedException("MOB-13");
    public void UpdateCommande(ClUpdateCommandeDto CommandDto) => throw new NotImplementedException("MOB-13");
    public IInvoicingRepository Invoicing => throw new NotImplementedException("MOB-13");

    // ── Mapping ERP (DTO) → domaine mobile ───────────────────────────────────────

    private ClMission BuildMission(Guid jobId, ClMissionFullDtoOut mission, ClEditOrderDtoOut? order)
    {
        // Corps « order » de la réponse d'édition (champs mode/sens/fréquence/bénéficiaire).
        var body = order?.Order;

        var schedule = mission.MissionDate.ToDateTime(mission.SchedulingTime);
        DateTime? appointment = mission.AppointmentTime.HasValue
            ? mission.MissionDate.ToDateTime(mission.AppointmentTime.Value)
            : null;

        // Mode de transport : passthrough de l'id REF_TRANSPORT_MODE ERP (convention MOB-5).
        // Un code inconnu de l'enum legacy s'affiche « Autre » côté adaptateur.
        var transportMode = new ClTransportMode(
            (ModEnumeration.TransportModeEnumeration)(body?.TransportModeId ?? 0));

        // Sens : aller simple vs aller/retour (HasReturn de la commande).
        var transportType = new ClTransportType(
            body?.HasReturn == true
                ? ModEnumeration.TripType.OneWayAndReturn
                : ModEnumeration.TripType.OneWay);

        return new ClMission
        {
            MissionId = mission.Id,
            ContactId = body?.BeneficiaryId ?? Guid.Empty,
            // MOB-8 : présence d'une signature en BD Mobile.
            IsSign = _signatures.Exists(jobId),
            Schedule = schedule,
            Appointment = appointment,
            IsAsap = mission.IsAsap,
            MaxDelay = mission.DelayMaxInMinutes,
            CallTime = null,                  // pas d'équivalent ERP
            IsLastDay = false,                // notion régulation legacy, sans source ERP
            IsIterativ = body is not null && (int)body.Frequency != 0,
            TransportMode = transportMode,
            TransportType = transportType,
            Departure = ToAddressLines(mission.Pickup),
            Arrival = ToAddressLines(mission.Dropoff),
            Comments = string.Empty           // pas de champ commentaire dans le DTO commande V0
        };
    }

    private static ClJobBeneficiary BuildBeneficiary(ClBeneficiaryDetailDtoOut? dto)
    {
        var ben = new ClJobBeneficiary { Phones = new List<string>() };
        if (dto is null) return ben;

        ben.Id = dto.Id;
        ben.Name = dto.FirstName ?? string.Empty;       // prénom
        ben.LastName = dto.LastName ?? string.Empty;
        ben.NIR = dto.Nir;
        ben.DDN = dto.BirthDate?.ToDateTime(TimeOnly.MinValue);
        if (!string.IsNullOrWhiteSpace(dto.PrimaryPhone))
            ben.Phones.Add(dto.PrimaryPhone);

        return ben;
    }

    /// <summary>
    /// Stage ERP résolu → lignes d'adresse (paragraphe départ/arrivée).
    /// Si la jointure est orpheline (lieu source supprimé), repli sur le <c>Label</c> figé.
    /// </summary>
    private static List<string> ToAddressLines(ClStageDetailDtoOut? stage)
    {
        var lines = new List<string>();
        if (stage is null) return lines;

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) lines.Add(value.Trim());
        }

        Add(stage.LocationName);
        Add(stage.ServiceLabel);
        Add(stage.AddressLine1);
        Add(stage.AddressLine2);
        Add(stage.AddressLine3);
        var cityLine = string.Join(" ",
            new[] { stage.PostalCode, stage.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Add(cityLine);
        Add(stage.Complement);

        if (lines.Count == 0) Add(stage.Label);
        return lines;
    }
}
