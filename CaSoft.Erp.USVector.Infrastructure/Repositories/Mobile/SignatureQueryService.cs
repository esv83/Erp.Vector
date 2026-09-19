using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// B5 — Signatures de plusieurs missions en une requête (<c>MOB_SIGNATURE</c>, clé = mission).
/// </summary>
public sealed class SignatureQueryService : ISignatureQueryService
{
    private readonly MobileDbContext _ctx;

    public SignatureQueryService(MobileDbContext ctx) => _ctx = ctx;

    public IReadOnlyList<ClSignatureBatchItemDtoOut> ReadMany(IReadOnlyCollection<Guid> missionIds)
    {
        var ids = (missionIds ?? Array.Empty<Guid>()).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<ClSignatureBatchItemDtoOut>();

        var signatures = _ctx.Signatures.AsNoTracking()
            .Where(s => ids.Contains(s.SIG_MISSION_ID))
            .Select(s => new { s.SIG_MISSION_ID, s.SIG_DATETIME, s.SIG_DATA })
            .ToList()
            .ToDictionary(s => s.SIG_MISSION_ID);

        return ids.Select(id => signatures.TryGetValue(id, out var s)
                ? new ClSignatureBatchItemDtoOut
                {
                    MissionId = id,
                    Status = ClSignatureBatchItemDtoOut.StatusFound,
                    SignedAt = s.SIG_DATETIME,
                    Data = s.SIG_DATA
                }
                : new ClSignatureBatchItemDtoOut { MissionId = id, Status = ClSignatureBatchItemDtoOut.StatusNotFound })
            .ToList();
    }
}
