
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

        // GET api/driver/{crewId} — conducteur actif + membres sélectionnables + véhicule de l'équipage.
        // L'équipage est TOUJOURS explicite (crew épinglé par l'app via /api/crew/mine) : plus de
        // résolution « canonique » arbitraire quand le personnel a plusieurs équipages le même jour.
        [HttpGet("{crewId}")]
        public IActionResult GetDriver(Guid crewId)
        {
            var error = CrewAccess.Authorize(this, _identity, crewId);
            if (error is not null) return error;

            return _crewService.GetDriver(crewId).ToActionResult();
        }

        // POST api/driver/{crewId} — change le conducteur de l'équipage. Corps = Guid du conducteur (un membre).
        [HttpPost("{crewId}")]
        public IActionResult PostDriver(Guid crewId, [FromBody] Guid DriverId)
        {
            var error = CrewAccess.Authorize(this, _identity, crewId);
            if (error is not null) return error;

            _logger.LogInformation("POST api/driver/{CrewId} — nouveau conducteur {DriverId}.", crewId, DriverId);
            return _crewService.ChangeDriver(crewId, DriverId).ToActionResult();
        }
    }
}
