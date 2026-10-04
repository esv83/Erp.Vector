using Microsoft.AspNetCore.Authorization;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// C2 — Qui peut appeler Vector avec un jeton Keycloak, et où. Trois familles d'appelants, distinguées
/// par l'<c>azp</c> (le client qui a obtenu le jeton) :
/// <list type="bullet">
///   <item><b>le client mobile</b> (<c>Keycloak:Audience</c>) — toutes les routes protégées ;</item>
///   <item><b>les modules de service</b> (<c>Keycloak:ServiceAzp</c>, la facturation) — seulement les
///   routes qui portent <see cref="ServiceOrMobilePolicy"/> ou <see cref="MutuelleCardReadPolicy"/> ;</item>
///   <item><b>les écrans</b> (<c>Keycloak:ScreenAzp</c>, régulation et certification, 04/10) — seulement
///   la lecture de la carte mutuelle, <see cref="MutuelleCardReadPolicy"/>.</item>
/// </list>
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

    /// <summary>Clients de service autorisés (<c>Keycloak:ServiceAzp</c>), sans vide ni doublon.</summary>
    public static IReadOnlyCollection<string> ReadServiceAzp(IConfiguration configuration)
        => ReadAzpList(configuration, "Keycloak:ServiceAzp");

    /// <summary>
    /// Écrans autorisés à lire la carte (<c>Keycloak:ScreenAzp</c>). Vide par défaut : la politique de
    /// lecture se réduit alors au mobile et aux services, comme avant.
    /// </summary>
    public static IReadOnlyCollection<string> ReadScreenAzp(IConfiguration configuration)
        => ReadAzpList(configuration, "Keycloak:ScreenAzp");

    private static IReadOnlyCollection<string> ReadAzpList(IConfiguration configuration, string section)
        => configuration.GetSection(section).Get<string[]>()?
               .Where(azp => !string.IsNullOrWhiteSpace(azp))
               .Select(azp => azp.Trim())
               .Distinct(StringComparer.Ordinal)
               .ToArray()
           ?? Array.Empty<string>();

    /// <summary>Le jeton a-t-il été émis pour un appelant connu ?</summary>
    public static bool IsAccepted(string? azp, string? mobileAzp, IReadOnlyCollection<string> serviceAzp)
        => !string.IsNullOrEmpty(azp)
           && (string.Equals(azp, mobileAzp, StringComparison.Ordinal) || serviceAzp.Contains(azp, StringComparer.Ordinal));

    /// <summary>Complément du message de refus : les clients de service acceptés, s'il y en a.</summary>
    public static string DescribeServices(IReadOnlyCollection<string> serviceAzp)
        => serviceAzp.Count == 0 ? string.Empty : $" ou l'un de : {string.Join(", ", serviceAzp)}";

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
