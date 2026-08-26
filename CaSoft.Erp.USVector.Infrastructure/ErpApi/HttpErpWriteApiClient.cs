using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// Adapter HTTP d'écriture vers Orders.Api (TRF-5). Enregistré via
/// <c>AddHttpClient&lt;IErpWriteApiClient, HttpErpWriteApiClient&gt;</c> (même BaseUrl que la lecture).
/// JSON Web (camelCase) — aligné sur le contrat minimal API d'Orders.Api.
/// </summary>
public sealed class HttpErpWriteApiClient : IErpWriteApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<HttpErpWriteApiClient> _logger;

    public HttpErpWriteApiClient(HttpClient http, ILogger<HttpErpWriteApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task ProjectOperationalAsync(
        Guid missionId,
        DateTime? ackAt, DateTime? readAt, DateTime? goAt,
        DateTime? onsiteAt, DateTime? terminateAt,
        Guid? sourceCrewId, CancellationToken ct = default)
    {
        var body = new
        {
            ack = ackAt,
            read = readAt,
            go = goAt,
            onsite = onsiteAt,
            terminate = terminateAt,
            sourceCrewId
        };
        var response = await _http.PutAsJsonAsync($"missions/{missionId}/operational", body, JsonOptions, ct);
        if (response.IsSuccessStatusCode) return;

        var content = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("Orders.Api PUT missions/{MissionId}/operational a échoué : {Status} {Body}",
            missionId, (int)response.StatusCode, content);
        throw new HttpRequestException($"Orders.Api PUT missions/{missionId}/operational → {(int)response.StatusCode}.");
    }

    public async Task SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default)
    {
        var body = new { driverPersonnelId, from };
        var response = await _http.PutAsJsonAsync($"crews/{crewId}/driver", body, JsonOptions, ct);
        if (response.IsSuccessStatusCode) return;

        var content = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("Orders.Api PUT crews/{CrewId}/driver a échoué : {Status} {Body}",
            crewId, (int)response.StatusCode, content);
        throw new HttpRequestException($"Orders.Api PUT crews/{crewId}/driver → {(int)response.StatusCode}.");
    }

    public async Task<EnContextOrderWriteOutcome> SetMissionContextOrderAsync(
        Guid missionId, int contextOrderId, string? setBy = null, CancellationToken ct = default)
    {
        var body = new { contextOrderId, setBy };
        var response = await _http.PatchAsJsonAsync($"missions/{missionId}/contextOrder", body, JsonOptions, ct);
        if (response.IsSuccessStatusCode) return EnContextOrderWriteOutcome.Applied;

        // 409/400/404 = réponses métier (ProblemDetails), pas des pannes : on les remonte à
        // l'appelant, qui les traduit en 409/400/404 mobile. Journalisées en Warning, pas en Error.
        var outcome = response.StatusCode switch
        {
            HttpStatusCode.Conflict => EnContextOrderWriteOutcome.LockedByRegulator,
            HttpStatusCode.BadRequest => EnContextOrderWriteOutcome.NotApplicable,
            HttpStatusCode.NotFound => EnContextOrderWriteOutcome.MissionNotFound,
            _ => (EnContextOrderWriteOutcome?)null
        };

        var content = await response.Content.ReadAsStringAsync(ct);
        if (outcome.HasValue)
        {
            _logger.LogWarning("Orders.Api PATCH missions/{MissionId}/contextOrder refusé ({Outcome}) : {Status} {Body}",
                missionId, outcome.Value, (int)response.StatusCode, content);
            return outcome.Value;
        }

        _logger.LogError("Orders.Api PATCH missions/{MissionId}/contextOrder a échoué : {Status} {Body}",
            missionId, (int)response.StatusCode, content);
        throw new HttpRequestException($"Orders.Api PATCH missions/{missionId}/contextOrder → {(int)response.StatusCode}.");
    }

    public async Task<ContextOrderValuesWriteResult> SetContextOrderValuesAsync(
        Guid missionId,
        IReadOnlyCollection<(string Name, string? Value)> values,
        string? setBy = null,
        CancellationToken ct = default)
    {
        var body = new { values = values.Select(v => new { name = v.Name, value = v.Value }).ToList(), setBy };
        var response = await _http.PatchAsJsonAsync($"missions/{missionId}/contextOrder/values", body, JsonOptions, ct);
        if (response.IsSuccessStatusCode)
            return new ContextOrderValuesWriteResult(EnContextOrderValuesWriteOutcome.Applied);

        // 409/400/404 = réponses métier (ProblemDetails), pas des pannes.
        var outcome = response.StatusCode switch
        {
            HttpStatusCode.Conflict => EnContextOrderValuesWriteOutcome.FieldLocked,
            HttpStatusCode.BadRequest => EnContextOrderValuesWriteOutcome.Invalid,
            HttpStatusCode.NotFound => EnContextOrderValuesWriteOutcome.MissionNotFound,
            _ => (EnContextOrderValuesWriteOutcome?)null
        };

        var content = await response.Content.ReadAsStringAsync(ct);
        if (outcome.HasValue)
        {
            // Le corps reste journalisé en entier : le motif extrait part vers le mobile, la trace
            // garde de quoi diagnostiquer si l'extraction elle-même se met à rendre Nothing.
            _logger.LogWarning("Orders.Api PATCH missions/{MissionId}/contextOrder/values refusé ({Outcome}) : {Status} {Body}",
                missionId, outcome.Value, (int)response.StatusCode, content);
            return new ContextOrderValuesWriteResult(outcome.Value, ReadProblemDetail(content));
        }

        _logger.LogError("Orders.Api PATCH missions/{MissionId}/contextOrder/values a échoué : {Status} {Body}",
            missionId, (int)response.StatusCode, content);
        throw new HttpRequestException($"Orders.Api PATCH missions/{missionId}/contextOrder/values → {(int)response.StatusCode}.");
    }

    /// <summary>
    /// Motif affichable d'un refus d'Orders.Api : le <c>detail</c> de son ProblemDetails (RFC 7807),
    /// à défaut son <c>title</c>. <c>null</c> si le corps n'est pas exploitable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Rien ne lève ici.</b> Un corps vide, tronqué ou non-JSON est un cas ordinaire — Orders.Api
    /// n'est pas le seul à pouvoir répondre sur ce chemin (un reverse proxy peut rendre du HTML sur
    /// un 400). Le refus, lui, est acquis : le perdre parce que sa formulation est illisible
    /// remplacerait un message imparfait par une exception, ce qui serait strictement pire.
    /// </para>
    /// <para>
    /// <c>title</c> en repli reste utile : « Conflit d'état » n'explique pas la règle, mais dit au
    /// moins que la saisie a été <b>refusée</b> — c'est déjà ce qui manquait à l'ambulancier.
    /// </para>
    /// </remarks>
    private static string? ReadProblemDetail(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            foreach (var name in new[] { "detail", "title" })
            {
                if (doc.RootElement.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
                }
            }
        }
        catch (JsonException)
        {
            // Corps non-JSON : pas de motif, et surtout pas de panne.
        }

        return null;
    }
}
