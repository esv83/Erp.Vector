using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// Relaie le contexte choisi par l'ambulancier vers <c>PATCH /missions/{id}/contextOrder</c>.
///
/// <para><b>L'identifiant reçu est vérifié avant d'être écrit.</b> Il doit figurer parmi les
/// contextes qu'on vient de proposer pour cette mission — la liste qu'Orders.Api sert déjà filtrée
/// par l'agence et le mode de la commande. Un identifiant étranger à cette liste est refusé sans
/// écriture.</para>
///
/// <para>Ce contrôle n'est pas une redite de celui d'Order : il arrête aussi le client resté sur une
/// liste périmée. Les deux catalogues n'ont jamais partagé leurs identifiants — <c>4</c> désignait
/// « Article 80 » côté Vector et désigne « Centre 15 » côté Order — et un tel appel écrirait un type
/// pour un autre, sans que rien ne le signale jusqu'à la facturation.</para>
///
/// <para>Les refus métier reviennent en <see cref="EnContextOrderSelectionOutcome"/>, jamais en
/// exception : seule une panne réelle lève.</para>
/// </summary>
public sealed class ContextOrderSelectionService : IContextOrderSelectionService
{
    private readonly IErpReadApiClient _read;
    private readonly IErpWriteApiClient _write;

    public ContextOrderSelectionService(IErpReadApiClient read, IErpWriteApiClient write)
    {
        _read = read;
        _write = write;
    }

    public async Task<EnContextOrderSelectionOutcome> SelectAsync(
        Guid missionId, int contextOrderId, string? setBy, CancellationToken ct)
    {
        var context = await _read.GetMissionContextOrderAsync(missionId, ct);
        if (context is null) return EnContextOrderSelectionOutcome.MissionNotFound;

        var match = context.AvailableContextOrders.FirstOrDefault(c => c.Id == contextOrderId);

        // Refus explicite plutôt qu'un type approchant écrit à la place de celui qu'on a coché : un
        // mauvais type part en facturation sans que personne ne le voie passer.
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
