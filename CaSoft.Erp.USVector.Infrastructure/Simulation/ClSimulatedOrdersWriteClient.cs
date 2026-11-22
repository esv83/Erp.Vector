using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using static CaSoft.Erp.USVector.Infrastructure.Simulation.ClSimulationIds;

namespace CaSoft.Erp.USVector.Infrastructure.Simulation;

/// <summary>
/// Le « faux Orders » du mode formation, en écriture : il applique au jeu du jour ce que le terrain envoie,
/// avec les mêmes refus qu'Orders (context imposé, champ verrouillé, conducteur hors équipage), pour que
/// l'app les rencontre en formation avant de les rencontrer en vrai. Rien n'en sort vers Orders.
/// </summary>
public sealed class ClSimulatedOrdersWriteClient : IErpWriteApiClient
{
    private readonly ClSimulationState _state;
    private readonly ClSimulatedOrdersReadClient _read;

    public ClSimulatedOrdersWriteClient(ClSimulationState state)
    {
        _state = state;
        _read = new ClSimulatedOrdersReadClient(state);
    }

    public Task<EnOperationalProjectionOutcome> ProjectOperationalAsync(
        Guid missionId, DateTime? ackAt, DateTime? readAt, DateTime? goAt, DateTime? onsiteAt, DateTime? terminateAt,
        Guid? sourceCrewId, CancellationToken ct = default)
    {
        if (ClSimulatedOrdersReadClient.ScenarioOf(missionId, EnKind.Mission) is null)
            return Task.FromResult(EnOperationalProjectionOutcome.MissionNotFound);

        var status = terminateAt.HasValue ? ClSimulationScenarios.Done
            : goAt.HasValue || onsiteAt.HasValue ? ClSimulationScenarios.InProgress
            : ClSimulationScenarios.ToDo;
        _state.AdvanceMissionStatus(missionId, status);
        return Task.FromResult(EnOperationalProjectionOutcome.Applied);
    }

    public Task<EnOperationalProjectionOutcome> ProjectFieldAsync(
        Guid missionId, bool signatureExists, string? waiverCode, DateTime? waiverAtUtc, CancellationToken ct = default)
        => Task.FromResult(ClSimulatedOrdersReadClient.ScenarioOf(missionId, EnKind.Mission) is null
            ? EnOperationalProjectionOutcome.MissionNotFound
            : EnOperationalProjectionOutcome.Applied);

    public async Task<CrewDriverWriteResult> SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default)
    {
        var crew = await _read.GetCrewFullAsync(crewId, ct);
        if (crew is null) return new CrewDriverWriteResult(EnCrewDriverWriteOutcome.CrewNotFound);
        if (crew.Members.All(m => m.Id != driverPersonnelId))
            return new CrewDriverWriteResult(EnCrewDriverWriteOutcome.Refused, "Le conducteur désigné n'est pas membre de l'équipage.");

        _state.SetCrewDriver(crewId, driverPersonnelId, from);
        return new CrewDriverWriteResult(EnCrewDriverWriteOutcome.Applied);
    }

    public Task<EnContextOrderWriteOutcome> SetMissionContextOrderAsync(
        Guid missionId, int contextOrderId, string? setBy = null, CancellationToken ct = default)
    {
        if (ClSimulatedOrdersReadClient.ScenarioOf(missionId, EnKind.Mission) is not { } scenario)
            return Task.FromResult(EnContextOrderWriteOutcome.MissionNotFound);
        if (scenario.ImposedContextOrderId is not null)
            return Task.FromResult(EnContextOrderWriteOutcome.LockedByRegulator);
        if (ClSimulationScenarios.ContextOrders.All(c => c.Id != contextOrderId))
            return Task.FromResult(EnContextOrderWriteOutcome.NotApplicable);

        _state.SetContextOrder(missionId, contextOrderId);
        return Task.FromResult(EnContextOrderWriteOutcome.Applied);
    }

    public Task<ContextOrderValuesWriteResult> SetContextOrderValuesAsync(
        Guid missionId, IReadOnlyCollection<(string Name, string? Value, string? OtherText)> values,
        string? setBy = null, CancellationToken ct = default)
    {
        if (ClSimulatedOrdersReadClient.ScenarioOf(missionId, EnKind.Mission) is not { } scenario)
            return Task.FromResult(new ContextOrderValuesWriteResult(EnContextOrderValuesWriteOutcome.MissionNotFound));

        var form = ClSimulatedOrdersReadClient.FormFor(scenario).ToDictionary(f => f.Name!, StringComparer.OrdinalIgnoreCase);

        // Comme Order : tout ou rien. Reposer la valeur d'un champ verrouillé est sans effet ; la changer est refusé.
        foreach (var (name, value, _) in values)
        {
            if (!form.TryGetValue(name, out var field))
                return Task.FromResult(new ContextOrderValuesWriteResult(EnContextOrderValuesWriteOutcome.Invalid, $"Champ inconnu : {name}."));
            if (field.IsReadOnly && !string.Equals(field.Value, value, StringComparison.Ordinal))
                return Task.FromResult(new ContextOrderValuesWriteResult(EnContextOrderValuesWriteOutcome.FieldLocked, $"{field.Label} : {field.ReadOnlyReason}."));
        }

        foreach (var (name, value, otherText) in values.Where(v => !form[v.Name].IsReadOnly))
            _state.SetValue(missionId, name, value, otherText);
        return Task.FromResult(new ContextOrderValuesWriteResult(EnContextOrderValuesWriteOutcome.Applied));
    }
}
