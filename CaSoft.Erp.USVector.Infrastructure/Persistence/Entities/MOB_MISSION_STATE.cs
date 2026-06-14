using System;
using System.Collections.Generic;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_MISSION_STATE
{
    public Guid MST_MISSION_ID { get; set; }

    public DateTime? MST_ACK_AT { get; set; }

    public DateTime? MST_READ_AT { get; set; }

    public DateTime? MST_GO_AT { get; set; }

    public DateTime? MST_ONSITE_AT { get; set; }

    public DateTime? MST_TERMINATED_AT { get; set; }

    public DateTime MST_UPDATED_AT { get; set; }
}
