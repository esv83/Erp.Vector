using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// Confirmation de prise de service, relayée vers Orders.Api. Enregistré via
/// <c>AddHttpClient&lt;IShiftConfirmationService, HttpShiftConfirmationService&gt;</c> (même BaseUrl
/// qu'<see cref="IErpReadApiClient"/>). JSON Web (camelCase) — contrat d'Orders.Api.
/// </summary>
/// <remarks>
/// <para>
/// ⚖️ <b>Client à part, et non deux méthodes de plus sur les clients ERP</b> : ces interfaces ont six
/// doublures dans les tests, qu'une méthode ajoutée casserait toutes pour une fonctionnalité qui ne les
/// concerne pas.
/// </para>
/// <para>
/// 🔴 <b>Aucune route appelée ici ne délivre de jeton.</b> Redélivrer le lien (<c>…/link</c>) tuerait
/// celui du courriel et inscrirait un faux « Envoi » : c'est exactement ce que ce chemin évite.
/// </para>
/// </remarks>
public sealed class HttpShiftConfirmationService : IShiftConfirmationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<HttpShiftConfirmationService> _logger;

    public HttpShiftConfirmationService(HttpClient http, ILogger<HttpShiftConfirmationService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ClMyShiftConfirmationsDtoOut> GetMineAsync(Guid personnelId, CancellationToken ct)
    {
        var url = $"personnel/{personnelId}/shift-confirmations/pending";
        var response = await _http.GetAsync(url, ct);

        // 404 = la route n'existe pas encore sur l'instance d'Orders.Api (déployée avant elle) : rien à
        // afficher plutôt qu'une application qui échoue à son lancement. Tracé, pour ne pas le taire.
        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Orders.Api GET {Url} → 404 : route absente ? Aucune confirmation affichée.", url);
            return new ClMyShiftConfirmationsDtoOut();
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Orders.Api GET {Url} a échoué : {Status} {Body}", url, (int)response.StatusCode, body);
            throw new HttpRequestException($"Orders.Api GET {url} → {(int)response.StatusCode}.");
        }

        var list = await response.Content.ReadFromJsonAsync<List<ErpPendingShiftConfirmationDto>>(JsonOptions, ct)
                   ?? new List<ErpPendingShiftConfirmationDto>();

        return new ClMyShiftConfirmationsDtoOut
        {
            HasPending = list.Count > 0,
            Pending = list.Select(p => new ClPendingShiftConfirmationDtoOut
            {
                RequestId = p.RequestId,
                CrewId = p.CrewId,
                CrewLabel = p.CrewLabel,
                ProposedLocalTime = p.ProposedLocalTime
            }).ToList()
        };
    }

    public async Task<ClShiftConfirmationConfirmResult> ConfirmAsync(
        Guid crewId, Guid requestId, Guid personnelId, CancellationToken ct)
    {
        var url = $"crews/{crewId}/shift-confirmations/{requestId}/confirm";
        var response = await _http.PostAsJsonAsync(url, new { personnelId }, JsonOptions, ct);

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<ErpShiftConfirmedDto>(JsonOptions, ct);
            return new ClShiftConfirmationConfirmResult
            {
                Outcome = dto?.AlreadyConfirmed == true
                    ? EnShiftConfirmationConfirmOutcome.AlreadyConfirmed
                    : EnShiftConfirmationConfirmOutcome.Confirmed,
                ProposedLocalTime = dto?.ProposedLocalTime
            };
        }

        // 404 / 400 / 409 = réponses métier (ProblemDetails), pas des pannes.
        var outcome = response.StatusCode switch
        {
            HttpStatusCode.NotFound => EnShiftConfirmationConfirmOutcome.NotFound,
            HttpStatusCode.BadRequest or HttpStatusCode.Conflict => EnShiftConfirmationConfirmOutcome.Refused,
            _ => (EnShiftConfirmationConfirmOutcome?)null
        };

        var content = await response.Content.ReadAsStringAsync(ct);
        if (outcome.HasValue)
        {
            _logger.LogWarning("Orders.Api POST {Url} refusé ({Outcome}) : {Status} {Body}",
                url, outcome.Value, (int)response.StatusCode, content);
            return new ClShiftConfirmationConfirmResult
            {
                Outcome = outcome.Value,
                Reason = ReadProblemDetail(content)
            };
        }

        _logger.LogError("Orders.Api POST {Url} a échoué : {Status} {Body}", url, (int)response.StatusCode, content);
        throw new HttpRequestException($"Orders.Api POST {url} → {(int)response.StatusCode}.");
    }

    /// <summary>
    /// Motif affichable d'un refus d'Orders.Api : <c>detail</c> du ProblemDetails, à défaut <c>title</c>.
    /// <c>null</c> si le corps n'est pas exploitable — le refus reste acquis, seul son libellé manque.
    /// Rien ne lève : un proxy peut répondre du HTML sur ce chemin.
    /// </summary>
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

    /// <summary>Miroir de <c>ClPendingShiftConfirmationDtoOut</c> d'Orders.Api.</summary>
    private sealed class ErpPendingShiftConfirmationDto
    {
        public Guid RequestId { get; set; }
        public Guid CrewId { get; set; }
        public string? CrewLabel { get; set; }
        public DateTime ProposedLocalTime { get; set; }
        public int Status { get; set; }
        public DateTime? SentAtUtc { get; set; }
    }

    /// <summary>Miroir de <c>ClShiftConfirmationByLinkDtoOut</c> d'Orders.Api.</summary>
    private sealed class ErpShiftConfirmedDto
    {
        public string? FirstName { get; set; }
        public DateTime ProposedLocalTime { get; set; }
        public bool Confirmed { get; set; }
        public bool AlreadyConfirmed { get; set; }
    }
}
