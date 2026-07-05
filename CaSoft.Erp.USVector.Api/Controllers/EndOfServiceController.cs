using CaSoft.Framework;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    public class EndOfServiceController : Controller
    {
        private ICrewRepository _crewRepository;
        private ICrewCache _crewCache;
        public EndOfServiceController([FromServices] ICrewCache crewCache,ICrewRepository CrewRepository)
        {
            _crewRepository = CrewRepository;
            _crewCache = crewCache;
        }

        [HttpGet("{CrewId}")]
        public IActionResult GetEndOfService(Guid intCrewId)
        {
            ClGetEndOfServiceUseCase useCase = new ClGetEndOfServiceUseCase(intCrewId, _crewRepository);
            return useCase.Handle().ToActionResult();

        }

        [HttpPost("{CrewId}")]
        public IActionResult PostEndOfService(Guid intCrewId, [FromBody] DateTime dteDate)
        {

            ClSetEndOfServiceCommand query = new ClSetEndOfServiceCommand(intCrewId, dteDate, "FromRegulation");
            ClSetEndOfServiceUseCase useCase = new ClSetEndOfServiceUseCase(query,_crewCache, _crewRepository);
                      

            return useCase.Handle().ToActionResult();

          

            //TODO Gerer les permission de poster une date de fin par la regul (fromAutority)
                 
                    }
    }
}