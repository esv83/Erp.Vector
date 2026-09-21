using Microsoft.Extensions.Http.Resilience;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// DEC-7 — Ce qui empêche un appel à Orders de pendre, et ce qui l'empêche de s'acharner.
/// </summary>
/// <remarks>
/// <para>
/// <b>Le défaut réparé.</b> Les clients vers Orders n'avaient que leur adresse : **100 secondes** de
/// délai par défaut, aucune nouvelle tentative, aucun disjoncteur. Une requête mobile pendait donc
/// jusqu'à ce que l'ambulancier abandonne — et depuis le 19/09, **un seul appel qui pend retient tout
/// un lot de 200 paquets** pour la facturation.
/// </para>
/// <para>
/// <b>Ce qui est retenté, et ce qui ne l'est pas.</b> Seules les pannes : 5xx, 408, coupure réseau,
/// délai dépassé. Un <b>404 n'est jamais retenté</b> — c'est une réponse, pas une panne, et plusieurs
/// lectures s'appuient dessus (l'équipage inconnu rend une liste vide, la mission inconnue sort du
/// lot en <c>NotFound</c>). Un 400 ou un 409 non plus : ce sont des refus métier, qui portent leur
/// motif.
/// </para>
/// <para>
/// <b>Le disjoncteur protège Orders autant que nous.</b> Orders sert aussi la régulation ; quand il
/// tombe, relancer 200 appels de lot ne le relèvera pas. Le disjoncteur ouvre, les appels échouent
/// vite et clairement, et le lot rend <c>Error</c> pour ces missions — au lieu d'une attente muette.
/// </para>
/// <para>
/// <b>Les écritures sont rejouables</b>, et c'est ce qui rend la nouvelle tentative sûre : la
/// projection pousse un instantané complet, le conducteur et le questionnaire s'écrivent en
/// remplacement, la confirmation de prise de service est idempotente côté Orders (un second appel
/// rend « déjà confirmé »).
/// </para>
/// </remarks>
public static class OrdersResilience
{
    /// <summary>Section de configuration : <c>OrdersApi:Resilience</c>.</summary>
    public const string SectionName = "OrdersApi:Resilience";

    /// <summary>
    /// Pose délai, nouvelles tentatives et disjoncteur sur un client vers Orders. Les valeurs se
    /// règlent par configuration ; les défauts valent pour la production.
    /// </summary>
    public static IHttpClientBuilder AddOrdersResilience(this IHttpClientBuilder builder, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        // Délai d'UN essai : au-delà, Orders ne répondra probablement plus mieux en attendant.
        var attempt = TimeSpan.FromSeconds(section.GetValue("AttemptTimeoutSeconds", 8d));
        // Délai TOTAL, tentatives comprises : c'est lui que l'app et le lot subissent.
        var total = TimeSpan.FromSeconds(section.GetValue("TotalTimeoutSeconds", 25d));
        var retries = section.GetValue("MaxRetries", 2);
        var delay = TimeSpan.FromMilliseconds(section.GetValue("RetryDelayMs", 500d));
        // Fenêtre du disjoncteur : au moins deux fois le délai d'un essai (exigé par la bibliothèque).
        var sampling = TimeSpan.FromSeconds(section.GetValue("BreakerSamplingSeconds", 30d));
        var breakDuration = TimeSpan.FromSeconds(section.GetValue("BreakerBreakSeconds", 10d));

        // Garde-fou explicite du client lui-même : sans lui, les 100 s par défaut restent la borne
        // ultime si la configuration devenait incohérente.
        builder.ConfigureHttpClient(c => c.Timeout = total + TimeSpan.FromSeconds(5));

        builder.AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = attempt;
            options.TotalRequestTimeout.Timeout = total;

            options.Retry.MaxRetryAttempts = retries;
            options.Retry.Delay = delay;
            options.Retry.UseJitter = true;   // deux instances ne repartent pas à la même seconde

            options.CircuitBreaker.SamplingDuration = sampling;
            options.CircuitBreaker.BreakDuration = breakDuration;
        });

        return builder;
    }
}
