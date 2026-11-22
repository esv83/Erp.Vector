using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Infrastructure.Simulation;

namespace CaSoft.Erp.USVector.Api.Workers;

/// <summary>
/// Purge de la nuit du mode formation : efface de la base Mobile les saisies de formation des jours passés,
/// et oublie l'état correspondant du faux Orders. Enregistré seulement si <c>Simulation:Enabled</c>.
/// <para>
/// Regarde toutes les 30 minutes, purge une fois par jour passé <see cref="SimulationOptions.PurgeHour"/>.
/// Ne touche jamais aux saisies du jour : un redémarrage du pool en pleine formation peut relancer la purge
/// sans rien effacer de ce qui est en cours.
/// </para>
/// </summary>
public sealed class SimulationPurgeWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly ClSimulationState _state;
    private readonly SimulationOptions _options;
    private readonly ILogger<SimulationPurgeWorker> _logger;
    private DateOnly _lastPurge;

    public SimulationPurgeWorker(IServiceScopeFactory scopes, ClSimulationState state, SimulationOptions options,
        ILogger<SimulationPurgeWorker> logger)
    {
        _scopes = scopes;
        _state = state;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            if (now.Hour >= _options.PurgeHour && _lastPurge != today)
            {
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    var deleted = await scope.ServiceProvider.GetRequiredService<ClSimulationMobileStore>()
                        .PurgeBeforeAsync(today, stoppingToken);
                    _state.Forget(id => ClSimulationIds.IsSimulated(id) && ClSimulationIds.DayOf(id) < today);
                    _lastPurge = today;
                    _logger.LogInformation("Mode formation : purge de la nuit, {Deleted} ligne(s) effacée(s).", deleted);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Retentée au prochain tour (30 min) : une base injoignable ne doit pas arrêter l'API.
                    _logger.LogError(ex, "Mode formation : la purge de la nuit a échoué, nouvel essai dans 30 minutes.");
                }
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
