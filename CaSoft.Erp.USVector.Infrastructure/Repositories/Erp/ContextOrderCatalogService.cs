using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// OC-3b — Sert <c>GET api/Contract/{jobId}</c> depuis le catalogue Order.
///
/// <para><b>La forme ne change pas, la source oui.</b> Le mobile reçoit toujours un tableau
/// <c>{ Id, Display, IsSelected, Locked }</c> ; ce sont les <c>availableContextOrders</c> d'Order
/// qui le remplissent, déjà filtrés par l'agence et le mode de la commande. Vector <b>ne refait pas
/// ce filtrage</b> : le tri d'applicabilité appartient à la source.</para>
///
/// <para><b>Un seul appel réseau</b> là où l'ancien chemin en faisait deux : la même réponse porte
/// la liste, le type effectif et le verrou. La lecture séparée de <c>/state</c> reste servie pour
/// qui veut l'état seul, avec la provenance en plus.</para>
///
/// <para><b>Plus de défaut automatique.</b> Quand la mission n'a pas de type posé, aucun item n'est
/// marqué : « non renseigné » devient un état valide. L'ancienne règle — pré-sélectionner le
/// premier type actif — faisait passer un défaut technique pour un choix, et c'est ce faux choix
/// qui partait en facturation.</para>
/// </summary>
public sealed class ContextOrderCatalogService : IContextOrderCatalogService
{
    private readonly IErpReadApiClient _erp;
    private readonly ILogger<ContextOrderCatalogService> _logger;

    public ContextOrderCatalogService(IErpReadApiClient erp, ILogger<ContextOrderCatalogService> logger)
    {
        _erp = erp;
        _logger = logger;
    }

    public async Task<List<ClContractChoiceDto>> GetChoicesAsync(Guid missionId, CancellationToken ct)
    {
        ErpMissionContextOrderDto? context;
        try
        {
            context = await _erp.GetMissionContextOrderAsync(missionId, ct);
        }
        catch (Exception ex)
        {
            // Avant la bascule, la liste venait de la BD Mobile et survivait à une panne de l'ERP ;
            // elle n'a plus cette autonomie, et lui rendre son ancien contenu serait pire que rien :
            // les ids Vector qu'elle porte seraient relus comme des ids Order au POST suivant — 4
            // vaut ART80 ici, CENTRE15 là-bas. On rend donc une liste vide plutôt qu'une liste
            // ambiguë : l'ambulancier ne choisit rien au lieu de choisir un type pour un autre.
            _logger.LogWarning(ex,
                "OC-3b : catalogue des types indisponible pour la mission {MissionId}, liste vide servie.",
                missionId);
            return new List<ClContractChoiceDto>(0);
        }

        // Mission inconnue d'Order : rien de sélectionnable, et le POST serait refusé en 404.
        if (context is null) return new List<ClContractChoiceDto>(0);

        return context.AvailableContextOrders
            .OrderBy(c => c.Index)
            .Select(c => new ClContractChoiceDto
            {
                Id = c.Id,
                Display = c.Display,
                // Aucun repli sur le premier item : sans type posé, rien n'est sélectionné.
                IsSelected = context.ContextOrderId.HasValue && c.Id == context.ContextOrderId.Value,
                // Le verrou porte sur la mission, pas sur le type : même valeur sur tous les items,
                // pour que le front grise la liste sans second appel.
                Locked = context.Locked
            })
            .ToList();
    }
}
