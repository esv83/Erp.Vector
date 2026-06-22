using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Mapping;

/// <summary>Mapping Infrastructure : entité EF ↔ métier (document terrain).</summary>
internal static class DocumentMappings
{
    public static MOB_DOCUMENT ToEntity(this ClDocument d) => new()
    {
        DOC_ID = d.Id,
        DOC_MISSION_ID = d.MissionId,
        DOC_CATEGORY = (int)d.Category,
        DOC_CONTENT = d.Content,
        DOC_CONTENT_TYPE = d.ContentType,
        DOC_BYTE_SIZE = d.ByteSize,
        DOC_FILE_NAME = d.FileName,
        DOC_CAPTURED_AT = d.CapturedAt,
        DOC_CAPTURED_CREW_ID = d.CapturedCrewId,
    };

    public static ClDocument ToDomain(this MOB_DOCUMENT e) => new()
    {
        Id = e.DOC_ID,
        MissionId = e.DOC_MISSION_ID,
        Category = (EnDocumentCategory)e.DOC_CATEGORY,
        Content = e.DOC_CONTENT,
        ContentType = e.DOC_CONTENT_TYPE,
        ByteSize = e.DOC_BYTE_SIZE,
        FileName = e.DOC_FILE_NAME,
        CapturedAt = e.DOC_CAPTURED_AT,
        CapturedCrewId = e.DOC_CAPTURED_CREW_ID,
    };
}
