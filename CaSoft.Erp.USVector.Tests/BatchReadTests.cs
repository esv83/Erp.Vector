using CaSoft.Erp.USVector.Api.Controllers;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// B5 — Lectures en lot pour la facturation : les signatures (itération 14) et les plafonds des deux
/// routes. Les paquets en lot sont couverts par <see cref="FieldDataReaderTests"/>.
/// </summary>
public class BatchReadTests
{
    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"batch-{Guid.NewGuid()}").Options);

    // ── Signatures en lot ─────────────────────────────────────────────────────

    [Fact]
    public void Les_signatures_sortent_par_mission_dans_l_ordre_de_la_demande()
    {
        using var ctx = NewContext();
        var signee = Guid.NewGuid();
        var nonSignee = Guid.NewGuid();
        var at = new DateTime(2026, 9, 19, 9, 0, 0);
        ctx.Signatures.Add(new MOB_SIGNATURE { SIG_MISSION_ID = signee, SIG_DATA = "iVBORw0KGgo=", SIG_DATETIME = at });
        ctx.SaveChanges();

        var lot = new SignatureQueryService(ctx).ReadMany(new[] { nonSignee, signee });

        lot.Select(s => s.MissionId).Should().Equal(nonSignee, signee);
        lot[0].Status.Should().Be(ClSignatureBatchItemDtoOut.StatusNotFound);
        lot[0].Data.Should().BeNull();
        lot[1].Status.Should().Be(ClSignatureBatchItemDtoOut.StatusFound);
        lot[1].SignedAt.Should().Be(at);
    }

    /// <summary>La facturation stocke l'image telle quelle : le lot doit servir la même valeur que la route unitaire.</summary>
    [Fact]
    public void L_image_du_lot_est_celle_de_la_route_unitaire()
    {
        using var ctx = NewContext();
        var mission = Guid.NewGuid();
        var repository = new SignatureRepository(ctx);
        repository.Insert(mission, "iVBORw0KGgoAAAANSUhEUg==");

        var lot = new SignatureQueryService(ctx).ReadMany(new[] { mission });

        lot.Single().Data.Should().Be(repository.Fetch(mission)!.Data);
    }

    [Fact]
    public void Le_lot_de_signatures_dedoublonne_et_ignore_l_identifiant_vide()
    {
        using var ctx = NewContext();
        var mission = Guid.NewGuid();

        var lot = new SignatureQueryService(ctx).ReadMany(new[] { mission, mission, Guid.Empty });

        lot.Should().ContainSingle().Which.MissionId.Should().Be(mission);
    }

    // ── Plafonds ──────────────────────────────────────────────────────────────

    [Fact]
    public void Au_dela_du_plafond_le_lot_de_signatures_est_refuse_avec_le_maximum()
    {
        using var ctx = NewContext();
        var ids = Enumerable.Range(0, SignatureController.MaxSignaturesParLot + 1).Select(_ => Guid.NewGuid()).ToList();

        var result = new SignatureController().GetMany(
            new SignatureController.SignatureBatchQuery { MissionIds = ids }, new SignatureQueryService(ctx));

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<string>().Which.Should().Contain($"{SignatureController.MaxSignaturesParLot}");
    }

    [Fact]
    public async Task Au_dela_du_plafond_le_lot_de_paquets_est_refuse_sans_rien_lire()
    {
        var reader = new ReaderQuiNeDoitPasEtreAppele();
        var ids = Enumerable.Range(0, FieldDataController.MaxMissionsParLot + 1).Select(_ => Guid.NewGuid()).ToList();

        var result = await new FieldDataController(reader).GetMany(
            new FieldDataController.FieldDataBatchQuery { MissionIds = ids }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private sealed class ReaderQuiNeDoitPasEtreAppele : IFieldDataReader
    {
        public Task<ClFieldEnrichmentDtoOut> GetAsync(Guid missionId, CancellationToken ct) => throw new InvalidOperationException();
        public Task<IReadOnlyList<ClFieldEnrichmentBatchItemDtoOut>> GetManyAsync(IReadOnlyCollection<Guid> missionIds, CancellationToken ct)
            => throw new InvalidOperationException("le plafond doit être vérifié avant toute lecture");
    }
}
