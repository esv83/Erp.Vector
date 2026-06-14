using System.Security.Claims;

namespace CaSoft.Erp.Mobile.Api.Infrastructure;

/// <summary>
/// MOB-4a — Extraction de l'identité Keycloak du token porté par la requête.
/// </summary>
public static class MobileCallerExtensions
{
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
}
