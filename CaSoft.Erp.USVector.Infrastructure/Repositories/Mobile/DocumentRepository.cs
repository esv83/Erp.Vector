using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>TRF-10 — Documents terrain en BD Mobile (<c>MOB_DOCUMENT</c>), rattachés à la mission.</summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly MobileDbContext _ctx;

    public DocumentRepository(MobileDbContext ctx) => _ctx = ctx;

    public void Save(ClDocument document)
    {
        _ctx.Documents.Add(document.ToEntity());
        _ctx.SaveChanges();
    }

    public IReadOnlyList<ClDocument> ListByMission(Guid missionId)
        => _ctx.Documents
            .Where(d => d.DOC_MISSION_ID == missionId)
            .OrderByDescending(d => d.DOC_CAPTURED_AT)
            .ToList()
            .Select(e => e.ToDomain())
            .ToList();

    public ClDocument? GetById(Guid documentId)
        => _ctx.Documents.SingleOrDefault(d => d.DOC_ID == documentId)?.ToDomain();
}
