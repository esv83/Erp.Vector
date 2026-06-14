
using CaSoft.Erp.USVector.Application;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : Controller
    {
        private ICrewService _crewService;
        public DriverController([FromServices]  ICrewService crewService)
        {
               _crewService=crewService;
        }

        [HttpGet("{CrewId}")]
        public IActionResult GetDriver(Guid CrewId)
        {
           
            ClWebApiPresenter presenter =  ClWebApiPresenter.GetPresenter();
            _crewService.GetDriver(CrewId,presenter);

            return presenter.Result;

        }

        [HttpPost("{CrewId}")]
        public IActionResult PostDriver(Guid CrewId,[FromBody] Guid DriverId)
        {
         
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
            _crewService.ChangeDriver(CrewId,DriverId, presenter);

            return presenter.Result;
                   
        }

    }
}
