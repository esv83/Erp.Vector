using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// Alerte signature, lot 1 — « le patient ne peut pas signer », et pourquoi (MOB_012). Le motif
    /// part vers Order avec la présence de la signature (PRJ-2) : une mission justifiée n'y est pas
    /// comptée en manque. Non bloquant : aucune étape de la mission n'en dépend.
    /// </summary>
    [Route("api")]
    [ApiController]
    public class SignatureWaiverController : Controller
    {
        private readonly ISignatureWaiverRepository _repository;

        public SignatureWaiverController(ISignatureWaiverRepository repository) => _repository = repository;

        /// <summary>Motifs proposés à l'équipage, dans l'ordre d'affichage.</summary>
        [HttpGet("signature-waiver-reasons")]
        public async Task<IActionResult> ListReasons(CancellationToken ct)
            => Ok((await _repository.ListActiveReasonsAsync(ct)).Select(r => r.ToDtoOut()));

        /// <summary>Motif déclaré pour la mission ; 204 s'il n'y en a pas.</summary>
        [HttpGet("missions/{gJobId:guid}/signature-waiver")]
        public async Task<IActionResult> Get(Guid gJobId, CancellationToken ct)
        {
            var waiver = await _repository.GetAsync(gJobId, ct);
            if (waiver is null) return NoContent();
            var reason = await _repository.GetReasonAsync(waiver.ReasonCode, ct);
            return Ok(waiver.ToDtoOut(reason?.Label));
        }

        /// <summary>Pose ou remplace le motif. Refus (400) motivé ; gelé (409) si la mission est transférée.</summary>
        [HttpPut("missions/{gJobId:guid}/signature-waiver")]
        [FreezeOnTransfer("gJobId")]
        public async Task<IActionResult> Declare(Guid gJobId, [FromBody] ClDeclareSignatureWaiverDtoIn input, CancellationToken ct)
            => (await new ClDeclareSignatureWaiverUseCase(gJobId, input, _repository).HandleAsync(ct)).ToActionResult();

        /// <summary>Retire le motif (le patient a finalement signé, ou erreur de saisie).</summary>
        [HttpDelete("missions/{gJobId:guid}/signature-waiver")]
        [FreezeOnTransfer("gJobId")]
        public async Task<IActionResult> Withdraw(Guid gJobId, CancellationToken ct)
        {
            await _repository.DeleteAsync(gJobId, ct);
            return NoContent();
        }
    }
}
