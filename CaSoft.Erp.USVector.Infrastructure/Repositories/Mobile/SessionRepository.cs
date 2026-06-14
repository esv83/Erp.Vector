using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Sessions / tokens équipage (MOB_SESSION). Une seule session active par équipage
/// (index unique filtré UX_MOB_SESSION_CREW_ACTIVE). L'équipage est référencé par
/// son Guid ERP (CRW_CREW), sans FK cross-database.
/// </summary>
public class SessionRepository : ISessionRepository
{
    private readonly MobileDbContext _ctx;

    public SessionRepository(MobileDbContext ctx)
    {
        _ctx = ctx;
    }

    public Guid GetOrCreateToken(Guid crewId)
    {
        var active = _ctx.Sessions.SingleOrDefault(s => s.SES_CREW_ID == crewId && s.SES_ENDED_AT == null);
        if (active is not null)
            return active.SES_TOKEN;

        var session = new MOB_SESSION
        {
            SES_TOKEN = Guid.NewGuid(),
            SES_CREW_ID = crewId,
            SES_STARTED_AT = DateTime.UtcNow
        };
        _ctx.Sessions.Add(session);
        _ctx.SaveChanges();

        return session.SES_TOKEN;
    }

    public Guid? GetCrewIdByToken(Guid token)
    {
        var active = _ctx.Sessions.SingleOrDefault(s => s.SES_TOKEN == token && s.SES_ENDED_AT == null);
        return active?.SES_CREW_ID;
    }

    public void CloseSession(Guid crewId)
    {
        var active = _ctx.Sessions.SingleOrDefault(s => s.SES_CREW_ID == crewId && s.SES_ENDED_AT == null);
        if (active is null) return;

        active.SES_ENDED_AT = DateTime.UtcNow;
        _ctx.SaveChanges();
    }
}
