using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using static CaSoft.Erp.USVector.Infrastructure.Simulation.ClSimulationIds;

namespace CaSoft.Erp.USVector.Infrastructure.Simulation;

/// <summary>
/// Le « faux Orders » du mode formation, en lecture. Tout se déduit de l'identifiant demandé
/// (<see cref="ClSimulationIds"/>) et du jeu du jour (<see cref="ClSimulationScenarios"/>), sauf ce que le
/// terrain a changé (<see cref="ClSimulationState"/>). Il ne répond QU'AUX identifiants de la plage
/// réservée : un autre identifiant est inconnu ici, comme une mission inconnue l'est chez Orders.
/// </summary>
public sealed class ClSimulatedOrdersReadClient : IErpReadApiClient
{
    private static readonly string[] PersonaNames = { "Camille ROUX", "Hugo FABRE", "Léa GIRAUD", "Nora VIDAL", "Théo GARCIA" };

    private readonly ClSimulationState _state;

    public ClSimulatedOrdersReadClient(ClSimulationState state) => _state = state;

    public static string PersonaName(int number)
        => number >= 1 && number <= PersonaNames.Length ? PersonaNames[number - 1] : $"Persona {number}";

    public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid keycloakSub, CancellationToken ct = default)
        => Task.FromResult<Guid?>(IsSimulated(keycloakSub) && KindOf(keycloakSub) == EnKind.Personnel ? keycloakSub : null);

    public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid personnelId, DateOnly onDate, int take, CancellationToken ct = default)
    {
        IReadOnlyList<Guid> crews = IsSimulated(personnelId) && KindOf(personnelId) is EnKind.Personnel or EnKind.Teammate
            ? new[] { Create(EnKind.Crew, OwnerOf(personnelId), onDate) }
            : Array.Empty<Guid>();
        return Task.FromResult(crews);
    }

    public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default)
    {
        if (!Is(crewId, EnKind.Crew)) return Task.FromResult<ErpCrewFullDto?>(null);

        var owner = OwnerOf(crewId);
        var day = DayOf(crewId).ToDateTime(TimeOnly.MinValue);
        var personnel = Create(EnKind.Personnel, owner);
        var (firstName, lastName) = SplitName(PersonaNumberOf(personnel) is { } n ? PersonaName(n) : "Stagiaire FORMATION");
        var driver = _state.CrewDriver(crewId);

        return Task.FromResult<ErpCrewFullDto?>(new ErpCrewFullDto
        {
            Id = crewId,
            ServiceStart = day.AddHours(7),
            ServiceEnd = day.AddHours(17),
            Status = 1,
            Vehicle = new ErpCrewVehicleDto { Id = Create(EnKind.Vehicle, owner), Immatriculation = "FORMATION" },
            ActiveDriver = driver is { } d ? new ErpCrewDriverDto { PersonnelId = d.PersonnelId, From = d.From } : null,
            Members = new List<ErpCrewMemberDto>
            {
                new() { Id = personnel, FirstName = firstName, LastName = lastName },
                new() { Id = Create(EnKind.Teammate, owner), FirstName = "Alex", LastName = "BLANC (FICTIF)" },
            }
        });
    }

    public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default)
    {
        if (!Is(crewId, EnKind.Crew)) return Task.FromResult<IReadOnlyList<ErpMissionListItemDto>>(Array.Empty<ErpMissionListItemDto>());

        var missions = ClSimulationScenarios.All
            .Select((scenario, slot) => (scenario, id: Create(EnKind.Mission, OwnerOf(crewId), DayOf(crewId), slot)))
            .Select(m => new ErpMissionListItemDto
            {
                Id = m.id,
                Status = StatusOf(m.id, m.scenario),
                AssignedCrewId = crewId,
                BeneficiaryDisplayName = m.scenario.Patient is { } p ? $"{p.LastName} {p.FirstName}" : null,
                TransportModeId = m.scenario.TransportModeId,
                Kind = m.scenario.Kind,
                MissionDate = DayOf(crewId),
                SchedulingTime = m.scenario.SchedulingTime,
                AppointmentTime = m.scenario.AppointmentTime,
                PickupLabel = m.scenario.Pickup.Label,
                DropoffLabel = m.scenario.Dropoff.Label,
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<ErpMissionListItemDto>>(missions);
    }

    public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid missionId, CancellationToken ct = default)
    {
        if (ScenarioOf(missionId, EnKind.Mission) is not { } scenario) return Task.FromResult<ErpMissionFullDto?>(null);

        return Task.FromResult<ErpMissionFullDto?>(new ErpMissionFullDto
        {
            Id = missionId,
            OrderId = Related(missionId, EnKind.Order),
            MissionDate = DayOf(missionId),
            SchedulingTime = scenario.SchedulingTime,
            AppointmentTime = scenario.AppointmentTime,
            IsAsap = scenario.IsAsap,
            DelayMaxInMinutes = scenario.DelayMaxInMinutes,
            AssignedCrewId = Create(EnKind.Crew, OwnerOf(missionId), DayOf(missionId)),
            Pickup = scenario.Pickup,
            Dropoff = scenario.Dropoff,
            Comment = scenario.Comment,
            BeneficiaryIdentification = scenario.Identification,
        });
    }

    public Task<ErpOrderEditDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        if (ScenarioOf(orderId, EnKind.Order) is not { } scenario) return Task.FromResult<ErpOrderEditDto?>(null);

        return Task.FromResult<ErpOrderEditDto?>(new ErpOrderEditDto
        {
            Order = new ErpOrderBodyDto
            {
                BeneficiaryId = BeneficiaryOf(orderId, scenario) ?? Guid.Empty,
                TransportModeId = scenario.TransportModeId,
                TransportSubCategoryId = scenario.TransportSubCategoryId,
                TransportSubCategoryLabel = scenario.TransportSubCategoryLabel,
                HasReturn = scenario.Kind == ClSimulationScenarios.Aller,
                Frequency = 0,
            }
        });
    }

    public Task<IReadOnlyList<ErpMissionBatchRefDto>?> GetMissionBatchRefsAsync(
        IReadOnlyCollection<Guid> missionIds, CancellationToken ct = default)
    {
        IReadOnlyList<ErpMissionBatchRefDto> refs = missionIds
            .Where(id => ScenarioOf(id, EnKind.Mission) is not null)
            .Select(id => new ErpMissionBatchRefDto
            {
                MissionId = id,
                OrderId = Related(id, EnKind.Order),
                BeneficiaryId = BeneficiaryOf(id, ScenarioOf(id, EnKind.Mission)!),
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<ErpMissionBatchRefDto>?>(refs);
    }

    public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid beneficiaryId, CancellationToken ct = default)
    {
        if (ScenarioOf(beneficiaryId, EnKind.Beneficiary) is not { Patient: { } patient }) return Task.FromResult<ErpBeneficiaryDetailDto?>(null);

        return Task.FromResult<ErpBeneficiaryDetailDto?>(new ErpBeneficiaryDetailDto
        {
            Id = beneficiaryId,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Nir = patient.Nir,
            BirthDate = patient.BirthDate,
            PrimaryPhone = patient.Phone,
        });
    }

    public Task<int?> GetMissionTransferStatusAsync(Guid missionId, CancellationToken ct = default)
        => Task.FromResult(ScenarioOf(missionId, EnKind.Mission)?.TransferStatus);

    public Task<ErpMissionContextOrderDto?> GetMissionContextOrderAsync(Guid missionId, CancellationToken ct = default)
    {
        if (ScenarioOf(missionId, EnKind.Mission) is not { } scenario) return Task.FromResult<ErpMissionContextOrderDto?>(null);

        var contextId = scenario.ImposedContextOrderId ?? _state.ContextOrder(missionId);
        var chosen = ClSimulationScenarios.ContextOrders.FirstOrDefault(c => c.Id == contextId);

        return Task.FromResult<ErpMissionContextOrderDto?>(new ErpMissionContextOrderDto
        {
            MissionId = missionId,
            OrderId = Related(missionId, EnKind.Order),
            ContextOrderId = chosen?.Id,
            ContextOrderCode = chosen?.Code,
            ContextOrderDisplay = chosen?.Display,
            Locked = scenario.ImposedContextOrderId is not null,
            Origin = chosen is null ? null : scenario.ImposedContextOrderId is not null ? "Regulator" : "Field",
            AvailableContextOrders = ClSimulationScenarios.ContextOrders.ToList(),
        });
    }

    public Task<IReadOnlyList<ErpContextOrderFieldDto>?> GetContextOrderFormStructureAsync(Guid missionId, CancellationToken ct = default)
    {
        if (ScenarioOf(missionId, EnKind.Mission) is not { } scenario) return Task.FromResult<IReadOnlyList<ErpContextOrderFieldDto>?>(null);

        IReadOnlyList<ErpContextOrderFieldDto> fields = FormFor(scenario)
            .Select(field =>
            {
                if (_state.Value(missionId, field.Name!) is { } saved && !field.IsReadOnly)
                {
                    field.Value = saved.Value;
                    field.OtherText = saved.OtherText;
                }
                return field;
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<ErpContextOrderFieldDto>?>(fields);
    }

    /// <summary>Le formulaire d'une mission, valeurs d'origine comprises : un exemplaire de chaque type de contrôle.</summary>
    internal static List<ErpContextOrderFieldDto> FormFor(ClSimulationScenario scenario) => new()
    {
        new() { Name = "Motif", Label = "Motif du transport", Index = 1, Type = "list", Required = true,
            Options = new() { new() { Key = 1, Label = "Consultation" }, new() { Key = 2, Label = "Hospitalisation" },
                              new() { Key = 3, Label = "Autre", TextRequired = true } } },
        new() { Name = "DateNaissance", Label = "Date de naissance", Index = 2, Type = "date",
            Value = scenario.Patient?.BirthDate.ToString("yyyy-MM-dd"),
            IsReadOnly = scenario.IsBirthDateLocked, ReadOnlyReason = scenario.IsBirthDateLocked ? "Vérifiée par la régulation" : null },
        new() { Name = "Telephone", Label = "Téléphones de contact", Index = 3, Type = "phone", IsMulti = true, PlaceHolder = "06 00 00 00 00" },
        new() { Name = "Email", Label = "Courriel", Index = 4, Type = "email" },
        new() { Name = "Poids", Label = "Poids (kg)", Index = 5, Type = "number" },
        new() { Name = "Oxygene", Label = "Sous oxygène", Index = 6, Type = "checkbox", InstantUpdate = true },
        new() { Name = "Accompagnant", Label = "Nom de l'accompagnant", Index = 7, Type = "text" },
        new() { Name = "Observations", Label = "Observations", Index = 8, Type = "textarea", PlaceHolder = "Précisions utiles à la régulation" },
    };

    internal int StatusOf(Guid missionId, ClSimulationScenario scenario)
        => Math.Max(scenario.Status, _state.MissionStatus(missionId) ?? 0);

    internal static ClSimulationScenario? ScenarioOf(Guid id, EnKind kind)
        => Is(id, kind) ? ClSimulationScenarios.At(SlotOf(id)) : null;

    private static Guid? BeneficiaryOf(Guid id, ClSimulationScenario scenario)
        => scenario.Patient is null ? null : Related(id, EnKind.Beneficiary);

    private static bool Is(Guid id, EnKind kind) => IsSimulated(id) && KindOf(id) == kind;

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var space = fullName.IndexOf(' ');
        return space < 0 ? (fullName, string.Empty) : (fullName[..space], fullName[(space + 1)..]);
    }
}
