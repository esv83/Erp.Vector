using CaSoft.Erp.USVector.Api.Workers;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Ocr;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// P3 — Lecture automatique des cartes mutuelle. La promesse tenue ici est <b>M5</b> : ce que le
/// modèle lit est <b>proposé</b>, jamais écrit dans les champs que la facturation exporte.
/// </summary>
public class MutuelleCardOcrTests
{
    private static readonly Guid Ben = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"ocr-{Guid.NewGuid()}").Options);

    private static ClMutuelleCard Carte(string statut = "pending") => new()
    {
        Id = Guid.NewGuid(),
        BeneficiaryId = Ben,
        Image = new byte[] { 1, 2, 3 },
        ContentType = "image/jpeg",
        ByteSize = 3,
        CapturedAt = DateTime.UtcNow,
        OcrStatus = statut,
        // Une saisie humaine déjà faite : elle ne doit pas bouger.
        AmcCode = "SAISI-A-LA-MAIN"
    };

    /// <summary>Le modèle rend ce qu'on lui a dit de rendre ; la file, elle, est simulée.</summary>
    private sealed class FakeOcr : IMutuelleCardOcrService
    {
        public ClMutuelleCardOcrProposal? Proposal;
        public Exception? Throws;
        public int Calls;

        public Task<ClMutuelleCardOcrProposal> ExtractAsync(byte[] image, string contentType, CancellationToken ct)
        {
            Calls++;
            if (Throws is not null) throw Throws;
            return Task.FromResult(Proposal ?? new ClMutuelleCardOcrProposal());
        }
    }

    private static MutuelleCardOcrDispatcher Dispatcher(MobileDbContext ctx, IMutuelleCardOcrService ocr,
        MutuelleCardOcrOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<IMutuelleCardRepository>(_ => new MutuelleCardRepository(ctx));
        services.AddScoped(_ => ocr);

        return new MutuelleCardOcrDispatcher(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            options ?? new MutuelleCardOcrOptions(),
            NullLogger<MutuelleCardOcrDispatcher>.Instance);
    }

    // ── Ce que la lecture écrit, et ce qu'elle ne touche pas ─────────────────

    [Fact]
    public async Task La_lecture_propose_sans_toucher_les_champs_officiels()
    {
        using var ctx = NewContext();
        var repository = new MutuelleCardRepository(ctx);
        var carte = Carte();
        repository.Save(carte);

        var ocr = new FakeOcr
        {
            Proposal = new ClMutuelleCardOcrProposal
            {
                MutuelleName = "Harmonie Mutuelle",
                AmcCode = "12345678",
                Confidence = 0.92m
            }
        };

        await Dispatcher(ctx, ocr).ReadPendingAsync(CancellationToken.None);

        var relue = repository.GetCurrentMetadata(Ben)!;
        relue.OcrStatus.Should().Be("extracted");
        relue.OcrMutuelleName.Should().Be("Harmonie Mutuelle");
        relue.OcrAmcCode.Should().Be("12345678");
        relue.OcrConfidence.Should().Be(0.92m);
        relue.OcrExtractedAt.Should().NotBeNull();
        // 🔴 M5 — la saisie humaine est intacte : rien n'est écrit d'autorité.
        relue.AmcCode.Should().Be("SAISI-A-LA-MAIN");
        relue.MutuelleName.Should().BeNull();
    }

    [Fact]
    public async Task Une_carte_deja_lue_ne_repart_pas_dans_la_file()
    {
        using var ctx = NewContext();
        new MutuelleCardRepository(ctx).Save(Carte("extracted"));
        var ocr = new FakeOcr();

        await Dispatcher(ctx, ocr).ReadPendingAsync(CancellationToken.None);

        ocr.Calls.Should().Be(0);
    }

    /// <summary>
    /// Le modèle a répondu et n'a rien lu : c'est un résultat. La carte sort de la file — une photo
    /// floue ne deviendra pas lisible à la dixième relance.
    /// </summary>
    [Fact]
    public async Task Une_carte_illisible_sort_de_la_file_sans_proposition()
    {
        using var ctx = NewContext();
        var repository = new MutuelleCardRepository(ctx);
        repository.Save(Carte());

        await Dispatcher(ctx, new FakeOcr { Proposal = new ClMutuelleCardOcrProposal() })
            .ReadPendingAsync(CancellationToken.None);

        var relue = repository.GetCurrentMetadata(Ben)!;
        relue.OcrStatus.Should().Be("extracted");
        relue.OcrAmcCode.Should().BeNull();
        relue.OcrLastError.Should().BeNull();
    }

    // ── Les échecs ───────────────────────────────────────────────────────────

    /// <summary>
    /// Une panne se retente, mais pas sans fin : la file de projection a relancé 55 450 fois une
    /// mission qui ne reviendrait jamais.
    /// </summary>
    [Fact]
    public async Task Une_panne_se_retente_puis_la_carte_est_abandonnee()
    {
        using var ctx = NewContext();
        var repository = new MutuelleCardRepository(ctx);
        repository.Save(Carte());
        var ocr = new FakeOcr { Throws = new HttpRequestException("service indisponible") };
        var dispatcher = Dispatcher(ctx, ocr, new MutuelleCardOcrOptions { MaxAttempts = 3 });

        await dispatcher.ReadPendingAsync(CancellationToken.None);
        repository.GetCurrentMetadata(Ben)!.OcrStatus.Should().Be("pending", "une panne peut passer");

        await dispatcher.ReadPendingAsync(CancellationToken.None);
        await dispatcher.ReadPendingAsync(CancellationToken.None);

        var relue = repository.GetCurrentMetadata(Ben)!;
        relue.OcrStatus.Should().Be("error");
        relue.OcrAttempts.Should().Be(3);
        relue.OcrLastError.Should().Contain("service indisponible");
        ocr.Calls.Should().Be(3, "une carte abandonnée ne coûte plus d'appel");
    }

    [Fact]
    public void Le_motif_d_echec_est_borne_a_la_colonne()
    {
        using var ctx = NewContext();
        var repository = new MutuelleCardRepository(ctx);
        var carte = Carte();
        repository.Save(carte);

        var issue = repository.MarkOcrFailure(carte.Id, new string('x', 900), maxAttempts: 5);

        issue.Attempts.Should().Be(1);
        issue.GaveUp.Should().BeFalse();
        repository.GetCurrentMetadata(Ben)!.OcrLastError!.Length.Should().Be(400);
    }

    // ── Les réglages, et la sortie du modèle ─────────────────────────────────

    /// <summary>Sans clé — et avec le secret de remplacement — tout est inerte : rien n'appelle de modèle.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(MutuelleCardOcrOptions.Placeholder)]
    public void Sans_cle_la_lecture_automatique_est_inerte(string? cle)
        => new MutuelleCardOcrOptions { ApiKey = cle }.IsConfigured.Should().BeFalse();

    [Fact]
    public void Avec_une_cle_la_lecture_est_configuree_et_le_modele_est_celui_par_defaut()
    {
        var options = new MutuelleCardOcrOptions { ApiKey = "sk-ant-…" };

        options.IsConfigured.Should().BeTrue();
        options.Model.Should().Be("claude-opus-5");
    }

    private static ClaudeMutuelleCardOcrService Service()
        => new(new Anthropic.AnthropicClient { ApiKey = "test" },
               new MutuelleCardOcrOptions { ApiKey = "test" },
               NullLogger<ClaudeMutuelleCardOcrService>.Instance);

    [Fact]
    public void La_reponse_du_modele_est_lue_champ_par_champ()
    {
        var proposition = Service().Parse(
            """{"mutuelleName":" Harmonie ","amcCode":"12345678","concentrateur":null,"teletransmission":"","confidence":0.87}""");

        proposition.MutuelleName.Should().Be("Harmonie");          // rogné
        proposition.AmcCode.Should().Be("12345678");
        proposition.Concentrateur.Should().BeNull();               // null reste null
        proposition.Teletransmission.Should().BeNull();            // vide vaut null
        proposition.Confidence.Should().Be(0.87m);
        proposition.HasValue.Should().BeTrue();
    }

    /// <summary>
    /// Le schéma impose la forme, mais la réponse traverse un réseau : un corps illisible rend une
    /// proposition vide, jamais une exception — sans quoi la carte repartirait en file pour rien.
    /// </summary>
    [Theory]
    [InlineData("<html>proxy</html>")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Une_reponse_inexploitable_ne_leve_pas(string corps)
    {
        var proposition = Service().Parse(corps);

        proposition.HasValue.Should().BeFalse();
        proposition.Confidence.Should().BeNull();
    }

    [Theory]
    [InlineData("1.4", 1.0)]
    [InlineData("-0.2", 0.0)]
    [InlineData("0.873", 0.873)]
    public void La_confiance_est_bornee_entre_zero_et_un(string rendue, double attendue)
    {
        // Le corps JSON s'écrit avec un point décimal, quelle que soit la culture de la machine.
        var proposition = Service().Parse($$"""{"amcCode":"1","confidence":{{rendue}}}""");

        proposition.Confidence.Should().Be((decimal)attendue);
    }
}
