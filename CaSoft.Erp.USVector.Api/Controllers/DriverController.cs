
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
        // Renvoie le conducteur actif + les membres sélectionnables + le véhicule de l'équipage
        // actif du personnel connecté. MOB-4a : identité = token ; erreurs explicites par étape.
        [HttpGet]
        public IActionResult GetCurrent()
        {
            // 1. Token présent mais rejeté par le pipeline JWT (déposé par OnAuthenticationFailed).
            var jwtError = HttpContext.GetJwtError();
            if (jwtError is not null)
            {
                _logger.LogWarning("GET api/driver — 401 : token rejeté → {Reason}.", jwtError);
                return Unauthorized($"Token rejeté par l'authentification Keycloak : {jwtError}");
            }

            // 2. Aucun token fourni.
            if (!HttpContext.HasAuthorizationHeader())
                return Unauthorized("Aucun token d'authentification fourni (header « Authorization: Bearer … » absent).");

            // 3. Claim 'sub' exploitable ?
            var sub = User.GetKeycloakSubject();
            if (sub is null)
                return Unauthorized("Token valide mais claim « sub » (identifiant Keycloak) absent ou non-Guid.");

            // 4. sub → personnel ERP.
            var personnelId = _identity.ResolvePersonnelId(sub.Value);
            if (personnelId is null)
            {
                _logger.LogWarning("GET api/driver — 403 : sub {Sub} non rattaché à un personnel.", sub.Value);
                return StatusCode(403, $"Compte Keycloak {sub.Value} non rattaché à un personnel. Contactez la régulation.");
            }

            // 5. Équipage actif « maintenant ».
            var crewId = _identity.ResolveActiveCrewId(personnelId.Value, DateTime.Now);
            if (crewId is null)
            {
                _logger.LogWarning("GET api/driver — 404 : PER_ID={PerId} sans équipage actif.", personnelId.Value);
                return NotFound("Aucun équipage actif pour ce personnel actuellement.");
            }
            _logger.LogInformation("GET api/driver — équipage actif {CrewId} (PER_ID={PerId}).", crewId.Value, personnelId.Value);

            // 6. Délègue au use case existant (conducteur + membres + véhicule).
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
            _crewService.GetDriver(crewId.Value, presenter);
            return presenter.Result;
        }

        // GET api/driver/{CrewId} — équipage explicite (compat).
        [HttpGet("{CrewId}")]
        public IActionResult GetDriver(Guid CrewId)
        {
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
            _crewService.GetDriver(CrewId, presenter);
            return presenter.Result;
        }

        // POST api/driver/{CrewId} — change le conducteur de l'équipage.
        [HttpPost("{CrewId}")]
        public IActionResult PostDriver(Guid CrewId, [FromBody] Guid DriverId)
        {
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
            _crewService.ChangeDriver(CrewId, DriverId, presenter);
            return presenter.Result;
        }
    }
}
