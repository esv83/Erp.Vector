using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

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
    private readonly MobileDbContext _ctx;

    public JobTimeRepository(MobileDbContext ctx) => _ctx = ctx;

    public async Task SaveAsync(Guid gJobId, ClJobTimeData timeData, CancellationToken ct)
    {
        var entity = await _ctx.MissionStates.SingleOrDefaultAsync(s => s.MST_MISSION_ID == gJobId, ct);

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
        // Debounce : la projection est repoussée de ProjectionOutbox.DebounceSeconds à chaque nouveau changement ;
        // le worker projette l'état consolidé après la rafale.
        await ProjectionOutbox.EnqueueAsync(_ctx, gJobId, ct);

        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<ClJobTimeData?> GetJobTimeDataAsync(Guid gJobId, CancellationToken ct)
    {
        var entity = await _ctx.MissionStates.SingleOrDefaultAsync(s => s.MST_MISSION_ID == gJobId, ct);
        return entity?.ToJobTimeData();
    }
}
