
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

        public JobListController(
            [FromServices] ICrewRepository crewRepository,
            [FromServices] IJobService jobSce,
            [FromServices] IMobileIdentityResolver identity)
        {
            _crewRepository = crewRepository;
            _jobSce = jobSce;
            _identity = identity;
        }

        // GET api/joblist  — canonique : crews dérivés du token Keycloak.
        // (le client ne dicte plus l'équipage ; cf. mobile_devplan.md §7).
        [HttpGet]
             public IActionResult Get()
        {
            // MOB-4a : identité = token Keycloak. Plus de crewId déclaratif.
            var sub = User.GetKeycloakSubject();
            if (sub is null)
                return Unauthorized("Authentification Keycloak requise.");

            var personnelId = _identity.ResolvePersonnelId(sub.Value);
            if (personnelId is null)
                return StatusCode(403, "Compte non rattaché à un personnel. Contactez la régulation.");

            // Union des missions du jour de tous les crews actifs du personnel.
            var crewIds = _identity.ResolveActiveCrewIds(personnelId.Value, DateOnly.FromDateTime(DateTime.Today));

            var useCase = new ClGetJobListUseCase(crewIds, _crewRepository);
            return new ClUseCaseHandler(useCase).Execute();
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
