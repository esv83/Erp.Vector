using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Dto;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// MOB-6 — Implémentation de <see cref="IJobRepository"/> pour le détail mission.
///
/// <para>Données de référence ERP lues via <see cref="IErpReadApiClient"/> (Orders.Api en HTTP,
/// découplage 4a) : détail mission, commande (mode/sens/fréquence), identité bénéficiaire.
/// Le flag de présence de signature (MOB-8) vient de la BD Mobile via <see cref="ISignatureRepository"/>.</para>
///
/// <para>La timeline opérationnelle (GetJobTime/SaveJobTime) reste BD Mobile (<see cref="IJobTimeRepository"/>).
/// Les attributs dynamiques (MOB-13) sont gérés par <see cref="IJobAttributeOverlay"/>.</para>
///
/// <para>Pont sync/async : le contrat legacy IJobRepository est synchrone. Sûr hors
/// SynchronizationContext (ASP.NET Core).</para>
/// </summary>
public class JobRepository : IJobRepository
{
    private readonly IErpReadApiClient _erp;
    private readonly IJobTimeRepository _jobTimeRepository;
    private readonly ISignatureRepository _signatures;
    private readonly IJobAttributeOverlay _overlay;

    public JobRepository(
        IErpReadApiClient erp,
        IJobTimeRepository jobTimeRepository,
        ISignatureRepository signatures,
        IJobAttributeOverlay overlay)
    {
        _erp = erp;
        _jobTimeRepository = jobTimeRepository;
        _signatures = signatures;
        _overlay = overlay;
    }

    public ClJob GetJob(Guid gJobId)
    {
        var mission = _erp.GetMissionFullAsync(gJobId, CancellationToken.None).GetAwaiter().GetResult();
        if (mission is null)
            throw new InvalidOperationException($"Mission {gJobId} introuvable côté ERP.");

        // La commande porte mode de transport, aller/retour, fréquence (itératif).
        var order = _erp.GetOrderAsync(mission.OrderId, CancellationToken.None).GetAwaiter().GetResult();

        // Identité patient (le bénéficiaire est rattaché à la commande).
        ErpBeneficiaryDetailDto? beneficiary = null;
        if (order?.Order is not null && order.Order.BeneficiaryId != Guid.Empty)
            beneficiary = _erp.GetBeneficiaryAsync(order.Order.BeneficiaryId, CancellationToken.None)
                .GetAwaiter().GetResult();

        var domainMission = BuildMission(gJobId, mission, order);
        var domainBeneficiary = BuildBeneficiary(beneficiary);
        var timeData = GetJobTime(gJobId);

        return ClJob.GetBuilder()
            .WithId(gJobId)
            .WithMission(domainMission)
            .WithBeneficiary(domainBeneficiary)
            .WithTimeData(timeData)
            // MOB-13 : attributs dynamiques (overlay BD Mobile). Coordonnées ERP = baseline verrouillée.
            .WithContractType(_overlay.BuildContractType(gJobId, BuildBaselines(domainBeneficiary)))
            .WithPersistentSource()
            .Build();
    }

