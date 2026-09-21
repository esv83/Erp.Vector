using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// TRF-6 / B5 — Assemblage du paquet d'enrichissement (field-data), à l'unité et en lot.
/// <para>
/// Les silos sont lus par le vrai <see cref="FieldDataQueryService"/> sur une base en mémoire ; seul
/// Orders est simulé. Le lot porte deux exigences de la facturation (19/09) : chaque paquet retrouvé
/// par son <c>MissionId</c>, et « inconnue » distincte de « en erreur ».
/// </para>
/// </summary>
public class FieldDataReaderTests
{
    private static readonly Guid Mission = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Retour = Guid.Parse("11111111-1111-1111-1111-111111111112");
    private static readonly Guid Order = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Ben = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ── Orders simulé ─────────────────────────────────────────────────────────
    private sealed class FakeErp : IErpReadApiClient
    {
        public HashSet<Guid> Unknown { get; } = new();
        public HashSet<Guid> Failing { get; } = new();
        public int OrderCalls;

        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid id, CancellationToken ct = default)
        {
            if (Failing.Contains(id)) throw new HttpRequestException("Orders.Api → 503");
            return Task.FromResult(Unknown.Contains(id) ? null : new ErpMissionFullDto { Id = id, OrderId = Order });
        }

        public Task<ErpOrderEditDto?> GetOrderAsync(Guid id, CancellationToken ct = default)
        {
            Interlocked.Increment(ref OrderCalls);
            return Task.FromResult<ErpOrderEditDto?>(new ErpOrderEditDto { Order = new ErpOrderBodyDto { BeneficiaryId = Ben } });
        }

        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid id, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid p, DateOnly d, int take, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid sub, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<int?> GetMissionTransferStatusAsync(Guid id, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ErpMissionContextOrderDto?> GetMissionContextOrderAsync(Guid id, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyList<ErpContextOrderFieldDto>?> GetContextOrderFormStructureAsync(Guid missionId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"fielddata-{Guid.NewGuid()}").Options);

    private static FieldDataReader Reader(MobileDbContext ctx, FakeErp? erp = null)
        => new(erp ?? new FakeErp(), new FieldDataQueryService(ctx));

    // ── À l'unité ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Assembles_all_silos_and_computes_watermark()
    {
        using var ctx = NewContext();
        var go = new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc);
        var signed = new DateTime(2026, 6, 20, 9, 30, 0, DateTimeKind.Utc);
        var docAt = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);   // le plus récent
        var anoAt = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

        ctx.MissionStates.Add(new MOB_MISSION_STATE { MST_MISSION_ID = Mission, MST_GO_AT = go });
        ctx.Signatures.Add(new MOB_SIGNATURE { SIG_MISSION_ID = Mission, SIG_DATA = "image", SIG_DATETIME = signed });
        ctx.SaveChanges();
        new DocumentRepository(ctx).Save(new ClDocument
        {
            Id = Guid.NewGuid(), MissionId = Mission, Category = EnDocumentCategory.TransportOrder,
            Content = new byte[] { 1 }, ContentType = "application/pdf", ByteSize = 1, CapturedAt = docAt
        });
        new AnomalyRepository(ctx).Save(new ClAnomaly
        {
            Id = Guid.NewGuid(), MissionId = Mission, Type = EnAnomalyType.Phone, Text = "tel KO", ReportedAt = anoAt
        });
        new MutuelleCardRepository(ctx).Save(new ClMutuelleCard
        {
            Id = Guid.NewGuid(), BeneficiaryId = Ben, Image = new byte[] { 9 }, ContentType = "image/jpeg",
            ByteSize = 1, CapturedAt = go, AmcCode = "AMC1"
        });

        var result = await Reader(ctx).GetAsync(Mission, CancellationToken.None);

