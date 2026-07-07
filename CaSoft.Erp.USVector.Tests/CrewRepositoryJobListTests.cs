using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Non-régression du filtre joblist terrain (<see cref="CrewRepository.FetchJobList(System.Collections.Generic.IReadOnlyCollection{System.Guid})"/>).
/// Périmètre = logique CLIENT de l'adaptateur : la joblist lit la route crew-scopée
/// <c>GET /crews/{crewId}/missions</c> (mockée ici), masque les missions clôturées (status ≥ 4),
/// déduplique sur plusieurs équipages, ordonne par date puis heure, et superpose les flags
/// opérationnels (vue / terminé / signature).
/// Le filtre « engagée » est délégué à Orders (engagedOnly=true, cf. endPoint.md §5) : c'est un
/// filtrage SERVEUR, hors périmètre de ce test unitaire.
/// </summary>
public class CrewRepositoryJobListTests
{
    private static readonly Guid CrewA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CrewB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // ── Fakes ─────────────────────────────────────────────────────────────────
    private sealed class FakeErp : IErpReadApiClient
    {
        public readonly Dictionary<Guid, List<ErpMissionListItemDto>> ByCrew = new();
        public readonly List<Guid> RequestedCrews = new();

        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default)
        {
            RequestedCrews.Add(crewId);
            var list = ByCrew.TryGetValue(crewId, out var m) ? m : new List<ErpMissionListItemDto>();
            return Task.FromResult<IReadOnlyList<ErpMissionListItemDto>>(list);
        }

