using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Mapping;

/// <summary>Motif « le patient ne peut pas signer » (MOB_012) : EF ↔ métier.</summary>
internal static class SignatureWaiverMappings
{
    public static ClSignatureWaiverReason ToDomain(this MOB_SIGNATURE_WAIVER_REASON r) => new()
    {
        Code = r.SWR_CODE,
        Label = r.SWR_LABEL,
        IsCommentRequired = r.SWR_COMMENT_REQUIRED,
        IsActive = r.SWR_ACTIVE
    };

    /// <summary>La colonne est en UTC ; EF la relit sans fuseau, on le lui rend.</summary>
    public static ClSignatureWaiver ToDomain(this MOB_SIGNATURE_WAIVER w) => new()
    {
        MissionId = w.SWV_MISSION_ID,
        ReasonCode = w.SWV_REASON_CODE,
        Comment = w.SWV_COMMENT,
        DeclaredAt = DateTime.SpecifyKind(w.SWV_DECLARED_AT, DateTimeKind.Utc),
        DeclaredCrewId = w.SWV_CREW_ID
    };

    /// <summary>Recopie la déclaration sur la ligne (nouvelle ou existante : un motif remplace le précédent).</summary>
    public static void Apply(this MOB_SIGNATURE_WAIVER entity, ClSignatureWaiver w)
    {
        entity.SWV_REASON_CODE = w.ReasonCode;
        entity.SWV_COMMENT = w.Comment;
        entity.SWV_DECLARED_AT = w.DeclaredAt;
        entity.SWV_CREW_ID = w.DeclaredCrewId;
    }
}
