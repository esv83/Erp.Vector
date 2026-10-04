using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>TRF-8 — Anomalies terrain en BD Mobile (<c>MOB_ANOMALY</c>), rattachées à la mission.</summary>
public class AnomalyRepository : IAnomalyRepository
{
    private readonly MobileDbContext _ctx;

    public AnomalyRepository(MobileDbContext ctx) => _ctx = ctx;

    public async Task SaveAsync(ClAnomaly anomaly, CancellationToken ct)
    {
        _ctx.Anomalies.Add(anomaly.ToEntity());
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ClAnomaly>> ListByMissionAsync(Guid missionId, CancellationToken ct)
    {
        var lignes = await _ctx.Anomalies
            .Where(a => a.ANO_MISSION_ID == missionId)
            .OrderByDescending(a => a.ANO_REPORTED_AT)
            .ToListAsync(ct);
        return lignes.Select(e => e.ToDomain()).ToList();
    }
}
