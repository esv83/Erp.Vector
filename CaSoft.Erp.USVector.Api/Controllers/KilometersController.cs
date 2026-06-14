using CaSoft.Framework;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KilometersController:Controller
            {
        private ICrewCache _crewCache;
        private ICrewRepository _repository;
        public KilometersController(ICrewCache crewCache,ICrewRepository repository)
        {
            _crewCache = crewCache;
            _repository=repository;
    }

    [HttpGet("{CrewId}")]
        public IActionResult GetKilometers(Guid CrewId)
        {

            ClGetKilometersUseCase useCase = new ClGetKilometersUseCase(CrewId,_crewCache, _repository);
            return new ClUseCaseHandler(useCase).Execute();

            //renvoyer un objet Km Debut, Km Fin
        //    return Ok(4444);  
        }

        [HttpPost("{CrewId}")]
        public IActionResult PostKilometers(Guid CrewId, [FromBody] int Kilometers)
        {
            ClSetKilometersCommand command = new ClSetKilometersCommand(CrewId, Kilometers, "");
            ClSetKilometersUseCase useCase = new ClSetKilometersUseCase(command, _crewCache, _repository);
          
            return new ClUseCaseHandler(useCase).Execute();
        }


    }
}
