using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Le détail mission sert l'identifiant du bénéficiaire.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ce que ce champ débloque.</b> La carte mutuelle s'attache au <b>patient</b> et non à la
/// mission (décision M4 : elle le suit d'un transport à l'autre), donc ses routes sont indexées par
/// bénéficiaire — <c>POST /api/beneficiaries/{id}/mutuelle-card</c>. L'écran qui la capture est
/// celui d'une <b>mission</b>, et aucun endpoint mobile ne rendait cet identifiant : le front ne
/// pouvait pas construire l'URL. <c>MOB_MUTUELLE_CARD</c> est restée vide depuis juin, et le plan
/// concluait à un défaut d'adoption.
/// </para>
/// <para>
/// D'où le second cas ci-dessous : servir <c>Guid.Empty</c> aurait laissé le front appeler quand
/// même, et produire des cartes rattachées à un identifiant qui ne désigne personne.
/// </para>
/// </remarks>
public class JobDetailBeneficiaryIdTests
{
    private static readonly Guid JobId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid BeneficiaryId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void L_identifiant_du_beneficiaire_est_servi_au_detail_mission()
    {
        var detail = Detail(withBeneficiary: true);

        detail.Beneficiary.BeneficiaryId.Should().Be(BeneficiaryId);
    }

    /// <summary>
    /// Sans bénéficiaire résolu, le champ est <b>absent</b> — au front de masquer ce qui s'attache
    /// au patient, plutôt que d'appeler avec une clé nulle.
    /// </summary>
    [Fact]
    public void Sans_beneficiaire_resolu_le_champ_reste_vide()
    {
        var detail = Detail(withBeneficiary: false);

        detail.Beneficiary.BeneficiaryId.Should().BeNull();
    }

    /// <summary>Le reste du bloc patient ne bouge pas : l'ajout est additif (D14).</summary>
    [Fact]
    public void Le_reste_du_bloc_patient_est_inchange()
    {
        var detail = Detail(withBeneficiary: true);

        detail.Beneficiary.CompleteName.Should().Be("DUPONT Jean");
        // DDN est servie en chaîne au contrat mobile : on vérifie qu'elle est renseignée, pas son
        // format — l'y figer ferait échouer la suite sur un changement de culture du serveur.
        detail.Beneficiary.DDN.Should().NotBeNullOrWhiteSpace();
        detail.Beneficiary.Phones.Should().ContainSingle().Which.Should().Be("0600000000");
    }

    // ── Carte mutuelle du patient (04/10) : l'équipage la voit dans le détail ─────────

    [Fact]
    public async Task Le_detail_dit_que_la_carte_est_connue_et_de_quand_date_la_photo()
    {
        var photo = new DateTime(2026, 9, 20, 8, 30, 0, DateTimeKind.Utc);
        var repris = new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc);
        var card = new ClMutuelleCard { Id = Guid.NewGuid(), BeneficiaryId = BeneficiaryId, CapturedAt = photo, FieldsInheritedFrom = repris };

        var detail = await DetailViaUseCase(withBeneficiary: true, new FakeCards { Current = card });

