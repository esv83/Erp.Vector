using System.Security.Claims;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// MOB-4a — Extraction de l'identité Keycloak du token porté par la requête.
/// </summary>
public static class MobileCallerExtensions
{
    /// <summary>
    /// Clé <see cref="HttpContext.Items"/> où le pipeline JWT (event
    /// <c>OnAuthenticationFailed</c>) dépose la raison de rejet d'un token
    /// (expiré, signature, issuer/audience…). Lue ensuite par les controllers
    /// pour renvoyer une erreur explicite.
    /// </summary>
    public const string JwtErrorKey = "jwt_error";

    /// <summary>
    /// Identifiant Keycloak (claim <c>sub</c>) du porteur du token, ou <c>null</c>
    /// si non authentifié / claim absent ou non-Guid. JwtBearer mappe par défaut
    /// <c>sub</c> → <see cref="ClaimTypes.NameIdentifier"/> : on regarde les deux.
    /// </summary>
    public static Guid? GetKeycloakSubject(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst("sub")?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(raw, out var sub) ? sub : null;
    }

    /// <summary>
    /// Raison de rejet du token JWT déposée par <c>OnAuthenticationFailed</c>,
    /// ou <c>null</c> si le token n'a pas été rejeté (absent, ou validé).
    /// </summary>
    public static string? GetJwtError(this HttpContext context)
        => context.Items.TryGetValue(JwtErrorKey, out var v) ? v as string : null;

    /// <summary>Le porteur a-t-il fourni un header <c>Authorization</c> ?</summary>
    public static bool HasAuthorizationHeader(this HttpContext context)
        => context.Request.Headers.Authorization.Count > 0;
}
