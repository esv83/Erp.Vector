using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>TRF-6 — Assemblage du paquet d'enrichissement consolidé (field-data).</summary>
public class FieldDataReaderTests
{
    private static readonly Guid Mission = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Order = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Ben = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ── Fakes ─────────────────────────────────────────────────────────────────
    private sealed class FakeErp : IErpReadApiClient
    {
        public bool MissionExists = true;
        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(MissionExists ? new ErpMissionFullDto { Id = id, OrderId = Order } : null);
        public Task<ErpOrderEditDto?> GetOrderAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpOrderEditDto?>(new ErpOrderEditDto { Order = new ErpOrderBodyDto { BeneficiaryId = Ben } });
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(DateTime f, DateTime t, int take, IReadOnlyCollection<Guid>? crews = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ErpMissionListItemDto>>(new List<ErpMissionListItemDto>());
        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpBeneficiaryDetailDto?>(null);
        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid p, DateOnly d, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default)
            => Task.FromResult<ErpCrewFullDto?>(null);
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid sub, CancellationToken ct = default)
            => Task.FromResult<Guid?>(null);
        public Task<int?> GetMissionTransferStatusAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<int?>(0);
    }

    private sealed class FakeJobTime : IJobTimeRepository
    {
        private readonly ClJobTimeData? _data;
        public FakeJobTime(ClJobTimeData? data) => _data = data;
        public void Save(Guid id, ClJobTimeData d) { }
        public ClJobTimeData? GetJobTimeData(Guid id) => _data;
    }

    private sealed class FakeSignature : ISignatureRepository
    {
        public bool DoesExist;
        public DateTime SignedAt;
        public ClSignatureDto Fetch(Guid id) => new() { JobId = id, DateTime = SignedAt, Data = "x" };
        public void Insert(Guid id, string d) { }
        public void Update(Guid id, string d) { }
        public void Delete(Guid id, string d) { }
        public bool Exists(Guid id) => DoesExist;
        public HashSet<Guid> ExistingFor(IEnumerable<Guid> ids) => new();
    }

    private sealed class FakeOverlay : IJobAttributeOverlay
    {
        public ClContractType BuildContractType(Guid missionId, IDictionary<string, IEnumerable<string>> baselines)
        {
            var attrs = new ClAttributCollection();
            var a = new ClContractAttribut(1) { Name = "COMMENTS", Value = "RAS" };
            var empty = new ClContractAttribut(2) { Name = "REFERENCE", Value = null };
            attrs.Add(a.Name, a);
            attrs.Add(empty.Name, empty);
            return new ClContractType(5, "STANDARD", attrs);
        }
        public void Save(Guid m, ClContractType c, IDictionary<string, IEnumerable<string>> b) { }
        public IReadOnlyList<ClContractType> GetContracts() => new List<ClContractType>();
        public int? GetSelectedContractId(Guid m) => null;
        public void SelectContract(Guid m, int c) { }
    }

    private sealed class FakeMutuelle : IMutuelleCardRepository
    {
        public ClMutuelleCard? Current;
        public void Save(ClMutuelleCard c) { }
        public ClMutuelleCard? GetCurrent(Guid b) => Current;
        public ClMutuelleCard? GetById(Guid id) => null;
        public ClMutuelleCard? Update(ClMutuelleCard c) => null;
    }

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"fielddata-{Guid.NewGuid()}").Options);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Assembles_all_silos_and_computes_watermark()
    {
        using var ctx = NewContext();
        var docs = new DocumentRepository(ctx);
        var anomalies = new AnomalyRepository(ctx);

        var go = new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc);
        var signed = new DateTime(2026, 6, 20, 9, 30, 0, DateTimeKind.Utc);
        var docAt = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);   // le plus récent
        var anoAt = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

        docs.Save(new ClDocument
        {
            Id = Guid.NewGuid(), MissionId = Mission, Category = EnDocumentCategory.TransportOrder,
            Content = new byte[] { 1 }, ContentType = "application/pdf", ByteSize = 1, CapturedAt = docAt
        });
        anomalies.Save(new ClAnomaly
        {
            Id = Guid.NewGuid(), MissionId = Mission, Type = EnAnomalyType.Phone, Text = "tel KO", ReportedAt = anoAt
        });

        var time = ClJobTimeData.GetBuilder().WithId(Mission).WithGoTime(go).Build();
        var sut = new FieldDataReader(
            new FakeErp(),
            new FakeJobTime(time),
            new FakeSignature { DoesExist = true, SignedAt = signed },
            new FakeOverlay(),
            new FakeMutuelle { Current = new ClMutuelleCard { Id = Guid.NewGuid(), BeneficiaryId = Ben, CapturedAt = go, AmcCode = "AMC1" } },
            docs,
            anomalies);

        var result = await sut.GetAsync(Mission, CancellationToken.None);

        result.Should().NotBeNull();
        result.OrderId.Should().Be(Order);
        result.SchemaVersion.Should().Be(1);
        result.Timeline.GoAt.Should().Be(go);
        result.Signature.Exists.Should().BeTrue();
        result.Signature.SignedAt.Should().Be(signed);
        result.Attributes.ContractDisplay.Should().Be("STANDARD");
        result.Attributes.Values.Should().ContainSingle(v => v.Name == "COMMENTS" && v.Value == "RAS"); // attribut vide écarté
        result.Mutuelle!.AmcCode.Should().Be("AMC1");
        result.Documents.Should().HaveCount(1);
        result.Anomalies.Should().HaveCount(1);
        result.UpdatedAt.Should().Be(docAt); // max de tous les horodatages
    }

    [Fact]
    public async Task Returns_null_when_mission_unknown()
    {
        using var ctx = NewContext();
        var sut = new FieldDataReader(
            new FakeErp { MissionExists = false },
            new FakeJobTime(null),
            new FakeSignature(),
            new FakeOverlay(),
            new FakeMutuelle(),
            new DocumentRepository(ctx),
            new AnomalyRepository(ctx));

        var result = await sut.GetAsync(Mission, CancellationToken.None);

        result.Should().BeNull();
    }
}
