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

        /// <summary>Plafond d'un lot : ~350 missions par journée chez la facturation, soit deux appels.</summary>
        public const int MaxMissionsParLot = 200;

        /// <summary>Corps de <c>POST api/missions/field-data</c>.</summary>
        public sealed class FieldDataBatchQuery
        {
            public List<Guid>? MissionIds { get; set; }
        }

        /// <summary>
        /// B5 — Paquets de plusieurs missions en un appel (19/09, demande de la facturation). Une entrée
        /// par mission demandée : <c>Found</c> (paquet dans <c>Data</c>), <c>NotFound</c> (l'ancien 404),
        /// <c>Error</c> (à retenter). Un échec sur une mission n'emporte pas le lot.
        /// </summary>
        /// <remarks>
        /// <b>POST</b> : 200 Guid dépassent la longueur d'URL admise par IIS. L'appel reste une lecture.
        /// Même politique que la route unitaire : la facturation ou l'app, avec jeton.
        /// </remarks>
        [Authorize(Policy = ClKeycloakCallers.ServiceOrMobilePolicy)]
        [HttpPost("missions/field-data")]
        public async Task<IActionResult> GetMany([FromBody] FieldDataBatchQuery query, CancellationToken ct)
        {
            var ids = query?.MissionIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();

            if (ids.Count > MaxMissionsParLot)
                return BadRequest($"Trop de missions demandées ({ids.Count}) : maximum {MaxMissionsParLot} par appel.");

            return Ok(await _reader.GetManyAsync(ids, ct));
        }
    }
}
