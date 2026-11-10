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
/// Succès → l'entrée est supprimée. Panne → relance avec backoff (aucune perte). Mission inconnue
/// d'Orders (404) → l'entrée est abandonnée : c'est définitif, relancer ne sert à rien.
/// Le debounce (repousser <c>OOB_DISPATCH_AFTER</c> à chaque changement) est fait côté écriture
/// (<see cref="Repositories.Mobile.JobTimeRepository"/>) : ici on ne traite que les entrées dues.
/// <para>
/// PRJ-2 — Quand <see cref="FieldProjectionOptions.Enabled"/> est vrai, la même entrée projette aussi
/// l'état terrain de la signature (<c>PUT missions/{id}/field</c>) : présence et motif « ne peut pas
/// signer ». Coupé par défaut : la route n'existe chez Orders qu'à partir de sa version qui la livre,
/// et un 404 de route serait pris pour une mission inconnue — l'entrée abandonnée, la donnée perdue.
/// </para>
/// </summary>
public sealed class OperationalOutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "OperationalOutboxDispatcher — erreur de cycle.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    /// <summary>Un cycle : traite les entrées dues. <c>internal</c> pour être testé sans attendre le poll.</summary>
    internal async Task DispatchDueAsync(CancellationToken ct)
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
            // Une signature ou un motif peut exister sans aucun jalon : la projection terrain ne dépend pas de la timeline.
            if (mst is null && !_isFieldProjectionEnabled)
            {
                ctx.OperationalOutbox.Remove(ob);   // plus de mission → rien à projeter
                continue;
            }

            try
            {
                var outcome = EnOperationalProjectionOutcome.Applied;

                // État consolidé courant (jalons null inclus → l'effacement/annulation est propagé).
                if (mst is not null)
                    outcome = await erpWrite.ProjectOperationalAsync(
                        ob.OOB_MISSION_ID,
                        mst.MST_ACK_AT, mst.MST_READ_AT, mst.MST_GO_AT,
                        mst.MST_ONSITE_AT, mst.MST_TERMINATED_AT,
                        sourceCrewId: null, ct);

                // PRJ-2 — instantané : signature présente ou non, motif ou rien (un motif retiré s'efface chez Order).
                if (_isFieldProjectionEnabled && outcome != EnOperationalProjectionOutcome.MissionNotFound)
                {
                    var isSigned = await ctx.Signatures.AnyAsync(s => s.SIG_MISSION_ID == ob.OOB_MISSION_ID, ct);
                    var waiver = await ctx.SignatureWaivers.AsNoTracking()
                        .SingleOrDefaultAsync(w => w.SWV_MISSION_ID == ob.OOB_MISSION_ID, ct);
                    outcome = await erpWrite.ProjectFieldAsync(
                        ob.OOB_MISSION_ID, isSigned, waiver?.SWV_REASON_CODE,
                        waiver is null ? null : DateTime.SpecifyKind(waiver.SWV_DECLARED_AT, DateTimeKind.Utc), ct);
                }

                if (outcome == EnOperationalProjectionOutcome.MissionNotFound)
                {
                    // Définitif : la mission n'existe pas (ou plus) chez Orders. La relancer ne la fera pas
                    // réapparaître — une seule entrée a tourné 55 414 fois avant ce correctif (2026-09-13).
                    _logger.LogWarning(
                        "Projection outbox mission {MissionId} abandonnée : mission introuvable chez Orders.Api (404) " +
                        "après {Attempts} tentative(s). Les jalons et la signature de cette mission ne seront pas projetés.",
                        ob.OOB_MISSION_ID, ob.OOB_ATTEMPTS + 1);
                }

                ctx.OperationalOutbox.Remove(ob);   // livré, ou abandonné → retiré de l'outbox
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
