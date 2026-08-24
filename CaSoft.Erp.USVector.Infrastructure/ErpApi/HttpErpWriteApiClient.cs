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
}
