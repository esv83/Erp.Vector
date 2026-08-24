using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// MOB-13.8 — Sélection du type de contrat d'une mission. Le contrat choisi détermine
    /// le jeu d'attributs (core + attributs liés) renvoyé par <c>FormStructure</c>.
    /// <para>
    /// OC-3a — Le verrou posé côté ERP est désormais <b>lisible</b> par le terrain, par deux ajouts
    /// strictement additifs (D14) : la propriété <c>Locked</c> sur chaque item de la liste, et la
    /// route <c>/state</c>. La <b>source</b> de la liste, elle, reste <c>MOB_CONTRACT_TYPE</c>
    /// jusqu'à OC-3b — les identifiants servis ici ne sont donc pas ceux du catalogue Order.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : Controller
    {
        private readonly IJobAttributeOverlay _overlay;
        private readonly IContextOrderStateQueryService _contextState;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            IJobAttributeOverlay overlay,
            IContextOrderStateQueryService contextState,
            ILogger<ContractController> logger)
        {
            _overlay = overlay;
            _contextState = contextState;
            _logger = logger;
        }

        /// <summary>Liste des contrats sélectionnables, avec le contrat effectif de la mission.</summary>
        [HttpGet("{gJobId}")]
        public async Task<IActionResult> GetContracts(Guid gJobId, CancellationToken ct)
            => new ClListContractsUseCase(gJobId, _overlay, await ReadLockedOrFalseAsync(gJobId, ct))
                .Handle().ToActionResult();

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
        [HttpPost("{gJobId}")]
        [FreezeOnTransfer]
        public IActionResult SelectContract(Guid gJobId, [FromBody] int contractId)
            => new ClSelectContractUseCase(new ClSelectContractCommand(gJobId, contractId), _overlay).Handle().ToActionResult();

        /// <summary>
        /// Verrou de la mission, ou <c>false</c> si l'ERP n'a pas répondu.
        /// <para>
        /// La liste des contrats est servie depuis la BD Mobile et fonctionnait avant qu'Orders.Api
        /// n'entre dans la boucle : une panne de l'ERP ne doit pas la faire tomber. On dégrade donc
        /// vers l'ancien comportement — liste servie, verrou inconnu supposé absent — plutôt que de
        /// propager l'erreur (D14 : un ajout ne casse pas ce qui marchait). L'ambulancier peut alors
        /// tenter une sélection qui sera refusée en 409 : c'est le comportement d'avant OC-3a, pas
        /// une régression.
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
