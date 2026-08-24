using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// OC-4 — Relaie la sélection de l'ambulancier vers <c>PATCH /missions/{id}/contextOrder</c>.
///
/// <para><b>Le point délicat est la correspondance des identifiants.</b> Tant qu'OC-3b n'a pas
/// basculé la source de la liste, le mobile reçoit des ids du catalogue <b>Vector</b>
/// (<c>MOB_CONTRACT_TYPE</c>) et l'ERP attend des ids du catalogue <b>Order</b>
/// (<c>ORD_ORDER_CONTEXT</c>). Les deux ne coïncident pas — et pas seulement en théorie :
/// <c>4</c> vaut <c>ART80</c> côté Vector et <c>CENTRE15</c> côté Order. Relayer l'entier tel quel
/// écrirait « Centre 15 » là où l'ambulancier a coché « Article 80 ».</para>
///
/// <para>La traduction se fait donc <b>par code</b>, et l'identifiant Order est repris de la réponse
/// d'Orders.Api plutôt que déduit : on cherche le code Vector dans
/// <c>availableContextOrders</c>, déjà filtré par l'agence et le mode de la commande. Un type
/// absent de cette liste est refusé sans appel réseau supplémentaire — et le jour où le catalogue
/// Order bouge, rien à resynchroniser ici.</para>
///
/// <para>⚠️ <b>Composant de transition.</b> Il disparaît avec OC-3b : quand la liste viendra d'Order,
/// l'id reçu sera déjà le bon et le relais deviendra direct.</para>
/// </summary>
public sealed class ContextOrderSelectionService : IContextOrderSelectionService
{
    private readonly MobileDbContext _ctx;
    private readonly IErpReadApiClient _read;
    private readonly IErpWriteApiClient _write;

    public ContextOrderSelectionService(MobileDbContext ctx, IErpReadApiClient read, IErpWriteApiClient write)
    {
        _ctx = ctx;
        _read = read;
        _write = write;
    }

    public async Task<EnContextOrderSelectionOutcome> SelectAsync(
        Guid missionId, int contractTypeId, string? setBy, CancellationToken ct)
    {
        var code = await _ctx.ContractTypes.AsNoTracking()
            .Where(t => t.CTT_ID == contractTypeId)
            .Select(t => t.CTT_CODE)
            .FirstOrDefaultAsync(ct);

        // Type inconnu du catalogue Vector : la requête ne correspond à rien de sélectionnable.
        if (string.IsNullOrWhiteSpace(code)) return EnContextOrderSelectionOutcome.NotApplicable;

        var context = await _read.GetMissionContextOrderAsync(missionId, ct);
        if (context is null) return EnContextOrderSelectionOutcome.MissionNotFound;

        var match = context.AvailableContextOrders
            .FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

        // Pas d'équivalent côté Order : soit le type n'est pas proposé pour cette commande, soit le
        // code n'existe pas du tout dans le catalogue Order (cas de STANDARD, sans correspondance
        // — arbitrage métier ouvert, cf. devplan §3.1 OC-4). Dans les deux cas, refus explicite
        // plutôt qu'un type approchant écrit à la place de celui qu'on a coché.
        if (match is null) return EnContextOrderSelectionOutcome.NotApplicable;

        var outcome = await _write.SetMissionContextOrderAsync(missionId, match.Id, setBy, ct);

        return outcome switch
        {
            EnContextOrderWriteOutcome.Applied => EnContextOrderSelectionOutcome.Applied,
            EnContextOrderWriteOutcome.LockedByRegulator => EnContextOrderSelectionOutcome.LockedByRegulator,
            EnContextOrderWriteOutcome.NotApplicable => EnContextOrderSelectionOutcome.NotApplicable,
            EnContextOrderWriteOutcome.MissionNotFound => EnContextOrderSelectionOutcome.MissionNotFound,
            _ => EnContextOrderSelectionOutcome.NotApplicable
        };
    }
}
