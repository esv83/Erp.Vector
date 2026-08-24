using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// OC-3a — Adapte l'état du context servi par Orders.Api au contrat mobile. Lecture seule,
/// aucune donnée propre : tout vient de <see cref="IErpReadApiClient.GetMissionContextOrderAsync"/>.
/// </summary>
public sealed class ContextOrderStateQueryService : IContextOrderStateQueryService
{
    /// <summary>Valeur posée par la régulation.</summary>
    public const string OriginRegulator = "Regulator";

    /// <summary>Valeur choisie par le terrain.</summary>
    public const string OriginField = "Field";

    private readonly IErpReadApiClient _erp;

    public ContextOrderStateQueryService(IErpReadApiClient erp) => _erp = erp;

    public async Task<ClContextOrderStateDtoOut?> GetAsync(Guid missionId, CancellationToken ct)
    {
        var context = await _erp.GetMissionContextOrderAsync(missionId, ct);
        if (context is null) return null;

        return new ClContextOrderStateDtoOut
        {
            MissionId = missionId,
            Locked = context.Locked,
            ContextOrderId = context.ContextOrderId,
            ContextOrderCode = context.ContextOrderCode,
            ContextOrderDisplay = context.ContextOrderDisplay,
            Origin = ResolveOrigin(context)
        };
    }

    /// <summary>
    /// Provenance servie par Orders.Api depuis <c>Order OC-28</c>, avec repli sur la déduction
    /// lorsqu'une instance antérieure ne sert pas encore le champ.
    /// <para>
    /// Ce repli rend <b>l'ordre de déploiement indifférent</b> : Vector livré avant Order continue
    /// de fonctionner, et bascule tout seul sur la vraie provenance dès qu'Order la sert. La
    /// déduction reste exacte tant qu'Order dérive <c>locked</c> de la provenance : un context
    /// verrouillé vient alors forcément de la régulation, et un context posé non verrouillé
    /// forcément du terrain.
    /// </para>
    /// <para>
    /// ⚠️ Le repli est <b>volontairement muet sur le cas nominal d'OC-28</b> : « posé par la
    /// régulation mais modifiable » y ressort en <c>Field</c>, faute de pouvoir le distinguer. Ce
    /// n'est pas une approximation à conserver — c'est la raison pour laquelle le vrai champ prime
    /// dès qu'il existe. À retirer quand toutes les instances d'Orders.Api auront OC-28.
    /// </para>
    /// </summary>
    private static string? ResolveOrigin(ErpMissionContextOrderDto context)
    {
        if (!string.IsNullOrWhiteSpace(context.Origin)) return context.Origin;
        if (context.Locked) return OriginRegulator;
        return context.ContextOrderId.HasValue ? OriginField : null;
    }
}
