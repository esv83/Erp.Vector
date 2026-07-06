
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

        // GET api/joblist/{crewId} — missions de l'équipage ÉPINGLÉ par l'app (via /api/crew/mine).
        // Scopée à un seul équipage : au changement d'équipage en cours de journée, l'app rappelle
        // avec l'autre crewId et la liste suit. Le garde-fou vérifie que l'équipage est bien l'un
        // de ceux du personnel aujourd'hui (l'UI ne peut pas cibler un équipage étranger).
        [HttpGet("{crewId}")]
        public IActionResult Get(Guid crewId)
        {
            var error = CrewAccess.Authorize(this, _identity, crewId);
            if (error is not null) return error;

            _logger.LogInformation("GET api/joblist/{CrewId} — début.", crewId);
            var useCase = new ClGetJobListUseCase(new[] { crewId }, _crewRepository);
            var result = useCase.Handle().ToActionResult();
            _logger.LogInformation("GET api/joblist/{CrewId} — fin, réponse construite.", crewId);
            return result;
        }

        // PATCH api/joblist — « Mission vue » (spec §10) : l'ambulancier signale « bien reçu »
        // → pose MST_READ_AT + projette MissionSeen à la régulation. Idempotent.
        // L'UI masque l'icône quand IsSeen=true.
        [HttpPatch()]
        public IActionResult PatchSeen(ClReadJobModel ReadModel)
        {
            if (!ReadModel.IsJob)
                return BadRequest("Requête invalide : IsJob attendu à true.");

            return _jobSce.MarkMissionSeen(ReadModel.JobId).ToActionResult();
        }
    }
}
