using Microsoft.AspNetCore.Mvc;
using CaSoft.Erp.USVector.Api.Infrastructure;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// MOB-4a — Endpoint de diagnostic de l'authentification Keycloak.
    /// Permet de tester l'<b>interception du JWT</b> en isolation, sans la logique
    /// métier (résolution personnel → crews). Renvoie les claims du token validé,
    /// ou une 401 avec la raison précise du rejet.
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController([FromServices] ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        // GET api/auth/whoami
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            // 1. Token présent mais rejeté par le pipeline JWT (raison via OnAuthenticationFailed).
            var jwtError = HttpContext.GetJwtError();
            if (jwtError is not null)
            {
                _logger.LogWarning("whoami — 401 : token rejeté → {Reason}.", jwtError);
                return Unauthorized(new { authenticated = false, reason = "token_rejected", error = jwtError });
            }

            // 2. Aucun token fourni.
            if (!HttpContext.HasAuthorizationHeader())
            {
                _logger.LogInformation("whoami — 401 : aucun header Authorization.");
                return Unauthorized(new { authenticated = false, reason = "no_token", error = "Aucun token fourni." });
            }

            // 3. Token présent mais principal non authentifié (cas limite : pas d'erreur capturée).
            if (User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("whoami — 401 : token présent mais non authentifié.");
                return Unauthorized(new { authenticated = false, reason = "not_authenticated", error = "Token présent mais non authentifié." });
            }

            // 4. Token validé → on renvoie l'identité décodée.
            var sub = User.GetKeycloakSubject();
            _logger.LogInformation("whoami — 200 : token validé, sub={Sub}.", sub);
            return Ok(new
            {
                authenticated = true,
                sub,
                name = User.Identity?.Name,
                claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }
    }
}
