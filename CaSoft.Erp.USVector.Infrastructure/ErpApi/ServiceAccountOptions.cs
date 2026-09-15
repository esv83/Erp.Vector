namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// C2 — Compte de service de Vector auprès d'Orders.Api (section <c>OrdersApi:ServiceAccount</c>).
/// Incomplet, il est <b>inerte</b> : les appels partent sans jeton, comme avant.
/// </summary>
/// <remarks>
/// Côté Keycloak, le client doit être confidentiel, en <c>Service accounts enabled</c>. Le secret ne
/// vit jamais dans un fichier suivi : <c>web.config</c> du serveur, variable
/// <c>OrdersApi__ServiceAccount__ClientSecret</c>.
/// </remarks>
public sealed class ServiceAccountOptions
{
    public const string SectionName = "OrdersApi:ServiceAccount";

    /// <summary>Valeur de remplacement des fichiers suivis : vaut « non configuré ».</summary>
    public const string Placeholder = "__SET_VIA_ENV__";

    /// <summary>Point de jeton du realm. Vide : déduit de <c>Keycloak:Authority</c> au démarrage.</summary>
    public string? TokenEndpoint { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// <summary>Portée demandée au jeton, facultative.</summary>
    public string? Scope { get; set; }

    /// <summary>Le jeton est renouvelé ce délai avant son expiration.</summary>
    public int TokenRenewalMarginSeconds { get; set; } = 60;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TokenEndpoint)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && ClientSecret != Placeholder;
}
