using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// P1 — Cartes mutuelle en BD Mobile (<c>MOB_MUTUELLE_CARD</c>). Rattachées au bénéficiaire ;
/// la plus récemment capturée fait foi.
/// </summary>
public class MutuelleCardRepository : IMutuelleCardRepository
{
    private readonly MobileDbContext _ctx;

    public MutuelleCardRepository(MobileDbContext ctx) => _ctx = ctx;

    public void Save(ClMutuelleCard card)
    {
        _ctx.MutuelleCards.Add(card.ToEntity());
        _ctx.SaveChanges();
    }

    public ClMutuelleCard? GetCurrent(Guid beneficiaryId)
        => _ctx.MutuelleCards
            .Where(c => c.MMC_BENEFICIARY_ID == beneficiaryId)
            .OrderByDescending(c => c.MMC_CAPTURED_AT)
            .FirstOrDefault()?.ToDomain();

    public ClMutuelleCard? GetById(Guid cardId)
        => _ctx.MutuelleCards.SingleOrDefault(c => c.MMC_ID == cardId)?.ToDomain();

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
}
