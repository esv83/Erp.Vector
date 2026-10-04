using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Domain;
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
    private static readonly CancellationToken Ct = CancellationToken.None;

    private static async Task<(MutuelleCardRepository Repository, Guid CardId)> CarteValidee()
    {
        var repository = new MutuelleCardRepository(new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"saisie-{Guid.NewGuid()}").Options));
        var cardId = (await new ClUploadMutuelleCardUseCase(
                new ClUploadMutuelleCardCommand(Patient, new byte[] { 1 }, "image/jpeg", null, null), repository)
            .HandleAsync(Ct)).Value.Id;
        (await Saisir(repository, cardId, new ClMutuelleFieldsDtoIn
        {
            MutuelleName = "MGEN", AmcCode = "12345678", Concentrateur = "SP Santé", Teletransmission = "Oui"
        })).IsSucces.Should().BeTrue();
        return (repository, cardId);
    }

    private static Task<CaSoft.Framework.ClResult<ClMutuelleCardDtoOut>> Saisir(
        MutuelleCardRepository repository, Guid cardId, ClMutuelleFieldsDtoIn? fields)
        => new ClSetMutuelleFieldsUseCase(new ClSetMutuelleFieldsCommand(cardId, fields!), repository).HandleAsync(Ct);

    private static async Task<ClMutuelleCard> Courante(MutuelleCardRepository repository)
        => (await repository.GetCurrentMetadataAsync(Patient, Ct))!;

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData("", " ", "   ", "")]
    public async Task Une_saisie_vide_est_refusee_et_n_efface_rien(string? nom, string? amc, string? concentrateur, string? tele)
    {
        var (repository, cardId) = await CarteValidee();

        var result = await Saisir(repository, cardId, new ClMutuelleFieldsDtoIn
        {
            MutuelleName = nom, AmcCode = amc, Concentrateur = concentrateur, Teletransmission = tele
        });

        var http = result.ToActionResult().Should().BeOfType<BadRequestObjectResult>().Subject;
        http.Value.Should().Be("Saisie vide : renseignez au moins un des quatre champs. Rien n'a été enregistré.");
        (await Courante(repository)).AmcCode.Should().Be("12345678");
    }

    [Fact]
    public async Task Un_corps_absent_est_refuse()
    {
        var (repository, cardId) = await CarteValidee();

        (await Saisir(repository, cardId, null)).InnerError!.ErrorText.Should().Be("Corps de la saisie manquant.");
    }

    [Fact]
    public async Task Un_champ_plus_long_que_sa_colonne_est_refuse_avec_son_nom()
    {
        var (repository, cardId) = await CarteValidee();

        var result = await Saisir(repository, cardId, new ClMutuelleFieldsDtoIn
        {
            AmcCode = new string('9', ClMutuelleFieldsDtoInValidator.AmcCodeMax + 1)
        });

        result.IsFail.Should().BeTrue();
        result.InnerError!.ErrorText.Should().Be("Code AMC trop long (50 caractères au plus).");
        (await Courante(repository)).AmcCode.Should().Be("12345678");
    }

    [Fact]
    public async Task A_la_limite_de_la_colonne_la_saisie_passe()
    {
        var (repository, cardId) = await CarteValidee();
        var amc = new string('9', ClMutuelleFieldsDtoInValidator.AmcCodeMax);

        (await Saisir(repository, cardId, new ClMutuelleFieldsDtoIn { AmcCode = amc })).IsSucces.Should().BeTrue();

        (await Courante(repository)).AmcCode.Should().Be(amc);
    }

    [Fact]
    public async Task Un_seul_champ_suffit_et_le_PATCH_remplace_toujours_les_quatre()
    {
        var (repository, cardId) = await CarteValidee();

        (await Saisir(repository, cardId, new ClMutuelleFieldsDtoIn { AmcCode = "87654321" })).IsSucces.Should().BeTrue();

        // Contrat inchangé (D14) : un champ absent repasse à vide.
        var carte = await Courante(repository);
        carte.AmcCode.Should().Be("87654321");
        carte.MutuelleName.Should().BeNull();
        carte.OcrStatus.Should().Be("validated");
    }
}
