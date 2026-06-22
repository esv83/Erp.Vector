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
}
