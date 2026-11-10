namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

/// <summary>Motif « le patient ne peut pas signer » d'une mission (1:1) — cf. <c>MOB_012</c>.</summary>
public partial class MOB_SIGNATURE_WAIVER
{
    public Guid SWV_MISSION_ID { get; set; }

    public string SWV_REASON_CODE { get; set; } = null!;

    public string? SWV_COMMENT { get; set; }

    /// <summary>UTC.</summary>
    public DateTime SWV_DECLARED_AT { get; set; }

    public Guid? SWV_CREW_ID { get; set; }
}

/// <summary>La liste des motifs — cf. <c>MOB_012</c>.</summary>
public partial class MOB_SIGNATURE_WAIVER_REASON
{
    public string SWR_CODE { get; set; } = null!;

    public string SWR_LABEL { get; set; } = null!;

    public bool SWR_COMMENT_REQUIRED { get; set; }

    public int SWR_INDEX { get; set; }

    public bool SWR_ACTIVE { get; set; }
}
