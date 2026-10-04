using CaSoft.Erp.USVector.Application;
using CaSoft.Framework;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Signature patient (MOB_SIGNATURE, 1:1 mission ERP).
/// Sémantique reprise du ClSignatureRepository legacy (T_SIGNATURE_SIGN) ;
/// Delete, non implémenté côté legacy, est ici réellement supporté (le contrat expose DELETE api/signature).
/// <para>⚖️ L'horodatage est en heure LOCALE, par convention partagée avec la facturation (C62) — voir
/// <see cref="ClHorloge"/>.</para>
/// </summary>
public class SignatureRepository : ISignatureRepository
{
    private readonly MobileDbContext _ctx;

    public SignatureRepository(MobileDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<ClSignatureDtoOut?> FetchAsync(Guid jobId, CancellationToken ct)
    {
        var entity = await _ctx.Signatures.SingleOrDefaultAsync(s => s.SIG_MISSION_ID == jobId, ct);
        return entity?.ToSignatureDto();
    }

    public async Task InsertAsync(Guid gJobId, string strSignData, CancellationToken ct)
    {
        _ctx.Signatures.Add(new MOB_SIGNATURE
        {
            SIG_MISSION_ID = gJobId,
            SIG_DATA = strSignData,
            SIG_DATETIME = ClHorloge.MaintenantLocal()
        });
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Guid gJobId, string strSignData, CancellationToken ct)
    {
        var entity = await _ctx.Signatures.SingleAsync(s => s.SIG_MISSION_ID == gJobId, ct);
        entity.SIG_DATA = strSignData;
        entity.SIG_DATETIME = ClHorloge.MaintenantLocal();
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid gJobId, string strSignData, CancellationToken ct)
    {
        var entity = await _ctx.Signatures.SingleOrDefaultAsync(s => s.SIG_MISSION_ID == gJobId, ct);
        if (entity is null) return;

        _ctx.Signatures.Remove(entity);
        await _ctx.SaveChangesAsync(ct);
    }

    // MOB-8 — Existence légère (clé seule) : alimente le flag MI_SIGNATURE_EXISTS du détail/liste.
    public Task<bool> ExistsAsync(Guid jobId, CancellationToken ct)
        => _ctx.Signatures.AnyAsync(s => s.SIG_MISSION_ID == jobId, ct);

    public async Task<HashSet<Guid>> ExistingForAsync(IEnumerable<Guid> jobIds, CancellationToken ct)
    {
        var ids = jobIds as IReadOnlyCollection<Guid> ?? jobIds.ToList();
        if (ids.Count == 0) return new HashSet<Guid>();

        var signees = await _ctx.Signatures
            .Where(s => ids.Contains(s.SIG_MISSION_ID))
            .Select(s => s.SIG_MISSION_ID)
            .ToListAsync(ct);
        return signees.ToHashSet();
    }
}
