using Microsoft.Data.SqlClient;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Schema;

/// <summary>
/// G4 — Lit `__VectorSchema` : quels scripts la base porte, et depuis quand.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lecture seule, et rien d'autre.</b> L'application ne migre jamais au démarrage : une migration
/// automatique s'exécuterait sans sauvegarde et sans que personne l'ait décidée. Elle constate, elle
/// le dit, et elle sert quand même — un schéma en retard casse les requêtes qui touchent ses tables,
/// pas le démarrage.
/// </para>
/// <para>
/// <b>Sa propre connexion, hors du <c>DbContext</c></b> : le contrôle tourne au démarrage, avant
/// toute requête, et doit pouvoir répondre « base injoignable » sans dépendre d'un contexte EF.
/// </para>
/// </remarks>
public sealed class SchemaJournal
{
    private const string Requete =
        "IF OBJECT_ID(N'dbo.__VectorSchema', N'U') IS NULL " +
        "    SELECT CAST(0 AS bit) AS Present, NULL AS ScriptId, NULL AS AppliqueLe, NULL AS Origine " +
        "ELSE " +
        "    SELECT CAST(1 AS bit) AS Present, ScriptId, AppliqueLe, Origine " +
        "    FROM dbo.__VectorSchema ORDER BY ScriptId;";

    private readonly string? _connectionString;

    public SchemaJournal(string? connectionString) => _connectionString = connectionString;

    public async Task<SchemaState> ReadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return SchemaState.Unreachable("aucune chaîne de connexion configurée");

        try
        {
            await using var connexion = new SqlConnection(_connectionString);
            await connexion.OpenAsync(ct);

            await using var commande = connexion.CreateCommand();
            commande.CommandText = Requete;

            await using var lecteur = await commande.ExecuteReaderAsync(ct);

            var appliques = new List<AppliedScript>();
            var journalPresent = false;

            while (await lecteur.ReadAsync(ct))
            {
                journalPresent = lecteur.GetBoolean(0);
                if (!journalPresent) break;

                appliques.Add(new AppliedScript(
                    lecteur.GetString(1),
                    lecteur.GetDateTimeOffset(2),
                    lecteur.IsDBNull(3) ? "script" : lecteur.GetString(3)));
            }

            return journalPresent ? SchemaState.From(appliques) : SchemaState.WithoutJournal();
        }
        catch (SqlException ex)
        {
            return SchemaState.Unreachable(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Chaîne de connexion illisible : même verdict qu'une base absente — inconnu, pas faux.
            return SchemaState.Unreachable(ex.Message);
        }
    }
}
