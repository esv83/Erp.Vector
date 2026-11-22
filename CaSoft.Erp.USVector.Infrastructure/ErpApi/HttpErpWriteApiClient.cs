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

    public Task<EnOperationalProjectionOutcome> ProjectOperationalAsync(
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
        return PutProjectionAsync(missionId, "operational", body, isRefusalFinal: false, ct);
    }

    public Task<EnOperationalProjectionOutcome> ProjectFieldAsync(
        Guid missionId, bool signatureExists, string? waiverCode, DateTime? waiverAtUtc, CancellationToken ct = default)
    {
        var body = new
        {
            signatureExists,
            waiver = waiverCode is null ? null : new { code = waiverCode, at = waiverAtUtc }
        };
        // DR-001 — un refus métier de l'état terrain est définitif : abandonné, jamais relancé.
        return PutProjectionAsync(missionId, "field", body, isRefusalFinal: true, ct);
    }

    /// <summary>
    /// <c>PUT missions/{id}/{resource}</c>, commun aux deux projections de l'outbox. 404 = mission inconnue
    /// d'Orders : définitif, pas une panne — l'outbox abandonne l'entrée au lieu de la relancer sans fin
    /// (55 414 tentatives constatées le 2026-09-13 sur une mission). Avec <paramref name="isRefusalFinal"/>,
    /// un 400/409 est un refus métier : erreur au journal avec le motif d'Order — c'est un état que Vector
    /// n'aurait pas dû produire, ou un statut qui ment (04/10) ; quelqu'un doit le lire.
    /// </summary>
    private async Task<EnOperationalProjectionOutcome> PutProjectionAsync(
        Guid missionId, string resource, object body, bool isRefusalFinal, CancellationToken ct)
    {
        var operation = $"PUT missions/{missionId}/{resource}";
        var response = await _http.PutAsJsonAsync($"missions/{missionId}/{resource}", body, JsonOptions, ct);
        if (response.IsSuccessStatusCode) return EnOperationalProjectionOutcome.Applied;

        var content = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Orders.Api {Operation} : mission introuvable (404) {Body}", operation, content);
            return EnOperationalProjectionOutcome.MissionNotFound;
        }

        if (isRefusalFinal && response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            _logger.LogError("Orders.Api {Operation} refusé ({Status}) : {Reason} — état terrain non projeté.",
                operation, (int)response.StatusCode, ReadProblemDetail(content) ?? content);
            return EnOperationalProjectionOutcome.Refused;
        }

        throw Failure(operation, response, content);
    }

    public async Task<CrewDriverWriteResult> SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default)
    {
        var body = new { driverPersonnelId, from };
        var response = await _http.PutAsJsonAsync($"crews/{crewId}/driver", body, JsonOptions, ct);
        if (response.IsSuccessStatusCode)
            return new CrewDriverWriteResult(EnCrewDriverWriteOutcome.Applied);

        // 400/409/404 = réponses métier (ProblemDetails), pas des pannes. Constaté le 2026-09-13 :
        // « La vacation s'est terminée le 13/09/2026 à 18:00 : on ne peut pas y désigner un conducteur
        // après. » partait en ERROR, puis au mobile sous la forme « Orders.Api PUT … → 400. » —
        // l'ambulancier réessayait cinq fois en 35 s sans savoir pourquoi.
        var outcome = response.StatusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.Conflict => EnCrewDriverWriteOutcome.Refused,
            HttpStatusCode.NotFound => EnCrewDriverWriteOutcome.CrewNotFound,
            _ => (EnCrewDriverWriteOutcome?)null
        };

        var content = await response.Content.ReadAsStringAsync(ct);
        if (outcome.HasValue)
        {
            _logger.LogWarning("Orders.Api PUT crews/{CrewId}/driver refusé ({Outcome}) : {Status} {Body}",
                crewId, outcome.Value, (int)response.StatusCode, content);
            return new CrewDriverWriteResult(outcome.Value, ReadProblemDetail(content));
        }

        throw Failure($"PUT crews/{crewId}/driver", response, content);
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

        throw Failure($"PATCH missions/{missionId}/contextOrder", response, content);
    }

    public async Task<ContextOrderValuesWriteResult> SetContextOrderValuesAsync(
        Guid missionId,
        IReadOnlyCollection<(string Name, string? Value, string? OtherText)> values,
        string? setBy = null,
        CancellationToken ct = default)
    {
        var body = new { values = values.Select(v => new { name = v.Name, value = v.Value, otherText = v.OtherText }).ToList(), setBy };
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

        throw Failure($"PATCH missions/{missionId}/contextOrder/values", response, content);
    }

    /// <summary>
    /// Panne d'Orders.Api (ni succès, ni réponse métier) : journalisée en erreur avec le corps, rendue
    /// en <see cref="HttpRequestException"/> porteuse du statut — l'appelant la lève, l'outbox la relance.
    /// </summary>
    private HttpRequestException Failure(string operation, HttpResponseMessage response, string content)
    {
        _logger.LogError("Orders.Api {Operation} a échoué : {Status} {Body}", operation, (int)response.StatusCode, content);
        return new HttpRequestException($"Orders.Api {operation} → {(int)response.StatusCode}.", null, response.StatusCode);
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
    internal static string? ReadProblemDetail(string? content)
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