        // Membres non exercés par la joblist.
        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpOrderEditDto?> GetOrderAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(DateTime f, DateTime t, int take, IReadOnlyCollection<Guid>? crews = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid p, DateOnly d, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid sub, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int?> GetMissionTransferStatusAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeWrite : IErpWriteApiClient
    {
        public Task ProjectOperationalAsync(Guid missionId, DateTime? ackAt, DateTime? readAt, DateTime? goAt, DateTime? onsiteAt, DateTime? terminateAt, Guid? sourceCrewId, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetCrewDriverAsync(Guid crewId, Guid driverPersonnelId, DateTime from, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeSignature : ISignatureRepository
    {
        public readonly HashSet<Guid> Signed = new();
        public HashSet<Guid> ExistingFor(IEnumerable<Guid> ids) => Signed;

        public ClSignatureDto Fetch(Guid id) => throw new NotSupportedException();
        public void Insert(Guid id, string d) => throw new NotSupportedException();
        public void Update(Guid id, string d) => throw new NotSupportedException();
        public void Delete(Guid id, string d) => throw new NotSupportedException();
        public bool Exists(Guid id) => Signed.Contains(id);
    }

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"joblist-{Guid.NewGuid()}").Options);

    private static ErpMissionListItemDto Mission(Guid id, int status, DateOnly? date = null, TimeOnly? sched = null)
        => new()
        {
            Id = id,
            Status = status,
            MissionDate = date ?? new DateOnly(2026, 7, 7),
            SchedulingTime = sched ?? new TimeOnly(8, 0),
        };

    private static CrewRepository NewSut(MobileDbContext ctx, FakeErp erp, FakeSignature? sig = null)
        => new(erp, new FakeWrite(), ctx, sig ?? new FakeSignature());

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Masque_les_missions_cloturees_et_garde_status_1_a_3()
    {
        var m1 = Guid.NewGuid(); var m2 = Guid.NewGuid(); var m3 = Guid.NewGuid();
        var mClosed = Guid.NewGuid(); var mCancelled = Guid.NewGuid();

        var erp = new FakeErp();
        erp.ByCrew[CrewA] = new()
        {
            Mission(m1, status: 1),        // Engagé / À faire
            Mission(m2, status: 2),        // En cours
            Mission(m3, status: 3),        // Terminé
            Mission(mClosed, status: 4),   // Clôturé   → masquée
            Mission(mCancelled, status: 5) // ≥ 4       → masquée
        };

        using var ctx = NewContext();
        var result = NewSut(ctx, erp).FetchJobList(new[] { CrewA });

        result.Select(r => r.JobId).Should().BeEquivalentTo(new[] { m1, m2, m3 });
        result.Select(r => r.JobId).Should().NotContain(new[] { mClosed, mCancelled });
    }

    [Fact]
    public void Deduplique_une_mission_partagee_par_deux_equipages()
    {
        var shared = Guid.NewGuid(); var only = Guid.NewGuid();

        var erp = new FakeErp();
        erp.ByCrew[CrewA] = new() { Mission(shared, status: 2) };
        erp.ByCrew[CrewB] = new() { Mission(shared, status: 2), Mission(only, status: 1) };

        using var ctx = NewContext();
        var result = NewSut(ctx, erp).FetchJobList(new[] { CrewA, CrewB });

        result.Should().HaveCount(2);
        result.Select(r => r.JobId).Should().OnlyHaveUniqueItems();
        result.Select(r => r.JobId).Should().Contain(new[] { shared, only });
    }

    [Fact]
    public void Ordonne_par_date_de_mission_puis_heure()
    {
        var late = Guid.NewGuid();   // 08/07 08:00
        var early = Guid.NewGuid();  // 07/07 07:00
        var mid = Guid.NewGuid();    // 07/07 09:00

        var erp = new FakeErp();
        erp.ByCrew[CrewA] = new()
        {
            Mission(late,  status: 1, date: new DateOnly(2026, 7, 8), sched: new TimeOnly(8, 0)),
            Mission(mid,   status: 1, date: new DateOnly(2026, 7, 7), sched: new TimeOnly(9, 0)),
            Mission(early, status: 1, date: new DateOnly(2026, 7, 7), sched: new TimeOnly(7, 0)),
        };

        using var ctx = NewContext();
        var result = NewSut(ctx, erp).FetchJobList(new[] { CrewA });

        result.Select(r => r.JobId).Should().ContainInOrder(early, mid, late);
        result.Select(r => r.Index).Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void Superpose_les_flags_vue_termine_et_signature()
    {
        var seenMission = Guid.NewGuid();
        var terminatedMission = Guid.NewGuid();

        var erp = new FakeErp();
        erp.ByCrew[CrewA] = new() { Mission(seenMission, status: 1), Mission(terminatedMission, status: 2) };

        var when = new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc);
        using var ctx = NewContext();
        ctx.MissionStates.Add(new MOB_MISSION_STATE { MST_MISSION_ID = seenMission, MST_READ_AT = when, MST_UPDATED_AT = when });
        ctx.MissionStates.Add(new MOB_MISSION_STATE { MST_MISSION_ID = terminatedMission, MST_TERMINATED_AT = when, MST_UPDATED_AT = when });
        ctx.SaveChanges();

        var sig = new FakeSignature();
        sig.Signed.Add(seenMission);

        var result = NewSut(ctx, erp, sig).FetchJobList(new[] { CrewA });

        var seen = result.Single(r => r.JobId == seenMission);
        seen.IsSeen.Should().BeTrue();
        seen.IsTerminated.Should().BeFalse();
        seen.SignatureExists.Should().BeTrue();

        var terminated = result.Single(r => r.JobId == terminatedMission);
        terminated.IsSeen.Should().BeFalse();
        terminated.IsTerminated.Should().BeTrue();
        terminated.SignatureExists.Should().BeFalse();
    }

    [Fact]
    public void Interroge_la_route_crew_scopee_pour_chaque_equipage()
    {
        var erp = new FakeErp();
        erp.ByCrew[CrewA] = new() { Mission(Guid.NewGuid(), status: 1) };
        erp.ByCrew[CrewB] = new() { Mission(Guid.NewGuid(), status: 1) };

        using var ctx = NewContext();
        NewSut(ctx, erp).FetchJobList(new[] { CrewA, CrewB });

        // Périmètre = équipage (GET /crews/{crewId}/missions), une requête par crew, jamais la route datée.
        erp.RequestedCrews.Should().BeEquivalentTo(new[] { CrewA, CrewB });
    }

    [Fact]
    public void Sans_equipage_retourne_une_liste_vide()
    {
        using var ctx = NewContext();
        var result = NewSut(ctx, new FakeErp()).FetchJobList(Array.Empty<Guid>());

        result.Should().BeEmpty();
    }
}
