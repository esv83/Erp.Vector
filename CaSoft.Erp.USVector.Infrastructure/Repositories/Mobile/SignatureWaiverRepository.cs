using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Motif « le patient ne peut pas signer » (MOB_012) : la liste des motifs et la déclaration par
/// mission (1:1). Toute écriture arme la projection vers Order dans la même transaction (PRJ-1).
/// </summary>
public sealed class SignatureWaiverRepository : ISignatureWaiverRepository
{
    private readonly MobileDbContext _ctx;

    public SignatureWaiverRepository(MobileDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<ClSignatureWaiverReason>> ListActiveReasonsAsync(CancellationToken ct)
    {
        var reasons = await _ctx.SignatureWaiverReasons.AsNoTracking()
            .Where(r => r.SWR_ACTIVE)
            .OrderBy(r => r.SWR_INDEX)
            .ToListAsync(ct);
        return reasons.Select(r => r.ToDomain()).ToList();
    }

    public async Task<ClSignatureWaiverReason?> GetReasonAsync(string code, CancellationToken ct)
    {
        var reason = await _ctx.SignatureWaiverReasons.AsNoTracking().SingleOrDefaultAsync(r => r.SWR_CODE == code, ct);
        return reason?.ToDomain();
    }

    public async Task<ClSignatureWaiver?> GetAsync(Guid missionId, CancellationToken ct)
    {
        var waiver = await _ctx.SignatureWaivers.AsNoTracking().SingleOrDefaultAsync(w => w.SWV_MISSION_ID == missionId, ct);
        return waiver?.ToDomain();
    }

    public async Task SaveAsync(ClSignatureWaiver waiver, CancellationToken ct)
    {
        var entity = await _ctx.SignatureWaivers.SingleOrDefaultAsync(w => w.SWV_MISSION_ID == waiver.MissionId, ct);
        if (entity is null)
        {
            entity = new MOB_SIGNATURE_WAIVER { SWV_MISSION_ID = waiver.MissionId };
            _ctx.SignatureWaivers.Add(entity);
        }

        entity.Apply(waiver);

        await ProjectionOutbox.EnqueueAsync(_ctx, waiver.MissionId, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid missionId, CancellationToken ct)
    {
        var entity = await _ctx.SignatureWaivers.SingleOrDefaultAsync(w => w.SWV_MISSION_ID == missionId, ct);
        if (entity is null) return;

        _ctx.SignatureWaivers.Remove(entity);
        await ProjectionOutbox.EnqueueAsync(_ctx, missionId, ct);
        await _ctx.SaveChangesAsync(ct);
    }
}
