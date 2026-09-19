using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>TRF-10 — Stockage des documents/photos terrain (EF Core InMemory).</summary>
public class DocumentRepositoryTests
{
    private static readonly Guid Mission = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"document-{Guid.NewGuid()}").Options);

    private static ClDocument Doc(EnDocumentCategory cat, DateTime at, params byte[] content)
        => new()
        {
            Id = Guid.NewGuid(),
            MissionId = Mission,
            Category = cat,
            Content = content,
            ContentType = "application/pdf",
            ByteSize = content.Length,
            FileName = "bon.pdf",
            CapturedAt = at,
        };

    [Fact]
    public void Save_then_GetById_returns_document_with_content()
    {
        using var ctx = NewContext();
        var sut = new DocumentRepository(ctx);
        var doc = Doc(EnDocumentCategory.TransportOrder, new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc), 1, 2, 3);

        sut.Save(doc);

        var loaded = sut.GetById(doc.Id);
        loaded.Should().NotBeNull();
        loaded!.Category.Should().Be(EnDocumentCategory.TransportOrder);
        loaded.Content.Should().Equal(1, 2, 3);
        loaded.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public void ListByMission_returns_most_recent_first()
    {
        using var ctx = NewContext();
        var sut = new DocumentRepository(ctx);
        sut.Save(Doc(EnDocumentCategory.Prescription, new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc), 1));
        var recent = Doc(EnDocumentCategory.Administrative, new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc), 2);
        sut.Save(recent);

        var list = sut.ListByMission(Mission);
        list.Should().HaveCount(2);
        list[0].Id.Should().Be(recent.Id);
    }

    /// <summary>
    /// La liste de l'app n'affiche que des métadonnées : elle ne doit pas sortir les octets de la base.
    /// Le document entier reste servi, un par un, par GetById.
    /// </summary>
    [Fact]
    public void ListByMission_ne_charge_pas_le_contenu()
    {
        using var ctx = NewContext();
        var sut = new DocumentRepository(ctx);
        var doc = Doc(EnDocumentCategory.TransportOrder, new DateTime(2026, 9, 19, 8, 0, 0, DateTimeKind.Utc), 1, 2, 3);
        sut.Save(doc);

        var listed = sut.ListByMission(Mission).Single();

        listed.Content.Should().BeNull();
        listed.ByteSize.Should().Be(doc.ByteSize);
        listed.ContentType.Should().Be("application/pdf");
        sut.GetById(doc.Id)!.Content.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void GetById_returns_null_when_unknown()
    {
        using var ctx = NewContext();
        var sut = new DocumentRepository(ctx);

        sut.GetById(Guid.NewGuid()).Should().BeNull();
    }
}
