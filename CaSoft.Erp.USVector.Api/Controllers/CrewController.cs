using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrewController : Controller
    {
        private readonly ICrewService _crewService;
        private readonly IMobileIdentityResolver _identity;
        private readonly ILogger<CrewController> _logger;

        public CrewController(
            [FromServices] ICrewService crewService,
            [FromServices] IMobileIdentityResolver identity,
            [FromServices] ILogger<CrewController> logger)
        {
            _crewService = crewService;
            _identity = identity;
            _logger = logger;
        }

        // GET api/crew/mine — sélecteur d'équipage actif du personnel (résolu du token Keycloak).
        // Réponse décision-complète : l'UI force le choix si RequiresSelection, sinon prend l'unique crew.
        // Rappelé tel quel pour le « changement d'équipage » en cours de journée (tous les crews du jour y figurent).
        [HttpGet("mine")]
        public IActionResult Mine()
        {
            var error = CrewAccess.ResolvePersonnel(this, _identity, out var personnelId);
            if (error is not null) return error;

            // Lecture FRAÎCHE : la (re)sélection est le seul moment où un crew créé le jour même doit
            // apparaître ; on contourne le cache (et on le rafraîchit pour le garde-fou qui suivra).
            var crewIds = _identity.ResolveActiveCrewIdsFresh(personnelId, DateOnly.FromDateTime(DateTime.Now));
            if (crewIds.Count == 0)
            {
                _logger.LogWarning("GET api/crew/mine — PER_ID={PerId} sans équipage actif aujourd'hui.", personnelId);
                return NotFound("Aucun équipage actif pour ce personnel aujourd'hui.");
            }

            _logger.LogInformation("GET api/crew/mine — PER_ID={PerId} : {Count} équipage(s) actif(s).",
                personnelId, crewIds.Count);
            return _crewService.GetMyActiveCrews(crewIds, DateTime.Now).ToActionResult();
        }
    }
}
