
using Microsoft.AspNetCore.Mvc;
using CaSoft.Framework;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Contracts;
using CaSoft.Erp.USVector.Api.Infrastructure;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobListController : Controller
    {
        private readonly IJobService _jobSce;
        private readonly ICrewRepository _crewRepository;
        private readonly IMobileIdentityResolver _identity;
        private readonly ILogger<JobListController> _logger;

        public JobListController(
            [FromServices] ICrewRepository crewRepository,
            [FromServices] IJobService jobSce,
            [FromServices] IMobileIdentityResolver identity,
            [FromServices] ILogger<JobListController> logger)

        {
            _crewRepository = crewRepository;
            _jobSce = jobSce;
            _identity = identity;
            _logger = logger;
        }

        // GET api/joblist  — canonique : crews dérivés du token Keycloak.
        // (le client ne dicte plus l'équipage ; cf. mobile_devplan.md §7).
        // MOB-4a : identité = token Keycloak ; chaque étape est tracée + renvoie une erreur explicite.
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("GET api/joblist — début (Authorization {Auth}).",
                HttpContext.HasAuthorizationHeader() ? "présent" : "absent");

            // 1. Token PRÉSENT mais REJETÉ par le pipeline JWT (expiré / signature / issuer / audience).
            //    La raison a été déposée par l'event OnAuthenticationFailed.
            var jwtError = HttpContext.GetJwtError();
            if (jwtError is not null)
            {
                _logger.LogWarning("GET api/joblist — 401 : token rejeté → {Reason}.", jwtError);
                return Unauthorized($"Token rejeté par l'authentification Keycloak : {jwtError}");
            }

            // 2. Aucun token fourni.
            if (!HttpContext.HasAuthorizationHeader())
            {
                _logger.LogWarning("GET api/joblist — 401 : aucun header Authorization.");
                return Unauthorized("Aucun token d'authentification fourni (header « Authorization: Bearer … » absent).");
            }

            // 3. Token accepté mais claim 'sub' inexploitable.
            var sub = User.GetKeycloakSubject();
            if (sub is null)
            {
                _logger.LogWarning("GET api/joblist — 401 : authentifié={Auth} mais claim 'sub' absent/non-Guid.",
                    User.Identity?.IsAuthenticated);
                return Unauthorized("Token valide mais claim « sub » (identifiant Keycloak) absent ou non-Guid.");
            }
            _logger.LogInformation("GET api/joblist — token OK, sub={Sub}.", sub.Value);

            // 4. sub → personnel ERP (liaison PER_KEYCLOAK_MAP).
            var personnelId = _identity.ResolvePersonnelId(sub.Value);
            if (personnelId is null)
            {
                _logger.LogWarning("GET api/joblist — 403 : sub {Sub} non rattaché à un personnel.", sub.Value);
                return StatusCode(403, $"Compte Keycloak {sub.Value} non rattaché à un personnel. Contactez la régulation.");
            }
            _logger.LogInformation("GET api/joblist — personnel résolu : PER_ID={PerId}.", personnelId.Value);

            // 5. Crews actifs du jour (union des missions). Vide = joblist vide (200), pas une erreur.
            var today = DateOnly.FromDateTime(DateTime.Today);
            var crewIds = _identity.ResolveActiveCrewIds(personnelId.Value, today);
            if (crewIds.Count == 0)
                _logger.LogWarning("GET api/joblist — PER_ID={PerId} sans équipage actif le {Date} → joblist vide.",
                    personnelId.Value, today);
            else
                _logger.LogInformation("GET api/joblist — {Count} équipage(s) actif(s) [{Crews}].",
                    crewIds.Count, string.Join(", ", crewIds));

            var useCase = new ClGetJobListUseCase(crewIds, _crewRepository);
            var result = new ClUseCaseHandler(useCase).Execute();
            _logger.LogInformation("GET api/joblist — fin, réponse construite.");
            return result;
        }

        // PATCH api/joblist — marquer une mission comme lue.
        [HttpPatch()]
        public IActionResult PatchRead(ClReadJobModel ReadModel)
        {
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
            if (ReadModel.IsJob)
            {
                _jobSce.ReadJob(ReadModel.JobId, presenter);
            }

            return presenter.Result;
        }
    }
}
