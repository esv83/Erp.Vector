using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// Timeline opérationnelle d'une mission (MOB_MISSION_STATE, 1:1 ORD_MISSION).
/// Sémantique reprise du ClJobTimeRepository legacy (T_JOB_TIME) : upsert par mission.
/// TRF-5 : après l'écriture locale (source détaillée), projette l'avancement vers Orders
/// (best-effort) pour que la régulation le voie en temps réel.
/// </summary>
public class JobTimeRepository : IJobTimeRepository
{
    private readonly MobileDbContext _ctx;
    private readonly IErpWriteApiClient _erpWrite;
    private readonly ILogger<JobTimeRepository> _logger;

    public JobTimeRepository(MobileDbContext ctx, IErpWriteApiClient erpWrite, ILogger<JobTimeRepository> logger)
    {
        _ctx = ctx;
        _erpWrite = erpWrite;
        _logger = logger;
    }

    public void Save(Guid gJobId, ClJobTimeData timeData)
    {
        var entity = _ctx.MissionStates.SingleOrDefault(s => s.MST_MISSION_ID == gJobId);

        if (entity is null)
        {
            entity = new MOB_MISSION_STATE { MST_MISSION_ID = gJobId };
            entity.ApplyJobTimeData(timeData);
            _ctx.MissionStates.Add(entity);
        }
        else
        {
            entity.ApplyJobTimeData(timeData);
        }

        _ctx.SaveChanges();

        ProjectToErp(gJobId, timeData);
    }

    /// <summary>
    /// TRF-5 — projection best-effort de l'avancement vers Orders. Un échec (ERP indisponible,
    /// transition refusée) est journalisé sans casser l'écriture locale ; la vérité reste en BD
    /// Mobile et sera reprise au prochain geste / au tirage field-data par la facturation.
    /// Pont sync→async assumé (port legacy synchrone, cf. devplan).
    /// </summary>
    private void ProjectToErp(Guid gJobId, ClJobTimeData timeData)
    {
        try
        {
            _erpWrite.ProjectOperationalAsync(
                gJobId,
                timeData.AckTime, timeData.ReadTime, timeData.GoTime,
                timeData.OnSiteTime, timeData.TerminateTime,
                sourceCrewId: null).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Projection avancement mission {MissionId} vers Orders échouée (best-effort).", gJobId);
        }
    }

    public ClJobTimeData? GetJobTimeData(Guid gJobId)
    {
        var entity = _ctx.MissionStates.SingleOrDefault(s => s.MST_MISSION_ID == gJobId);
        return entity?.ToJobTimeData();
    }
}
