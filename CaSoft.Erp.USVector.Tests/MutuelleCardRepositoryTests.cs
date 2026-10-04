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
    public async Task Save_then_GetImage_returns_bytes()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);
        var card = Card(new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc), 1, 2, 3);

        await sut.SaveAsync(card, CancellationToken.None);

        var loaded = (await sut.GetImageAsync(card.Id, CancellationToken.None));
        loaded.Should().NotBeNull();
        loaded!.Bytes.Should().Equal(1, 2, 3);
        loaded.ContentType.Should().Be("image/jpeg");
    }

    /// <summary>
    /// Le point du correctif : lire des métadonnées ne doit pas sortir le binaire de la base. Une
    /// carte de 8 Mo était chargée en entier pour rendre un nom de mutuelle — une fois par mission
    /// dans la construction du paquet terrain.
    /// </summary>
    [Fact]
    public async Task GetCurrentMetadata_ne_charge_pas_le_binaire()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);
        await sut.SaveAsync(Card(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), 1, 2, 3), CancellationToken.None);

        var meta = (await sut.GetCurrentMetadataAsync(Ben, CancellationToken.None));

        meta.Should().NotBeNull();
        meta!.ByteSize.Should().Be(3, "la taille reste une métadonnée, elle");
        meta.ContentType.Should().Be("image/jpeg");
        meta.Image.Should().BeNull("le binaire n'est pas dans la projection");
    }

    /// <summary>
    /// Le sondage par lot : une entrée par bénéficiaire qui porte une carte, la plus récente, et
    /// <b>rien</b> pour ceux qui n'en ont pas — c'est ce qui permet à l'appelant de ne rien afficher
    /// sans avoir demandé ligne par ligne.
    /// </summary>
    [Fact]
    public async Task ListPresence_rend_la_plus_recente_et_ignore_les_beneficiaires_sans_carte()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);
        var autre = Guid.Parse("dddddddd-0000-0000-0000-000000000002");
        var sansCarte = Guid.Parse("dddddddd-0000-0000-0000-000000000003");

        await sut.SaveAsync(Card(new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc), 1), CancellationToken.None);
        await sut.SaveAsync(Card(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), 9), CancellationToken.None);
        var carteAutre = Card(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc), 7);
        carteAutre.BeneficiaryId = autre;
        await sut.SaveAsync(carteAutre, CancellationToken.None);

        var presences = (await sut.ListPresenceAsync(new[] { Ben, autre, sansCarte }, CancellationToken.None));

        presences.Should().HaveCount(2);
        presences.Single(p => p.BeneficiaryId == Ben).CapturedAt
            .Should().Be(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        presences.Should().NotContain(p => p.BeneficiaryId == sansCarte);
    }

    [Fact]
    public async Task ListPresence_sur_un_lot_vide_ne_touche_pas_la_base()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);

        (await sut.ListPresenceAsync(Array.Empty<Guid>(), CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentMetadata_returns_most_recent_capture()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);
        await sut.SaveAsync(Card(new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc), 1), CancellationToken.None);
        var recent = Card(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), 9);
        await sut.SaveAsync(recent, CancellationToken.None);

        (await sut.GetCurrentMetadataAsync(Ben, CancellationToken.None))!.Id.Should().Be(recent.Id);
    }

    [Fact]
    public async Task GetCurrentMetadata_returns_null_when_no_card()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);

        (await sut.GetCurrentMetadataAsync(Ben, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Update_sets_mutuelle_fields_and_keeps_image()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);
        var card = Card(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), 1, 2);
        await sut.SaveAsync(card, CancellationToken.None);

        var patch = new ClMutuelleCard
        {
            Id = card.Id,
            AmcCode = "AMC123",
            MutuelleName = "Ma Mutuelle",
            Concentrateur = "ConcentX",
            Teletransmission = "TLT9",
            OcrStatus = "validated",
            OcrValidatedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
        };

        var updated = (await sut.UpdateAsync(patch, CancellationToken.None));

        updated.Should().NotBeNull();
        updated!.AmcCode.Should().Be("AMC123");
        updated.OcrStatus.Should().Be("validated");
        (await sut.GetImageAsync(card.Id, CancellationToken.None))!.Bytes.Should().Equal(1, 2); // image intacte
    }

    [Fact]
    public async Task Update_returns_null_when_card_unknown()
    {
        using var ctx = NewContext();
        var sut = new MutuelleCardRepository(ctx);

        (await sut.UpdateAsync(new ClMutuelleCard { Id = Guid.NewGuid(), AmcCode = "X" }, CancellationToken.None)).Should().BeNull();
    }
}
