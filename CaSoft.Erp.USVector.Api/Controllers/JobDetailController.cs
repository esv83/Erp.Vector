using Microsoft.AspNetCore.Mvc;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Api.Infrastructure;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobDetailController : Controller
    {
        [HttpGet("{gJobId}")]
        public IActionResult GetDetail(
            Guid gJobId,
            [FromServices] IJobCache jobCache,
            [FromServices] IMobileIdentityResolver identity)
        {
            // MOB-4a : personnel résolu depuis le token (sub → personnel via Orders.Api),
            // via le chokepoint mutualisé CrewAccess.
            var error = CrewAccess.ResolvePersonnel(this, identity, out var personnelId);
            if (error is not null) return error;

            // Le personnel ne voit que les missions de ses crews.
            if (!identity.IsMissionAccessible(personnelId, gJobId))
                return StatusCode(403, "Mission hors de vos équipages.");

            ClGetJobUseCase useCase = new ClGetJobUseCase(gJobId, jobCache);
            return useCase.Handle().ToActionResult();
        }
    }
}