        result.Should().NotBeNull();
        result.OrderId.Should().Be(Order);
        result.SchemaVersion.Should().Be(1);
        result.Timeline.GoAt.Should().Be(go);
        result.Signature.Exists.Should().BeTrue();
        result.Signature.SignedAt.Should().Be(signed);
        result.Signature.ImageUrl.Should().Be($"api/Signature/{Mission}");
        result.Mutuelle!.AmcCode.Should().Be("AMC1");
        result.Documents.Should().HaveCount(1);
        result.Anomalies.Should().HaveCount(1);
        result.UpdatedAt.Should().Be(docAt); // max de tous les horodatages
    }

    /// <summary>
    /// OC-8 — le magasin d'attributs Vector est retiré : le bloc part toujours à null. La facturation
    /// lit les attributs chez Order et tolère ce null ; le paquet ne doit pas en inventer.
    /// </summary>
    [Fact]
    public async Task Le_bloc_attributs_part_toujours_a_null()
    {
        using var ctx = NewContext();

        var result = await Reader(ctx).GetAsync(Mission, CancellationToken.None);

        result.Attributes.Should().BeNull();
        result.Signature.Exists.Should().BeFalse();
        result.Signature.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_mission_unknown()
    {
        using var ctx = NewContext();
        var erp = new FakeErp();
        erp.Unknown.Add(Mission);

        var result = await Reader(ctx, erp).GetAsync(Mission, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task A_l_unite_une_panne_d_Orders_reste_une_panne()
    {
        using var ctx = NewContext();
        var erp = new FakeErp();
        erp.Failing.Add(Mission);

        var act = () => Reader(ctx, erp).GetAsync(Mission, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── En lot (B5) ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Le_lot_rend_une_entree_par_mission_avec_son_statut()
    {
        using var ctx = NewContext();
        var inconnue = Guid.NewGuid();
        var enPanne = Guid.NewGuid();
        var erp = new FakeErp();
        erp.Unknown.Add(inconnue);
        erp.Failing.Add(enPanne);

        var lot = await Reader(ctx, erp).GetManyAsync(new[] { Mission, inconnue, enPanne }, CancellationToken.None);

        lot.Select(i => i.MissionId).Should().Equal(Mission, inconnue, enPanne);
        lot[0].Status.Should().Be(ClFieldEnrichmentBatchItemDtoOut.StatusFound);
        lot[0].Data.MissionId.Should().Be(Mission);
        lot[1].Status.Should().Be(ClFieldEnrichmentBatchItemDtoOut.StatusNotFound);
        lot[1].Data.Should().BeNull();
        lot[2].Status.Should().Be(ClFieldEnrichmentBatchItemDtoOut.StatusError);
        lot[2].Error.Should().Contain("503");
    }

    [Fact]
    public async Task Le_lot_ne_demande_qu_une_fois_la_commande_partagee_par_l_aller_et_le_retour()
    {
        using var ctx = NewContext();
        var erp = new FakeErp();

        var lot = await Reader(ctx, erp).GetManyAsync(new[] { Mission, Retour }, CancellationToken.None);

        lot.Should().OnlyContain(i => i.Status == ClFieldEnrichmentBatchItemDtoOut.StatusFound);
        erp.OrderCalls.Should().Be(1);
    }

    [Fact]
    public async Task Le_lot_repartit_les_silos_par_mission()
    {
        using var ctx = NewContext();
        var signed = new DateTime(2026, 9, 19, 9, 0, 0, DateTimeKind.Utc);
        ctx.Signatures.Add(new MOB_SIGNATURE { SIG_MISSION_ID = Retour, SIG_DATA = "image", SIG_DATETIME = signed });
        ctx.SaveChanges();

        var lot = await Reader(ctx).GetManyAsync(new[] { Mission, Retour }, CancellationToken.None);

        lot.Single(i => i.MissionId == Mission).Data.Signature.Exists.Should().BeFalse();
        lot.Single(i => i.MissionId == Retour).Data.Signature.SignedAt.Should().Be(signed);
    }

    [Fact]
    public async Task Le_lot_dedoublonne_et_ignore_l_identifiant_vide()
    {
        using var ctx = NewContext();

        var lot = await Reader(ctx).GetManyAsync(new[] { Mission, Mission, Guid.Empty }, CancellationToken.None);

        lot.Should().ContainSingle().Which.MissionId.Should().Be(Mission);
    }

    [Fact]
    public async Task Un_lot_vide_ne_sollicite_personne()
    {
        using var ctx = NewContext();
        var erp = new FakeErp();

        var lot = await Reader(ctx, erp).GetManyAsync(Array.Empty<Guid>(), CancellationToken.None);

        lot.Should().BeEmpty();
        erp.OrderCalls.Should().Be(0);
    }
}
