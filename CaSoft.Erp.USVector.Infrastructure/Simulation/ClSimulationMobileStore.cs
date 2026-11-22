using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Simulation;

/// <summary>
/// Ce que le mode formation laisse dans la base Mobile — horaires, signatures, cartes, documents… — et
/// qu'il faut savoir reconnaître, effacer chaque nuit, ou remettre à zéro pour une session.
/// <para>
/// Toutes ces lignes sont repérées par la plage réservée (<see cref="ClSimulationIds"/>) d'une de leurs
/// colonnes : mission, équipage ou patient. Aucune colonne ni table n'est ajoutée au schéma.
/// </para>
/// </summary>
public sealed class ClSimulationMobileStore
{
    /// <summary>
    /// Chaque table qui reçoit une saisie terrain, et la colonne qui porte l'identifiant simulé. Une table
    /// ajoutée au schéma et indexée par mission doit être ajoutée ici, sinon la purge l'oubliera.
    /// </summary>
    internal static readonly (string Table, string Column)[] Targets =
    {
        ("dbo.MOB_OPERATIONAL_OUTBOX", "OOB_MISSION_ID"),
        ("dbo.MOB_MISSION_STATE", "MST_MISSION_ID"),
        ("dbo.MOB_SIGNATURE", "SIG_MISSION_ID"),
        ("dbo.MOB_SIGNATURE_WAIVER", "SWV_MISSION_ID"),
        ("dbo.MOB_ANOMALY", "ANO_MISSION_ID"),
        ("dbo.MOB_DOCUMENT", "DOC_MISSION_ID"),
        ("dbo.MOB_MUTUELLE_CARD", "MMC_BENEFICIARY_ID"),
        ("dbo.MOB_SESSION", "SES_CREW_ID"),
    };

    private readonly MobileDbContext _db;

    public ClSimulationMobileStore(MobileDbContext db) => _db = db;

    /// <summary>
    /// Efface les saisies de formation des jours ANTÉRIEURS à <paramref name="today"/>. Rejouable à toute
    /// heure : celles du jour ne sont jamais touchées, même si le pool redémarre en pleine formation.
    /// </summary>
    public async Task<int> PurgeBeforeAsync(DateOnly today, CancellationToken ct)
    {
        var deleted = 0;
        foreach (var (table, column) in Targets)
        {
            // Jour = 3e groupe de la forme texte (positions 15 à 18), lu en hexadécimal. Table et colonne
            // viennent de la liste figée ci-dessus, jamais d'une saisie.
            var sql = $"DELETE FROM {table} WHERE CONVERT(char(36), {column}) LIKE @prefix "
                    + $"AND CONVERT(int, CONVERT(binary(2), SUBSTRING(CONVERT(char(36), {column}), 15, 4), 2)) < @day";
            deleted += await _db.Database.ExecuteSqlRawAsync(sql, new object[]
            {
                new SqlParameter("@prefix", ClSimulationIds.SqlPrefix + "%"),
                new SqlParameter("@day", ClSimulationIds.DayNumber(today)),
            }, ct);
        }
        return deleted;
    }

    /// <summary>Efface les saisies d'un seul équipage de formation pour un jour : la session repart de zéro.</summary>
    public async Task<int> ResetAsync(Guid personnelId, DateOnly day, CancellationToken ct)
    {
        var pattern = ClSimulationIds.SqlPatternFor(ClSimulationIds.OwnerOf(personnelId), day);
        var deleted = 0;
        foreach (var (table, column) in Targets)
            deleted += await _db.Database.ExecuteSqlRawAsync(
                $"DELETE FROM {table} WHERE CONVERT(char(36), {column}) LIKE @pattern",
                new object[] { new SqlParameter("@pattern", pattern) }, ct);
        return deleted;
    }

    /// <summary>La carte appartient-elle à un patient fictif ? Faux si elle n'existe pas.</summary>
    public async Task<bool> IsSimulatedCardAsync(Guid cardId, CancellationToken ct)
    {
        var beneficiaryId = await _db.MutuelleCards.AsNoTracking()
            .Where(c => c.MMC_ID == cardId).Select(c => (Guid?)c.MMC_BENEFICIARY_ID).FirstOrDefaultAsync(ct);
        return ClSimulationIds.IsSimulated(beneficiaryId);
    }

    /// <summary>Le document appartient-il à une mission fictive ? Faux s'il n'existe pas.</summary>
    public async Task<bool> IsSimulatedDocumentAsync(Guid documentId, CancellationToken ct)
    {
        var missionId = await _db.Documents.AsNoTracking()
            .Where(d => d.DOC_ID == documentId).Select(d => (Guid?)d.DOC_MISSION_ID).FirstOrDefaultAsync(ct);
        return ClSimulationIds.IsSimulated(missionId);
    }
}
