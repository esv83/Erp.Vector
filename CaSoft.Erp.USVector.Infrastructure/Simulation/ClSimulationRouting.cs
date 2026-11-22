using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using static CaSoft.Erp.USVector.Infrastructure.Simulation.ClSimulationIds;

namespace CaSoft.Erp.USVector.Infrastructure.Simulation;

// Les aiguillages du mode formation, posés devant chaque client d'Orders : un identifiant de la plage
// réservée va au faux Orders, tout autre au vrai. Le choix tient à l'IDENTIFIANT, jamais à la requête —
// c'est ce qui couvre aussi le worker de projection, et garantit qu'une mission fictive n'atteint jamais
// Orders, ni une vraie le catalogue. Enregistrés seulement si Simulation:Enabled (Program.cs).

/// <summary>Aiguillage des lectures vers Orders.</summary>
public sealed class ClSimulationRoutingErpReadApiClient : IErpReadApiClient
{
    private readonly IErpReadApiClient _real;
    private readonly IErpReadApiClient _simulated;

    public ClSimulationRoutingErpReadApiClient(IErpReadApiClient real, IErpReadApiClient simulated)
    {
        _real = real;
        _simulated = simulated;
    }

    private IErpReadApiClient For(Guid id) => IsSimulated(id) ? _simulated : _real;

    public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid missionId, CancellationToken ct = default) => For(missionId).GetMissionFullAsync(missionId, ct);
    public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default) => For(crewId).ListMissionsByCrewAsync(crewId, ct);
    public Task<ErpOrderEditDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default) => For(orderId).GetOrderAsync(orderId, ct);
    public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid beneficiaryId, CancellationToken ct = default) => For(beneficiaryId).GetBeneficiaryAsync(beneficiaryId, ct);
    public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid personnelId, DateOnly onDate, int take, CancellationToken ct = default) => For(personnelId).ListCrewIdsAsync(personnelId, onDate, take, ct);
    public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default) => For(crewId).GetCrewFullAsync(crewId, ct);
    public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid keycloakSub, CancellationToken ct = default) => For(keycloakSub).ResolvePersonnelIdByKeycloakAsync(keycloakSub, ct);
    public Task<int?> GetMissionTransferStatusAsync(Guid missionId, CancellationToken ct = default) => For(missionId).GetMissionTransferStatusAsync(missionId, ct);
    public Task<ErpMissionContextOrderDto?> GetMissionContextOrderAsync(Guid missionId, CancellationToken ct = default) => For(missionId).GetMissionContextOrderAsync(missionId, ct);
    public Task<IReadOnlyList<ErpContextOrderFieldDto>?> GetContextOrderFormStructureAsync(Guid missionId, CancellationToken ct = default) => For(missionId).GetContextOrderFormStructureAsync(missionId, ct);

    /// <summary>
    /// Un lot peut mêler les deux mondes (la facturation n'en enverra jamais, mais rien ne l'interdit) :
    /// chaque moitié part de son côté. Le « null » du vrai Orders (route absente, repli de l'appelant) est
    /// conservé tel quel quand le lot ne contient que de vraies missions.
    /// </summary>
    public async Task<IReadOnlyList<ErpMissionBatchRefDto>?> GetMissionBatchRefsAsync(IReadOnlyCollection<Guid> missionIds, CancellationToken ct = default)
    {
        var simulated = missionIds.Where(IsSimulated).ToList();
        if (simulated.Count == 0) return await _real.GetMissionBatchRefsAsync(missionIds, ct);

        var simulatedRefs = await _simulated.GetMissionBatchRefsAsync(simulated, ct) ?? Array.Empty<ErpMissionBatchRefDto>();
        var real = missionIds.Where(id => !IsSimulated(id)).ToList();
        if (real.Count == 0) return simulatedRefs;

        var realRefs = await _real.GetMissionBatchRefsAsync(real, ct);
        return realRefs is null ? null : realRefs.Concat(simulatedRefs).ToList();
    }
}

/// <summary>Aiguillage des écritures vers Orders (projections, conducteur, context, attributs).</summary>
public sealed class ClSimulationRoutingErpWriteApiClient : IErpWriteApiClient
{
    private readonly IErpWriteApiClient _real;
    private readonly IErpWriteApiClient _simulated;

    public ClSimulationRoutingErpWriteApiClient(IErpWriteApiClient real, IErpWriteApiClient simulated)
    {
        _real = real;
        _simulated = simulated;
    }

    private IErpWriteApiClient For(Guid id) => IsSimulated(id) ? _simulated : _real;

    public Task<EnOperationalProjectionOutcome> ProjectOperationalAsync(Guid missionId, DateTime? ackAt, DateTime? readAt, DateTime? goAt,
        DateTime? onsiteAt, DateTime? terminateAt, Guid? sourceCrewId, CancellationToken ct = default)
        => For(missionId).ProjectOperationalAsync(missionId, ackAt, readAt, goAt, onsiteAt, terminateAt, sourceCrewId, ct);

    public Task<EnOperationalProjectionOutcome> ProjectFieldAsync(Guid missionId, bool signatureExists, string? waiverCode, DateTime? waiverAtUtc, CancellationToken ct = default)
        => For(missionId).ProjectFieldAsync(missionId, signatureExists, waiverCode, waiverAtUtc, ct);

