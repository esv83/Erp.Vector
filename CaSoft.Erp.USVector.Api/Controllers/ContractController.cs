using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// Sélection du contexte d'une mission — le « type de mission » côté terrain. Le contexte choisi
    /// détermine le jeu d'attributs que sert <c>FormStructure</c>.
    /// <para>
    /// La source est Order, et elle seule : la liste vient du catalogue de la régulation, déjà
    /// restreinte à l'agence et au mode de la commande, et le choix de l'ambulancier y retourne. La
    /// forme des réponses reste celle d'avant la bascule (D14) — tableau
    /// <c>{ Id, Display, IsSelected, Locked }</c> ; seuls les identifiants ont changé d'espace.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : Controller
    {
        private readonly IContextOrderStateQueryService _contextState;
        private readonly IContextOrderCatalogService _catalog;
        private readonly IContextOrderSelectionService _selection;

        public ContractController(
            IContextOrderStateQueryService contextState,
            IContextOrderCatalogService catalog,
            IContextOrderSelectionService selection)
        {
            _contextState = contextState;
            _catalog = catalog;
            _selection = selection;
        }

        /// <summary>Contextes sélectionnables, celui en vigueur marqué <c>IsSelected</c>.</summary>
        /// <remarks>
        /// ⚠️ Une panne d'Orders.Api rend une liste <b>vide</b>, jamais le catalogue local : ses
        /// identifiants ne désignent pas les mêmes types, et les servir ferait enregistrer un
        /// contexte pour un autre. Ne rien proposer vaut mieux que proposer faux.
        /// </remarks>
        [HttpGet("{gJobId}")]
        public async Task<IActionResult> GetContracts(Guid gJobId, CancellationToken ct)
            => Ok(await _catalog.GetChoicesAsync(gJobId, ct));

        /// <summary>
        /// État du contexte côté ERP : verrou et provenance. 404 si la mission est introuvable.
        /// </summary>
        [HttpGet("{gJobId}/state")]
        public async Task<IActionResult> GetState(Guid gJobId, CancellationToken ct)
        {
            var state = await _contextState.GetAsync(gJobId, ct);
            return state is null ? NotFound() : Ok(state);
        }

        /// <summary>Enregistre le contexte choisi pour la mission.</summary>
        /// <remarks>
        /// ⚠️ <b>Ce verbe peut refuser</b>, là où il réussissait toujours avant la bascule :
        /// <b>409</b> quand la régulation a imposé le type, <b>400</b> quand il n'est pas applicable
        /// à la commande. Dans les deux cas, rien n'est enregistré.
        /// </remarks>
        [HttpPost("{gJobId}")]
        [FreezeOnTransfer]
        public async Task<IActionResult> SelectContract(Guid gJobId, [FromBody] int contractId, CancellationToken ct)
        {
            // setBy : le « sub » Keycloak de l'ambulancier, tracé tel quel côté Order. On ne résout
            // pas le PER_ID ici — ce serait ajouter à une route d'écriture un maillon d'identité de
            // plus, donc un mode d'échec de plus, pour une donnée qu'Order stocke en texte libre.
            var setBy = User.GetKeycloakSubject()?.ToString();

            var outcome = await _selection.SelectAsync(gJobId, contractId, setBy, ct);

            return outcome switch
            {
                EnContextOrderSelectionOutcome.Applied => Ok(true),
                EnContextOrderSelectionOutcome.LockedByRegulator =>
                    Conflict("Type de mission fixé par la régulation : non modifiable depuis le terrain."),
                EnContextOrderSelectionOutcome.NotApplicable =>
                    BadRequest("Ce type de mission n'est pas disponible pour cette mission."),
                EnContextOrderSelectionOutcome.MissionNotFound => NotFound($"Mission {gJobId} introuvable."),
                _ => BadRequest("Type de mission refusé.")
            };
        }
    }
}
