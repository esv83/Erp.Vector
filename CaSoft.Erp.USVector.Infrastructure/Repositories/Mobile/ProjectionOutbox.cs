using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// PRJ-1 (voie « paquet consolidé ») — arme la projection d'une mission vers Order. Une ligne par
/// mission dans <c>MOB_OPERATIONAL_OUTBOX</c>, quelle que soit la nature du changement : le dispatcher
/// relit l'état consolidé de tous les silos au moment de l'envoi.
/// </summary>
/// <remarks>
/// <b>À appeler AVANT le <c>SaveChangesAsync</c> du changement</b> : l'entrée part dans la même
/// transaction, c'est ce qui garantit qu'aucun changement n'est perdu. Debounce : chaque changement
/// repousse l'envoi, le dispatcher projette après la rafale.
/// </remarks>
internal static class ProjectionOutbox
{
    /// <summary>Calme exigé avant d'envoyer (coalesce les manipulations rapprochées).</summary>
    internal const int DebounceSeconds = 5;

    internal static async Task EnqueueAsync(MobileDbContext ctx, Guid missionId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var dispatchAfter = now.AddSeconds(DebounceSeconds);

        // Local d'abord : deux écritures dans la même unité de travail ne doivent pas poser deux lignes.
        var ob = ctx.OperationalOutbox.Local.SingleOrDefault(o => o.OOB_MISSION_ID == missionId)
                 ?? await ctx.OperationalOutbox.SingleOrDefaultAsync(o => o.OOB_MISSION_ID == missionId, ct);

        if (ob is null)
        {
            ctx.OperationalOutbox.Add(new MOB_OPERATIONAL_OUTBOX
            {
                OOB_MISSION_ID = missionId,
                OOB_DISPATCH_AFTER = dispatchAfter,
                OOB_ATTEMPTS = 0,
                OOB_UPDATED_AT = now
            });
            return;
        }

        // Nouveau changement : on repousse (debounce) et on relance le compteur de tentatives.
        ob.OOB_DISPATCH_AFTER = dispatchAfter;
        ob.OOB_ATTEMPTS = 0;
        ob.OOB_LAST_ERROR = null;
        ob.OOB_UPDATED_AT = now;
    }
}
