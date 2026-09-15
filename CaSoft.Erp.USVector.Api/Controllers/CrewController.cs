using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Framework;
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
                // C3 — dans la quasi-totalité des cas mesurés (journaux 04/07→24/08), la régulation n'a pas
                // encore composé l'équipage : l'ambulancier passe dès qu'elle l'a fait. Le message dit donc
                // quoi faire plutôt que de constater une absence. Même code, texte seul (D14).
                return NotFound("Votre équipage n'est pas encore composé par la régulation. "
                    + "Réessayez dans quelques minutes ; si rien ne change, appelez la régulation.");
            }

            _logger.LogInformation("GET api/crew/mine — PER_ID={PerId} : {Count} équipage(s) actif(s).",
                personnelId, crewIds.Count);

            var result = _crewService.GetMyActiveCrews(crewIds, DateTime.Now);

            // Équipage composé mais hors fenêtre (pas encore ouvert, clôturé, expiré) : le message du cas
            // d'usage dit quoi faire. ToActionResult rendrait un 404 nu — c'est le cas pour tous les
            // NotFound de l'API, et le changer ici ajouterait un corps à chacun d'eux.
            if (result.InnerError is ClError { IsNotFound: true } notFound)
            {
                _logger.LogWarning("GET api/crew/mine — PER_ID={PerId} : équipage(s) hors fenêtre — {Message}",
                    personnelId, notFound.ErrorText);
                return NotFound(notFound.ErrorText);
            }

            return result.ToActionResult();
        }
    }
}