    public Task<CrewDriverWriteResult> SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default)
        => For(crewId).SetCrewDriverAsync(crewId, driverPersonnelId, from, ct);

    public Task<EnContextOrderWriteOutcome> SetMissionContextOrderAsync(Guid missionId, int contextOrderId, string? setBy = null, CancellationToken ct = default)
        => For(missionId).SetMissionContextOrderAsync(missionId, contextOrderId, setBy, ct);

    public Task<ContextOrderValuesWriteResult> SetContextOrderValuesAsync(Guid missionId,
        IReadOnlyCollection<(string Name, string? Value, string? OtherText)> values, string? setBy = null, CancellationToken ct = default)
        => For(missionId).SetContextOrderValuesAsync(missionId, values, setBy, ct);
}

/// <summary>
/// Confirmations de prise de service : un personnel fictif n'en a jamais à confirmer (rien n'est envoyé
/// par courriel en formation).
/// </summary>
public sealed class ClSimulationRoutingShiftConfirmationService : IShiftConfirmationService
{
    private readonly IShiftConfirmationService _real;

    public ClSimulationRoutingShiftConfirmationService(IShiftConfirmationService real) => _real = real;

    public Task<ClMyShiftConfirmationsDtoOut> GetMineAsync(Guid personnelId, CancellationToken ct)
        => IsSimulated(personnelId)
            ? Task.FromResult(new ClMyShiftConfirmationsDtoOut { HasPending = false })
            : _real.GetMineAsync(personnelId, ct);

    public Task<ClShiftConfirmationConfirmResult> ConfirmAsync(Guid crewId, Guid requestId, Guid personnelId, CancellationToken ct)
        => IsSimulated(personnelId) || IsSimulated(crewId)
            ? Task.FromResult(new ClShiftConfirmationConfirmResult
            {
                Outcome = EnShiftConfirmationConfirmOutcome.NotFound,
                Reason = "Aucune prise de service à confirmer en mode formation."
            })
            : _real.ConfirmAsync(crewId, requestId, personnelId, ct);
}

/// <summary>
/// Saisies de l'équipage sur le patient (identité, contact) : acceptées en formation, sans rien écrire —
/// la fiche d'un patient fictif n'existe nulle part.
/// </summary>
public sealed class ClSimulationRoutingBeneficiaryFieldEntryService : IBeneficiaryFieldEntryService
{
    private readonly IBeneficiaryFieldEntryService _real;

    public ClSimulationRoutingBeneficiaryFieldEntryService(IBeneficiaryFieldEntryService real) => _real = real;

    public Task<ClBeneficiaryEntryResult> RecordIdentityAsync(Guid missionId, ClBeneficiaryIdentityDtoIn identity, string setBy, CancellationToken ct)
        => IsSimulated(missionId) ? Task.FromResult(ClBeneficiaryEntryResult.Applied()) : _real.RecordIdentityAsync(missionId, identity, setBy, ct);

    public Task<ClBeneficiaryEntryResult> RecordContactAsync(Guid missionId, ClBeneficiaryContactDtoIn contact, string setBy, CancellationToken ct)
        => IsSimulated(missionId) ? Task.FromResult(ClBeneficiaryEntryResult.Applied()) : _real.RecordContactAsync(missionId, contact, setBy, ct);
}

/// <summary>
/// Le seul point où la REQUÊTE compte : un ambulancier qui demande le mode formation est rattaché à son
/// personnel fictif (<see cref="ClSimulationIds.TraineeFor"/>) ; un persona l'est déjà par son jeton. Tout
/// ce qui suit (équipages, missions, accès) découle de cet identifiant, par les aiguillages ci-dessus.
/// <para>
/// Posé DEVANT le cache d'identité : sinon la correspondance « compte → personnel fictif » d'une session
/// de formation y resterait, et l'ambulancier reverrait ses missions fictives une fois sorti du mode.
/// </para>
/// </summary>
public sealed class ClSimulationIdentityResolver : IMobileIdentityResolver
{
    private readonly IMobileIdentityResolver _inner;
    private readonly Func<bool> _isTrainingRequested;

    public ClSimulationIdentityResolver(IMobileIdentityResolver inner, Func<bool> isTrainingRequested)
    {
        _inner = inner;
        _isTrainingRequested = isTrainingRequested;
    }

    public Task<Guid?> ResolvePersonnelIdAsync(Guid keyCloakSub, CancellationToken ct)
    {
        if (IsSimulated(keyCloakSub)) return Task.FromResult<Guid?>(keyCloakSub);
        if (_isTrainingRequested()) return Task.FromResult<Guid?>(TraineeFor(keyCloakSub));
        return _inner.ResolvePersonnelIdAsync(keyCloakSub, ct);
    }

    public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
        => _inner.ResolveActiveCrewIdsAsync(personnelId, onDate, ct);

    public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsFreshAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
        => _inner.ResolveActiveCrewIdsFreshAsync(personnelId, onDate, ct);

    public Task<bool> IsMissionAccessibleAsync(Guid personnelId, Guid missionId, CancellationToken ct)
        => _inner.IsMissionAccessibleAsync(personnelId, missionId, ct);
}
