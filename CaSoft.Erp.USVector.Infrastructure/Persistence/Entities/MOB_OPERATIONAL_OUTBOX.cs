namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

/// <summary>
/// Outbox de projection opérationnelle (1 ligne par mission en attente). Écrite dans la même
/// transaction qu'un changement de jalon (MOB_MISSION_STATE) : garantit qu'aucun changement
/// n'est perdu. Un worker (OperationalOutboxDispatcher) projette l'état consolidé vers Orders.Api
/// après <c>OOB_DISPATCH_AFTER</c> (debounce), avec relance jusqu'à succès (livraison garantie).
/// La ligne est supprimée une fois projetée avec succès.
/// </summary>
public partial class MOB_OPERATIONAL_OUTBOX
{
    public Guid OOB_MISSION_ID { get; set; }
    /// <summary>Instant (UTC) à partir duquel projeter. Repoussé à chaque nouveau changement (debounce).</summary>
    public DateTime OOB_DISPATCH_AFTER { get; set; }
    public int OOB_ATTEMPTS { get; set; }
    public string? OOB_LAST_ERROR { get; set; }
    public DateTime OOB_UPDATED_AT { get; set; }
}
