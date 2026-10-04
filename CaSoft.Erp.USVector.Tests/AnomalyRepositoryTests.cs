using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>TRF-8 — Stockage des anomalies terrain (EF Core InMemory).</summary>
public class AnomalyRepositoryTests
{
    private static readonly Guid Mission = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"anomaly-{Guid.NewGuid()}").Options);

    private static ClAnomaly Anomaly(EnAnomalyType type, DateTime at, string text = "x")
        => new() { Id = Guid.NewGuid(), MissionId = Mission, Type = type, Text = text, ReportedAt = at };

    [Fact]
    public async Task Save_then_ListByMission_returns_anomaly()
    {
        using var ctx = NewContext();
        var sut = new AnomalyRepository(ctx);

        await sut.SaveAsync(Anomaly(EnAnomalyType.Address, new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc), "mauvaise adresse"), CancellationToken.None);

        var list = (await sut.ListByMissionAsync(Mission, CancellationToken.None));
        list.Should().HaveCount(1);
        list[0].Type.Should().Be(EnAnomalyType.Address);
        list[0].Text.Should().Be("mauvaise adresse");
    }

    [Fact]
    public async Task ListByMission_returns_most_recent_first()
    {
        using var ctx = NewContext();
        var sut = new AnomalyRepository(ctx);
        await sut.SaveAsync(Anomaly(EnAnomalyType.Phone, new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc)), CancellationToken.None);
        await sut.SaveAsync(Anomaly(EnAnomalyType.Patient, new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        var list = (await sut.ListByMissionAsync(Mission, CancellationToken.None));
        list.Should().HaveCount(2);
        list[0].Type.Should().Be(EnAnomalyType.Patient); // plus récente d'abord
    }

    [Fact]
    public async Task ListByMission_empty_when_none()
    {
        using var ctx = NewContext();
        var sut = new AnomalyRepository(ctx);

        (await sut.ListByMissionAsync(Mission, CancellationToken.None)).Should().BeEmpty();
    }
}
