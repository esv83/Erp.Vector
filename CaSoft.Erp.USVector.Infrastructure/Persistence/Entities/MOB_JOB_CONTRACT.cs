using System;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_JOB_CONTRACT
{
    public Guid JCT_MISSION_ID { get; set; }

    public int JCT_CONTRACT_ID { get; set; }

    public DateTime JCT_UPDATED_AT { get; set; }
}
