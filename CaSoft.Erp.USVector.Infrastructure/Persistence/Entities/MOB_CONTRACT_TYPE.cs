namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_CONTRACT_TYPE
{
    public int CTT_ID { get; set; }

    public string CTT_CODE { get; set; } = null!;

    public string CTT_DISPLAY { get; set; } = null!;

    public bool CTT_ACTIVE { get; set; }
}
