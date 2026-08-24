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
            Origin = DeriveOrigin(context)
        };
    }

    /// <summary>
    /// Reconstitue la provenance, qu'Orders.Api <b>n'expose pas encore</b> (dette <c>Order OC-24</c>).
    /// <para>
    /// La déduction est exacte au regard du code d'Order d'aujourd'hui, où <c>locked</c> n'est pas
    /// une donnée mais un dérivé de la provenance (<c>Origin = Regulator</c> ⇒ <c>locked</c>) : un
    /// context verrouillé vient donc forcément de la régulation, et un context posé et non
    /// verrouillé vient forcément du terrain.
    /// </para>
    /// <para>
    /// ⚠️ Cette équivalence <b>tombera</b> le jour où Order distinguera verrou et provenance — c'est
    /// précisément l'objet d'<c>Order OC-24</c>, qui doit rendre possible « posé par la régulation,
    /// modifiable par le terrain ». Ce jour-là, cette méthode est à remplacer par la lecture du
    /// champ <c>origin</c> servi par l'API, sans rien changer au contrat mobile ci-dessus : c'est
    /// la raison d'être de cette indirection.
    /// </para>
    /// </summary>
    private static string? DeriveOrigin(ErpMissionContextOrderDto context)
    {
        if (context.Locked) return OriginRegulator;
        return context.ContextOrderId.HasValue ? OriginField : null;
    }
}
