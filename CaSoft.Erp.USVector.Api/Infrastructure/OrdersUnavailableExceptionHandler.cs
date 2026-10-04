using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// Orders ne répond pas → <b>503 ProblemDetails</b>, avec un <c>detail</c> affichable et un
/// <c>Retry-After</c>, au lieu d'un 500 brut.
/// </summary>
/// <remarks>
/// <para>
/// <b>Le défaut réparé.</b> Aucun gestionnaire global : une panne d'Orders remontait en exception non
/// gérée. Du 21/09 au 04/10, <b>~2 600 requêtes en 500</b> — coupures des 22/09 et 02/10, mises à jour
/// d'Orders des 21/09 et 04/10 — dont 72 % sur <c>Crew/mine</c>, l'écran d'entrée de l'ambulancier.
/// Même <c>ShiftConfirmationController</c>, qui promettait un 503, rendait 500 : il n'attrapait que
/// <c>HttpRequestException</c>, et le disjoncteur lève <see cref="BrokenCircuitException"/>.
/// </para>
/// <para>
/// <b>Une panne, pas un refus.</b> Une <c>HttpRequestException</c> n'est une panne que sans statut
/// (réseau, DNS) ou avec un 5xx / 408. Un 4xx d'Orders qui arrive jusqu'ici n'a été traité par aucun
/// appelant : c'est un défaut, il reste un 500 — « réessayez dans une minute » serait un mensonge.
/// </para>
/// </remarks>
public sealed class OrdersUnavailableExceptionHandler : IExceptionHandler
{
    /// <summary>Message destiné à l'ambulancier : l'app peut l'afficher tel quel.</summary>
    public const string Detail =
        "La régulation ne répond pas pour le moment. Réessayez dans une minute.";

    /// <summary>Durée de coupure du disjoncteur par défaut (<see cref="OrdersResilience"/>).</summary>
    public const int RetryAfterSeconds = 10;

    private readonly IProblemDetailsService _problems;
    private readonly ILogger<OrdersUnavailableExceptionHandler> _logger;

    public OrdersUnavailableExceptionHandler(
        IProblemDetailsService problems,
        ILogger<OrdersUnavailableExceptionHandler> logger)
    {
        _problems = problems;
        _logger = logger;
    }

    /// <summary>
    /// Cette exception dit-elle qu'Orders est indisponible ? Public et sans dépendance au pipeline,
    /// pour que le test pose la question directement.
    /// </summary>
    public static bool IsOrdersUnavailable(Exception exception) => exception switch
    {
        BrokenCircuitException => true,
        TimeoutRejectedException => true,
        HttpRequestException { StatusCode: null } => true,
        HttpRequestException { StatusCode: HttpStatusCode.RequestTimeout } => true,
        HttpRequestException { StatusCode: { } status } => (int)status >= 500,
        _ => false
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (!IsOrdersUnavailable(exception))
        {
            return false;
        }

        // Une ligne, pas une pile : la panne est déjà détaillée par le client Orders et par Polly.
        _logger.LogWarning("{Method} {Path} — Orders indisponible, 503 rendu ({Exception} : {Message}).",
            context.Request.Method, context.Request.Path.Value, exception.GetType().Name, exception.Message);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();

        return await _problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service momentanément indisponible",
                Detail = Detail
            }
        });
    }
}
