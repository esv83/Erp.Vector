using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// C2 — Jeton du compte de service de Vector, obtenu par <c>client_credentials</c> et conservé jusqu'à
/// l'approche de son expiration. Calqué sur <c>ClServiceAccountTokenProvider</c> de
/// <c>CaSoft.Identity.Client</c>, que Vector ne référence pas : ce paquet exige le socle 2.8.0, Vector
/// est en 2.3.5.
/// </summary>
/// <remarks>
/// <b>Un jeton par processus, pas un par requête</b> : enregistré en singleton. Le demander à chaque
/// appel doublerait les allers-retours et saturerait le realm.
/// </remarks>
public sealed class ServiceAccountTokenProvider
{
    private readonly HttpClient _realm;
    private readonly ServiceAccountOptions _options;
    private readonly ILogger _logger;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    /// <param name="realm">
    /// Client HTTP <b>sans</b> le gestionnaire qui pose le jeton : sinon demander un jeton exigerait un
    /// jeton — une récursion qui ne se voit qu'à l'exécution.
    /// </param>
    public ServiceAccountTokenProvider(
        HttpClient realm,
        ServiceAccountOptions options,
        ILogger<ServiceAccountTokenProvider>? logger = null,
        TimeProvider? clock = null)
    {
        _realm = realm ?? throw new ArgumentNullException(nameof(realm));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = (ILogger?)logger ?? NullLogger.Instance;
        _clock = clock ?? TimeProvider.System;
    }

    public bool IsConfigured => _options.IsConfigured;

    /// <summary>Le jeton courant, renouvelé si besoin ; <c>null</c> sans compte de service configuré.</summary>
    public async Task<string?> GetTokenAsync(CancellationToken ct = default)
    {
        if (!IsConfigured) return null;
        if (IsValid()) return _token;

        await _lock.WaitAsync(ct);
        try
        {
            if (IsValid()) return _token;
            await RenewAsync(ct);
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Oublie le jeton : le prochain appel en demandera un neuf. Appelé sur un 401 — un jeton révoqué
    /// ou un décalage d'horloge produit un refus alors qu'il semble valide ici.
    /// </summary>
    public void Forget()
    {
        _token = null;
        _expiresAt = DateTimeOffset.MinValue;
    }

    private bool IsValid() => _token is not null && _clock.GetUtcNow() < _expiresAt;

    private async Task RenewAsync(CancellationToken ct)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", _options.ClientId!.Trim()),
            new("client_secret", _options.ClientSecret!)
        };
        if (!string.IsNullOrWhiteSpace(_options.Scope))
            fields.Add(new("scope", _options.Scope.Trim()));

        using var content = new FormUrlEncodedContent(fields);
        using var response = await _realm.PostAsync(_options.TokenEndpoint!.Trim(), content, ct);

        if (!response.IsSuccessStatusCode)
            // Le corps n'est pas journalisé : rien ne garantit ce que le realm y met. Le statut et le
            // clientId suffisent au diagnostic.
            throw new InvalidOperationException(
                $"Le realm a refusé le compte de service '{_options.ClientId}' (HTTP {(int)response.StatusCode}). " +
                "Vérifier le secret, l'activation des comptes de service sur ce client, et le point de jeton.");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        if (!root.TryGetProperty("access_token", out var accessToken))
            throw new InvalidOperationException("La réponse du realm ne porte pas d'access_token.");

        // expires_in absent ou illisible : une minute — court, sans risque, et visible au journal.
        var seconds = root.TryGetProperty("expires_in", out var expiresIn) && expiresIn.TryGetInt32(out var read) ? read : 0;
        if (seconds <= 0) seconds = 60;

        var margin = Math.Min(_options.TokenRenewalMarginSeconds, Math.Max(seconds - 1, 0));
        _token = accessToken.GetString();
        _expiresAt = _clock.GetUtcNow().AddSeconds(seconds - margin);

        _logger.LogDebug("Jeton de service obtenu pour {ClientId}, valable jusqu'à {Expiration}.",
            _options.ClientId, _expiresAt);
    }
}
