using System.Security.Claims;
using CaSoft.Erp.USVector.Infrastructure.Simulation;
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
    /// <param name="simulation">Mode formation : avec <see cref="SimulationOptions.PersonasEnabled"/>, un jeton
    /// <c>simulation-persona-{n}</c> authentifie un persona sans compte Keycloak (développement de l'app).</param>
    /// <param name="mobileAzp">azp posé sur le persona, pour qu'il passe la politique de repli comme l'app mobile.</param>
    public static void Apply(JwtBearerOptions options, bool disableValidation,
        SimulationOptions? simulation = null, string? mobileAzp = null)
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

            if (simulation?.PersonasEnabled == true && PersonaFor(ctx.Request.Headers.Authorization.ToString(), mobileAzp) is { } persona)
            {
                ctx.Principal = persona;
                ctx.Success();
                log.LogInformation("JWT — persona de formation {Sub} sur {Path}.", persona.FindFirst("sub")?.Value, ctx.Request.Path);
            }
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

    /// <summary>
    /// Principal d'un persona de formation, ou null si l'en-tête n'en porte pas. Le persona n'a de droits que
    /// sur le jeu de formation : son personnel est dans la plage réservée, et le filtre de cantonnement
    /// refuse tout identifiant hors de cette plage.
    /// </summary>
    internal static ClaimsPrincipal? PersonaFor(string authorization, string? mobileAzp)
    {
        const string bearer = "Bearer ";
        if (!authorization.StartsWith(bearer + SimulationRequest.PersonaTokenPrefix, StringComparison.OrdinalIgnoreCase)) return null;
        if (!int.TryParse(authorization[(bearer.Length + SimulationRequest.PersonaTokenPrefix.Length)..], out var number)
            || number < 1 || number > 99) return null;

        var claims = new List<Claim>
        {
            new("sub", ClSimulationIds.Persona(number).ToString()),
            new("preferred_username", $"persona-{number}"),
            new("name", ClSimulatedOrdersReadClient.PersonaName(number)),
            new(SimulationRequest.PersonaClaim, "persona"),
        };
        if (!string.IsNullOrWhiteSpace(mobileAzp)) claims.Add(new Claim(ClKeycloakCallers.AzpClaim, mobileAzp));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme, "preferred_username", null));
    }
}
