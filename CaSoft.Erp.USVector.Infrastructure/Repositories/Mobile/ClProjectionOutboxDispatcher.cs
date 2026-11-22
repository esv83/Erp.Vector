using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Framework.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Un cycle de l'outbox de projection vers Orders : chaque mission due part avec son état
/// <b>consolidé</b> — jalons (<c>PUT missions/{id}/operational</c>) et, si allumé, état terrain de la
/// signature (<c>PUT missions/{id}/field</c>, PRJ-2).
/// <list type="bullet">
///   <item>livrée → l'entrée est retirée ;</item>
///   <item>mission inconnue d'Orders (404) ou refus métier (400/409, DR-001) → retirée, dit au journal :
///   c'est définitif, relancer ne sert à rien ;</item>
///   <item>panne (5xx, réseau) → gardée, relancée avec un délai croissant (aucune perte).</item>
/// </list>
/// Le debounce (repousser <c>OOB_DISPATCH_AFTER</c> à chaque changement) est fait à l'écriture
/// (<see cref="ProjectionOutbox"/>) : ici on ne traite que les entrées dues.
/// </summary>
/// <remarks>
/// AR-001 / DR-002 (22/11) — sorti du worker de la couche Api, qui lisait le <c>DbContext</c> et
/// l'enregistrait lui-même : la lecture est ici, la validation passe par l'<see cref="IUnitOfWork"/>,
/// une fois par cycle.
/// </remarks>
public sealed class ClProjectionOutboxDispatcher
{
    private const int BatchSize = 50;
    private const int LastErrorMaxLength = 500;
    private const int MaxBackoffSeconds = 60;

    private readonly MobileDbContext _ctx;
    private readonly IErpWriteApiClient _erpWrite;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClProjectionOutboxDispatcher> _logger;

    public ClProjectionOutboxDispatcher(MobileDbContext ctx, IErpWriteApiClient erpWrite, IUnitOfWork unitOfWork,
        ILogger<ClProjectionOutboxDispatcher> logger)
    {
        _ctx = ctx;
        _erpWrite = erpWrite;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>Traite les entrées dues, puis valide une fois.</summary>
    public async Task DispatchDueAsync(bool isFieldProjectionEnabled, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var due = await _ctx.OperationalOutbox
            .Where(o => o.OOB_DISPATCH_AFTER <= now)
            .OrderBy(o => o.OOB_DISPATCH_AFTER)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        foreach (var entry in due)
        {
            var missionState = await _ctx.MissionStates.SingleOrDefaultAsync(s => s.MST_MISSION_ID == entry.OOB_MISSION_ID, ct);
            // Une signature ou un motif peut exister sans aucun jalon : la projection terrain ne dépend pas de la timeline.
            if (missionState is null && !isFieldProjectionEnabled)
            {
                _ctx.OperationalOutbox.Remove(entry);   // plus de mission → rien à projeter
                continue;
            }

            try
            {
                var outcome = await ProjectEntryAsync(entry.OOB_MISSION_ID, missionState, isFieldProjectionEnabled, ct);
                LogIfAbandoned(entry, outcome);
                _ctx.OperationalOutbox.Remove(entry);   // livré, ou abandonné → retiré de l'outbox
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                ScheduleRetry(entry, now, ex);
            }
        }

        await _unitOfWork.CommitAsync(ct);
    }

    /// <summary>
    /// Jalons (si la mission en a), puis état terrain (PRJ-2) tant qu'Orders connaît la mission. Rend
    /// <c>MissionNotFound</c> dès qu'une des deux projections l'a constaté.
    /// </summary>
    private async Task<EnOperationalProjectionOutcome> ProjectEntryAsync(
        Guid missionId, MOB_MISSION_STATE? missionState, bool isFieldProjectionEnabled, CancellationToken ct)
    {
        var outcome = EnOperationalProjectionOutcome.Applied;

        // État consolidé courant (jalons null inclus → l'effacement/annulation est propagé).
        if (missionState is not null)
            outcome = await _erpWrite.ProjectOperationalAsync(
                missionId,
                missionState.MST_ACK_AT, missionState.MST_READ_AT, missionState.MST_GO_AT,
                missionState.MST_ONSITE_AT, missionState.MST_TERMINATED_AT,
                sourceCrewId: null, ct);

        if (isFieldProjectionEnabled && outcome != EnOperationalProjectionOutcome.MissionNotFound)
            outcome = await ProjectFieldStateAsync(missionId, ct);

        return outcome;
    }

    /// <summary>PRJ-2 — instantané : signature présente ou non, motif ou rien (un motif retiré s'efface chez Order).</summary>
    private async Task<EnOperationalProjectionOutcome> ProjectFieldStateAsync(Guid missionId, CancellationToken ct)
    {
        var isSigned = await _ctx.Signatures.AnyAsync(s => s.SIG_MISSION_ID == missionId, ct);
        var waiver = (await _ctx.SignatureWaivers.AsNoTracking()
            .SingleOrDefaultAsync(w => w.SWV_MISSION_ID == missionId, ct))?.ToDomain();
        return await _erpWrite.ProjectFieldAsync(missionId, isSigned, waiver?.ReasonCode, waiver?.DeclaredAt, ct);
    }

    /// <summary>Une entrée retirée sans avoir été livrée se dit au journal : 404 (mission inconnue) ou refus métier (DR-001).</summary>
    private void LogIfAbandoned(MOB_OPERATIONAL_OUTBOX entry, EnOperationalProjectionOutcome outcome)
    {
        if (outcome == EnOperationalProjectionOutcome.MissionNotFound)
        {
            // Définitif : la mission n'existe pas (ou plus) chez Orders. La relancer ne la fera pas
            // réapparaître — une seule entrée a tourné 55 414 fois avant ce correctif (2026-09-13).
            _logger.LogWarning(
                "Projection outbox mission {MissionId} abandonnée : mission introuvable chez Orders.Api (404) " +
                "après {Attempts} tentative(s). Les jalons et la signature de cette mission ne seront pas projetés.",
                entry.OOB_MISSION_ID, entry.OOB_ATTEMPTS + 1);
        }
        else if (outcome == EnOperationalProjectionOutcome.Refused)
        {
            // Refus métier d'Order (motif journalisé par le client) : définitif, jamais relancé.
            _logger.LogError(
                "Projection outbox mission {MissionId} abandonnée : état terrain refusé par Orders.Api (400/409). " +
                "Les jalons sont projetés ; la signature et le motif ne le sont pas.",
                entry.OOB_MISSION_ID);
        }
    }

    /// <summary>Panne (5xx, réseau) : l'entrée reste dans l'outbox, relancée avec un délai croissant (2, 4, … 60 s).</summary>
    private void ScheduleRetry(MOB_OPERATIONAL_OUTBOX entry, DateTime now, Exception ex)
    {
        entry.OOB_ATTEMPTS += 1;
        entry.OOB_LAST_ERROR = ex.Message.Length > LastErrorMaxLength ? ex.Message[..LastErrorMaxLength] : ex.Message;
        var backoffSeconds = Math.Min(MaxBackoffSeconds, Math.Pow(2, Math.Min(entry.OOB_ATTEMPTS, 6)));
        entry.OOB_DISPATCH_AFTER = now.AddSeconds(backoffSeconds);
        entry.OOB_UPDATED_AT = now;
        _logger.LogWarning(ex,
            "Projection outbox mission {MissionId} échouée (tentative {Attempts}) — relance dans {Backoff}s.",
            entry.OOB_MISSION_ID, entry.OOB_ATTEMPTS, backoffSeconds);
    }
}
