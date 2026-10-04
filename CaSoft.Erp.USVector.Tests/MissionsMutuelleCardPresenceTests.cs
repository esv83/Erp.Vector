using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// 04/10 — Présence de la carte mutuelle pour une liste de missions : ce que les écrans de régulation
/// et de certification interrogent pour activer leur menu « Carte mutuelle ». Ils ne connaissent que
/// la mission ; le patient est résolu par Vector, en un appel à Orders.
/// </summary>
public class MissionsMutuelleCardPresenceTests
{
    private static readonly Guid AvecCarte = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid SansCarte = Guid.Parse("11111111-0000-0000-0000-000000000002");
    private static readonly Guid SansPatient = Guid.Parse("11111111-0000-0000-0000-000000000003");
    private static readonly Guid Inconnue = Guid.Parse("11111111-0000-0000-0000-000000000004");
    private static readonly Guid PatientA = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly Guid PatientB = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly DateTime Photo = new(2026, 9, 20, 8, 30, 0, DateTimeKind.Utc);

    /// <summary>Orders simulé côté Application : missions connues et leur patient.</summary>
    private sealed class FakeBeneficiaries : IMissionBeneficiaryQueryService
    {
        public Dictionary<Guid, Guid?> Connues { get; } = new()
        {
            [AvecCarte] = PatientA,
            [SansCarte] = PatientB,
            [SansPatient] = null
        };
        public Exception? Throws;
        public int Appels;

        public Task<Guid?> GetBeneficiaryIdAsync(Guid missionId, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetBeneficiaryIdsAsync(IReadOnlyCollection<Guid> missionIds, CancellationToken ct)
        {
            Appels++;
            if (Throws is not null) throw Throws;
            IReadOnlyDictionary<Guid, Guid?> rendues = missionIds.Where(Connues.ContainsKey).ToDictionary(id => id, id => Connues[id]);
            return Task.FromResult(rendues);
        }
    }

    private static MutuelleCardRepository Repository()
    {
        var ctx = new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"presence-missions-{Guid.NewGuid()}").Options);
        var repository = new MutuelleCardRepository(ctx);
        repository.Save(new ClMutuelleCard
        {
            Id = Guid.NewGuid(), BeneficiaryId = PatientA, Image = new byte[] { 1 }, ContentType = "image/jpeg",
            ByteSize = 1, CapturedAt = Photo, OcrStatus = "pending"
        });
        return repository;
    }

    private static Task<CaSoft.Framework.ClResult<IReadOnlyList<ClMissionMutuelleCardPresenceDtoOut>>> Presence(
        IReadOnlyCollection<Guid> missionIds, FakeBeneficiaries? beneficiaries = null)
        => new ClGetMissionsMutuelleCardPresenceUseCase(missionIds, beneficiaries ?? new FakeBeneficiaries(), Repository())
            .HandleAsync(CancellationToken.None);

    [Fact]
    public async Task Une_ligne_par_mission_demandee_dans_l_ordre_de_la_demande()
    {
        var result = await Presence(new[] { Inconnue, SansPatient, SansCarte, AvecCarte });

        result.IsSucces.Should().BeTrue();
        result.Value.Select(l => l.MissionId).Should().Equal(Inconnue, SansPatient, SansCarte, AvecCarte);

        var avec = result.Value.Single(l => l.MissionId == AvecCarte);
        avec.HasCard.Should().BeTrue();
        avec.BeneficiaryId.Should().Be(PatientA);
        avec.CapturedAt.Should().Be(Photo);
        avec.ImageUrl.Should().Be($"api/missions/{AvecCarte}/mutuelle-card/image");
    }

    [Fact]
    public async Task Sans_carte_sans_patient_ou_inconnue_la_ligne_le_dit_sans_url()
    {
        var result = await Presence(new[] { SansCarte, SansPatient, Inconnue });

        result.Value.Should().OnlyContain(l => !l.HasCard && l.CapturedAt == null && l.ImageUrl == null);
        result.Value.Single(l => l.MissionId == SansCarte).BeneficiaryId.Should().Be(PatientB);
        result.Value.Single(l => l.MissionId == SansPatient).BeneficiaryId.Should().BeNull();
        result.Value.Single(l => l.MissionId == Inconnue).BeneficiaryId.Should().BeNull();
    }

    [Fact]
    public async Task Les_doublons_et_l_identifiant_vide_sont_ignores()
    {
        var result = await Presence(new[] { AvecCarte, AvecCarte, Guid.Empty });

        result.Value.Should().ContainSingle().Which.MissionId.Should().Be(AvecCarte);
    }

    [Fact]
    public async Task Une_liste_vide_ne_sollicite_pas_Orders()
    {
        var beneficiaries = new FakeBeneficiaries();

        var result = await Presence(Array.Empty<Guid>(), beneficiaries);

        result.IsSucces.Should().BeTrue();
        result.Value.Should().BeEmpty();
        beneficiaries.Appels.Should().Be(0);
    }

    [Fact]
    public async Task Au_dela_de_200_missions_c_est_un_refus()
    {
        var result = await Presence(Enumerable.Range(0, 201).Select(_ => Guid.NewGuid()).ToList());

        result.IsFail.Should().BeTrue();
        result.InnerError!.ErrorText.Should().Contain("maximum 200");
    }

    [Fact]
    public async Task Une_panne_d_Orders_remonte_au_lieu_de_devenir_un_refus()
    {
        var beneficiaries = new FakeBeneficiaries { Throws = new HttpRequestException("Orders.Api → 503") };

        var act = () => Presence(new[] { AvecCarte }, beneficiaries);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── Résolution par lot chez Orders ─────────────────────────────────────────

    private sealed class BatchErp : IErpReadApiClient
    {
        public IReadOnlyList<ErpMissionBatchRefDto>? Rows;

        public Task<IReadOnlyList<ErpMissionBatchRefDto>?> GetMissionBatchRefsAsync(IReadOnlyCollection<Guid> missionIds, CancellationToken ct = default)
            => Task.FromResult(Rows);

        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpOrderEditDto?> GetOrderAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid p, DateOnly d, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid sub, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int?> GetMissionTransferStatusAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpMissionContextOrderDto?> GetMissionContextOrderAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ErpContextOrderFieldDto>?> GetContextOrderFormStructureAsync(Guid missionId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Le_lot_rend_le_patient_de_chaque_mission_connue_et_l_identifiant_vide_vaut_sans_patient()
    {
        var erp = new BatchErp
        {
            Rows = new[]
            {
                new ErpMissionBatchRefDto { MissionId = AvecCarte, OrderId = Guid.NewGuid(), BeneficiaryId = PatientA },
                new ErpMissionBatchRefDto { MissionId = SansPatient, OrderId = Guid.NewGuid(), BeneficiaryId = Guid.Empty }
            }
        };

        var patients = await new MissionBeneficiaryQueryService(erp)
            .GetBeneficiaryIdsAsync(new[] { AvecCarte, SansPatient, Inconnue }, CancellationToken.None);

        patients.Should().HaveCount(2);
        patients[AvecCarte].Should().Be(PatientA);
        patients[SansPatient].Should().BeNull();
        patients.Should().NotContainKey(Inconnue);
    }

    [Fact]
    public async Task Une_route_absente_chez_Orders_est_un_defaut_pas_une_absence_de_carte()
    {
        var act = () => new MissionBeneficiaryQueryService(new BatchErp { Rows = null })
            .GetBeneficiaryIdsAsync(new[] { AvecCarte }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
