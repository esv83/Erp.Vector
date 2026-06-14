namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

/// <summary>Liaison N..N : attribut non global applicable à un (ou plusieurs) types de contrat.</summary>
public partial class MOB_CONTRACT_ATTRIBUTE_CONTRACT
{
    public int CAC_ATTRIBUTE_ID { get; set; }

    public int CAC_CONTRACT_ID { get; set; }
}
