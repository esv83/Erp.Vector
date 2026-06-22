using System;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

/// <summary>Anomalie terrain signalée par l'équipage (TRF-8, spec §17). Rattachée à la mission.</summary>
public partial class MOB_ANOMALY
{
    public Guid ANO_ID { get; set; }
    public Guid ANO_MISSION_ID { get; set; }
    public int ANO_TYPE { get; set; }
    public string? ANO_TEXT { get; set; }
    public DateTime ANO_REPORTED_AT { get; set; }
    public Guid? ANO_REPORTED_CREW_ID { get; set; }
}
