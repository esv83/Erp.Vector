using Microsoft.AspNetCore.Authorization;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// C2 — Qui peut appeler Vector avec un jeton Keycloak, et où. Trois familles d'appelants, distinguées
/// par l'<c>azp</c> (le client qui a obtenu le jeton) :
/// <list type="bullet">
///   <item><b>le client mobile</b> (<c>Keycloak:ClientId</c>) — toutes les routes protégées ;</item>
///   <item><b>les modules de service</b> (<c>Keycloak:AllowedAzp</c>, la facturation) — seulement les
///   routes qui portent <see cref="ServiceOrMobilePolicy"/> ou <see cref="MutuelleCardReadPolicy"/> ;</item>
///   <item><b>les écrans</b> (<c>Keycloak:FrontClientIds</c>, régulation et certification, 04/10) — seulement
///   la lecture de la carte mutuelle, <see cref="MutuelleCardReadPolicy"/>.</item>
/// </list>
/// <para>
/// <b>L'admission</b> (jeton émis pour l'un de ces clients) est jugée par le socle,
/// <c>AddCaSoftKeycloakJwtBearer</c>, sur les mêmes clés (Itération 11). Cette classe ne porte plus que
/// <b>l'autorisation</b> propre à Vector : qui entre sur quelles routes.
/// </para>
/// <para>
/// ⚠️ Accepter un jeton à l'authentification ne suffit pas pour entrer : la politique de repli exige
/// l'azp <b>mobile</b>. Sans cette séparation, le jeton de la facturation ou d'un écran ouvrirait
/// toutes les routes du terrain — joblist, formulaires, identité des patients.
/// </para>
/// </summary>
public static class ClKeycloakCallers
{
    /// <summary>Claim Keycloak du client émetteur (lu brut : <c>MapInboundClaims = false</c>).</summary>
    public const string AzpClaim = "azp";

    /// <summary>
    /// Routes tirées serveur-à-serveur (paquet terrain, octets de signature, documents, carte
    /// mutuelle) : client mobile <b>ou</b> module de service déclaré. Posée à la fermeture des sorties
    /// anonymes, quand la facturation présentera son jeton.
    /// </summary>
    public const string ServiceOrMobilePolicy = "AppelantServiceOuMobile";

    /// <summary>
    /// Lecture de la carte mutuelle — présence, métadonnées, image — par le mobile, un module de
    /// service <b>ou un écran</b> (régulation, certification). Aucune route d'écriture ne la porte :
    /// <c>AnonymousSurfaceTests</c> fige la liste.
    /// </summary>
    public const string MutuelleCardReadPolicy = "LecteurCarteMutuelle";

    /// <summary>Le client mobile (<c>Keycloak:ClientId</c>, le « front » du socle).</summary>
    public static string? ReadMobileAzp(IConfiguration configuration)
        => configuration["Keycloak:ClientId"]?.Trim();

    /// <summary>Modules de service autorisés (<c>Keycloak:AllowedAzp</c>), sans vide ni doublon.</summary>
    public static IReadOnlyCollection<string> ReadServiceAzp(IConfiguration configuration)
        => ReadAzpList(configuration, "Keycloak:AllowedAzp");

    /// <summary>
    /// Écrans autorisés à lire la carte (<c>Keycloak:FrontClientIds</c>, les « autres fronts » du socle).
    /// Vide : la politique de lecture se réduit au mobile et aux services.
    /// </summary>
    public static IReadOnlyCollection<string> ReadScreenAzp(IConfiguration configuration)
        => ReadAzpList(configuration, "Keycloak:FrontClientIds");

    private static IReadOnlyCollection<string> ReadAzpList(IConfiguration configuration, string section)
        => configuration.GetSection(section).Get<string[]>()?
               .Where(azp => !string.IsNullOrWhiteSpace(azp))
               .Select(azp => azp.Trim())
               .Distinct(StringComparer.Ordinal)
               .ToArray()
           ?? Array.Empty<string>();

    /// <summary>Politique de repli : authentifié <b>et</b> émis pour le client mobile.</summary>
    public static AuthorizationPolicy MobileOnly(string mobileAzp)
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(AzpClaim, mobileAzp)
            .Build();

    /// <summary>Voir <see cref="ServiceOrMobilePolicy"/>.</summary>
    public static AuthorizationPolicy ServiceOrMobile(string mobileAzp, IReadOnlyCollection<string> serviceAzp)
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(AzpClaim, new[] { mobileAzp }.Concat(serviceAzp).Distinct(StringComparer.Ordinal))
            .Build();

    /// <summary>Voir <see cref="MutuelleCardReadPolicy"/>.</summary>
    public static AuthorizationPolicy MutuelleCardRead(
        string mobileAzp, IReadOnlyCollection<string> serviceAzp, IReadOnlyCollection<string> screenAzp)
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(AzpClaim, new[] { mobileAzp }.Concat(serviceAzp).Concat(screenAzp).Distinct(StringComparer.Ordinal))
            .Build();
}
