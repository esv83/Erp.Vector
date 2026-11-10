namespace CaSoft.Erp.USVector.Api.Workers;

/// <summary>
/// PRJ-2 — Section <c>FieldProjection</c> : projection de l'état terrain de la signature vers Orders
/// (<c>PUT missions/{id}/field</c>). Coupée par défaut ; s'allume dans <c>appsettings.Production.json</c>
/// une fois constatée, sur <c>/version</c> d'Orders, la version qui sert la route.
/// </summary>
public sealed class FieldProjectionOptions
{
    public const string SectionName = "FieldProjection";

    public bool Enabled { get; set; }
}
