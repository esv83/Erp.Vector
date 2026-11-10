using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using CaSoft.Framework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Alerte signature, lot 1 (MOB_012) — « le patient ne peut pas signer » : la déclaration, son
/// stockage, l'armement de la projection vers Order (PRJ-1) et sa place dans le paquet field-data.
/// </summary>
public class SignatureWaiverTests
{
    private static readonly Guid Mission = Guid.Parse("2b1f7c3e-63c0-4a8e-9a41-6f1c1e2d9b01");

    private static MobileDbContext NewDb()
    {
        var ctx = new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"waiver-{Guid.NewGuid()}").Options);
        ctx.SignatureWaiverReasons.AddRange(
            new MOB_SIGNATURE_WAIVER_REASON { SWR_CODE = "REFUS_PATIENT", SWR_LABEL = "Refus du patient", SWR_INDEX = 30, SWR_ACTIVE = true },
            new MOB_SIGNATURE_WAIVER_REASON { SWR_CODE = "AUTRE", SWR_LABEL = "Autre (à préciser)", SWR_INDEX = 90, SWR_ACTIVE = true, SWR_COMMENT_REQUIRED = true },
            new MOB_SIGNATURE_WAIVER_REASON { SWR_CODE = "ANCIEN", SWR_LABEL = "Retiré", SWR_INDEX = 50, SWR_ACTIVE = false });
        ctx.SaveChanges();
        return ctx;
    }

    private static Task<ClResult<ClSignatureWaiverDtoOut>> Declare(MobileDbContext ctx, ClDeclareSignatureWaiverDtoIn input)
        => new ClDeclareSignatureWaiverUseCase(Mission, input, new SignatureWaiverRepository(ctx)).HandleAsync(CancellationToken.None);

    // ── La déclaration ───────────────────────────────────────────────────────

    [Fact]
    public async Task Un_motif_declare_est_stocke_en_UTC_et_arme_la_projection()
    {
        await using var ctx = NewDb();

        var result = await Declare(ctx, new ClDeclareSignatureWaiverDtoIn { ReasonCode = " refus_patient " });

        result.IsSucces.Should().BeTrue();
        result.Value.ReasonCode.Should().Be("REFUS_PATIENT");
        result.Value.ReasonLabel.Should().Be("Refus du patient");
        result.Value.DeclaredAt.Kind.Should().Be(DateTimeKind.Utc);

        ctx.SignatureWaivers.Should().ContainSingle(w => w.SWV_MISSION_ID == Mission && w.SWV_REASON_CODE == "REFUS_PATIENT");
        ctx.OperationalOutbox.Should().ContainSingle(o => o.OOB_MISSION_ID == Mission, "le motif part vers Order (PRJ-1)");
    }

    [Theory]
    [InlineData(null, null, "Choisissez un motif")]
    [InlineData("INCONNU", null, "inconnu ou retiré")]
    [InlineData("ANCIEN", null, "inconnu ou retiré")]
    [InlineData("AUTRE", "  ", "demande un commentaire")]
    public async Task Un_motif_refuse_n_ecrit_rien(string? code, string? comment, string because)
    {
        await using var ctx = NewDb();

        var result = await Declare(ctx, new ClDeclareSignatureWaiverDtoIn { ReasonCode = code, Comment = comment });

        result.IsFail.Should().BeTrue();
        result.InnerError.ErrorText.Should().Contain(because);
        ctx.SignatureWaivers.Should().BeEmpty();
        ctx.OperationalOutbox.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_nouveau_motif_remplace_le_precedent()
    {
        await using var ctx = NewDb();

        await Declare(ctx, new ClDeclareSignatureWaiverDtoIn { ReasonCode = "REFUS_PATIENT" });
        await Declare(ctx, new ClDeclareSignatureWaiverDtoIn { ReasonCode = "AUTRE", Comment = "Patient sourd, sans interprète" });

        ctx.SignatureWaivers.Should().ContainSingle().Which.SWV_COMMENT.Should().Be("Patient sourd, sans interprète");
        ctx.OperationalOutbox.Should().ContainSingle("une ligne par mission, quelle que soit la rafale");
    }

    [Fact]
    public async Task Retirer_le_motif_arme_aussi_la_projection_pour_l_effacer_chez_Order()
    {
        await using var ctx = NewDb();
        var repository = new SignatureWaiverRepository(ctx);
        await Declare(ctx, new ClDeclareSignatureWaiverDtoIn { ReasonCode = "REFUS_PATIENT" });
        ctx.OperationalOutbox.RemoveRange(ctx.OperationalOutbox);
        await ctx.SaveChangesAsync();

        await repository.DeleteAsync(Mission, CancellationToken.None);

        ctx.SignatureWaivers.Should().BeEmpty();
        ctx.OperationalOutbox.Should().ContainSingle(o => o.OOB_MISSION_ID == Mission);
    }

    [Fact]
    public async Task La_liste_ne_propose_que_les_motifs_actifs_dans_l_ordre()
    {
        await using var ctx = NewDb();

        var reasons = await new SignatureWaiverRepository(ctx).ListActiveReasonsAsync(CancellationToken.None);

        reasons.Select(r => r.Code).Should().Equal("REFUS_PATIENT", "AUTRE");
    }

    // ── La signature arme la projection, elle aussi ──────────────────────────

    [Fact]
    public async Task Poser_ou_retirer_une_signature_arme_la_projection()
    {
        await using var ctx = NewDb();
        var signatures = new SignatureRepository(ctx);

        await signatures.InsertAsync(Mission, "data:image/png;base64,AAAA", CancellationToken.None);
        ctx.OperationalOutbox.Should().ContainSingle(o => o.OOB_MISSION_ID == Mission);

        ctx.OperationalOutbox.RemoveRange(ctx.OperationalOutbox);
        await ctx.SaveChangesAsync();

        await signatures.DeleteAsync(Mission, "", CancellationToken.None);
        ctx.OperationalOutbox.Should().ContainSingle(o => o.OOB_MISSION_ID == Mission, "un retrait doit aussi être su d'Order");
    }

    // ── Le paquet field-data ─────────────────────────────────────────────────

    [Fact]
    public async Task Le_motif_figure_dans_les_silos_du_paquet_terrain()
    {
        await using var ctx = NewDb();
        await Declare(ctx, new ClDeclareSignatureWaiverDtoIn { ReasonCode = "REFUS_PATIENT" });

        var silos = new FieldDataQueryService(ctx).ReadSilos(new[] { Mission, Guid.NewGuid() }, Array.Empty<Guid>());

        silos.Waivers.Should().ContainKey(Mission).WhoseValue.ReasonCode.Should().Be("REFUS_PATIENT");
        silos.Waivers.Should().HaveCount(1);
    }
}
