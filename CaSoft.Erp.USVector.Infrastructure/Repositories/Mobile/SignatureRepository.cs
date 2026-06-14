using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Signature patient (MOB_SIGNATURE, 1:1 mission ERP).
/// Sémantique reprise du ClSignatureRepository legacy (T_SIGNATURE_SIGN) ;
/// Delete, non implémenté côté legacy, est ici réellement supporté (le contrat expose DELETE api/signature).
/// </summary>
public class SignatureRepository : ISignatureRepository
{
    private readonly MobileDbContext _ctx;

    public SignatureRepository(MobileDbContext ctx)
    {
        _ctx = ctx;
    }

    public ClSignatureDto? Fetch(Guid jobId)
    {
        var entity = _ctx.Signatures.SingleOrDefault(s => s.SIG_MISSION_ID == jobId);
        return entity?.ToSignatureDto();
    }

    public void Insert(Guid gJobId, string strSignData)
    {
        _ctx.Signatures.Add(new MOB_SIGNATURE
        {
            SIG_MISSION_ID = gJobId,
            SIG_DATA = strSignData,
            SIG_DATETIME = DateTime.Now
        });
        _ctx.SaveChanges();
    }

    public void Update(Guid gJobId, string strSignData)
    {
        var entity = _ctx.Signatures.Single(s => s.SIG_MISSION_ID == gJobId);
        entity.SIG_DATA = strSignData;
        entity.SIG_DATETIME = DateTime.Now;
        _ctx.SaveChanges();
    }

    public void Delete(Guid gJobId, string strSignData)
    {
        var entity = _ctx.Signatures.SingleOrDefault(s => s.SIG_MISSION_ID == gJobId);
        if (entity is null) return;

        _ctx.Signatures.Remove(entity);
        _ctx.SaveChanges();
    }

    // MOB-8 — Existence légère (clé seule) : alimente le flag MI_SIGNATURE_EXISTS du détail/liste.
    public bool Exists(Guid jobId)
        => _ctx.Signatures.Any(s => s.SIG_MISSION_ID == jobId);

    public HashSet<Guid> ExistingFor(IEnumerable<Guid> jobIds)
    {
        var ids = jobIds as IReadOnlyCollection<Guid> ?? jobIds.ToList();
        if (ids.Count == 0) return new HashSet<Guid>();

        return _ctx.Signatures
            .Where(s => ids.Contains(s.SIG_MISSION_ID))
            .Select(s => s.SIG_MISSION_ID)
            .ToHashSet();
    }
}
