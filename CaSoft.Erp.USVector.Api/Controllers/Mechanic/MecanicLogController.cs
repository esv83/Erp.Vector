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

            ClGetMechanicLogUseCase UseCase = new ClGetMechanicLogUseCase(_logRepository);
            ClWebApiPresenter presenter = new ClWebApiPresenter();
            UseCase.Execute(presenter);

            return presenter.Result;

        }
        [HttpGet("{intCrewId}")]
        public IActionResult GetLogs(Guid intCrewId)
        {

            ClGetMechanicLogUseCase UseCase = new ClGetMechanicLogUseCase(intCrewId,_logRepository);
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
                   UseCase.Execute(presenter);
            
            return presenter.Result;

        }

        [HttpPost()]
        public IActionResult PostMechanicLog([FromBody] ClInsertLogModel model)

        {
            ClWebApiPresenter presenter = new ClWebApiPresenter();
            ClInsertMechanicLogUseCase UseCase = new ClInsertMechanicLogUseCase(model, _logRepository);

            UseCase.Execute(presenter);

            return presenter.Result;

        }
    
    }
}
