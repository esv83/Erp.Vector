using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Simulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers;

/// <summary>
/// Mode formation : savoir s'il est ouvert, obtenir un persona sans compte (développement de l'app), et
/// remettre sa session à zéro. Les missions elles-mêmes passent par les routes ordinaires, au contrat
/// inchangé : c'est tout l'intérêt du mode.
/// </summary>
[ApiController]
[Route("api/simulation")]
public sealed class SimulationController : ControllerBase
{
    private readonly SimulationOptions _options;

    public SimulationController(SimulationOptions options) => _options = options;

    /// <summary>
    /// Le mode formation est-il ouvert, et cette requête y est-elle ? Authentifiée : un drapeau de
    /// configuration n'est pas servi sans jeton (même règle que <c>api/version/runtime</c>).
    /// </summary>
    [HttpGet]
    public IActionResult Status() => Ok(new
    {
        IsEnabled = _options.Enabled,
        HasPersonas = _options.PersonasEnabled,
        IsSimulatedRequest = SimulationRequest.IsSimulated(HttpContext, _options),
        HeaderName = SimulationRequest.HeaderName,
    });

    /// <summary>
    /// Personas sans compte Keycloak, chacun avec son jeton à poser en <c>Authorization: Bearer …</c>.
    /// Anonyme par nécessité — c'est elle qui donne le jeton — et 404 là où les personas sont fermés
    /// (Production, par refus au démarrage). Ouverture justifiée dans <c>AnonymousSurfaceTests</c>.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("personas")]
    public IActionResult Personas()
    {
        if (!_options.PersonasEnabled) return NotFound();

        return Ok(Enumerable.Range(1, 5).Select(n => new
        {
            Number = n,
            Name = ClSimulatedOrdersReadClient.PersonaName(n),
            Token = SimulationRequest.PersonaTokenPrefix + n,
        }));
    }

    /// <summary>
    /// Efface les saisies du jour de l'équipage de formation de l'appelant (horaires, signatures, cartes…)
    /// et l'état de son faux Orders : les missions repartent « à faire ». Refusé hors mode formation.
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(
        [FromServices] IMobileIdentityResolver identity,
        [FromServices] ClSimulationMobileStore store,
        [FromServices] ClSimulationState state,
        CancellationToken ct)
    {
        if (!SimulationRequest.IsSimulated(HttpContext, _options))
            return StatusCode(403, $"Remise à zéro réservée au mode formation (en-tête {SimulationRequest.HeaderName}).");

        var (error, personnelId) = await CrewAccess.ResolvePersonnelAsync(this, identity, ct);
        if (error is not null) return error;
        if (!ClSimulationIds.IsSimulated(personnelId))
            return StatusCode(403, "Remise à zéro réservée au mode formation.");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var owner = ClSimulationIds.OwnerOf(personnelId);
        var deleted = await store.ResetAsync(personnelId, today, ct);
        state.Forget(id => ClSimulationIds.IsSimulated(id)
                           && ClSimulationIds.OwnerOf(id).AsSpan().SequenceEqual(owner)
                           && ClSimulationIds.DayOf(id) == today);

        return Ok(new { DeletedRows = deleted });
    }
}
