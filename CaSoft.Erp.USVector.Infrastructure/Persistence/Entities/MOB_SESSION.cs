using System;
using System.Collections.Generic;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_SESSION
{
    public Guid SES_ID { get; set; }

    public Guid SES_TOKEN { get; set; }

    public Guid SES_CREW_ID { get; set; }

    public DateTime SES_STARTED_AT { get; set; }

    public DateTime? SES_ENDED_AT { get; set; }
}
