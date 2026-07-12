using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace CaSoft.Erp.USVector.Infrastructure.Diagnostics;

/// <summary>
/// Client Admin Keycloak minimal (DEV-ONLY, support de l'outil de diag) : résout un username en
/// utilisateur(s) Keycloak (dont l'<c>id</c> = le <c>sub</c>). S'authentifie en <b>service account</b>
/// (client_credentials) — le client doit avoir les rôles realm-management <c>view-users</c>/<c>query-users</c>.
/// Config : <c>Keycloak:Authority</c> (https://host/realms/{realm}), <c>Keycloak:AdminClientId</c>,
/// <c>Keycloak:AdminClientSecret</c> (secret externalisé, jamais commité).
/// </summary>
public sealed class KeycloakAdminClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public KeycloakAdminClient(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_cfg["Keycloak:Authority"]) &&
        !string.IsNullOrWhiteSpace(_cfg["Keycloak:AdminClientId"]) &&
        !string.IsNullOrWhiteSpace(_cfg["Keycloak:AdminClientSecret"]) &&
        _cfg["Keycloak:AdminClientSecret"] != "__SET_VIA_ENV__";

    public async Task<IReadOnlyList<KeycloakUser>> FindUsersByUsernameAsync(string username, CancellationToken ct = default)
    {
        var (serverRoot, realm) = ParseAuthority(_cfg["Keycloak:Authority"]!);
        var token = await GetAdminTokenAsync(serverRoot, realm, ct);

        var url = $"{serverRoot}/admin/realms/{realm}/users?username={Uri.EscapeDataString(username)}&exact=false&max=20";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"GET users → {(int)res.StatusCode} {res.ReasonPhrase}.");

        return await res.Content.ReadFromJsonAsync<List<KeycloakUser>>(cancellationToken: ct) ?? new();
    }

    private async Task<string> GetAdminTokenAsync(string serverRoot, string realm, CancellationToken ct)
    {
        var tokenUrl = $"{serverRoot}/realms/{realm}/protocol/openid-connect/token";
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _cfg["Keycloak:AdminClientId"]!,
            ["client_secret"] = _cfg["Keycloak:AdminClientSecret"]!
        });

        using var res = await _http.PostAsync(tokenUrl, form, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Token service account → {(int)res.StatusCode} {res.ReasonPhrase} (client/secret ou rôles realm-management ?).");

        var doc = await res.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        return doc?.access_token ?? throw new InvalidOperationException("Token Keycloak sans access_token.");
    }

    // authority = https://host/realms/{realm}  →  (serverRoot="https://host", realm="{realm}")
    private static (string serverRoot, string realm) ParseAuthority(string authority)
    {
        const string marker = "/realms/";
        var i = authority.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            throw new InvalidOperationException("Keycloak:Authority doit être de la forme https://host/realms/{realm}.");
        return (authority[..i], authority[(i + marker.Length)..].Trim('/'));
    }

    private sealed class TokenResponse
    {
        public string? access_token { get; set; }
    }
}

public sealed class KeycloakUser
{
    public string Id { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool Enabled { get; set; }
}
