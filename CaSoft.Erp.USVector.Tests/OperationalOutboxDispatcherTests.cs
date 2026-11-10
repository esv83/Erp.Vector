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
        public Func<EnOperationalProjectionOutcome> FieldProjection = () => EnOperationalProjectionOutcome.Applied;
        public List<(bool SignatureExists, string? WaiverCode, DateTime? WaiverAt)> FieldCalls = new();

        public Task<EnOperationalProjectionOutcome> ProjectFieldAsync(Guid missionId, bool signatureExists, string? waiverCode, DateTime? waiverAtUtc, CancellationToken ct = default)
        {
            FieldCalls.Add((signatureExists, waiverCode, waiverAtUtc));
            return Task.FromResult(FieldProjection());
        }
        public Task<CrewDriverWriteResult> SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<EnContextOrderWriteOutcome> SetMissionContextOrderAsync(Guid missionId, int contextOrderId, string? setBy = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ContextOrderValuesWriteResult> SetContextOrderValuesAsync(Guid missionId, IReadOnlyCollection<(string Name, string? Value)> values, string? setBy = null, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static (OperationalOutboxDispatcher Sut, ServiceProvider Services) Arrange(FakeWrite write, int attempts = 0,
        bool fieldProjection = false, bool withTimeline = true, Action<MobileDbContext>? seed = null)
    {
        var dbName = $"outbox-{Guid.NewGuid()}";
        var services = new ServiceCollection()
            .AddDbContext<MobileDbContext>(o => o.UseInMemoryDatabase(dbName))
            .AddSingleton<IErpWriteApiClient>(write)
            .BuildServiceProvider();

        using (var scope = services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MobileDbContext>();
            if (withTimeline)
                ctx.MissionStates.Add(new MOB_MISSION_STATE { MST_MISSION_ID = Mission, MST_GO_AT = DateTime.UtcNow.AddMinutes(-5) });
            seed?.Invoke(ctx);
            ctx.OperationalOutbox.Add(new MOB_OPERATIONAL_OUTBOX
            {
                OOB_MISSION_ID = Mission, OOB_ATTEMPTS = attempts,
                OOB_DISPATCH_AFTER = DateTime.UtcNow.AddSeconds(-1), OOB_UPDATED_AT = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        var sut = new OperationalOutboxDispatcher(
            services.GetRequiredService<IServiceScopeFactory>(), NullLogger<OperationalOutboxDispatcher>.Instance,
            new FieldProjectionOptions { Enabled = fieldProjection });
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

    // ── PRJ-2 : la signature part avec la même entrée ─────────────────────────

    private static readonly DateTime DeclaredAt = new(2026, 11, 10, 9, 30, 0);

    [Fact]
    public async Task Coupee_par_defaut_la_projection_signature_n_appelle_pas_Orders()
    {
        var write = new FakeWrite();
        var (sut, _) = Arrange(write, seed: c => c.Signatures.Add(new MOB_SIGNATURE { SIG_MISSION_ID = Mission, SIG_DATA = "x" }));

        await sut.DispatchDueAsync(CancellationToken.None);

        write.Calls.Should().Be(1);
        write.FieldCalls.Should().BeEmpty("la route n'existe chez Orders qu'à partir de la version qui la livre");
    }

    [Fact]
    public async Task Active_elle_projette_la_presence_de_la_signature_et_le_motif_en_UTC()
    {
        var write = new FakeWrite();
        var (sut, services) = Arrange(write, fieldProjection: true, seed: c =>
        {
            c.Signatures.Add(new MOB_SIGNATURE { SIG_MISSION_ID = Mission, SIG_DATA = "x" });
            c.SignatureWaivers.Add(new MOB_SIGNATURE_WAIVER { SWV_MISSION_ID = Mission, SWV_REASON_CODE = "REFUS_PATIENT", SWV_DECLARED_AT = DeclaredAt });
        });

        await sut.DispatchDueAsync(CancellationToken.None);

        write.FieldCalls.Should().ContainSingle().Which.Should().Be((true, "REFUS_PATIENT", (DateTime?)DeclaredAt));
        write.FieldCalls[0].WaiverAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        Entry(services).Should().BeNull();
    }

    [Fact]
    public async Task Une_signature_sans_aucun_jalon_est_projetee_quand_meme()
    {
        // L'ancienne règle « pas de timeline → rien à projeter » aurait jeté l'entrée.
        var write = new FakeWrite();
        var (sut, _) = Arrange(write, fieldProjection: true, withTimeline: false,
            seed: c => c.Signatures.Add(new MOB_SIGNATURE { SIG_MISSION_ID = Mission, SIG_DATA = "x" }));

        await sut.DispatchDueAsync(CancellationToken.None);

        write.Calls.Should().Be(0, "pas de jalon à projeter");
        write.FieldCalls.Should().ContainSingle().Which.SignatureExists.Should().BeTrue();
    }

    [Fact]
    public async Task Une_signature_retiree_part_comme_absente_pour_effacer_l_etat_chez_Order()
    {
        var write = new FakeWrite();
        var (sut, _) = Arrange(write, fieldProjection: true);

        await sut.DispatchDueAsync(CancellationToken.None);

        write.FieldCalls.Should().ContainSingle().Which.Should().Be((false, (string?)null, (DateTime?)null));
    }

    [Fact]
    public async Task Une_mission_inconnue_n_est_pas_relancee_cote_signature_non_plus()
    {
        var write = new FakeWrite { Projection = () => EnOperationalProjectionOutcome.MissionNotFound };
        var (sut, services) = Arrange(write, fieldProjection: true);

        await sut.DispatchDueAsync(CancellationToken.None);

        write.FieldCalls.Should().BeEmpty("la mission est inconnue d'Orders : inutile d'insister");
        Entry(services).Should().BeNull();
    }

    [Fact]
    public async Task Une_panne_de_la_projection_signature_garde_l_entree()
    {
        var write = new FakeWrite { FieldProjection = () => throw new HttpRequestException("Orders.Api PUT → 503.") };
        var (sut, services) = Arrange(write, fieldProjection: true);

        await sut.DispatchDueAsync(CancellationToken.None);

        Entry(services).Should().NotBeNull("les jalons sont rejoués avec, sans dommage : le PUT est idempotent");
    }
}
