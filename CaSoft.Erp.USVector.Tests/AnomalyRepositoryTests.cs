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
    public void Save_then_ListByMission_returns_anomaly()
    {
        using var ctx = NewContext();
        var sut = new AnomalyRepository(ctx);

        sut.Save(Anomaly(EnAnomalyType.Address, new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc), "mauvaise adresse"));

        var list = sut.ListByMission(Mission);
        list.Should().HaveCount(1);
        list[0].Type.Should().Be(EnAnomalyType.Address);
        list[0].Text.Should().Be("mauvaise adresse");
    }

    [Fact]
    public void ListByMission_returns_most_recent_first()
    {
        using var ctx = NewContext();
        var sut = new AnomalyRepository(ctx);
        sut.Save(Anomaly(EnAnomalyType.Phone, new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc)));
        sut.Save(Anomaly(EnAnomalyType.Patient, new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc)));

        var list = sut.ListByMission(Mission);
        list.Should().HaveCount(2);
        list[0].Type.Should().Be(EnAnomalyType.Patient); // plus récente d'abord
    }

    [Fact]
    public void ListByMission_empty_when_none()
    {
        using var ctx = NewContext();
        var sut = new AnomalyRepository(ctx);

        sut.ListByMission(Mission).Should().BeEmpty();
    }
}
