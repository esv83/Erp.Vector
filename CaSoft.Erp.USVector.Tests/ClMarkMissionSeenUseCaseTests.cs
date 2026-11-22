using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Phase 1 (pilote Result pattern) — « Mission vue » migré en Handle() : ClResult(Of Boolean).
/// Première vue → pose MST_READ_AT + Save ; déjà vue → idempotent (no-op, pas de re-Save).
/// </summary>
public class ClMarkMissionSeenUseCaseTests
{
    private sealed class FakeJobTime : IJobTimeRepository
    {
        public ClJobTimeData? Stored;
        public int SaveCount;
        public Exception? Throws;
        public FakeJobTime(ClJobTimeData? existing) => Stored = existing;
        public Task SaveAsync(Guid id, ClJobTimeData d, CancellationToken ct) { Stored = d; SaveCount++; return Task.CompletedTask; }
        public Task<ClJobTimeData> GetJobTimeDataAsync(Guid id, CancellationToken ct)
            => Throws is null ? Task.FromResult(Stored!) : throw Throws;
    }

    [Fact]
    public async Task Premiere_vue_pose_ReadTime_et_sauvegarde()
    {
        var repo = new FakeJobTime(null);
        var unitOfWork = new UnitOfWorkDoubles.Counting();

        var result = await new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo, unitOfWork).HandleAsync(CancellationToken.None);

        result.IsSucces.Should().BeTrue();
        result.Value.Should().BeTrue();
        repo.SaveCount.Should().Be(1);
        unitOfWork.Commits.Should().Be(1, "DR-002 : le cas d'usage enregistre, une fois");
        repo.Stored!.ReadTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Deja_vue_est_idempotent_sans_re_sauvegarde()
    {
        var existing = ClJobTimeData.GetBuilder()
            .WithId(Guid.NewGuid())
            .WithReadTime(new DateTime(2026, 1, 1))
            .Build();
        var repo = new FakeJobTime(existing);
        var unitOfWork = new UnitOfWorkDoubles.Counting();

        var result = await new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo, unitOfWork).HandleAsync(CancellationToken.None);

        result.IsSucces.Should().BeTrue();
        result.Value.Should().BeTrue();
        repo.SaveCount.Should().Be(0); // no-op : l'horodatage d'origine est conservé
        unitOfWork.Commits.Should().Be(0, "rien à enregistrer");
    }

    [Fact]
    public async Task Une_panne_de_la_base_remonte_au_lieu_de_devenir_un_400()
    {
        // Règle du 04/10 : le gestionnaire de l'API la rendra en 503. Avant, l'ambulancier recevait
        // un 400 portant le message technique de SQL Server.
        var repo = new FakeJobTime(null) { Throws = new InvalidOperationException("transient failure") };

        var act = () => new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo, new UnitOfWorkDoubles.Counting()).HandleAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Une_signature_demandee_sans_mission_reste_un_400()
    {
        var result = await new ClGetSignatureUseCase(Guid.Empty, null!).HandleAsync(CancellationToken.None);

        result.IsFail.Should().BeTrue();
        result.InnerError!.ErrorText.Should().Be("Identifiant de mission vide.");
    }

    // ── Heure du clic (22/11, J-172 : « Vu » hors réseau, envoyé au retour du réseau) ──────────

    private static async Task<(CaSoft.Framework.ClResult<bool> Resultat, FakeJobTime Repo)> VuAAsync(DateTimeOffset? clic)
    {
        var repo = new FakeJobTime(null);
        var resultat = await new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo, new UnitOfWorkDoubles.Counting(), clic)
            .HandleAsync(CancellationToken.None);
        return (resultat, repo);
    }

    [Fact(DisplayName = "Un « Vu » donné hors réseau porte l'heure du clic, en heure locale")]
    public async Task Le_vu_porte_l_heure_du_clic()
    {
        var clic = DateTimeOffset.Now.AddMinutes(-40);

        var (resultat, repo) = await VuAAsync(clic.ToUniversalTime());

        resultat.IsSucces.Should().BeTrue();
        repo.Stored!.ReadTime.Should().BeCloseTo(clic.LocalDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Une horloge de téléphone un peu en avance : ramenée à maintenant")]
    public async Task Une_heure_legerement_dans_le_futur_est_ramenee_a_maintenant()
    {
        var (resultat, repo) = await VuAAsync(DateTimeOffset.Now.AddMinutes(3));

        resultat.IsSucces.Should().BeTrue();
        repo.Stored!.ReadTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Plus de 5 minutes dans le futur : refusé, rien n'est écrit")]
    public async Task Une_heure_dans_le_futur_est_refusee()
    {
        var (resultat, repo) = await VuAAsync(DateTimeOffset.Now.AddMinutes(10));

        resultat.IsFail.Should().BeTrue();
        repo.SaveCount.Should().Be(0);
    }

    [Fact(DisplayName = "Plus de 24 heures en arrière : refusé, rien n'est écrit")]
    public async Task Une_heure_trop_ancienne_est_refusee()
    {
        var (resultat, repo) = await VuAAsync(DateTimeOffset.Now.AddHours(-25));

        resultat.IsFail.Should().BeTrue();
        repo.SaveCount.Should().Be(0);
    }
}
