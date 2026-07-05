using CaSoft.Framework;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormStructureController : Controller
    {
        [HttpGet("{gJobId}")]
        public IActionResult GetDetail(Guid gJobId,  [FromServices] IJobRepository repository)
        {

           
            ClGetJobEditFormStructureUseCase useCase = new ClGetJobEditFormStructureUseCase(gJobId,  repository);

            return useCase.Handle().ToActionResult();



        }
    }
}
