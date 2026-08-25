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
/// <para><b>OC-3b — une fois la bascule armée, la traduction n'a plus lieu d'être</b> : la liste
/// servie vient d'Order, donc l'id reçu est déjà le bon. Le service reste malgré tout le passage
/// obligé, et il en profite pour <b>vérifier que l'id figure bien dans les types proposés pour cette
/// mission</b>. Ce contrôle n'est pas décoratif : c'est le filet qui rattrape un client resté sur
/// l'ancienne liste au moment où le drapeau s'arme — il posterait un id Vector là où l'on attend
/// désormais un id Order.</para>
/// </summary>
public sealed class ContextOrderSelectionService : IContextOrderSelectionService
{
    private readonly MobileDbContext _ctx;
    private readonly IErpReadApiClient _read;
    private readonly IErpWriteApiClient _write;
    private readonly ContextOrderOptions _options;

    public ContextOrderSelectionService(
        MobileDbContext ctx, IErpReadApiClient read, IErpWriteApiClient write, ContextOrderOptions? options = null)
    {
        _ctx = ctx;
        _read = read;
        _write = write;
        _options = options ?? new ContextOrderOptions();
    }

    public async Task<EnContextOrderSelectionOutcome> SelectAsync(
        Guid missionId, int contractTypeId, string? setBy, CancellationToken ct)
    {
        // Avant la bascule, l'id vient du catalogue Vector : il faut le traduire, et par le CODE.
        // Après, il vient déjà d'Order : le traduire une seconde fois serait faux.
        string? targetCode = null;
        if (!_options.UseOrderCatalog)
        {
            var code = await _ctx.ContractTypes.AsNoTracking()
                .Where(t => t.CTT_ID == contractTypeId)
                .Select(t => t.CTT_CODE)
                .FirstOrDefaultAsync(ct);

            // Type inconnu du catalogue Vector : la requête ne correspond à rien de sélectionnable.
            if (string.IsNullOrWhiteSpace(code)) return EnContextOrderSelectionOutcome.NotApplicable;

            targetCode = ContextOrderCodeAliases.ToOrder(code);
        }

        var context = await _read.GetMissionContextOrderAsync(missionId, ct);
        if (context is null) return EnContextOrderSelectionOutcome.MissionNotFound;

        var match = targetCode is null
            // Bascule armée : l'id doit être l'un de ceux qu'on vient de proposer. Un id étranger à
            // cette liste est refusé sans écriture — c'est aussi ce qui arrête un client qui aurait
            // gardé l'ancienne liste en cache et posterait « 4 » pour ART80, quand 4 vaut CENTRE15.
            ? context.AvailableContextOrders.FirstOrDefault(c => c.Id == contractTypeId)
            : context.AvailableContextOrders
                .FirstOrDefault(c => string.Equals(c.Code, targetCode, StringComparison.OrdinalIgnoreCase));

        // Refus explicite plutôt qu'un type approchant écrit à la place de celui qu'on a coché : un
        // mauvais type part en facturation sans que personne ne le voie passer. Ce chemin reste
        // atteignable même avec les alias — le type peut être hors agence/mode de la commande.
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
