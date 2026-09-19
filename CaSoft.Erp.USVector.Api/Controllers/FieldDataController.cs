using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Authorization;
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
        // Tiré par la facturation en serveur-à-serveur, avec son jeton de service (erp-billinggateway-api)
        // depuis le 19/09. Anonyme jusque-là, faute de DEC-6. La politique de repli n'admettrait que
        // l'app : cette route admet le service OU l'app.
        [Authorize(Policy = ClKeycloakCallers.ServiceOrMobilePolicy)]
        [HttpGet("missions/{gJobId:guid}/field-data")]
        public async Task<IActionResult> Get(Guid gJobId, CancellationToken ct)
        {
            var data = await _reader.GetAsync(gJobId, ct);
            return data is null ? NotFound() : Ok(data);
        }
    }
}
