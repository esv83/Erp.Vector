using System.Linq;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// TRF-8 — Anomalies terrain d'une mission (spec §17). Signalement + liste. Non bloquantes :
    /// transférées dans le paquet field-data, arbitrées par la facturation.
    /// </summary>
    [Route("api")]
    [ApiController]
    public class AnomalyController : Controller
    {
        private readonly IAnomalyRepository _repository;

        public AnomalyController(IAnomalyRepository repository) => _repository = repository;

        /// <summary>Signale une anomalie sur la mission. Bloqué (409) si la mission est déjà transférée.</summary>
        [HttpPost("missions/{gJobId:guid}/anomalies")]
        [FreezeOnTransfer("gJobId")]
        public IActionResult Report(Guid gJobId, [FromBody] ClReportAnomalyDtoIn input)
            => new ClReportAnomalyUseCase(new ClReportAnomalyCommand(gJobId, input), _repository).Handle().ToActionResult();

        /// <summary>Anomalies de la mission (de la plus récente à la plus ancienne).</summary>
        [HttpGet("missions/{gJobId:guid}/anomalies")]
        public IActionResult List(Guid gJobId)
            => Ok(_repository.ListByMission(gJobId).Select(a => a.ToDtoOut()));
    }
}
