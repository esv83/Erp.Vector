using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// MOB-13.8 — Sélection du type de contrat d'une mission. Le contrat choisi détermine
    /// le jeu d'attributs (core + attributs liés) renvoyé par <c>FormStructure</c>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : Controller
    {
        private readonly IJobAttributeOverlay _overlay;

        public ContractController(IJobAttributeOverlay overlay) => _overlay = overlay;

        /// <summary>Liste des contrats sélectionnables, avec le contrat effectif de la mission.</summary>
        [HttpGet("{gJobId}")]
        public IActionResult GetContracts(Guid gJobId)
            => new ClUseCaseHandler(new ClListContractsUseCase(gJobId, _overlay)).Execute();

        /// <summary>Enregistre le contrat choisi pour la mission.</summary>
        [HttpPost("{gJobId}")]
        public IActionResult SelectContract(Guid gJobId, [FromBody] int contractId)
            => new ClUseCaseHandler(new ClSelectContractUseCase(new ClSelectContractCommand(gJobId, contractId), _overlay)).Execute();
    }
}
