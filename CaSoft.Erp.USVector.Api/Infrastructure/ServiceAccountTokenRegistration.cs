using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Identity.Client;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// C2 — Pose le jeton du compte de service de Vector sur un client HTTP, avec le gestionnaire du
/// paquet partagé <c>CaSoft.Identity.Client</c> (<c>AddCaSoftServiceAccountToken</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Pourquoi une traduction et pas la section <c>Identity</c> directement.</b> Le compte de
/// Vector est configuré sous <c>OrdersApi:ServiceAccount</c> — en production, le secret vit dans le
/// <c>web.config</c> du serveur sous ce nom. Lire la section du paquet imposerait de retoucher ce
/// <c>web.config</c> à la main le jour de la publication, sous peine de voir le jeton disparaître
/// sans erreur. La traduction garde la configuration telle qu'elle est.
/// </para>
/// <para>
/// <b>Inerte tant que le compte est incomplet</b>, comme l'était la version locale. Le paquet
/// s'active dès que <c>TokenEndpoint</c> est renseigné ; or Vector le déduit de
/// <c>Keycloak:Authority</c>, donc toujours. Sans ce filtre, un poste de dev au secret de
/// remplacement (<c>__SET_VIA_ENV__</c>) demanderait un jeton à chaque appel et journaliserait
/// une erreur à chaque fois.
/// </para>
/// </remarks>
public static class ServiceAccountTokenRegistration
{
    /// <summary>
    /// Réglages du paquet (section <c>Identity</c>) tirés du compte de Vector. Vide quand le compte
    /// est incomplet : le gestionnaire est alors inerte, les appels partent sans jeton.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> ToIdentityClientSettings(ServiceAccountOptions account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var settings = new Dictionary<string, string?>();
        if (!account.IsConfigured) return settings;

        var section = ClIdentityClientOptions.SectionName;
        settings[$"{section}:{nameof(ClIdentityClientOptions.TokenEndpoint)}"] = account.TokenEndpoint;
        settings[$"{section}:{nameof(ClIdentityClientOptions.ClientId)}"] = account.ClientId;
        settings[$"{section}:{nameof(ClIdentityClientOptions.ClientSecret)}"] = account.ClientSecret;
        settings[$"{section}:{nameof(ClIdentityClientOptions.TokenRenewalMarginSeconds)}"] =
            account.TokenRenewalMarginSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(account.Scope))
            settings[$"{section}:{nameof(ClIdentityClientOptions.Scope)}"] = account.Scope;

        return settings;
    }

    /// <summary>
    /// Pose le jeton du compte de service de Vector sur les appels de ce client. Inerte si le
    /// compte est incomplet ; un realm indisponible ne bloque pas l'appel ; un 401 oublie le jeton.
    /// </summary>
    public static IHttpClientBuilder AddVectorServiceAccountToken(this IHttpClientBuilder builder, ServiceAccountOptions account)
        => builder.AddCaSoftServiceAccountToken(
            new ConfigurationBuilder().AddInMemoryCollection(ToIdentityClientSettings(account)).Build());
}
