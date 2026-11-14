using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// J-144 / contact — relais vers la <b>bordure d'Order</b> (<c>OrdersEdge:BaseUrl</c>, chemins
/// <c>vector/missions/{id}/beneficiary/…</c>), avec le jeton de service de Vector. Order juge
/// l'appelant (<c>azp</c>), l'auteur (<c>setBy</c> dans l'équipage affecté) et le délai (deux jours).
/// <para>
/// Les refus du contrat (400, 403, 404, 409) remontent en issue métier avec le <c>detail</c> d'Order ;
/// tout le reste lève avec son statut : un 5xx devient 503 par le gestionnaire commun, un 401 (notre
/// jeton de service refusé) reste une erreur serveur — l'ambulancier n'y est pour rien.
/// </para>
/// </summary>
public sealed class HttpBeneficiaryFieldEntryService : IBeneficiaryFieldEntryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<HttpBeneficiaryFieldEntryService> _logger;

    public HttpBeneficiaryFieldEntryService(HttpClient http, ILogger<HttpBeneficiaryFieldEntryService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<ClBeneficiaryEntryResult> RecordIdentityAsync(
        Guid missionId, ClBeneficiaryIdentityDtoIn identity, string setBy, CancellationToken ct)
        => PutAsync(missionId, "identity", new
        {
            lastName = identity.LastName,
            firstName = identity.FirstName,
            birthDate = string.IsNullOrWhiteSpace(identity.BirthDate) ? null : identity.BirthDate,
            gender = identity.Gender,
            setBy
        }, ct);

    public Task<ClBeneficiaryEntryResult> RecordContactAsync(
        Guid missionId, ClBeneficiaryContactDtoIn contact, string setBy, CancellationToken ct)
        => PutAsync(missionId, "contact", new { phone = contact.Phone, email = contact.Email, setBy }, ct);

    private async Task<ClBeneficiaryEntryResult> PutAsync(Guid missionId, string geste, object body, CancellationToken ct)
    {
        var path = $"vector/missions/{missionId}/beneficiary/{geste}";
        using var response = await _http.PutAsJsonAsync(path, body, JsonOptions, ct);
        if (response.IsSuccessStatusCode) return ClBeneficiaryEntryResult.Applied();

        var outcome = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => EnBeneficiaryEntryOutcome.Invalid,
            HttpStatusCode.Forbidden => EnBeneficiaryEntryOutcome.Forbidden,
            HttpStatusCode.NotFound => EnBeneficiaryEntryOutcome.NotFound,
            HttpStatusCode.Conflict => EnBeneficiaryEntryOutcome.Locked,
            _ => (EnBeneficiaryEntryOutcome?)null
        };

        var content = await response.Content.ReadAsStringAsync(ct);
        if (outcome.HasValue)
        {
            // Le corps peut nommer le patient : seul le motif extrait est journalisé.
            var reason = HttpErpWriteApiClient.ReadProblemDetail(content);
            _logger.LogWarning("Bordure Order PUT {Path} refusé ({Outcome}) : {Status} {Reason}",
                path, outcome.Value, (int)response.StatusCode, reason);
            return ClBeneficiaryEntryResult.Refused(outcome.Value, reason);
        }

        _logger.LogError("Bordure Order PUT {Path} a échoué : {Status}", path, (int)response.StatusCode);
        throw new HttpRequestException($"Bordure Order PUT {path} → {(int)response.StatusCode}.", null, response.StatusCode);
    }
}
