using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// P1 — Cartes mutuelle en BD Mobile (<c>MOB_MUTUELLE_CARD</c>). Rattachées au bénéficiaire ;
/// la plus récemment capturée fait foi.
/// </summary>
/// <remarks>
/// <para><b>Toutes les lectures sont des projections explicites.</b> La colonne <c>MMC_IMAGE</c>
/// n'apparaît que dans les deux méthodes qui servent réellement des octets. Charger l'entité entière
/// — ce que faisait la version précédente — sortait jusqu'à 8 Mo de la base pour rendre un nom de
/// mutuelle, y compris dans la construction du paquet terrain, une fois par mission.</para>
/// <para>L'index <c>IX_MOB_MUTUELLE_CARD_BENEFICIARY</c> (<c>MMC_BENEFICIARY_ID</c>,
/// <c>MMC_CAPTURED_AT</c>) couvre les trois accès par bénéficiaire.</para>
/// </remarks>
public class MutuelleCardRepository : IMutuelleCardRepository
{
    private readonly MobileDbContext _ctx;

    public MutuelleCardRepository(MobileDbContext ctx) => _ctx = ctx;

    public void Save(ClMutuelleCard card)
    {
        _ctx.MutuelleCards.Add(card.ToEntity());
        _ctx.SaveChanges();
    }

    public ClMutuelleCard? GetCurrentMetadata(Guid beneficiaryId)
    {
        // Projection nommée champ par champ : le jour où une colonne s'ajoute à l'entité, elle
        // n'entre pas ici par accident — et surtout pas si c'est un second binaire.
        var row = _ctx.MutuelleCards.AsNoTracking()
            .Where(c => c.MMC_BENEFICIARY_ID == beneficiaryId)
            .OrderByDescending(c => c.MMC_CAPTURED_AT)
            .Select(c => new
            {
                c.MMC_ID,
                c.MMC_BENEFICIARY_ID,
                c.MMC_CONTENT_TYPE,
                c.MMC_BYTE_SIZE,
                c.MMC_CAPTURED_AT,
                c.MMC_CAPTURED_CREW_ID,
                c.MMC_MISSION_ID,
                c.MMC_MUTUELLE_NAME,
                c.MMC_AMC_CODE,
                c.MMC_CONCENTRATEUR,
                c.MMC_TELETRANSMISSION,
                c.MMC_OCR_STATUS,
                c.MMC_OCR_VALIDATED_AT,
                // MOB_010 — la proposition de la lecture automatique : l'écran de validation la lit
                // à côté des champs officiels. Toujours pas d'image dans cette projection.
                c.MMC_OCR_MUTUELLE_NAME,
                c.MMC_OCR_AMC_CODE,
                c.MMC_OCR_CONCENTRATEUR,
                c.MMC_OCR_TELETRANSMISSION,
                c.MMC_OCR_CONFIDENCE,
                c.MMC_OCR_EXTRACTED_AT,
                c.MMC_OCR_ATTEMPTS,
                c.MMC_OCR_LAST_ERROR
            })
            .FirstOrDefault();

        if (row is null) return null;

        return new ClMutuelleCard
        {
            Id = row.MMC_ID,
            BeneficiaryId = row.MMC_BENEFICIARY_ID,
            // Image : volontairement absente — cf. IMutuelleCardRepository.GetCurrentMetadata.
            ContentType = row.MMC_CONTENT_TYPE,
            ByteSize = row.MMC_BYTE_SIZE,
            CapturedAt = row.MMC_CAPTURED_AT,
            CapturedCrewId = row.MMC_CAPTURED_CREW_ID,
            MissionId = row.MMC_MISSION_ID,
            MutuelleName = row.MMC_MUTUELLE_NAME,
            AmcCode = row.MMC_AMC_CODE,
            Concentrateur = row.MMC_CONCENTRATEUR,
            Teletransmission = row.MMC_TELETRANSMISSION,
            OcrStatus = row.MMC_OCR_STATUS,
            OcrMutuelleName = row.MMC_OCR_MUTUELLE_NAME,
            OcrAmcCode = row.MMC_OCR_AMC_CODE,
            OcrConcentrateur = row.MMC_OCR_CONCENTRATEUR,
            OcrTeletransmission = row.MMC_OCR_TELETRANSMISSION,
            OcrConfidence = row.MMC_OCR_CONFIDENCE,
            OcrExtractedAt = row.MMC_OCR_EXTRACTED_AT,
            OcrAttempts = row.MMC_OCR_ATTEMPTS,
            OcrLastError = row.MMC_OCR_LAST_ERROR,
            OcrValidatedAt = row.MMC_OCR_VALIDATED_AT
        };
    }

    public ClMutuelleCardImage? GetImage(Guid cardId)
        => _ctx.MutuelleCards.AsNoTracking()
            .Where(c => c.MMC_ID == cardId)
            .Select(c => new ClMutuelleCardImage
            {
                Bytes = c.MMC_IMAGE,
                ContentType = c.MMC_CONTENT_TYPE
            })
            .FirstOrDefault();

