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
}
