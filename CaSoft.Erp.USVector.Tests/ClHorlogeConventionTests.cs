using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Dto;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// 04/10 — La convention des heures, figée. Trois horodatages partent en heure LOCALE parce qu'Orders et
/// la facturation les lisent ainsi (« mission vue », signature, prise de volant) ; tout le reste part en
/// UTC. Ces tests ne disent pas que c'est idéal : ils disent qu'on ne le change pas d'ici seul — voir
/// <c>ClHorloge</c>. Si l'un d'eux casse, c'est qu'une « correction » s'apprête à décaler de deux heures
/// la régulation ou la facturation.
/// </summary>
public class ClHorlogeConventionTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(10);

    // ── Heure LOCALE : la convention partagée ────────────────────────────────────

    private sealed class FakeJobTime : IJobTimeRepository
    {
        public ClJobTimeData? Stored;
        public Task SaveAsync(Guid id, ClJobTimeData d, CancellationToken ct) { Stored = d; return Task.CompletedTask; }
        public Task<ClJobTimeData> GetJobTimeDataAsync(Guid id, CancellationToken ct) => Task.FromResult(Stored!);
    }

    [Fact]
    public async Task La_mission_vue_est_en_heure_locale_car_Orders_la_range_sans_conversion()
    {
        var repo = new FakeJobTime();

        await new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo).HandleAsync(Ct);

        repo.Stored!.ReadTime!.Value.Kind.Should().Be(DateTimeKind.Local);
        repo.Stored.ReadTime.Value.Should().BeCloseTo(DateTime.Now, Tolerance);
    }

    private sealed class FakeCrews : ICrewRepository
    {
        private readonly ClCrew _crew;
        public ClCrew? Updated;
        public FakeCrews(ClCrew crew) => _crew = crew;
        public Task<ClCrew> GetCrewAsync(Guid gCrewID, CancellationToken ct) => Task.FromResult(_crew);
        public Task<ClCrewDriverWriteResult> UpdateAsync(ClCrew crew, CancellationToken ct)
        {
            Updated = crew;
            return Task.FromResult(ClCrewDriverWriteResult.Applied());
        }
        public Task<List<ClJobListItemModel>> FetchJobListAsync(IReadOnlyCollection<Guid> gCrewIds, CancellationToken ct) => throw new NotSupportedException();
        public List<ClInstructionListItemModel> FetchInstructionList(Guid gCrewId) => throw new NotSupportedException();
    }

    [Fact]
    public async Task La_prise_de_volant_est_en_heure_locale_car_Orders_la_compare_a_la_fin_de_vacation()
    {
        var conducteur = new ClEmployee(Guid.NewGuid(), "Jean", "Martin");
        var crew = new ClCrew(Guid.NewGuid(), new List<ClEmployee> { conducteur }, DateTime.Today,
            new ClVehicle(Guid.Empty, string.Empty, new ClKilometers()));
        var repo = new FakeCrews(crew);

        await new ClSetDriverUseCase(new ClSetDriverCommand(crew.CrewId, conducteur.Id), repo).HandleAsync(Ct);

        repo.Updated!.LastDriver!.From.Kind.Should().Be(DateTimeKind.Local);
        repo.Updated.LastDriver.From.Should().BeCloseTo(DateTime.Now, Tolerance);
    }

    [Fact]
    public async Task La_signature_est_en_heure_locale_car_la_facturation_la_reporte_telle_quelle()
    {
        using var ctx = new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"horloge-{Guid.NewGuid()}").Options);
        var mission = Guid.NewGuid();

        await new SignatureRepository(ctx).InsertAsync(mission, "image", Ct);

        var ecrite = ctx.Signatures.Single(s => s.SIG_MISSION_ID == mission).SIG_DATETIME;
        ecrite.Kind.Should().Be(DateTimeKind.Local);
        ecrite.Should().BeCloseTo(DateTime.Now, Tolerance);
    }

    // ── UTC : tout le reste ──────────────────────────────────────────────────────

    private sealed class FakeJobs : IJobRepository
    {
        public ClJobTimeData Stored = ClJobTimeData.GetBuilder().WithId(Guid.NewGuid()).Build();
        public Task<ClJob> GetJobAsync(Guid gJobId, CancellationToken ct) => throw new NotSupportedException();
        public Task<ClJobTimeData> GetJobTimeAsync(Guid jobId, CancellationToken ct) => Task.FromResult(Stored);
        public Task SaveJobTimeAsync(ClJobTimeData jobTime, CancellationToken ct) { Stored = jobTime; return Task.CompletedTask; }
    }

    [Fact]
    public async Task Une_etape_recue_avec_son_fuseau_est_stockee_en_UTC_et_servie_avec_Z()
    {
        var jobs = new FakeJobs();
        var saisie = new ClJobTimeModel { GoTime = "2026-10-04T10:00:00+02:00" };

        await new ClUpdateTimeUseCase(new ClJobTimeCommand(Guid.NewGuid(), saisie), jobs).HandleAsync(Ct);

        // Orders et la facturation convertissent ces étapes : elles DOIVENT être en UTC.
        jobs.Stored.GoTime.Should().Be(new DateTime(2026, 10, 4, 8, 0, 0));
        var servie = await new ClGetTimeUseCase(Guid.NewGuid(), jobs).HandleAsync(Ct);
        servie.Value.GoTime.Should().Be("2026-10-04T08:00:00Z");
    }

    [Fact]
    public async Task Une_anomalie_est_horodatee_en_UTC()
    {
        using var ctx = new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"horloge-ano-{Guid.NewGuid()}").Options);
        var mission = Guid.NewGuid();

        (await new ClReportAnomalyUseCase(
                new ClReportAnomalyCommand(mission, new ClReportAnomalyDtoIn { Type = (int)EnAnomalyType.Phone, Text = "tel" }),
                new AnomalyRepository(ctx))
            .HandleAsync(Ct)).IsSucces.Should().BeTrue();

        ctx.Anomalies.Single().ANO_REPORTED_AT.Should().BeCloseTo(DateTime.UtcNow, Tolerance);
    }
}
