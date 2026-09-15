using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// C2 — Pose le jeton du compte de service de Vector sur les appels à Orders.Api. Sans compte
/// configuré, il laisse passer la requête telle quelle : c'est l'état d'aujourd'hui.
/// </summary>
/// <remarks>
/// <b>Un jeton indisponible ne bloque pas l'appel.</b> Orders reste anonyme pour le terrain : faire
/// tomber la joblist parce que le realm ne répond pas serait une panne créée de toutes pièces. L'échec
/// est journalisé en erreur ; le jour où Orders exigera le jeton, l'appel sera refusé en 401 et le
/// journal dira pourquoi.
/// </remarks>
public sealed class ServiceAccountTokenHandler : DelegatingHandler
{
    private readonly ServiceAccountTokenProvider _tokens;
    private readonly ILogger<ServiceAccountTokenHandler> _logger;

    public ServiceAccountTokenHandler(ServiceAccountTokenProvider tokens, ILogger<ServiceAccountTokenHandler> logger)
    {
        _tokens = tokens;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_tokens.IsConfigured && request.Headers.Authorization is null)
        {
            try
            {
                var token = await _tokens.GetTokenAsync(ct);
                if (token is not null)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Jeton de service de Vector indisponible — {Method} {Uri} envoyé sans jeton.",
                    request.Method, request.RequestUri);
            }
        }

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized && request.Headers.Authorization is not null)
            _tokens.Forget();

        return response;
    }
}
