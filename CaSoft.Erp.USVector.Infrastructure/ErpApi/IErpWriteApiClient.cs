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

    /// <summary>
    /// Désigne le conducteur d'un équipage (PUT /crews/{id}/driver, MOB-11). Endpoint additif
    /// côté Orders.Api : le personnel indiqué devient le conducteur actif à la date fournie.
    /// </summary>
    Task SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default);

    /// <summary>
    /// Pose le <b>context de la mission</b> choisi par l'ambulancier — OC-2
    /// (<c>PATCH /missions/{id}/contextOrder</c>). L'origine <c>Field</c> est imposée par
    /// l'endpoint : un choix terrain ne verrouille jamais, il reste modifiable.
    /// <para>
    /// Le refus métier n'est <b>pas</b> une erreur technique : verrou régulateur et context non
    /// applicable sont des issues normales, remontées par
    /// <see cref="EnContextOrderWriteOutcome"/> et non par une exception. Seul un échec réellement
    /// technique (5xx, réseau) lève.
    /// </para>
    /// </summary>
    /// <param name="setBy">Identifiant ambulancier/équipage, tracé côté Order. Optionnel.</param>
    Task<EnContextOrderWriteOutcome> SetMissionContextOrderAsync(
        Guid missionId, int contextOrderId, string? setBy = null, CancellationToken ct = default);
}

/// <summary>Issue d'une écriture de context (OC-2). Les trois refus sont des cas métier attendus.</summary>
public enum EnContextOrderWriteOutcome
{
    /// <summary>204 — choix terrain enregistré.</summary>
    Applied,

    /// <summary>409 — context fixé par le régulateur : lecture seule côté terrain.</summary>
    LockedByRegulator,

    /// <summary>400 — context non applicable à la commande (agence/mode) ou inactif.</summary>
    NotApplicable,

    /// <summary>404 — mission introuvable côté ERP.</summary>
    MissionNotFound
}
