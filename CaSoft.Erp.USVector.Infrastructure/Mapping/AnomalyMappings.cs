using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Mapping;

/// <summary>Mapping Infrastructure : entité EF ↔ métier (anomalie terrain).</summary>
internal static class AnomalyMappings
{
    public static MOB_ANOMALY ToEntity(this ClAnomaly a) => new()
    {
        ANO_ID = a.Id,
        ANO_MISSION_ID = a.MissionId,
        ANO_TYPE = (int)a.Type,
        ANO_TEXT = a.Text,
        ANO_REPORTED_AT = a.ReportedAt,
        ANO_REPORTED_CREW_ID = a.ReportedCrewId,
    };

    public static ClAnomaly ToDomain(this MOB_ANOMALY e) => new()
    {
        Id = e.ANO_ID,
        MissionId = e.ANO_MISSION_ID,
        Type = (EnAnomalyType)e.ANO_TYPE,
        Text = e.ANO_TEXT,
        ReportedAt = e.ANO_REPORTED_AT,
        ReportedCrewId = e.ANO_REPORTED_CREW_ID,
    };
}
