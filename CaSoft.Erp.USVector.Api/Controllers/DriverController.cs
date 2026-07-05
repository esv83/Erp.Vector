
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : Controller
    {
        private readonly ICrewService _crewService;
        private readonly IMobileIdentityResolver _identity;
        private readonly ILogger<DriverController> _logger;

        public DriverController(
            [FromServices] ICrewService crewService,
            [FromServices] IMobileIdentityResolver identity,
            [FromServices] ILogger<DriverController> logger)
        {
            _crewService = crewService;
            _identity = identity;
            _logger = logger;
        }

        // GET api/driver — canonique : l'équipage est dérivé du token Keycloak (comme JobList).
        // Renvoie le conducteur actif + les membres sélectionnables + le véhicule de l'équipage actif.
        [HttpGet]
        public IActionResult GetCurrent()
        {
            var error = ResolveCurrentCrew(out var crewId);
            if (error is not null) return error;

            return _crewService.GetDriver(crewId).ToActionResult();
        }

        // POST api/driver — canonique : change le conducteur de l'équipage actif du token.
        // Corps = l'identifiant (Guid) du conducteur choisi (un membre de l'équipage).
        [HttpPost]
        public IActionResult PostCurrent([FromBody] Guid DriverId)
        {
            var error = ResolveCurrentCrew(out var crewId);
            if (error is not null) return error;

            _logger.LogInformation("POST api/driver — équipage {CrewId}, nouveau conducteur {DriverId}.", crewId, DriverId);
            return _crewService.ChangeDriver(crewId, DriverId).ToActionResult();
        }

        // GET api/driver/{CrewId} — équipage explicite (compat).
        [HttpGet("{CrewId}")]
        public IActionResult GetDriver(Guid CrewId)
            => _crewService.GetDriver(CrewId).ToActionResult();

        // POST api/driver/{CrewId} — équipage explicite (compat).
        [HttpPost("{CrewId}")]
        public IActionResult PostDriver(Guid CrewId, [FromBody] Guid DriverId)
            => _crewService.ChangeDriver(CrewId, DriverId).ToActionResult();

        // Résolution token → équipage actif « maintenant » (pipeline commun GET/POST canoniques,
        // aligné sur JobListController). Renvoie un IActionResult d'erreur, ou null si OK (crewId posé).
        private IActionResult? ResolveCurrentCrew(out Guid crewId)
        {
            crewId = Guid.Empty;

            var jwtError = HttpContext.GetJwtError();
            if (jwtError is not null)
            {
                _logger.LogWarning("api/driver — 401 : token rejeté → {Reason}.", jwtError);
                return Unauthorized($"Token rejeté par l'authentification Keycloak : {jwtError}");
            }

            if (!HttpContext.HasAuthorizationHeader())
                return Unauthorized("Aucun token d'authentification fourni (header « Authorization: Bearer … » absent).");

            var sub = User.GetKeycloakSubject();
            if (sub is null)
                return Unauthorized("Token valide mais claim « sub » (identifiant Keycloak) absent ou non-Guid.");

            var personnelId = _identity.ResolvePersonnelId(sub.Value);
            if (personnelId is null)
            {
                _logger.LogWarning("api/driver — 403 : sub {Sub} non rattaché à un personnel.", sub.Value);
                return StatusCode(403, $"Compte Keycloak {sub.Value} non rattaché à un personnel. Contactez la régulation.");
            }

            var cid = _identity.ResolveActiveCrewId(personnelId.Value, DateTime.Now);
            if (cid is null)
            {
                _logger.LogWarning("api/driver — 404 : PER_ID={PerId} sans équipage actif.", personnelId.Value);
                return NotFound("Aucun équipage actif pour ce personnel actuellement.");
            }

            crewId = cid.Value;
            return null;
        }
    }
}
