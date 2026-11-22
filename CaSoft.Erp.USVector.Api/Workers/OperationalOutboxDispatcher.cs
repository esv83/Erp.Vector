using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Api.Workers;

/// <summary>
/// Boucle de l'outbox de projection opérationnelle → synchro régulation GARANTIE. Toutes les
/// <see cref="PollInterval"/>, un cycle de <see cref="ClProjectionOutboxDispatcher"/> (Infrastructure) :
/// jalons vers <c>PUT missions/{id}/operational</c>, et état terrain de la signature vers
/// <c>PUT missions/{id}/field</c> quand <see cref="FieldProjectionOptions.Enabled"/> est vrai (PRJ-2,
/// allumé le 22/11). Le worker ne touche plus la base (AR-001).
/// </summary>
public sealed class OperationalOutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationalOutboxDispatcher> _logger;
    private readonly bool _isFieldProjectionEnabled;

    public OperationalOutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OperationalOutboxDispatcher> logger,
        FieldProjectionOptions? fieldProjection = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _isFieldProjectionEnabled = fieldProjection?.Enabled ?? false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OperationalOutboxDispatcher démarré (poll {Interval}s, projection signature {Field}).",
            PollInterval.TotalSeconds, _isFieldProjectionEnabled ? "active" : "coupée");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchDueAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "OperationalOutboxDispatcher — erreur de cycle.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    /// <summary>
    /// Un cycle, dans sa propre portée : le travail est celui de <see cref="ClProjectionOutboxDispatcher"/>
    /// (AR-001, 22/11). <c>internal</c> pour être testé sans attendre le poll.
    /// </summary>
    internal async Task DispatchDueAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ClProjectionOutboxDispatcher>()
            .DispatchDueAsync(_isFieldProjectionEnabled, ct);
    }
}
