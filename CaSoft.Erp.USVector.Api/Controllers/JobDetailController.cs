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
        public async Task<IActionResult> GetDetail(
            Guid gJobId,
            [FromServices] IJobRepository jobs,
            [FromServices] IMobileIdentityResolver identity,
            CancellationToken ct)
        {
            // MOB-4a : personnel résolu depuis le token (sub → personnel via Orders.Api),
            // via le chokepoint mutualisé CrewAccess.
            var (error, personnelId) = await CrewAccess.ResolvePersonnelAsync(this, identity, ct);
            if (error is not null) return error;

            // Le personnel ne voit que les missions de ses crews.
            if (!await identity.IsMissionAccessibleAsync(personnelId, gJobId, ct))
                return StatusCode(403, "Mission hors de vos équipages.");

            var useCase = new ClGetJobUseCase(gJobId, jobs);
            return (await useCase.HandleAsync(ct)).ToActionResult();
        }
    }
}
