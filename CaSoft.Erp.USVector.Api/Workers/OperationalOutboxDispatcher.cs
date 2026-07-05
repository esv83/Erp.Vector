using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Api.Workers;

/// <summary>
/// Dispatcher de l'Outbox de projection opérationnelle → synchro régulation GARANTIE.
/// Poll périodique : pour chaque mission dont le délai de debounce est écoulé, projette l'état
/// <b>consolidé</b> (MOB_MISSION_STATE) vers Orders.Api (<c>PUT missions/{id}/operational</c>).
/// Succès → l'entrée est supprimée. Échec → relance avec backoff (aucune perte).
/// Le debounce (repousser <c>OOB_DISPATCH_AFTER</c> à chaque changement) est fait côté écriture
/// (<see cref="Repositories.Mobile.JobTimeRepository"/>) : ici on ne traite que les entrées dues.
/// </summary>
public sealed class OperationalOutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationalOutboxDispatcher> _logger;

    public OperationalOutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OperationalOutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OperationalOutboxDispatcher démarré (poll {Interval}s).", PollInterval.TotalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchDueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OperationalOutboxDispatcher — erreur de cycle.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task DispatchDueAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MobileDbContext>();
        var erpWrite = scope.ServiceProvider.GetRequiredService<IErpWriteApiClient>();

        var now = DateTime.UtcNow;
        var due = await ctx.OperationalOutbox
            .Where(o => o.OOB_DISPATCH_AFTER <= now)
            .OrderBy(o => o.OOB_DISPATCH_AFTER)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        foreach (var ob in due)
        {
            var mst = await ctx.MissionStates.SingleOrDefaultAsync(s => s.MST_MISSION_ID == ob.OOB_MISSION_ID, ct);
            if (mst is null)
            {
                ctx.OperationalOutbox.Remove(ob);   // plus de mission → rien à projeter
                continue;
            }

            try
            {
                // État consolidé courant (jalons null inclus → l'effacement/annulation est propagé).
                await erpWrite.ProjectOperationalAsync(
                    ob.OOB_MISSION_ID,
                    mst.MST_ACK_AT, mst.MST_READ_AT, mst.MST_GO_AT,
                    mst.MST_ONSITE_AT, mst.MST_TERMINATED_AT,
                    sourceCrewId: null, ct);

                ctx.OperationalOutbox.Remove(ob);   // livré → retiré de l'outbox
            }
            catch (Exception ex)
            {
                ob.OOB_ATTEMPTS += 1;
                ob.OOB_LAST_ERROR = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                var backoff = Math.Min(60, Math.Pow(2, Math.Min(ob.OOB_ATTEMPTS, 6)));   // 2,4,…,60 s
                ob.OOB_DISPATCH_AFTER = now.AddSeconds(backoff);
                ob.OOB_UPDATED_AT = now;
                _logger.LogWarning(ex,
                    "Projection outbox mission {MissionId} échouée (tentative {Attempts}) — relance dans {Backoff}s.",
                    ob.OOB_MISSION_ID, ob.OOB_ATTEMPTS, backoff);
            }
        }

        await ctx.SaveChangesAsync(ct);
    }
}
