using CaSoft.Framework;
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
            ClWebApiPresenter presenter = new ClWebApiPresenter();
            useCase.Execute(presenter);

            return presenter.Result;

        }

        [HttpPost("{CrewId}")]
        public IActionResult PostEndOfService(Guid intCrewId, [FromBody] DateTime dteDate)
        {

            ClSetEndOfServiceCommand query = new ClSetEndOfServiceCommand(intCrewId, dteDate, "FromRegulation");
            ClSetEndOfServiceUseCase useCase = new ClSetEndOfServiceUseCase(query,_crewCache, _crewRepository);
                      

            return new ClUseCaseHandler(useCase).Execute();

          

            //TODO Gerer les permission de poster une date de fin par la regul (fromAutority)
                 
                    }
    }
}