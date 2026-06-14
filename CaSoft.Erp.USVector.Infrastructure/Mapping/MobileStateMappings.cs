using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Mapping;

/// <summary>
/// Mappings EF (MOB_*) ↔ métier/DTO mobile.
/// MOB_MISSION_STATE reprend la sémantique du T_JOB_TIME legacy :
/// ACK / LU(read) / ER(go) / SP(onsite) / TER(terminate).
/// </summary>
public static class MobileStateMappings
{
    public static ClJobTimeData ToJobTimeData(this MOB_MISSION_STATE entity)
    {
        return ClJobTimeData.GetBuilder()
            .WithId(entity.MST_MISSION_ID)
            .WithAckTime(entity.MST_ACK_AT)
            .WithReadTime(entity.MST_READ_AT)
            .WithGoTime(entity.MST_GO_AT)
            .WithOnSiteTime(entity.MST_ONSITE_AT)
            .WithTerminateTime(entity.MST_TERMINATED_AT)
            .WithPersistentOrigine()
            .Build();
    }

    public static void ApplyJobTimeData(this MOB_MISSION_STATE entity, ClJobTimeData timeData)
    {
        entity.MST_ACK_AT = timeData.AckTime;
        entity.MST_READ_AT = timeData.ReadTime;
        entity.MST_GO_AT = timeData.GoTime;
        entity.MST_ONSITE_AT = timeData.OnSiteTime;
        entity.MST_TERMINATED_AT = timeData.TerminateTime;
        entity.MST_UPDATED_AT = DateTime.UtcNow;
    }

    public static ClSignatureDto ToSignatureDto(this MOB_SIGNATURE entity)
    {
        return new ClSignatureDto
        {
            JobId = entity.SIG_MISSION_ID,
            Data = entity.SIG_DATA,
            DateTime = entity.SIG_DATETIME
        };
    }
}
