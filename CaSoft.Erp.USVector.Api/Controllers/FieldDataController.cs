using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// TRF-6 — Paquet d'enrichissement terrain consolidé d'une mission, tiré par le module
    /// Certification au transfert en facturation (timeline + signature + attributs + mutuelle +
    /// km + documents + anomalies + watermark).
    /// </summary>
    [Route("api")]
    [ApiController]
    public class FieldDataController : Controller
    {
        private readonly IFieldDataReader _reader;

        public FieldDataController(IFieldDataReader reader) => _reader = reader;

        /// <summary>Paquet consolidé de la mission. 404 si la mission est introuvable côté ERP.</summary>
        [HttpGet("missions/{gJobId:guid}/field-data")]
        public async Task<IActionResult> Get(Guid gJobId, CancellationToken ct)
        {
            var data = await _reader.GetAsync(gJobId, ct);
            return data is null ? NotFound() : Ok(data);
        }
    }
}
