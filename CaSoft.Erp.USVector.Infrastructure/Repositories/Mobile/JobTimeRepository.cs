using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Timeline opérationnelle d'une mission (MOB_MISSION_STATE, 1:1 ORD_MISSION).
/// Sémantique reprise du ClJobTimeRepository legacy (T_JOB_TIME) : upsert par mission.
/// </summary>
public class JobTimeRepository : IJobTimeRepository
{
    private readonly MobileDbContext _ctx;

    public JobTimeRepository(MobileDbContext ctx)
    {
        _ctx = ctx;
    }

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

        _ctx.SaveChanges();
    }

    public ClJobTimeData? GetJobTimeData(Guid gJobId)
    {
        var entity = _ctx.MissionStates.SingleOrDefault(s => s.MST_MISSION_ID == gJobId);
        return entity?.ToJobTimeData();
    }
}
