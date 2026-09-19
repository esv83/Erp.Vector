using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    // Projection nommée, SANS DOC_CONTENT : la liste de l'app n'en affiche que les métadonnées, et
    // chargeait jusqu'au 19/09 le contenu de chaque document pour rien. Les octets se servent un par
    // un, par GetById. Même principe que FieldDataQueryService et la carte mutuelle.
    public IReadOnlyList<ClDocument> ListByMission(Guid missionId)
        => _ctx.Documents.AsNoTracking()
            .Where(d => d.DOC_MISSION_ID == missionId)
            .OrderByDescending(d => d.DOC_CAPTURED_AT)
            .Select(d => new ClDocument
            {
                Id = d.DOC_ID,
                MissionId = d.DOC_MISSION_ID,
                Category = (EnDocumentCategory)d.DOC_CATEGORY,
                ContentType = d.DOC_CONTENT_TYPE,
                ByteSize = d.DOC_BYTE_SIZE,
                FileName = d.DOC_FILE_NAME,
                CapturedAt = d.DOC_CAPTURED_AT,
                CapturedCrewId = d.DOC_CAPTURED_CREW_ID
            })
            .ToList();

    public ClDocument? GetById(Guid documentId)
        => _ctx.Documents.SingleOrDefault(d => d.DOC_ID == documentId)?.ToDomain();
}
