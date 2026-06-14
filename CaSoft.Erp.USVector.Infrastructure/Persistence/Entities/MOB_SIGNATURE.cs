using System;
using System.Collections.Generic;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_SIGNATURE
{
    public Guid SIG_MISSION_ID { get; set; }

    public string SIG_DATA { get; set; } = null!;

    public DateTime SIG_DATETIME { get; set; }
}
