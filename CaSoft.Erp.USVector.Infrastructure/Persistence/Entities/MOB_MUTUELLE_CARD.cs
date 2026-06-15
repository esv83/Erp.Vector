using System;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_MUTUELLE_CARD
{
    public Guid MMC_ID { get; set; }
    public Guid MMC_BENEFICIARY_ID { get; set; }

    public byte[] MMC_IMAGE { get; set; } = Array.Empty<byte>();
    public string MMC_CONTENT_TYPE { get; set; } = null!;
    public int MMC_BYTE_SIZE { get; set; }

    public DateTime MMC_CAPTURED_AT { get; set; }
    public Guid? MMC_CAPTURED_CREW_ID { get; set; }
    public Guid? MMC_MISSION_ID { get; set; }

    // Champs extraits (OCR/IA — P3)
    public string? MMC_MUTUELLE_NAME { get; set; }
    public string? MMC_AMC_CODE { get; set; }
    public string? MMC_CONCENTRATEUR { get; set; }
    public string? MMC_TELETRANSMISSION { get; set; }
    public string? MMC_OCR_STATUS { get; set; }
    public DateTime? MMC_OCR_VALIDATED_AT { get; set; }
}
