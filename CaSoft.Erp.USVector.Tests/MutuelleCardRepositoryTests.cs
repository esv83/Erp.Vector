using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>P1 — Tests du stockage des cartes mutuelle (EF Core InMemory).</summary>
public class MutuelleCardRepositoryTests
{
    private static readonly Guid Ben = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"mutuelle-{Guid.NewGuid()}").Options);

    private static ClMutuelleCard Card(DateTime capturedAt, params byte[] image)
        => new()
        {
            Id = Guid.NewGuid(),
            BeneficiaryId = Ben,
            Image = image,
            ContentType = "image/jpeg",
            ByteSize = image.Length,
            CapturedAt = capturedAt,
            OcrStatus = "none",
        };

    [Fact]
    public void Save_then_GetById_returns_card_with_image()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);
        var card = Card(new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc), 1, 2, 3);

        sut.Save(card);

        var loaded = sut.GetById(card.Id);
        loaded.Should().NotBeNull();
        loaded!.BeneficiaryId.Should().Be(Ben);
        loaded.Image.Should().Equal(1, 2, 3);
        loaded.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public void GetCurrent_returns_most_recent_capture()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);
        sut.Save(Card(new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc), 1));
        var recent = Card(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), 9);
        sut.Save(recent);

        sut.GetCurrent(Ben)!.Id.Should().Be(recent.Id);
    }

    [Fact]
    public void GetCurrent_returns_null_when_no_card()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);

        sut.GetCurrent(Ben).Should().BeNull();
    }
}
