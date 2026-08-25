using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-7 — Le bloc « attributs » du paquet terrain, coupé de la résolution de type.
/// <para>
/// Ce que ces tests protègent : le paquet <b>ne paie aucun appel réseau</b> pour ses attributs, quel
/// que soit l'état des drapeaux de bascule, et il continue de porter les valeurs des missions saisies
/// <b>avant</b> celle-ci — celles qui n'existent que côté Vector.
/// </para>
/// </summary>
public class FieldAttributesReaderTests
{
    private static readonly Guid Mission = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Le cas nominal : ce que le terrain a saisi part avec le dossier, les champs restés vides sont
    /// écartés — le paquet ne transporte pas des cases blanches.
    /// </summary>
    [Fact]
    public void Sert_les_valeurs_saisies_et_ecarte_les_champs_vides()
    {
        var block = Build(SeededContext()).Read(Mission);

        block.Values.Select(v => v.Name).Should().BeEquivalentTo("COMMENTS");
        block.Values.Single().Value.Should().Be("RAS");
        block.ContractDisplay.Should().Be("Transport standard");
    }

    /// <summary>
    /// ⚠️ Le cœur d'OC-7. Le paquet passait par la résolution du type de mission, qui depuis la
    /// bascule interroge Orders.Api : un appel réseau par mission, sur un traitement mesuré à 14,7 s
    /// pour 284 missions, pour une information dont ce paquet ne fait rien.
    /// <para>
    /// Le test met les deux montages côte à côte : celui du détail mission, qui consulte bien le
    /// résolveur, et celui du paquet, qui s'en passe. C'est la seule façon de rendre la différence
    /// visible — recâbler le résolveur sur le paquet ne casserait rien d'observable, juste la
    /// facturation qui ralentit.
    /// </para>
    /// </summary>
    [Fact]
    public void N_interroge_pas_l_ERP_la_ou_le_detail_mission_le_ferait()
    {
        var ctx = SeededContext();
        var resolveur = new ExplosiveResolver();

        // 1. Monté AVEC le résolveur — c'est le montage du détail mission : la résolution du type est
        //    bien vivante, donc l'appel Orders.Api aussi.
        var commeLeDetailMission = new JobAttributeOverlayRepository(ctx, resolveur);
        var act = () => commeLeDetailMission.BuildContractType(Mission, new Dictionary<string, IEnumerable<string>>());
        act.Should().Throw<InvalidOperationException>();
        resolveur.Consulté.Should().BeTrue("sinon ce test ne prouverait rien du montage d'en face");

        // 2. Monté SANS résolveur — c'est le montage du paquet, celui de la racine de composition.
        //    Aucune résolution, donc aucun appel réseau, et le bloc sort quand même.
        var block = new FieldAttributesReader(new JobAttributeOverlayRepository(ctx)).Read(Mission);

        block.Values.Should().NotBeEmpty();
    }

    /// <summary>
    /// La raison d'être du bloc après la bascule : les missions saisies avant elle n'ont leurs valeurs
    /// que côté Vector. La facturation fait primer celles d'Order ; celles-ci comblent les trous.
    /// Elles disparaîtront avec la décision OC-8 sur le sort de ces lignes.
    /// </summary>
    [Fact]
    public void Une_mission_sans_valeur_saisie_donne_un_bloc_vide_mais_identifie()
    {
        var ctx = SeededContext(withValues: false);

        var block = Build(ctx).Read(Mission);

        block.Values.Should().BeEmpty();
        block.ContractId.Should().NotBe(0, "le type reste identifié, même sans valeur saisie");
    }

    // ── Harnais ─────────────────────────────────────────────────────────────────

    private static FieldAttributesReader Build(MobileDbContext ctx)
        => new(new JobAttributeOverlayRepository(ctx));

    private static MobileDbContext SeededContext(bool withValues = true)
    {
        var ctx = new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"fieldattrs-{Guid.NewGuid()}").Options);

        ctx.ContractTypes.Add(new MOB_CONTRACT_TYPE
        {
            CTT_ID = 1, CTT_CODE = "STANDARD", CTT_DISPLAY = "Transport standard", CTT_ACTIVE = true
        });
        ctx.ContractAttributes.AddRange(
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 1, CAT_NAME = "COMMENTS", CAT_LABEL = "Commentaires", CAT_FIELD_TYPE = "textarea", CAT_INDEX = 10, CAT_IS_GLOBAL = true },
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 2, CAT_NAME = "REFERENCE", CAT_LABEL = "Référence", CAT_FIELD_TYPE = "text", CAT_INDEX = 20, CAT_IS_GLOBAL = true });

        if (withValues)
        {
            ctx.JobAttributeValues.Add(new MOB_JOB_ATTRIBUTE_VALUE
            {
                JAV_MISSION_ID = Mission, JAV_ATTRIBUTE_NAME = "COMMENTS", JAV_VALUE = "RAS"
            });
        }

        ctx.SaveChanges();
        return ctx;
    }

    /// <summary>Résolveur qui n'a pas le droit d'être appelé sur le chemin du paquet.</summary>
    private sealed class ExplosiveResolver : IEffectiveContractTypeResolver
    {
        public bool Consulté { get; private set; }

        public EffectiveContractType? Resolve(Guid missionId)
        {
            Consulté = true;
            throw new InvalidOperationException(
                "Le paquet terrain ne doit pas résoudre le type de mission : ce serait un appel Orders.Api par mission.");
        }
    }
}
