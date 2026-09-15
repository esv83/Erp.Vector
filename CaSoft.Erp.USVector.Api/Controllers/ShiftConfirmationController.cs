using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// Confirmation de prise de service <b>depuis l'application</b> : savoir au lancement si une
    /// confirmation attend, et la donner — utile quand le lien du courriel n'est jamais arrivé.
    /// <para>
    /// ⚖️ <b>Routes scopées au porteur du jeton, pas à un équipage actif</b> : <c>CrewAccess.Authorize</c>
    /// n'ouvre un équipage que 30 min avant sa prise de service, alors qu'une demande part souvent la
    /// veille. L'ambulancier est résolu du jeton ; c'est Order qui vérifie que la demande est la sienne.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftConfirmationController : Controller
    {
        private readonly IShiftConfirmationService _shiftConfirmation;
        private readonly IMobileIdentityResolver _identity;
        private readonly ILogger<ShiftConfirmationController> _logger;

        public ShiftConfirmationController(
            IShiftConfirmationService shiftConfirmation,
            IMobileIdentityResolver identity,
            ILogger<ShiftConfirmationController> logger)
        {
            _shiftConfirmation = shiftConfirmation;
            _identity = identity;
            _logger = logger;
        }

        /// <summary>
        /// Ce qui attend ma confirmation. <c>HasPending = false</c> et liste vide si rien n'attend.
        /// <c>me</c> = le porteur du jeton : le mobile ne fournit aucun identifiant de personnel.
        /// </summary>
        [HttpGet("~/api/personnel/me/shift-confirmations/pending")]
        public async Task<IActionResult> ListMyPending(CancellationToken ct)
        {
            var error = CrewAccess.ResolvePersonnel(this, _identity, out var personnelId);
            if (error is not null) return error;

            try
            {
                return Ok(await _shiftConfirmation.GetMineAsync(personnelId, ct));
            }
            catch (HttpRequestException ex)
            {
                // Rejouable à l'identique : ce n'est pas la requête qui est mauvaise.
                _logger.LogError(ex, "GET api/personnel/me/shift-confirmations/pending — PER_ID={PerId} : Orders.Api indisponible.", personnelId);
                return StatusCode(503, "Confirmation de prise de service momentanément indisponible.");
            }
        }

        /// <summary>
        /// Je confirme ma prise de service. <c>crewId</c> et <c>requestId</c> viennent de
        /// <c>GET api/personnel/me/shift-confirmations/pending</c>. Rejouable : un second appel rend
        /// <c>AlreadyConfirmed</c>.
        /// </summary>
        [HttpPost("{crewId:guid}/{requestId:guid}/confirm")]
        public async Task<IActionResult> Confirm(Guid crewId, Guid requestId, CancellationToken ct)
        {
            var error = CrewAccess.ResolvePersonnel(this, _identity, out var personnelId);
            if (error is not null) return error;

            ClShiftConfirmationConfirmResult result;
            try
            {
                result = await _shiftConfirmation.ConfirmAsync(crewId, requestId, personnelId, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "POST api/ShiftConfirmation/{CrewId}/{RequestId}/confirm — PER_ID={PerId} : Orders.Api indisponible.",
                    crewId, requestId, personnelId);
                return StatusCode(503, "Confirmation de prise de service momentanément indisponible.");
            }

            _logger.LogInformation("POST api/ShiftConfirmation/{CrewId}/{RequestId}/confirm — PER_ID={PerId} : {Outcome}.",
                crewId, requestId, personnelId, result.Outcome);

            return result.Outcome switch
            {
                EnShiftConfirmationConfirmOutcome.Confirmed or EnShiftConfirmationConfirmOutcome.AlreadyConfirmed =>
                    Ok(new ClShiftConfirmedDtoOut
                    {
                        Confirmed = true,
                        AlreadyConfirmed = result.Outcome == EnShiftConfirmationConfirmOutcome.AlreadyConfirmed,
                        ProposedLocalTime = result.ProposedLocalTime
                    }),
                EnShiftConfirmationConfirmOutcome.NotFound =>
                    NotFound("Aucune confirmation de prise de service en attente pour vous sur cette demande."),
                _ => BadRequest(result.Reason ?? "Confirmation de prise de service refusée.")
            };
        }
    }
}
