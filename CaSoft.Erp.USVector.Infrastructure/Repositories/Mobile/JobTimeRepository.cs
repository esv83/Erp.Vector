using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Timeline opérationnelle d'une mission (MOB_MISSION_STATE, 1:1 ORD_MISSION) : upsert par mission.
/// La projection vers Orders (régulation) n'est plus synchrone/best-effort : chaque changement
/// inscrit une entrée d'<b>Outbox</b> dans la même transaction (aucune perte), et le
/// <see cref="OperationalOutboxDispatcher"/> projette l'état consolidé après un délai de debounce,
/// avec relance jusqu'à succès → <b>synchro régulation garantie</b>.
/// </summary>
public class JobTimeRepository : IJobTimeRepository
{
    // Debounce : on projette l'état consolidé après N s de calme (coalesce les manipulations rapprochées).
    private const int DebounceSeconds = 5;

    private readonly MobileDbContext _ctx;

    public JobTimeRepository(MobileDbContext ctx) => _ctx = ctx;

    public void Save(Guid gJobId, ClJobTimeData timeData)
    {
        var entity = _ctx.MissionStates.SingleOrDefault(s => s.MST_MISSION_ID == gJobId);

        if (entity is null)
        {
            entity = new MOB_MISSION_STATE { MST_MISSION_ID = gJobId };
            entity.ApplyJobTimeData(timeData);
            _ctx.MissionStates.Add(entity);
        }
        else
        {
            entity.ApplyJobTimeData(timeData);
        }

        // Marque la mission à projeter — MÊME transaction que le changement de jalon (zéro perte).
        // Debounce : la projection est repoussée de DebounceSeconds à chaque nouveau changement ;
        // le worker projette l'état consolidé après la rafale.
        EnqueueProjection(gJobId);

        _ctx.SaveChanges();
    }

    private void EnqueueProjection(Guid gJobId)
    {
        var now = DateTime.UtcNow;
        var dispatchAfter = now.AddSeconds(DebounceSeconds);
        var ob = _ctx.OperationalOutbox.SingleOrDefault(o => o.OOB_MISSION_ID == gJobId);

        if (ob is null)
        {
            _ctx.OperationalOutbox.Add(new MOB_OPERATIONAL_OUTBOX
            {
                OOB_MISSION_ID = gJobId,
                OOB_DISPATCH_AFTER = dispatchAfter,
                OOB_ATTEMPTS = 0,
                OOB_UPDATED_AT = now
            });
        }
        else
        {
            // Nouveau changement : on repousse (debounce) et on relance le compteur de tentatives.
            ob.OOB_DISPATCH_AFTER = dispatchAfter;
            ob.OOB_ATTEMPTS = 0;
            ob.OOB_LAST_ERROR = null;
            ob.OOB_UPDATED_AT = now;
        }
    }

    public ClJobTimeData? GetJobTimeData(Guid gJobId)
    {
        var entity = _ctx.MissionStates.SingleOrDefault(s => s.MST_MISSION_ID == gJobId);
        return entity?.ToJobTimeData();
    }
}
