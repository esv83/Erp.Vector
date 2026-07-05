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
        public FakeJobTime(ClJobTimeData? existing) => Stored = existing;
        public void Save(Guid id, ClJobTimeData d) { Stored = d; SaveCount++; }
        public ClJobTimeData? GetJobTimeData(Guid id) => Stored;
    }

    [Fact]
    public void Premiere_vue_pose_ReadTime_et_sauvegarde()
    {
        var repo = new FakeJobTime(null);

        var result = new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo).Handle();

        result.IsSucces.Should().BeTrue();
        result.Value.Should().BeTrue();
        repo.SaveCount.Should().Be(1);
        repo.Stored!.ReadTime.Should().NotBeNull();
    }

    [Fact]
    public void Deja_vue_est_idempotent_sans_re_sauvegarde()
    {
        var existing = ClJobTimeData.GetBuilder()
            .WithId(Guid.NewGuid())
            .WithReadTime(new DateTime(2026, 1, 1))
            .Build();
        var repo = new FakeJobTime(existing);

        var result = new ClMarkMissionSeenUseCase(Guid.NewGuid(), repo).Handle();

        result.IsSucces.Should().BeTrue();
        result.Value.Should().BeTrue();
        repo.SaveCount.Should().Be(0); // no-op : l'horodatage d'origine est conservé
    }
}
