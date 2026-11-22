using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Framework.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence;

/// <summary>
/// Unité de travail de la base Vector : celle du socle, plus la résolution d'une <b>création
/// concurrente</b> des lignes tenues une par mission (état terrain, file de projection).
/// </summary>
/// <remarks>
/// <para>
/// POURQUOI (22/11) — deux écritures simultanées sur une mission encore sans état (l'app double un
/// envoi d'étape, ou une étape croise une signature) lisent toutes deux « aucune ligne » et insèrent
/// toutes deux : la seconde violait la clé primaire et rendait un 500 brut (20/11 à 08:28).
/// </para>
/// <para>
/// RÉSOLUTION — l'insertion perdante devient la mise à jour qu'elle aurait été une milliseconde plus
/// tard : ses valeurs <b>renseignées</b> s'appliquent sur la ligne gagnante, ses valeurs vides
/// laissent celle-ci intacte — la règle cumulative des jalons. Une seule nouvelle tentative, dans la
/// même unité de travail : le reste des changements suivis part avec. Toute autre violation de
/// clé remonte telle quelle.
/// </para>
/// </remarks>
public sealed class ClMobileUnitOfWork : ClEfUnitOfWork<MobileDbContext>
{
    /// <summary>Violation de contrainte PRIMARY KEY ou UNIQUE (2627), d'index unique (2601).</summary>
    private static readonly int[] SqlDuplicateKeyNumbers = { 2627, 2601 };

    private readonly Func<Exception, bool> _isDuplicateKey;

    public ClMobileUnitOfWork(MobileDbContext dbContext)
        : this(dbContext, IsSqlDuplicateKey) { }

    /// <summary>Pour les tests : la base en mémoire signale un doublon par une <c>ArgumentException</c>.</summary>
    internal ClMobileUnitOfWork(MobileDbContext dbContext, Func<Exception, bool> isDuplicateKey)
        : base(dbContext)
        => _isDuplicateKey = isDuplicateKey;

    public override async Task<int> CommitAsync(CancellationToken ct = default)
    {
        try
        {
            return await base.CommitAsync(ct);
        }
        catch (Exception ex) when (_isDuplicateKey(ex))
        {
            if (!await ApplyLosingCreationsAsUpdatesAsync(ct))
                throw;

            return await base.CommitAsync(ct);
        }
    }

    /// <summary>
    /// Change chaque création perdante en mise à jour de la ligne existante. Faux si aucune ne l'est :
    /// la violation vient d'ailleurs et doit remonter.
    /// </summary>
    private async Task<bool> ApplyLosingCreationsAsUpdatesAsync(CancellationToken ct)
    {
        var creations = DbContext.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Added
                            && entry.Entity is MOB_MISSION_STATE or MOB_OPERATIONAL_OUTBOX)
            .ToList();

        var isAnyApplied = false;
        foreach (var creation in creations)
        {
            var storedValues = await creation.GetDatabaseValuesAsync(ct);
            if (storedValues is null)
                continue;

            var wantedValues = creation.CurrentValues.Clone();
            creation.State = EntityState.Unchanged;
            creation.OriginalValues.SetValues(storedValues);
            creation.CurrentValues.SetValues(storedValues);

            foreach (var property in creation.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
            {
                var wanted = wantedValues[property.Metadata];
                if (wanted is null)
                    continue;

                property.CurrentValue = wanted;
                property.IsModified = true;
            }

            isAnyApplied = true;
        }

        return isAnyApplied;
    }

    private static bool IsSqlDuplicateKey(Exception ex)
        => ex is DbUpdateException { InnerException: SqlException sql } && SqlDuplicateKeyNumbers.Contains(sql.Number);
}
