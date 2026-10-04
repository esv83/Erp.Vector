using Microsoft.AspNetCore.Mvc;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : Controller
    {
        private readonly IJobService _jobService;

        public TimeController([FromServices] IJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet("{gJobId}")]
        public async Task<IActionResult> Get(Guid gJobId, CancellationToken ct)
            => (await _jobService.GetJobTimeAsync(gJobId, ct)).ToActionResult();

        // GET api/time/{id}/timeline — contrat riche (Option A) : liste ordonnée de jalons
        // { Order, Code, Label, At } prête à afficher, sans inférence côté UI. L'ancien endpoint
        // plat ci-dessus reste servi pour compatibilité (app mobile) le temps de la migration.
        [HttpGet("{gJobId}/timeline")]
        public async Task<IActionResult> GetTimeline(Guid gJobId, CancellationToken ct)
            => (await _jobService.GetJobTimelineAsync(gJobId, ct)).ToActionResult();

        // PATCH api/time/{id}
        [HttpPatch("{gJobId}")]
        [FreezeOnTransfer]
        public async Task<IActionResult> PatchJobTime(Guid gJobId, [FromBody] ClJobTimeModel JobTime, CancellationToken ct)
            => (await _jobService.SetJobTimeAsync(gJobId, JobTime, ct)).ToActionResult();

        // DELETE api/time/{id}/{jalon} — retour arrière : efface un jalon (seen | go | onsite | terminate).
        // L'effacement est projeté (Outbox) → régulation resynchronisée dès qu'Orders.Api gère « null = effacé ».
        [HttpDelete("{gJobId}/{jalon}")]
        [FreezeOnTransfer]
        public async Task<IActionResult> ClearJobTime(Guid gJobId, string jalon, CancellationToken ct)
            => (await _jobService.ClearJobTimeAsync(gJobId, jalon, ct)).ToActionResult();
    }
}