        detail.MutuelleCardKnown.Should().BeTrue();
        detail.MutuelleCardId.Should().Be(card.Id);
        detail.MutuelleCardCapturedAt.Should().Be(photo);
        detail.MutuelleCardFieldsInheritedFrom.Should().Be(repris);
        detail.MutuelleCardImageUrl.Should().Be($"api/mutuelle-card/{card.Id}/image");
    }

    [Fact]
    public async Task Sans_carte_le_detail_le_dit_sans_url()
    {
        var detail = await DetailViaUseCase(withBeneficiary: true, new FakeCards());

        detail.MutuelleCardKnown.Should().BeFalse();
        detail.MutuelleCardId.Should().BeNull();
        detail.MutuelleCardCapturedAt.Should().BeNull();
        detail.MutuelleCardImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Sans_patient_la_carte_n_est_pas_cherchee()
    {
        var cards = new FakeCards();

        var detail = await DetailViaUseCase(withBeneficiary: false, cards);

        detail.MutuelleCardKnown.Should().BeFalse();
        cards.Lectures.Should().Be(0);
    }

    private static async Task<ClJobDetailModel> DetailViaUseCase(bool withBeneficiary, FakeCards cards)
    {
        var repo = new JobRepository(new FakeErp { WithBeneficiary = withBeneficiary }, new FakeJobTime(),
                                     new FakeSignature(), NullLogger<JobRepository>.Instance);
        var result = await new ClGetJobUseCase(JobId, repo, cards).HandleAsync(CancellationToken.None);
        result.IsSucces.Should().BeTrue();
        return result.Value;
    }

    /// <summary>Dépôt de cartes simulé : la carte courante, et combien de fois on l'a lue.</summary>
    private sealed class FakeCards : IMutuelleCardRepository
    {
        public ClMutuelleCard? Current;
        public int Lectures;

        public ClMutuelleCard GetCurrentMetadata(Guid beneficiaryId)
        {
            Lectures++;
            return Current!;
        }

        public void Save(ClMutuelleCard card) => throw new NotSupportedException();
        public ClMutuelleCardImage GetImage(Guid cardId) => throw new NotSupportedException();
        public ClMutuelleCardImage GetCurrentImage(Guid beneficiaryId) => throw new NotSupportedException();
        public IReadOnlyList<ClMutuelleCardPresence> ListPresence(IReadOnlyCollection<Guid> beneficiaryIds) => throw new NotSupportedException();
        public ClMutuelleCard Update(ClMutuelleCard card) => throw new NotSupportedException();
        public IReadOnlyList<Guid> ListPendingOcr(int take) => throw new NotSupportedException();
        public void SaveOcrProposal(Guid cardId, ClMutuelleCardOcrProposal proposal) => throw new NotSupportedException();
        public ClOcrFailureOutcome MarkOcrFailure(Guid cardId, string reason, int maxAttempts) => throw new NotSupportedException();
    }

    // ── Harnais ──────────────────────────────────────────────────────────────────

    private static ClJobDetailModel Detail(bool withBeneficiary)
    {
        var erp = new FakeErp { WithBeneficiary = withBeneficiary };
        var repo = new JobRepository(erp, new FakeJobTime(), new FakeSignature(),
                                     NullLogger<JobRepository>.Instance);
        return new ClJobDetailAdapter(repo.GetJobAsync(JobId, CancellationToken.None).GetAwaiter().GetResult()!);
    }

    private sealed class FakeErp : IErpReadApiClient
    {
        public bool WithBeneficiary;

        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpMissionFullDto?>(new ErpMissionFullDto
            {
                Id = id,
                OrderId = OrderId,
                MissionDate = new DateOnly(2026, 8, 26),
                SchedulingTime = new TimeOnly(9, 30)
            });

        public Task<ErpOrderEditDto?> GetOrderAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpOrderEditDto?>(new ErpOrderEditDto
            {
                Order = new ErpOrderBodyDto
                {
                    BeneficiaryId = WithBeneficiary ? BeneficiaryId : Guid.Empty
                }
            });

        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpBeneficiaryDetailDto?>(new ErpBeneficiaryDetailDto
            {
                Id = id,
                FirstName = "Jean",
                LastName = "DUPONT",
                BirthDate = new DateOnly(1954, 3, 2),
                PrimaryPhone = "0600000000"
            });

        // Membres non exercés par le détail mission.
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid p, DateOnly d, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid sub, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int?> GetMissionTransferStatusAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpMissionContextOrderDto?> GetMissionContextOrderAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ErpContextOrderFieldDto>?> GetContextOrderFormStructureAsync(Guid missionId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeJobTime : IJobTimeRepository
    {
        public void Save(Guid gJobId, ClJobTimeData timeData) { }
        public ClJobTimeData GetJobTimeData(Guid gJobId) => null!;
    }

    private sealed class FakeSignature : ISignatureRepository
    {
        public HashSet<Guid> ExistingFor(IEnumerable<Guid> ids) => new();
        public ClSignatureDtoOut Fetch(Guid id) => throw new NotSupportedException();
        public void Insert(Guid id, string d) => throw new NotSupportedException();
        public void Update(Guid id, string d) => throw new NotSupportedException();
        public void Delete(Guid id, string d) => throw new NotSupportedException();
        public bool Exists(Guid id) => false;
    }
}
