using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// 04/10 — Saisie des quatre champs d'une carte mutuelle : le <c>PATCH</c> remplace toujours les
/// quatre (contrat inchangé), mais un corps entièrement vide ne passe plus pour une validation, et un
/// champ trop long donne un 400 lisible au lieu d'un échec en base.
/// </summary>
public class MutuelleFieldsValidationTests
{
    private static readonly Guid Patient = Guid.Parse("22222222-0000-0000-0000-00000000000a");

    private static (MutuelleCardRepository Repository, Guid CardId) CarteValidee()
    {
        var repository = new MutuelleCardRepository(new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"saisie-{Guid.NewGuid()}").Options));
        var cardId = new ClUploadMutuelleCardUseCase(
                new ClUploadMutuelleCardCommand(Patient, new byte[] { 1 }, "image/jpeg", null, null), repository)
            .Handle().Value.Id;
        Saisir(repository, cardId, new ClMutuelleFieldsDtoIn
        {
            MutuelleName = "MGEN", AmcCode = "12345678", Concentrateur = "SP Santé", Teletransmission = "Oui"
        }).IsSucces.Should().BeTrue();
        return (repository, cardId);
    }

    private static CaSoft.Framework.ClResult<ClMutuelleCardDtoOut> Saisir(
        MutuelleCardRepository repository, Guid cardId, ClMutuelleFieldsDtoIn? fields)
        => new ClSetMutuelleFieldsUseCase(new ClSetMutuelleFieldsCommand(cardId, fields!), repository).Handle();

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData("", " ", "   ", "")]
    public void Une_saisie_vide_est_refusee_et_n_efface_rien(string? nom, string? amc, string? concentrateur, string? tele)
    {
        var (repository, cardId) = CarteValidee();

        var result = Saisir(repository, cardId, new ClMutuelleFieldsDtoIn
        {
            MutuelleName = nom, AmcCode = amc, Concentrateur = concentrateur, Teletransmission = tele
        });

        var http = result.ToActionResult().Should().BeOfType<BadRequestObjectResult>().Subject;
        http.Value.Should().Be("Saisie vide : renseignez au moins un des quatre champs. Rien n'a été enregistré.");
        repository.GetCurrentMetadata(Patient)!.AmcCode.Should().Be("12345678");
    }

    [Fact]
    public void Un_corps_absent_est_refuse()
    {
        var (repository, cardId) = CarteValidee();

        Saisir(repository, cardId, null).InnerError!.ErrorText.Should().Be("Corps de la saisie manquant.");
    }

    [Fact]
    public void Un_champ_plus_long_que_sa_colonne_est_refuse_avec_son_nom()
    {
        var (repository, cardId) = CarteValidee();

        var result = Saisir(repository, cardId, new ClMutuelleFieldsDtoIn
        {
            AmcCode = new string('9', ClMutuelleFieldsDtoInValidator.AmcCodeMax + 1)
        });

        result.IsFail.Should().BeTrue();
        result.InnerError!.ErrorText.Should().Be("Code AMC trop long (50 caractères au plus).");
        repository.GetCurrentMetadata(Patient)!.AmcCode.Should().Be("12345678");
    }

    [Fact]
    public void A_la_limite_de_la_colonne_la_saisie_passe()
    {
        var (repository, cardId) = CarteValidee();
        var amc = new string('9', ClMutuelleFieldsDtoInValidator.AmcCodeMax);

        Saisir(repository, cardId, new ClMutuelleFieldsDtoIn { AmcCode = amc }).IsSucces.Should().BeTrue();

        repository.GetCurrentMetadata(Patient)!.AmcCode.Should().Be(amc);
    }

    [Fact]
    public void Un_seul_champ_suffit_et_le_PATCH_remplace_toujours_les_quatre()
    {
        var (repository, cardId) = CarteValidee();

        Saisir(repository, cardId, new ClMutuelleFieldsDtoIn { AmcCode = "87654321" }).IsSucces.Should().BeTrue();

        // Contrat inchangé (D14) : un champ absent repasse à vide.
        var carte = repository.GetCurrentMetadata(Patient)!;
        carte.AmcCode.Should().Be("87654321");
        carte.MutuelleName.Should().BeNull();
        carte.OcrStatus.Should().Be("validated");
    }
}
