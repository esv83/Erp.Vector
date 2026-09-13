using CaSoft.Erp.USVector.Api.Workers;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Outbox de projection opérationnelle : livraison garantie sur panne, mais abandon d'une mission
/// inconnue d'Orders. Avant G9, un 404 était relancé sans fin — 55 414 tentatives sur une mission.
/// </summary>
public class OperationalOutboxDispatcherTests
{
    private static readonly Guid Mission = Guid.Parse("745c9f76-9ba1-41d6-a873-b9c0e9868b10");

    private sealed class FakeWrite : IErpWriteApiClient
    {
        public Func<EnOperationalProjectionOutcome> Projection = () => EnOperationalProjectionOutcome.Applied;
        public int Calls;

        public Task<EnOperationalProjectionOutcome> ProjectOperationalAsync(Guid missionId, DateTime? ackAt, DateTime? readAt, DateTime? goAt, DateTime? onsiteAt, DateTime? terminateAt, Guid? sourceCrewId, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Projection());
        }
        public Task SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<EnContextOrderWriteOutcome> SetMissionContextOrderAsync(Guid missionId, int contextOrderId, string? setBy = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<EnContextOrderValuesWriteOutcome> SetContextOrderValuesAsync(Guid missionId, IReadOnlyCollection<(string Name, string? Value)> values, string? setBy = null, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static (OperationalOutboxDispatcher Sut, ServiceProvider Services) Arrange(FakeWrite write, int attempts = 0)
    {
        var dbName = $"outbox-{Guid.NewGuid()}";
        var services = new ServiceCollection()
            .AddDbContext<MobileDbContext>(o => o.UseInMemoryDatabase(dbName))
            .AddSingleton<IErpWriteApiClient>(write)
            .BuildServiceProvider();

        using (var scope = services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MobileDbContext>();
            ctx.MissionStates.Add(new MOB_MISSION_STATE { MST_MISSION_ID = Mission, MST_GO_AT = DateTime.UtcNow.AddMinutes(-5) });
            ctx.OperationalOutbox.Add(new MOB_OPERATIONAL_OUTBOX
            {
                OOB_MISSION_ID = Mission, OOB_ATTEMPTS = attempts,
                OOB_DISPATCH_AFTER = DateTime.UtcNow.AddSeconds(-1), OOB_UPDATED_AT = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        var sut = new OperationalOutboxDispatcher(
            services.GetRequiredService<IServiceScopeFactory>(), NullLogger<OperationalOutboxDispatcher>.Instance);
        return (sut, services);
    }

    private static MOB_OPERATIONAL_OUTBOX? Entry(ServiceProvider services)
    {
        using var scope = services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MobileDbContext>().OperationalOutbox.SingleOrDefault();
    }

    [Fact]
    public async Task Une_projection_livree_retire_l_entree()
    {
        var write = new FakeWrite();
        var (sut, services) = Arrange(write);

        await sut.DispatchDueAsync(CancellationToken.None);

        write.Calls.Should().Be(1);
        Entry(services).Should().BeNull();
    }

    [Fact]
    public async Task Une_mission_inconnue_d_Orders_est_abandonnee_au_lieu_d_etre_relancee()
    {
        var write = new FakeWrite { Projection = () => EnOperationalProjectionOutcome.MissionNotFound };
        var (sut, services) = Arrange(write, attempts: 55_413);

        await sut.DispatchDueAsync(CancellationToken.None);

        Entry(services).Should().BeNull("un 404 d'Orders est définitif");
    }

    [Fact]
    public async Task Une_panne_passagere_garde_l_entree_et_la_relance_plus_tard()
    {
        var write = new FakeWrite { Projection = () => throw new HttpRequestException("Orders.Api PUT → 503.") };
        var (sut, services) = Arrange(write, attempts: 2);

        await sut.DispatchDueAsync(CancellationToken.None);

        var entry = Entry(services);
        entry.Should().NotBeNull("aucune perte de jalon sur une panne");
        entry!.OOB_ATTEMPTS.Should().Be(3);
        entry.OOB_LAST_ERROR.Should().Contain("503");
        entry.OOB_DISPATCH_AFTER.Should().BeAfter(DateTime.UtcNow);
    }
}
