using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// MOB-13.8 — Sélection du type de contrat d'une mission. Le contrat choisi détermine
    /// le jeu d'attributs (core + attributs liés) renvoyé par <c>FormStructure</c>.
    /// <para>
    /// OC-3a — Le verrou posé côté ERP est <b>lisible</b> par le terrain, par deux ajouts
    /// strictement additifs (D14) : la propriété <c>Locked</c> sur chaque item de la liste, et la
    /// route <c>/state</c>.
    /// </para>
    /// <para>
    /// OC-3b — La <b>source</b> bascule vers Order, sous drapeau (<see cref="ContextOrderOptions"/>).
    /// Les deux verbes ci-dessous portent donc chacun un aiguillage, et un seul : la forme des
    /// réponses est identique de part et d'autre, seuls la source de la liste et le destinataire de
    /// l'écriture changent. C'est volontairement visible plutôt que caché derrière une injection —
    /// on doit pouvoir lire ici, en dix lignes, ce que l'armement change réellement pour le terrain.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : Controller
    {
        private readonly IJobAttributeOverlay _overlay;
        private readonly IContextOrderStateQueryService _contextState;
        private readonly IContextOrderCatalogService _catalog;
        private readonly IContextOrderSelectionService _selection;
        private readonly ContextOrderOptions _options;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            IJobAttributeOverlay overlay,
            IContextOrderStateQueryService contextState,
            IContextOrderCatalogService catalog,
            IContextOrderSelectionService selection,
            ContextOrderOptions options,
            ILogger<ContractController> logger)
        {
            _overlay = overlay;
            _contextState = contextState;
            _catalog = catalog;
            _selection = selection;
            _options = options;
            _logger = logger;
        }

        /// <summary>Liste des contrats sélectionnables, avec le contrat effectif de la mission.</summary>
        /// <remarks>
        /// La forme ne change pas d'un côté à l'autre de la bascule : tableau de
        /// <c>{ Id, Display, IsSelected, Locked }</c>. Le passage à un objet et le renommage de la
        /// route en <c>/api/ContextOrder</c> attendent que le front ait basculé (D14).
        /// </remarks>
        [HttpGet("{gJobId}")]
        public async Task<IActionResult> GetContracts(Guid gJobId, CancellationToken ct)
        {
            if (_options.UseOrderCatalog)
                return Ok(await _catalog.GetChoicesAsync(gJobId, ct));

            return new ClListContractsUseCase(gJobId, _overlay, await ReadLockedOrFalseAsync(gJobId, ct))
                .Handle().ToActionResult();
        }

        /// <summary>
        /// OC-3a — État du context de la mission côté ERP : verrou et provenance. 404 si la mission
        /// est introuvable côté ERP.
        /// </summary>
        [HttpGet("{gJobId}/state")]
        public async Task<IActionResult> GetState(Guid gJobId, CancellationToken ct)
        {
            var state = await _contextState.GetAsync(gJobId, ct);
            return state is null ? NotFound() : Ok(state);
        }

        /// <summary>Enregistre le contrat choisi pour la mission.</summary>
        /// <remarks>
        /// ⚠️ <b>Une fois la bascule armée, ce verbe peut refuser.</b> Il réussissait toujours ; il
        /// répond désormais <b>409</b> quand la régulation a imposé le type et <b>400</b> quand le
        /// type n'est pas applicable à la commande. C'est le seul changement de comportement visible
        /// par le front, et la raison pour laquelle l'armement se coordonne avec le dev web.
        /// </remarks>
        [HttpPost("{gJobId}")]
        [FreezeOnTransfer]
        public async Task<IActionResult> SelectContract(Guid gJobId, [FromBody] int contractId, CancellationToken ct)
        {
            if (!_options.UseOrderCatalog)
                return new ClSelectContractUseCase(new ClSelectContractCommand(gJobId, contractId), _overlay)
                    .Handle().ToActionResult();

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

        /// <summary>
        /// Verrou de la mission, ou <c>false</c> si l'ERP n'a pas répondu.
        /// <para>
        /// Chemin d'avant la bascule uniquement. La liste des contrats est alors servie depuis la BD
        /// Mobile et fonctionnait avant qu'Orders.Api n'entre dans la boucle : une panne de l'ERP ne
        /// doit pas la faire tomber. On dégrade donc vers l'ancien comportement — liste servie,
        /// verrou inconnu supposé absent — plutôt que de propager l'erreur (D14 : un ajout ne casse
        /// pas ce qui marchait). L'ambulancier peut alors tenter une sélection qui sera refusée en
        /// 409 : c'est le comportement d'avant OC-3a, pas une régression.
        /// </para>
        /// <para>
        /// Après la bascule, ce repli n'a plus de sens : la liste <i>est</i> celle d'Order, et
        /// <see cref="IContextOrderCatalogService"/> rend une liste vide plutôt qu'une liste dont les
        /// identifiants ne veulent plus rien dire.
        /// </para>
        /// </summary>
        private async Task<bool> ReadLockedOrFalseAsync(Guid missionId, CancellationToken ct)
        {
            try
            {
                var state = await _contextState.GetAsync(missionId, ct);
                return state?.Locked ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "OC-3a : verrou du context indisponible pour la mission {MissionId}, liste servie sans verrou.",
                    missionId);
                return false;
            }
        }
    }
}
