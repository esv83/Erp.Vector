using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// Itération 11 — ce que Vector ajoute autour de la validation du socle (<c>AddCaSoftKeycloakJwtBearer</c>),
/// sans rien lui retirer : le motif d'un rejet déposé pour les contrôleurs
/// (<see cref="MobileCallerExtensions.GetJwtError"/>), le journal « Keycloak.Jwt », et le mode dégradé
/// <c>Keycloak:DisableValidation</c> (développement seulement).
/// </summary>
public static class VectorJwtBearerEvents
{
    /// <summary>À appeler APRÈS le socle (PostConfigure) : son contrôle de l'azp est conservé et enchaîné.</summary>
    public static void Apply(JwtBearerOptions options, bool disableValidation)
    {
        var admitCaller = options.Events.OnTokenValidated;

        if (disableValidation)
        {
            options.Authority = null; // évite le fetch OIDC metadata
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, _) => new JsonWebToken(token)
            };
        }

        options.Events.OnMessageReceived = ctx =>
        {
            var log = ctx.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>().CreateLogger("Keycloak.Jwt");
            log.LogDebug("JWT — requête {Method} {Path} : header Authorization {Present}.",
                ctx.HttpContext.Request.Method, ctx.HttpContext.Request.Path,
                ctx.HttpContext.HasAuthorizationHeader() ? "présent" : "ABSENT");
            return Task.CompletedTask;
        };
        options.Events.OnTokenValidated = async ctx =>
        {
            var log = ctx.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>().CreateLogger("Keycloak.Jwt");

            // Mode dégradé : aucun azp garanti, rien à admettre ni refuser (comme avant).
            if (!disableValidation) await admitCaller(ctx);

            if (ctx.Result?.Failure is { } failure)
            {
                ctx.HttpContext.Items[MobileCallerExtensions.JwtErrorKey] = failure.Message;
                log.LogWarning("JWT REJETÉ sur {Path} : {Reason}.", ctx.HttpContext.Request.Path, failure.Message);
                return;
            }

            log.LogInformation("JWT validé : sub={Sub} azp={Azp} iss={Iss} aud={Aud} exp={Exp}.",
                ctx.Principal?.FindFirst("sub")?.Value,
                ctx.Principal?.FindFirst(ClKeycloakCallers.AzpClaim)?.Value,
                ctx.Principal?.FindFirst("iss")?.Value,
                ctx.Principal?.FindFirst("aud")?.Value,
                ctx.Principal?.FindFirst("exp")?.Value);
        };
        options.Events.OnAuthenticationFailed = ctx =>
        {
            var reason = $"{ctx.Exception.GetType().Name}: {ctx.Exception.Message}";
            // Déposé pour lecture par les controllers (cf. MobileCallerExtensions.GetJwtError).
            ctx.HttpContext.Items[MobileCallerExtensions.JwtErrorKey] = reason;
            var log = ctx.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>().CreateLogger("Keycloak.Jwt");
            log.LogWarning(ctx.Exception, "JWT REJETÉ sur {Path} : {Reason}.",
                ctx.HttpContext.Request.Path, reason);
            return Task.CompletedTask;
        };
    }
}
