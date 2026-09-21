using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Mapping;

/// <summary>Mapping Infrastructure : entité EF ↔ métier (carte mutuelle).</summary>
internal static class MutuelleCardMappings
{
    public static MOB_MUTUELLE_CARD ToEntity(this ClMutuelleCard c) => new()
    {
        MMC_ID = c.Id,
        MMC_BENEFICIARY_ID = c.BeneficiaryId,
        MMC_IMAGE = c.Image,
        MMC_CONTENT_TYPE = c.ContentType,
        MMC_BYTE_SIZE = c.ByteSize,
        MMC_CAPTURED_AT = c.CapturedAt,
        MMC_CAPTURED_CREW_ID = c.CapturedCrewId,
        MMC_MISSION_ID = c.MissionId,
        MMC_MUTUELLE_NAME = c.MutuelleName,
        MMC_AMC_CODE = c.AmcCode,
        MMC_CONCENTRATEUR = c.Concentrateur,
        MMC_TELETRANSMISSION = c.Teletransmission,
        MMC_OCR_STATUS = c.OcrStatus,
        MMC_OCR_VALIDATED_AT = c.OcrValidatedAt,
        MMC_OCR_MUTUELLE_NAME = c.OcrMutuelleName,
        MMC_OCR_AMC_CODE = c.OcrAmcCode,
        MMC_OCR_CONCENTRATEUR = c.OcrConcentrateur,
        MMC_OCR_TELETRANSMISSION = c.OcrTeletransmission,
        MMC_OCR_CONFIDENCE = c.OcrConfidence,
        MMC_OCR_EXTRACTED_AT = c.OcrExtractedAt,
        MMC_OCR_ATTEMPTS = c.OcrAttempts,
        MMC_OCR_LAST_ERROR = c.OcrLastError,
    };

    public static ClMutuelleCard ToDomain(this MOB_MUTUELLE_CARD e) => new()
    {
        Id = e.MMC_ID,
        BeneficiaryId = e.MMC_BENEFICIARY_ID,
        Image = e.MMC_IMAGE,
        ContentType = e.MMC_CONTENT_TYPE,
        ByteSize = e.MMC_BYTE_SIZE,
        CapturedAt = e.MMC_CAPTURED_AT,
        CapturedCrewId = e.MMC_CAPTURED_CREW_ID,
        MissionId = e.MMC_MISSION_ID,
        MutuelleName = e.MMC_MUTUELLE_NAME,
        AmcCode = e.MMC_AMC_CODE,
        Concentrateur = e.MMC_CONCENTRATEUR,
        Teletransmission = e.MMC_TELETRANSMISSION,
        OcrStatus = e.MMC_OCR_STATUS,
        OcrValidatedAt = e.MMC_OCR_VALIDATED_AT,
        OcrMutuelleName = e.MMC_OCR_MUTUELLE_NAME,
        OcrAmcCode = e.MMC_OCR_AMC_CODE,
        OcrConcentrateur = e.MMC_OCR_CONCENTRATEUR,
        OcrTeletransmission = e.MMC_OCR_TELETRANSMISSION,
        OcrConfidence = e.MMC_OCR_CONFIDENCE,
        OcrExtractedAt = e.MMC_OCR_EXTRACTED_AT,
        OcrAttempts = e.MMC_OCR_ATTEMPTS,
        OcrLastError = e.MMC_OCR_LAST_ERROR,
    };
}
