using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// Carte mutuelle — patient d'une mission, lu sur Orders.Api : mission → commande → bénéficiaire.
/// Même chemin que <see cref="FieldDataReader"/> pour rattacher la carte au paquet terrain, de
/// sorte qu'une carte capturée par mission ressorte bien dans ce paquet.
/// </summary>
public sealed class MissionBeneficiaryQueryService : IMissionBeneficiaryQueryService
{
    private readonly IErpReadApiClient _erp;

    public MissionBeneficiaryQueryService(IErpReadApiClient erp) => _erp = erp;

    public async Task<Guid?> GetBeneficiaryIdAsync(Guid missionId, CancellationToken ct)
    {
        var mission = await _erp.GetMissionFullAsync(missionId, ct);
        if (mission is null) return null;

        var order = await _erp.GetOrderAsync(mission.OrderId, ct);
        var beneficiaryId = order?.Order?.BeneficiaryId ?? Guid.Empty;
        return beneficiaryId == Guid.Empty ? null : beneficiaryId;
    }

    // Écrans de régulation et de certification (04/10) : une liste de missions, un seul appel. Orders
    // porte batch-refs depuis sa 1.8.5 — pas de repli ici : relancer 200 lectures unitaires pour
    // afficher une pastille ne se justifie pas, et une route absente est un défaut, pas une panne.
    public async Task<IReadOnlyDictionary<Guid, Guid?>> GetBeneficiaryIdsAsync(
        IReadOnlyCollection<Guid> missionIds, CancellationToken ct)
    {
        if (missionIds.Count == 0) return new Dictionary<Guid, Guid?>();

        var rows = await _erp.GetMissionBatchRefsAsync(missionIds, ct)
            ?? throw new InvalidOperationException("Orders ne porte pas POST /missions/batch-refs.");

        // Côté Orders, une commande sans patient porte un identifiant vide : ce n'est pas un patient.
        return rows.ToDictionary(
            r => r.MissionId,
            r => r.BeneficiaryId is { } id && id != Guid.Empty ? (Guid?)id : null);
    }
}
