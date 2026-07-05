using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// Adapter HTTP d'Orders.Api. Enregistré via <c>AddHttpClient&lt;IErpReadApiClient, HttpErpReadApiClient&gt;</c>
/// avec <c>BaseAddress = OrdersApi:BaseUrl</c>. JSON Web (camelCase, enums entiers) — aligné sur Orders.Api.
/// </summary>
public sealed class HttpErpReadApiClient : IErpReadApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<HttpErpReadApiClient> _logger;

    public HttpErpReadApiClient(HttpClient http, ILogger<HttpErpReadApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ErpMissionFullDto?> GetMissionFullAsync(Guid missionId, CancellationToken ct = default)
        => await GetOrNullAsync<ErpMissionFullDto>($"missions/{missionId}/full", ct);

    public async Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(
        DateTime from, DateTime to, int take, CancellationToken ct = default)
    {
        var url = $"missions?from={Uri.EscapeDataString(from.ToString("o"))}"
                + $"&to={Uri.EscapeDataString(to.ToString("o"))}"
                + $"&unassignedOnly=false&includeCancelled=false&take={take}";
        var list = await _http.GetFromJsonAsync<List<ErpMissionListItemDto>>(url, JsonOptions, ct);
        return list ?? new List<ErpMissionListItemDto>(0);
    }

    public async Task<ErpOrderEditDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
        => await GetOrNullAsync<ErpOrderEditDto>($"orders/{orderId}", ct);

    public async Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid beneficiaryId, CancellationToken ct = default)
        => await GetOrNullAsync<ErpBeneficiaryDetailDto>($"beneficiaries/{beneficiaryId}", ct);

    public async Task<IReadOnlyList<Guid>> ListCrewIdsAsync(
        Guid personnelId, DateOnly onDate, int take, CancellationToken ct = default)
    {
        var url = $"crews?personnelId={personnelId}&date={onDate:yyyy-MM-dd}&take={take}";
        // 404 = aucun équipage (cf. JobListController : crews vide ⇒ joblist vide, pas une erreur).
        var response = await _http.GetAsync(url, ct);
        if (response.StatusCode is HttpStatusCode.NotFound) return Array.Empty<Guid>();
        await EnsureSuccessAsync(response, $"GET {url}", ct);
        var list = await response.Content.ReadFromJsonAsync<List<ErpCrewListItemDto>>(JsonOptions, ct);
        return list is null ? Array.Empty<Guid>() : list.Select(c => c.Id).ToList();
    }

    public async Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default)
        => await GetOrNullAsync<ErpCrewFullDto>($"crews/{crewId}", ct);

    public async Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid keycloakSub, CancellationToken ct = default)
    {
        // sub Keycloak → PER_ID via PER_KEYCLOAK_MAP (Orders.Api). 404 → null (compte non rattaché).
        var response = await _http.GetAsync($"personnel/by-keycloak/{keycloakSub}", ct);
        if (response.StatusCode is HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, $"GET personnel/by-keycloak/{keycloakSub}", ct);
        return await response.Content.ReadFromJsonAsync<Guid?>(JsonOptions, ct);
    }

    public async Task<int?> GetMissionTransferStatusAsync(Guid missionId, CancellationToken ct = default)
    {
        var dto = await GetOrNullAsync<ErpMissionStatusDto>($"missions/{missionId}", ct);
        return dto?.TransferStatus;
    }

    private async Task<T?> GetOrNullAsync<T>(string url, CancellationToken ct) where T : class
    {
        var response = await _http.GetAsync(url, ct);
        if (response.StatusCode is HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, $"GET {url}", ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string what, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("Orders.Api {What} a échoué : {Status} {Body}", what, (int)response.StatusCode, body);
        throw new HttpRequestException($"Orders.Api {what} → {(int)response.StatusCode}.");
    }
}
