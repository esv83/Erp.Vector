using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Deux écritures simultanées sur une mission sans état (20/11 à 08:28) : la seconde insertion ne
/// rend plus 500, elle devient la mise à jour qu'elle aurait été un instant plus tard.
/// </summary>
public class ClMobileUnitOfWorkTests
{
    private static readonly Guid Mission = Guid.Parse("90af8d6d-90f9-4b9b-89c0-ba31594d1e2f");
    private static readonly DateTime Go = new(2026, 11, 20, 7, 28, 0, DateTimeKind.Utc);
    private static readonly DateTime OnSite = new(2026, 11, 20, 7, 28, 1, DateTimeKind.Utc);

    private static DbContextOptions<MobileDbContext> SharedDatabase()
        => new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"concurrent-creation-{Guid.NewGuid()}").Options;

    /// <summary>La base en mémoire n'est pas SQL Server : toute erreur d'enregistrement vaut doublon ici.</summary>
    private static ClMobileUnitOfWork UnitOfWork(MobileDbContext ctx) => new(ctx, _ => true);

    [Fact]
    public async Task Losing_creation_is_applied_as_update_and_keeps_the_winning_milestone()
    {
        var options = SharedDatabase();
        using var winnerContext = new MobileDbContext(options);
        using var loserContext = new MobileDbContext(options);
        var winner = new JobTimeRepository(winnerContext);
        var loser = new JobTimeRepository(loserContext);

        // Les deux requêtes ont lu « aucune ligne » avant que l'une n'enregistre.
        (await winner.GetJobTimeDataAsync(Mission, CancellationToken.None)).Should().BeNull();
        (await loser.GetJobTimeDataAsync(Mission, CancellationToken.None)).Should().BeNull();

        await winner.SaveAsync(Mission, ClJobTimeData.GetBuilder().WithId(Mission).WithGoTime(Go).Build(), CancellationToken.None);
        await loser.SaveAsync(Mission, ClJobTimeData.GetBuilder().WithId(Mission).WithOnSiteTime(OnSite).Build(), CancellationToken.None);
        await UnitOfWork(winnerContext).CommitAsync();

        var act = () => UnitOfWork(loserContext).CommitAsync();

        await act.Should().NotThrowAsync();
        using var readContext = new MobileDbContext(options);
        var state = await readContext.MissionStates.SingleAsync(s => s.MST_MISSION_ID == Mission);
        state.MST_GO_AT.Should().Be(Go, "la valeur vide du perdant ne doit pas effacer celle du gagnant");
        state.MST_ONSITE_AT.Should().Be(OnSite);
        (await readContext.OperationalOutbox.CountAsync(o => o.OOB_MISSION_ID == Mission)).Should().Be(1);
    }

    [Fact]
    public async Task Other_duplicate_is_not_swallowed()
    {
        var options = SharedDatabase();
        using (var first = new MobileDbContext(options))
        {
            await new AnomalyRepository(first).SaveAsync(Anomaly(), CancellationToken.None);
            await UnitOfWork(first).CommitAsync();
        }

        using var second = new MobileDbContext(options);
        await new AnomalyRepository(second).SaveAsync(Anomaly(), CancellationToken.None);

        var act = () => UnitOfWork(second).CommitAsync();

        await act.Should().ThrowAsync<Exception>();
    }

    private static readonly Guid AnomalyId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ClAnomaly Anomaly()
        => new() { Id = AnomalyId, MissionId = Mission, Type = EnAnomalyType.Address, Text = "x", ReportedAt = Go };
}
