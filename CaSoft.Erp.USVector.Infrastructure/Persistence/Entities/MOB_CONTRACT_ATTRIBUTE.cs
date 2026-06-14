namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

public partial class MOB_CONTRACT_ATTRIBUTE
{
    public int CAT_ID { get; set; }

    public string CAT_NAME { get; set; } = null!;

    public string CAT_LABEL { get; set; } = null!;

    public string CAT_FIELD_TYPE { get; set; } = null!;

    public int CAT_INDEX { get; set; }

    public bool CAT_REQUIRED { get; set; }

    public string? CAT_PLACEHOLDER { get; set; }

    public bool CAT_INSTANT_UPDATE { get; set; }

    /// <summary>Champ multi-valué (saisie répétable : téléphones, e-mails).</summary>
    public bool CAT_IS_MULTI { get; set; }

    /// <summary>1 = appliqué à TOUS les contrats ; 0 = appliqué aux contrats liés (table de liaison).</summary>
    public bool CAT_IS_GLOBAL { get; set; }
}
