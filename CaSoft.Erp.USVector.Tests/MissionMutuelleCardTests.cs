using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using CaSoft.Framework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Carte mutuelle par mission : le terrain ne connaît que la mission, le patient est résolu côté
/// serveur (mission → commande → bénéficiaire).
/// </summary>
public class MissionMutuelleCardTests
{
    private static readonly Guid Mission = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Ben = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Crew = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // ── Fakes ─────────────────────────────────────────────────────────────────
    private sealed class FakeBeneficiaries : IMissionBeneficiaryQueryService
    {
        public Guid? BeneficiaryId;
        public Task<Guid?> GetBeneficiaryIdAsync(Guid missionId, CancellationToken ct) => Task.FromResult(BeneficiaryId);
    }

    private sealed class FakeErp : IErpReadApiClient
    {
        public bool MissionExists = true;
        public ErpOrderEditDto? Order;
        public Guid? RequestedOrderId;

        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(MissionExists ? new ErpMissionFullDto { Id = id, OrderId = OrderId } : null);
        public Task<ErpOrderEditDto?> GetOrderAsync(Guid id, CancellationToken ct = default)
        {
            RequestedOrderId = id;
            return Task.FromResult(Order);
        }
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(DateTime f, DateTime t, int take, IReadOnlyCollection<Guid>? crews = null, CancellationToken ct = default)
            => throw new NotSupportedException();
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
            .UseInMemoryDatabase($"mission-mutuelle-{Guid.NewGuid()}").Options);

    private static Task<ClResult<ClMutuelleCardCreatedDtoOut>> Upload(
        MutuelleCardRepository repository, Guid? beneficiaryId, string contentType = "image/jpeg")
        => new ClUploadMissionMutuelleCardUseCase(
                new ClUploadMissionMutuelleCardCommand(Mission, new byte[] { 1, 2, 3 }, contentType, Crew),
                new FakeBeneficiaries { BeneficiaryId = beneficiaryId },
                repository)
            .HandleAsync(CancellationToken.None);

    // ── Capture ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task La_capture_rattache_la_carte_au_patient_de_la_mission_et_trace_la_mission()
    {
        using var ctx = NewContext();
        var repository = new MutuelleCardRepository(ctx);

        var result = await Upload(repository, Ben);

        result.IsSucces.Should().BeTrue();
        var stored = repository.GetCurrent(Ben);
        stored.Should().NotBeNull();
        stored!.Id.Should().Be(result.Value.Id);
        stored.MissionId.Should().Be(Mission);
        stored.CapturedCrewId.Should().Be(Crew);
        stored.OcrStatus.Should().Be("none");
    }

    [Fact]
    public async Task Une_mission_sans_patient_renvoie_NotFound_et_n_ecrit_rien()
    {
        using var ctx = NewContext();

        var result = await Upload(new MutuelleCardRepository(ctx), beneficiaryId: null);

        result.IsFail.Should().BeTrue();
        result.InnerError.Should().BeOfType<ClError>().Which.IsNotFound.Should().BeTrue();
        ctx.MutuelleCards.Should().BeEmpty();
    }

    [Fact]
    public async Task La_validation_de_l_image_reste_celle_de_la_capture_par_beneficiaire()
    {
        using var ctx = NewContext();

        var result = await Upload(new MutuelleCardRepository(ctx), Ben, contentType: "application/pdf");

        result.IsFail.Should().BeTrue();
        result.InnerError.Should().BeOfType<ClError>().Which.IsNotFound.Should().BeFalse(); // 400, pas 404
        ctx.MutuelleCards.Should().BeEmpty();
    }

    // ── Lecture ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task La_lecture_renvoie_la_carte_la_plus_recente_du_patient()
    {
        using var ctx = NewContext();
        var repository = new MutuelleCardRepository(ctx);
        var older = NewCard(new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc));
        var newer = NewCard(new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc));
        repository.Save(older);
        repository.Save(newer);

        var result = await new ClGetMissionMutuelleCardUseCase(Mission, new FakeBeneficiaries { BeneficiaryId = Ben }, repository)
            .HandleAsync(CancellationToken.None);

        result.IsSucces.Should().BeTrue();
        result.Value.Id.Should().Be(newer.Id);
        result.Value.ImageUrl.Should().Be($"api/mutuelle-card/{newer.Id}/image");
    }

    [Fact]
    public async Task Un_patient_sans_carte_renvoie_NotFound()
    {
        using var ctx = NewContext();

        var result = await new ClGetMissionMutuelleCardUseCase(Mission, new FakeBeneficiaries { BeneficiaryId = Ben }, new MutuelleCardRepository(ctx))
            .HandleAsync(CancellationToken.None);

        result.InnerError.Should().BeOfType<ClError>().Which.IsNotFound.Should().BeTrue();
    }

    // ── Résolution mission → patient (Orders.Api) ───────────────────────────────

    [Fact]
    public async Task La_resolution_suit_mission_puis_commande_puis_beneficiaire()
    {
        var erp = new FakeErp { Order = new ErpOrderEditDto { Order = new ErpOrderBodyDto { BeneficiaryId = Ben } } };

        var id = await new MissionBeneficiaryQueryService(erp).GetBeneficiaryIdAsync(Mission, CancellationToken.None);

        id.Should().Be(Ben);
        erp.RequestedOrderId.Should().Be(OrderId);
    }

    [Fact]
    public async Task Une_mission_inconnue_ne_resout_aucun_patient()
    {
        var erp = new FakeErp { MissionExists = false };

        var id = await new MissionBeneficiaryQueryService(erp).GetBeneficiaryIdAsync(Mission, CancellationToken.None);

        id.Should().BeNull();
        erp.RequestedOrderId.Should().BeNull();
    }

    [Fact]
    public async Task Une_commande_sans_beneficiaire_ne_resout_aucun_patient()
    {
        var erp = new FakeErp { Order = new ErpOrderEditDto { Order = new ErpOrderBodyDto { BeneficiaryId = Guid.Empty } } };

        var id = await new MissionBeneficiaryQueryService(erp).GetBeneficiaryIdAsync(Mission, CancellationToken.None);

        id.Should().BeNull();
    }

    private static ClMutuelleCard NewCard(DateTime capturedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            BeneficiaryId = Ben,
            Image = new byte[] { 1 },
            ContentType = "image/jpeg",
            ByteSize = 1,
            CapturedAt = capturedAt,
            OcrStatus = "none",
        };
}
