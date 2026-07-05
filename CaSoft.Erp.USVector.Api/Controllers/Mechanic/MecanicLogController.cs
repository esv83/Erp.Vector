using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MecanicLogController : Controller
    {

        private ILogRepository _logRepository;
        public MecanicLogController([FromServices] ILogRepository repository)
        {
            _logRepository = repository;
        }


        [HttpGet()]
        public IActionResult GetLogs()
        {

            return new ClGetMechanicLogUseCase(_logRepository).Handle().ToActionResult();

        }
        [HttpGet("{intCrewId}")]
        public IActionResult GetLogs(Guid intCrewId)
        {

            return new ClGetMechanicLogUseCase(intCrewId, _logRepository).Handle().ToActionResult();

        }

        [HttpPost()]
        public IActionResult PostMechanicLog([FromBody] ClInsertLogModel model)

        {
            return new ClInsertMechanicLogUseCase(model, _logRepository).Handle().ToActionResult();

        }
    
    }
}
