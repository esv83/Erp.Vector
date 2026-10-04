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

        var result = await new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo).HandleAsync(CancellationToken.None);

        result.IsSucces.Should().BeTrue();
        result.Value.Should().BeTrue();
        repo.SaveCount.Should().Be(1);
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

        var result = await new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo).HandleAsync(CancellationToken.None);

        result.IsSucces.Should().BeTrue();
        result.Value.Should().BeTrue();
        repo.SaveCount.Should().Be(0); // no-op : l'horodatage d'origine est conservé
    }

    [Fact]
    public async Task Une_panne_de_la_base_remonte_au_lieu_de_devenir_un_400()
    {
        // Règle du 04/10 : le gestionnaire de l'API la rendra en 503. Avant, l'ambulancier recevait
        // un 400 portant le message technique de SQL Server.
        var repo = new FakeJobTime(null) { Throws = new InvalidOperationException("transient failure") };

        var act = () => new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo).HandleAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Une_signature_demandee_sans_mission_reste_un_400()
    {
        var result = await new ClGetSignatureUseCase(Guid.Empty, null!).HandleAsync(CancellationToken.None);

        result.IsFail.Should().BeTrue();
        result.InnerError!.ErrorText.Should().Be("Identifiant de mission vide.");
    }
}
