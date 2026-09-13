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
}
