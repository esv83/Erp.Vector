using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// 04/10 — Une nouvelle photo d'un patient connu hérite des champs validés de la précédente, marqués
/// « repris de la photo du JJ/MM ». Avant, la mutuelle disparaissait de la carte courante à chaque
/// photo, alors que la carte n'avait le plus souvent pas changé.
/// </summary>
public class MutuelleCardInheritanceTests
{
    private static readonly Guid Patient = Guid.Parse("22222222-0000-0000-0000-000000000009");

    private static MutuelleCardRepository Repository()
        => new(new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"heritage-{Guid.NewGuid()}").Options));

    private static Guid Photographier(MutuelleCardRepository repository)
    {
        var result = new ClUploadMutuelleCardUseCase(
                new ClUploadMutuelleCardCommand(Patient, new byte[] { 1, 2, 3 }, "image/jpeg", null, null), repository)
            .Handle();
        result.IsSucces.Should().BeTrue();
        // Deux photos dans la même milliseconde se départageraient mal : la plus récente fait foi.
        Thread.Sleep(5);
        return result.Value.Id;
    }

    private static void Valider(MutuelleCardRepository repository, Guid cardId, string amc)
        => new ClSetMutuelleFieldsUseCase(
                new ClSetMutuelleFieldsCommand(cardId, new ClMutuelleFieldsDtoIn
                {
                    MutuelleName = "MGEN", AmcCode = amc, Concentrateur = "SP Santé", Teletransmission = "Oui"
                }),
                repository)
            .Handle().IsSucces.Should().BeTrue();

    [Fact]
    public void Une_premiere_photo_n_herite_de_rien()
    {
        var repository = Repository();

        Photographier(repository);

        var courante = repository.GetCurrentMetadata(Patient)!;
        courante.AmcCode.Should().BeNull();
        courante.FieldsInheritedFrom.Should().BeNull();
    }

    [Fact]
    public void Une_nouvelle_photo_reprend_les_champs_valides_et_dit_de_quelle_photo()
    {
        var repository = Repository();
        var premiere = Photographier(repository);
        Valider(repository, premiere, "12345678");
        var datePremiere = repository.GetCurrentMetadata(Patient)!.CapturedAt;

        Photographier(repository);

        var courante = repository.GetCurrentMetadata(Patient)!;
        courante.Id.Should().NotBe(premiere);
        courante.MutuelleName.Should().Be("MGEN");
        courante.AmcCode.Should().Be("12345678");
        courante.Concentrateur.Should().Be("SP Santé");
        courante.Teletransmission.Should().Be("Oui");
        courante.FieldsInheritedFrom.Should().Be(datePremiere);
        courante.ToDtoOut().FieldsInheritedFrom.Should().Be(datePremiere);
        // La lecture automatique, elle, a sa nouvelle photo à lire.
        courante.OcrStatus.Should().Be("pending");
    }

    [Fact]
    public void Reprise_deux_fois_elle_garde_la_date_de_la_photo_d_origine()
    {
        var repository = Repository();
        var premiere = Photographier(repository);
        Valider(repository, premiere, "12345678");
        var datePremiere = repository.GetCurrentMetadata(Patient)!.CapturedAt;

        Photographier(repository);
        Photographier(repository);

        var courante = repository.GetCurrentMetadata(Patient)!;
        courante.AmcCode.Should().Be("12345678");
        courante.FieldsInheritedFrom.Should().Be(datePremiere);
    }

    [Fact]
    public void Valider_sur_la_nouvelle_photo_efface_la_mention_de_reprise()
    {
        var repository = Repository();
        Valider(repository, Photographier(repository), "12345678");
        var seconde = Photographier(repository);

        Valider(repository, seconde, "87654321");

        var courante = repository.GetCurrentMetadata(Patient)!;
        courante.AmcCode.Should().Be("87654321");
        courante.FieldsInheritedFrom.Should().BeNull();
    }

    [Fact]
    public void Une_photo_precedente_jamais_validee_ne_transmet_rien()
    {
        var repository = Repository();
        Photographier(repository);

        Photographier(repository);

        var courante = repository.GetCurrentMetadata(Patient)!;
        courante.AmcCode.Should().BeNull();
        courante.FieldsInheritedFrom.Should().BeNull();
    }
}