    public ClMutuelleCardImage? GetCurrentImage(Guid beneficiaryId)
        => _ctx.MutuelleCards.AsNoTracking()
            .Where(c => c.MMC_BENEFICIARY_ID == beneficiaryId)
            .OrderByDescending(c => c.MMC_CAPTURED_AT)
            .Select(c => new ClMutuelleCardImage
            {
                Bytes = c.MMC_IMAGE,
                ContentType = c.MMC_CONTENT_TYPE
            })
            .FirstOrDefault();

    public IReadOnlyList<ClMutuelleCardPresence> ListPresence(IReadOnlyCollection<Guid> beneficiaryIds)
    {
        if (beneficiaryIds is null || beneficiaryIds.Count == 0)
            return Array.Empty<ClMutuelleCardPresence>();

        // Dédoublonnage côté client : un écran qui répète le même patient sur plusieurs lignes ne doit
        // pas allonger la clause IN pour rien.
        var ids = beneficiaryIds.Distinct().ToList();

        // Un GROUP BY, pas N requêtes — et aucune colonne binaire dans la projection.
        return _ctx.MutuelleCards.AsNoTracking()
            .Where(c => ids.Contains(c.MMC_BENEFICIARY_ID))
            .GroupBy(c => c.MMC_BENEFICIARY_ID)
            .Select(g => new ClMutuelleCardPresence
            {
                BeneficiaryId = g.Key,
                CapturedAt = g.Max(c => c.MMC_CAPTURED_AT)
            })
            .ToList();
    }

    public ClMutuelleCard? Update(ClMutuelleCard card)
    {
        var entity = _ctx.MutuelleCards.SingleOrDefault(c => c.MMC_ID == card.Id);
        if (entity is null) return null;

        // Seuls les champs mutuelle sont modifiables (image/traçabilité figées).
        entity.MMC_MUTUELLE_NAME = card.MutuelleName;
        entity.MMC_AMC_CODE = card.AmcCode;
        entity.MMC_CONCENTRATEUR = card.Concentrateur;
        entity.MMC_TELETRANSMISSION = card.Teletransmission;
        entity.MMC_OCR_STATUS = card.OcrStatus;
        entity.MMC_OCR_VALIDATED_AT = card.OcrValidatedAt;

        _ctx.SaveChanges();
        return entity.ToDomain();
    }

    // ── P3 : la file de lecture automatique ──────────────────────────────────

    public IReadOnlyList<Guid> ListPendingOcr(int take)
        => _ctx.MutuelleCards.AsNoTracking()
            .Where(c => c.MMC_OCR_STATUS == PendingStatus)
            .OrderBy(c => c.MMC_CAPTURED_AT)
            .Take(take)
            .Select(c => c.MMC_ID)   // l'image se tire une par une, au moment de la lire
            .ToList();

    public void SaveOcrProposal(Guid cardId, ClMutuelleCardOcrProposal proposal)
    {
        var entity = _ctx.MutuelleCards.SingleOrDefault(c => c.MMC_ID == cardId);
        if (entity is null) return;

        // ⚠️ Les quatre champs officiels (MMC_MUTUELLE_NAME, MMC_AMC_CODE, …) ne sont PAS touchés :
        // la proposition vit à côté, et c'est la validation humaine qui recopie (M5).
        entity.MMC_OCR_MUTUELLE_NAME = proposal.MutuelleName;
        entity.MMC_OCR_AMC_CODE = proposal.AmcCode;
        entity.MMC_OCR_CONCENTRATEUR = proposal.Concentrateur;
        entity.MMC_OCR_TELETRANSMISSION = proposal.Teletransmission;
        entity.MMC_OCR_CONFIDENCE = proposal.Confidence;
        entity.MMC_OCR_EXTRACTED_AT = DateTime.UtcNow;
        entity.MMC_OCR_ATTEMPTS += 1;
        entity.MMC_OCR_LAST_ERROR = null;
        entity.MMC_OCR_STATUS = ExtractedStatus;

        _ctx.SaveChanges();
    }

    public ClOcrFailureOutcome MarkOcrFailure(Guid cardId, string reason, int maxAttempts)
    {
        var entity = _ctx.MutuelleCards.SingleOrDefault(c => c.MMC_ID == cardId);
        if (entity is null) return new ClOcrFailureOutcome { Attempts = 0, GaveUp = true };

        entity.MMC_OCR_ATTEMPTS += 1;
        var giveUp = entity.MMC_OCR_ATTEMPTS >= maxAttempts;
        // Le motif est borné à la colonne : un message d'exception entier la ferait échouer, et on
        // perdrait l'échec en voulant l'enregistrer.
        entity.MMC_OCR_LAST_ERROR = reason.Length > 400 ? reason[..400] : reason;
        if (giveUp) entity.MMC_OCR_STATUS = ErrorStatus;

        _ctx.SaveChanges();
        return new ClOcrFailureOutcome { Attempts = entity.MMC_OCR_ATTEMPTS, GaveUp = giveUp };
    }

    private const string PendingStatus = "pending";
    private const string ExtractedStatus = "extracted";
    private const string ErrorStatus = "error";
}
