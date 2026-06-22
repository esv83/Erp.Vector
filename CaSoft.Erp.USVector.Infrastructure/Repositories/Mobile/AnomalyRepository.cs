using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>TRF-8 — Anomalies terrain en BD Mobile (<c>MOB_ANOMALY</c>), rattachées à la mission.</summary>
public class AnomalyRepository : IAnomalyRepository
{
    private readonly MobileDbContext _ctx;

    public AnomalyRepository(MobileDbContext ctx) => _ctx = ctx;

    public void Save(ClAnomaly anomaly)
    {
        _ctx.Anomalies.Add(anomaly.ToEntity());
        _ctx.SaveChanges();
    }

    public IReadOnlyList<ClAnomaly> ListByMission(Guid missionId)
        => _ctx.Anomalies
            .Where(a => a.ANO_MISSION_ID == missionId)
            .OrderByDescending(a => a.ANO_REPORTED_AT)
            .ToList()
            .Select(e => e.ToDomain())
            .ToList();
}
