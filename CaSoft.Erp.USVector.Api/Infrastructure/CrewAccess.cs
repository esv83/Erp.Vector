using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// Garde-fou d'accès équipage, mutualisé par tous les endpoints crew-scoped (<c>/{crewId}</c>).
/// Centralise le pipeline token Keycloak → personnel ERP → équipages actifs, ce qui :
/// <list type="bullet">
///   <item>évite le boilerplate JWT copié dans chaque controller ;</item>
///   <item><b>empêche structurellement l'UI de cibler un équipage qui n'est pas le sien</b> :
///   la validation est garantie serveur, l'app ne peut pas se tromper (dépendance minimale au dev web).</item>
/// </list>
/// Les méthodes renvoient un <see cref="IActionResult"/> d'erreur (401/403/404) prêt à retourner,
/// ou <c>null</c> si l'accès est accordé.
/// </summary>
public static class CrewAccess
{
    /// <summary>
    /// Résout le personnel depuis le token (sans exiger d'équipage). <c>null</c> = OK (<paramref name="personnelId"/> posé).
    /// </summary>
    public static IActionResult? ResolvePersonnel(ControllerBase ctrl, IMobileIdentityResolver identity, out Guid personnelId)
    {
        personnelId = Guid.Empty;

        var http = ctrl.HttpContext;
        var jwtError = http.GetJwtError();
        if (jwtError is not null)
            return ctrl.Unauthorized($"Token rejeté par l'authentification Keycloak : {jwtError}");

        if (!http.HasAuthorizationHeader())
            return ctrl.Unauthorized("Aucun token d'authentification fourni (header « Authorization: Bearer … » absent).");

        var sub = ctrl.User.GetKeycloakSubject();
        if (sub is null)
        {
            // Piège de déploiement : si Keycloak est désactivé côté serveur (Keycloak:Enabled=false),
            // le pipeline JWT n'est pas branché → le token n'est JAMAIS décodé → aucun claim « sub »,
            // alors même qu'un header Authorization est présent. Le message « claim sub absent » est
            // alors trompeur : le vrai problème est la config serveur, pas le token du client.
            var keycloakEnabled = http.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetValue("Keycloak:Enabled", false);
            if (!keycloakEnabled)
                return ctrl.StatusCode(500,
                    "Authentification Keycloak désactivée côté serveur (Keycloak:Enabled=false) : le token n'est pas "
                    + "décodé, le claim « sub » est donc introuvable. Activer Keycloak sur le serveur "
                    + "(variable Keycloak__Enabled=true, ou appsettings.{Environnement}.json).");

            return ctrl.Unauthorized("Token valide mais claim « sub » (identifiant Keycloak) absent ou non-Guid.");
        }

        var pid = identity.ResolvePersonnelId(sub.Value);
        if (pid is null)
            return ctrl.StatusCode(403, $"Compte Keycloak {sub.Value} non rattaché à un personnel. Contactez la régulation.");

        personnelId = pid.Value;
        return null;
    }

    /// <summary>
    /// Autorise l'accès à <paramref name="crewId"/> : vérifie qu'il fait partie des équipages actifs
    /// du personnel aujourd'hui. <c>null</c> = accès accordé.
    /// </summary>
    public static IActionResult? Authorize(ControllerBase ctrl, IMobileIdentityResolver identity, Guid crewId)
    {
        var error = ResolvePersonnel(ctrl, identity, out var personnelId);
        if (error is not null) return error;

        var activeCrews = identity.ResolveActiveCrewIds(personnelId, DateOnly.FromDateTime(DateTime.Now));
        if (!activeCrews.Contains(crewId))
            return ctrl.StatusCode(403,
                $"L'équipage {crewId} ne fait pas partie de vos équipages actifs aujourd'hui.");

        return null;
    }
}
