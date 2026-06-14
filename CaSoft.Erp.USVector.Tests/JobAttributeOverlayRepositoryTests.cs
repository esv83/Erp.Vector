using System.Text.Json;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// MOB-13.10 — Tests de l'overlay attributs (catalogue + valeurs en BD Mobile).
/// EF Core InMemory ; le repo n'utilise que du LINQ simple.
/// Catalogue seedé : 2 contrats (STANDARD=1, VSL=2), globaux COMMENTS/PHONES,
/// STANDARD→REFERENCE+PMT(list), VSL→BT.
/// </summary>
public class JobAttributeOverlayRepositoryTests
{
    private static readonly Guid Mission = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly IDictionary<string, IEnumerable<string>> NoBaseline =
        new Dictionary<string, IEnumerable<string>>();

    private static MobileDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"overlay-{Guid.NewGuid()}")
            .Options;
        var ctx = new MobileDbContext(options);
        Seed(ctx);
        return ctx;
    }

    private static void Seed(MobileDbContext ctx)
    {
        ctx.ContractTypes.AddRange(
            new MOB_CONTRACT_TYPE { CTT_ID = 1, CTT_CODE = "STANDARD", CTT_DISPLAY = "Transport standard", CTT_ACTIVE = true },
            new MOB_CONTRACT_TYPE { CTT_ID = 2, CTT_CODE = "VSL", CTT_DISPLAY = "VSL", CTT_ACTIVE = true });

        ctx.ContractAttributes.AddRange(
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 1, CAT_NAME = "COMMENTS", CAT_LABEL = "Commentaires", CAT_FIELD_TYPE = "textarea", CAT_INDEX = 100, CAT_IS_GLOBAL = true },
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 2, CAT_NAME = "PHONES", CAT_LABEL = "Téléphones", CAT_FIELD_TYPE = "phone", CAT_INDEX = 101, CAT_IS_GLOBAL = true, CAT_IS_MULTI = true },
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 3, CAT_NAME = "REFERENCE", CAT_LABEL = "Référence", CAT_FIELD_TYPE = "text", CAT_INDEX = 1 },
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 4, CAT_NAME = "PMT", CAT_LABEL = "PMT", CAT_FIELD_TYPE = "list", CAT_INDEX = 2 },
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 5, CAT_NAME = "BT", CAT_LABEL = "BT", CAT_FIELD_TYPE = "checkbox", CAT_INDEX = 3 });

        ctx.ContractAttributeContracts.AddRange(
            new MOB_CONTRACT_ATTRIBUTE_CONTRACT { CAC_ATTRIBUTE_ID = 3, CAC_CONTRACT_ID = 1 },
            new MOB_CONTRACT_ATTRIBUTE_CONTRACT { CAC_ATTRIBUTE_ID = 4, CAC_CONTRACT_ID = 1 },
            new MOB_CONTRACT_ATTRIBUTE_CONTRACT { CAC_ATTRIBUTE_ID = 5, CAC_CONTRACT_ID = 2 });

        ctx.ContractAttributeOptions.AddRange(
            new MOB_CONTRACT_ATTRIBUTE_OPTION { CAO_ID = 1, CAO_ATTRIBUTE_ID = 4, CAO_KEY = 0, CAO_LABEL = "Non" },
            new MOB_CONTRACT_ATTRIBUTE_OPTION { CAO_ID = 2, CAO_ATTRIBUTE_ID = 4, CAO_KEY = 1, CAO_LABEL = "Oui" });

        ctx.SaveChanges();
    }

    private static IDictionary<string, IEnumerable<string>> Baseline(params string[] phones)
        => new Dictionary<string, IEnumerable<string>> { ["PHONES"] = phones };

    private static List<string> AsList(string? json)
        => JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? new();

    // ── BuildContractType ────────────────────────────────────────────────────

    [Fact]
    public void BuildContractType_no_selection_uses_first_active_contract_with_globals()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);

        var contract = sut.BuildContractType(Mission, NoBaseline);

        contract.Id.Should().Be(1); // STANDARD (premier actif)
        contract.Attributs.Keys.Should().BeEquivalentTo("COMMENTS", "PHONES", "REFERENCE", "PMT");
        contract.Attributs.Keys.Should().NotContain("BT"); // attribut du contrat VSL
    }

    [Fact]
    public void BuildContractType_selected_contract_swaps_attribute_set()
    {
        using var ctx = NewContext();
        ctx.JobContracts.Add(new MOB_JOB_CONTRACT { JCT_MISSION_ID = Mission, JCT_CONTRACT_ID = 2 });
        ctx.SaveChanges();
        var sut = new JobAttributeOverlayRepository(ctx);

        var contract = sut.BuildContractType(Mission, NoBaseline);

        contract.Id.Should().Be(2); // VSL
        contract.Attributs.Keys.Should().BeEquivalentTo("COMMENTS", "PHONES", "BT");
        contract.Attributs.Keys.Should().NotContain(new[] { "REFERENCE", "PMT" });
    }

    [Fact]
    public void BuildContractType_list_attribute_exposes_options_and_control_type()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);

        var pmt = sut.BuildContractType(Mission, NoBaseline).Attributs["PMT"];

        pmt.FieldType.Should().Be("list");
        pmt.IsList.Should().BeTrue();
        pmt.Options.Should().BeEquivalentTo(new Dictionary<int, string> { [0] = "Non", [1] = "Oui" });
    }

    [Fact]
    public void BuildContractType_multi_merges_erp_baseline_then_overlay_deduped()
    {
        using var ctx = NewContext();
        ctx.JobAttributeValues.Add(new MOB_JOB_ATTRIBUTE_VALUE
        {
            JAV_MISSION_ID = Mission,
            JAV_ATTRIBUTE_NAME = "PHONES",
            JAV_VALUE = JsonSerializer.Serialize(new[] { "0600000000", "0611111111" }) // dont un doublon de l'ERP
        });
        ctx.SaveChanges();
        var sut = new JobAttributeOverlayRepository(ctx);

        var phones = sut.BuildContractType(Mission, Baseline("0611111111")).Attributs["PHONES"];

        phones.IsMulti.Should().BeTrue();
        // ERP d'abord, puis overlay, sans doublon.
        AsList(phones.Value).Should().Equal("0611111111", "0600000000");
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Save_scalar_value_is_upserted()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);
        var contract = sut.BuildContractType(Mission, NoBaseline);
        contract.Attributs["COMMENTS"].Value = "RAS";

        sut.Save(Mission, contract, NoBaseline);

        ctx.JobAttributeValues.Single(v => v.JAV_ATTRIBUTE_NAME == "COMMENTS").JAV_VALUE.Should().Be("RAS");
    }

    [Fact]
    public void Save_does_not_create_empty_rows()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);
        var contract = sut.BuildContractType(Mission, NoBaseline);
        contract.Attributs["REFERENCE"].Value = "REF-1"; // seul renseigné

        sut.Save(Mission, contract, NoBaseline);

        var names = ctx.JobAttributeValues.Where(v => v.JAV_MISSION_ID == Mission)
            .Select(v => v.JAV_ATTRIBUTE_NAME).ToList();
        names.Should().ContainSingle().Which.Should().Be("REFERENCE");
    }

    [Fact]
    public void Save_multi_persists_only_items_outside_erp_baseline()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);
        var contract = sut.BuildContractType(Mission, Baseline("0611111111"));
        // L'utilisateur soumet le numéro ERP + un ajout : seul l'ajout doit être stocké.
        contract.Attributs["PHONES"].Value = JsonSerializer.Serialize(new[] { "0611111111", "0600000000" });

        sut.Save(Mission, contract, Baseline("0611111111"));

        var stored = ctx.JobAttributeValues.Single(v => v.JAV_ATTRIBUTE_NAME == "PHONES").JAV_VALUE;
        AsList(stored).Should().Equal("0600000000");
    }

    [Fact]
    public void Save_existing_row_can_be_updated()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);
        var contract = sut.BuildContractType(Mission, NoBaseline);

        contract.Attributs["COMMENTS"].Value = "v1";
        sut.Save(Mission, contract, NoBaseline);
        contract.Attributs["COMMENTS"].Value = "v2";
        sut.Save(Mission, contract, NoBaseline);

        ctx.JobAttributeValues.Single(v => v.JAV_ATTRIBUTE_NAME == "COMMENTS").JAV_VALUE.Should().Be("v2");
    }

    // ── Sélection de contrat ─────────────────────────────────────────────────

    [Fact]
    public void GetContracts_returns_active_contracts()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);

        sut.GetContracts().Select(c => c.Id).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void SelectContract_upserts_and_is_read_back()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);

        sut.SelectContract(Mission, 2);
        sut.GetSelectedContractId(Mission).Should().Be(2);

        sut.SelectContract(Mission, 1); // upsert
        sut.GetSelectedContractId(Mission).Should().Be(1);
    }

    [Fact]
    public void SelectContract_unknown_contract_throws()
    {
        using var ctx = NewContext();
        var sut = new JobAttributeOverlayRepository(ctx);

        var act = () => sut.SelectContract(Mission, 999);

        act.Should().Throw<InvalidOperationException>();
    }
}