    /// <summary>
    /// Baseline ERP (verrouillée) des attributs multi-valués : on peut ajouter des
    /// téléphones/e-mails côté mobile, jamais modifier ceux déjà présents dans l'ERP.
    /// </summary>
    private static IDictionary<string, IEnumerable<string>> BuildBaselines(ClJobBeneficiary beneficiary)
        => new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PHONES"] = beneficiary.Phones ?? new List<string>(),
            ["MAILS"] = beneficiary.Emails ?? new List<string>(),
        };

    public bool IsExist(Guid jobId)
        => _erp.GetMissionFullAsync(jobId, CancellationToken.None).GetAwaiter().GetResult() is not null;

    // ── Timeline opérationnelle : déléguée à la BD Mobile (MOB-2/MOB-7) ──────────
    public ClJobTimeData GetJobTime(Guid jobId)
        => _jobTimeRepository.GetJobTimeData(jobId)
           ?? ClJobTimeData.GetBuilder().WithId(jobId).Build();

    public void SaveJobTime(ClJobTimeData jobTime)
        => _jobTimeRepository.Save(jobTime.JobId, jobTime);

    // ── Édition des attributs : overlay BD Mobile (MOB-13) ───────────────────────
    public void Save(ClJob Job)
        => _overlay.Save(Job.Id, Job.ContractType, BuildBaselines(Job.Beneficiary));

    // Écriture commande ERP + sélection de contrat (liste) : hors ossature (MOB-13.8).
    public void UpdateCommande(ClUpdateCommandeDto CommandDto) => throw new NotImplementedException("MOB-13.8");
    public IInvoicingRepository Invoicing => throw new NotImplementedException("MOB-13.8");

    // ── Mapping ERP (DTO HTTP) → domaine mobile ──────────────────────────────────

    private ClMission BuildMission(Guid jobId, ErpMissionFullDto mission, ErpOrderEditDto? order)
    {
        var body = order?.Order;

        var schedule = mission.MissionDate.ToDateTime(mission.SchedulingTime);
        DateTime? appointment = mission.AppointmentTime.HasValue
            ? mission.MissionDate.ToDateTime(mission.AppointmentTime.Value)
            : null;

        // Mode de transport : passthrough de l'id REF_TRANSPORT_MODE ERP (convention MOB-5).
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
            IsIterativ = body is not null && body.Frequency != 0,
            TransportMode = transportMode,
            TransportType = transportType,
            // Libellés ERP : principal (id→label, l'order n'expose que l'id) + secondaire.
            TransportModeLabel = MapTransportModeLabel(body?.TransportModeId),
            TransportSubCategoryLabel = body?.TransportSubCategoryLabel ?? string.Empty,
            Departure = ToAddressLines(mission.Pickup),   // compat : version paragraphe
            Arrival = ToAddressLines(mission.Dropoff),
            PickupLocation = ToJobLocation(mission.Pickup),
            DropoffLocation = ToJobLocation(mission.Dropoff),
            Comments = mission.Comment ?? string.Empty
        };
    }

    /// <summary>
    /// Libellé du mode de transport principal à partir de l'id ERP (l'endpoint order ne renvoie
    /// que l'id ; libellés capturés du référentiel Orders.Api). À compléter si de nouveaux modes.
    /// </summary>
    private static string MapTransportModeLabel(int? modeId) => modeId switch
    {
        1 => "AMBULANCE",
        2 => "VSL",
        4 => "Produits Sanguins",
        _ => string.Empty
    };

    /// <summary>
    /// Stage ERP → lieu détaillé structuré (mapping « au mieux » des champs disponibles).
    /// Lieu non référencé (structuré vide) : repli du Label figé dans Nom.
    /// </summary>
    private static ClJobLocation ToJobLocation(ErpStageDto? stage)
    {
        var loc = new ClJobLocation();
        if (stage is null) return loc;

        static string S(string? v) => v?.Trim() ?? string.Empty;

        loc.Nom = S(stage.LocationName);
        // DET-1 : le service médical (ServiceLabel, ex. « Cardiologie » — établissement de santé / FreeText,
        // jamais une adresse bénéficiaire) a désormais un champ dédié, affiché après Nom. BatEtage ne porte
        // plus que la ligne 3. Contrat UI basculé (cf. note_ui_alex.md) : l'app doit rendre la ligne Service.
        loc.Service = S(stage.ServiceLabel);
        loc.Adresse = S(stage.AddressLine1);
        loc.Residence = S(stage.AddressLine2);
        loc.BatEtage = S(stage.AddressLine3);
        loc.Commune = string.Join(" ",
            new[] { stage.PostalCode, stage.City }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        loc.Complement = S(stage.Complement);

        // Aucun champ structuré → repli sur le label figé.
        var hasStructured = new[] { loc.Nom, loc.Service, loc.Adresse, loc.Residence, loc.BatEtage, loc.Commune, loc.Complement }
            .Any(v => !string.IsNullOrWhiteSpace(v));
        if (!hasStructured) loc.Nom = S(stage.Label);

        return loc;
    }

    private static ClJobBeneficiary BuildBeneficiary(ErpBeneficiaryDetailDto? dto)
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
    private static List<string> ToAddressLines(ErpStageDto? stage)
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
