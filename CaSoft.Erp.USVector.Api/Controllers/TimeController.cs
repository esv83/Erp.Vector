using Microsoft.AspNetCore.Mvc;
using CaSoft.Framework;
using CaSoft.Erp.USVector.Api.Infrastructure;
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
        [FreezeOnTransfer]
        public IActionResult PatchJobTime(Guid gJobId, [FromBody] ClJobTimeModel JobTime)
        {


            ClWebApiPresenter presenter= ClWebApiPresenter.GetPresenter();
                      _jobService.SetJobTime(gJobId, JobTime, (IResponseHandler) presenter);
            return presenter.Result;

                  }

        // DELETE api/time/{id}/{jalon} — retour arrière : efface un jalon (seen | go | onsite | terminate).
        // L'effacement est projeté (Outbox) → régulation resynchronisée dès qu'Orders.Api gère « null = effacé ».
        [HttpDelete("{gJobId}/{jalon}")]
        [FreezeOnTransfer]
        public IActionResult ClearJobTime(Guid gJobId, string jalon)
        {
            ClWebApiPresenter presenter = ClWebApiPresenter.GetPresenter();
            _jobService.ClearJobTime(gJobId, jalon, (IResponseHandler) presenter);
            return presenter.Result;
        }

    }
}

