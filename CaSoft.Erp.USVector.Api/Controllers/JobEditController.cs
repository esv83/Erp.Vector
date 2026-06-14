using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobEditController : Controller
    {
        private IJobCache _jobCache;
        private IJobRepository _jobRepository;
        public JobEditController([FromServices] IJobCache jobCache,IJobRepository jobRepository)
        {
            _jobCache = jobCache;
            _jobRepository = jobRepository;
        }

        //[HttpGet("{gJobId}")]
        //public IActionResult Get(Guid gJobId)
        //{

        //    if (!ClAutorizationCommand.AutorizeJob(Request, gJobId))
        //    {
        //        return BadRequest("Autorisation refusée");
        //    }
         
        //    IUseCaseResponse Response = jobService.GetJobValue(gJobId);
        //    ClResponseHandler Handler = new ClResponseHandler(this, Response);
           
        //    return Handler.Result();

        //}

        [HttpPatch("{gJobId}")]
        public IActionResult PatchEditableJob(Guid gJobId ,List<ClAttributValueModel> Values)

        {
            ClUpdateJobEditCommand Cmd = new ClUpdateJobEditCommand(gJobId, Values);
            ClWebApiPresenter presenter = new ClWebApiPresenter();
            ClUpdateJobEditUseCase UseCase = new ClUpdateJobEditUseCase(Cmd, _jobCache, _jobRepository);

        UseCase.Execute(presenter);

            return presenter.Result;

           //  response = jobService.UpdateAttributValues(gJobId, Values);
                  }

    }
}

    

