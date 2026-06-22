namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

/// <summary>
/// Accès en écriture vers <c>Orders.Api</c> (HTTP) — TRF-5. Pendant écriture de
/// <see cref="IErpReadApiClient"/> : Vector pousse l'avancement opérationnel terrain vers
/// l'ERP pour que la régulation le voie en temps réel (projection ORD_MISSION_OPERATIONAL).
/// </summary>
public interface IErpWriteApiClient
{
    /// <summary>
    /// Projette les jalons opérationnels d'une mission (PUT /missions/{id}/operational, TRF-3).
    /// Jalons cumulatifs : seuls ceux fournis (non null) sont posés côté ERP.
    /// </summary>
    Task ProjectOperationalAsync(
        Guid missionId,
        DateTime? ackAt, DateTime? readAt, DateTime? goAt,
        DateTime? onsiteAt, DateTime? terminateAt,
        Guid? sourceCrewId, CancellationToken ct = default);
}
