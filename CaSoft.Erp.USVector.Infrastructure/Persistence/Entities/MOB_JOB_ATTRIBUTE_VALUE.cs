using System;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_JOB_ATTRIBUTE_VALUE
{
    public Guid JAV_MISSION_ID { get; set; }

    public string JAV_ATTRIBUTE_NAME { get; set; } = null!;

    /// <summary>Scalaire : la valeur. Liste (tél/mail) : JSON des items AJOUTÉS (hors baseline ERP).</summary>
    public string? JAV_VALUE { get; set; }

    public DateTime JAV_UPDATED_AT { get; set; }
}
