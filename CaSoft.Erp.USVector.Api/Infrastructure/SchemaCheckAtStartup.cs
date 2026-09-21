using CaSoft.Erp.USVector.Infrastructure.Persistence.Schema;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// G4 — Dit, au démarrage, quels scripts de schéma la base porte — et lesquels manquent.
/// </summary>
/// <remarks>
/// <para>
/// <b>Elle ne refuse jamais de démarrer.</b> Le 06/08, un schéma en retard a produit un 500 opaque et
/// une journée sans données terrain ; le remède est de le <b>nommer</b>, pas de couper le terrain qui,
/// lui, marche sur tout le reste. Un script manquant casse les requêtes qui touchent ses tables, et
/// le journal dira alors lequel.
/// </para>
/// <para>
/// <b>Base injoignable = avertissement, pas erreur.</b> À cet instant du démarrage, SQL Server peut
/// monter plus lentement que l'application. Un script manquant, lui, ne se résorbe pas tout seul.
/// </para>
/// </remarks>
public sealed class SchemaCheckAtStartup : IHostedService
{
    private readonly SchemaJournal _journal;
    private readonly ILogger<SchemaCheckAtStartup> _logger;

    public SchemaCheckAtStartup(SchemaJournal journal, ILogger<SchemaCheckAtStartup> logger)
    {
        _journal = journal;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var etat = await _journal.ReadAsync(cancellationToken);

        if (etat.UpToDate)
        {
            _logger.LogInformation("{Constat}", etat.Statement());
            return;
        }

        if (!etat.Reachable)
        {
            // Le message brut de SQL Server est journalisé ICI, et nulle part ailleurs : il nomme le
            // compte de connexion, qui n'a rien à faire sur une route.
            _logger.LogWarning("{Constat} Détail SQL : {Erreur}. Réinterroger api/version/runtime une "
                             + "fois la base joignable.", etat.Statement(), etat.Error);
            return;
        }

        _logger.LogError("{Constat} Ces scripts sont dans CaSoft.Erp.USVector.Infrastructure\\Sql et "
                       + "s'appliquent tels quels — tous sont rejouables. Sans eux, les requêtes qui "
                       + "touchent leurs tables échoueront en « Nom de colonne non valide », pas au "
                       + "démarrage.", etat.Statement());
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
