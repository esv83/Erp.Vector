using System;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

/// <summary>Document/photo terrain rattaché à une mission (TRF-10, spec §14).</summary>
public partial class MOB_DOCUMENT
{
    public Guid DOC_ID { get; set; }
    public Guid DOC_MISSION_ID { get; set; }
    public int DOC_CATEGORY { get; set; }
    public byte[] DOC_CONTENT { get; set; } = Array.Empty<byte>();
    public string DOC_CONTENT_TYPE { get; set; } = null!;
    public int DOC_BYTE_SIZE { get; set; }
    public string? DOC_FILE_NAME { get; set; }
    public DateTime DOC_CAPTURED_AT { get; set; }
    public Guid? DOC_CAPTURED_CREW_ID { get; set; }
}
