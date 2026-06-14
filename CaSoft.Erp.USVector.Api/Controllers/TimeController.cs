using Microsoft.AspNetCore.Mvc;
using CaSoft.Framework;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : Controller
    {
        //private IJobCache _jobCache;
        //private IJobRepository _jobRepository;
        private IJobService _jobService;

        public TimeController([FromServices]  IJobService jobService)
        {
            _jobService = jobService;
            //_jobCache = jobCache;
            //_jobRepository = jobRepository;
        }

        [HttpGet("{gJobId}")]
        public IActionResult Get(Guid gJobId)
        {

         
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
                     _jobService.GetJobTime(gJobId,presenter);
            return presenter.Result;

        }

        // PATCH api/<ValuesController1>/5
        [HttpPatch("{gJobId}")]
        public IActionResult PatchJobTime(Guid gJobId, [FromBody] ClJobTimeModel JobTime)
        {


            ClWebApiPresenter presenter= ClWebApiPresenter.GetPresenter();
                      _jobService.SetJobTime(gJobId, JobTime, (IResponseHandler) presenter);
            return presenter.Result;

                  }

    }
}

