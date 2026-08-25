using System.Net;
using System.Text;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-3b — Le lien entre le type de mission et le jeu d'attributs, une fois que le type ne vit plus
/// dans <c>MOB_JOB_CONTRACT</c>.
/// <para>
/// Sans ce résolveur, la bascule couperait silencieusement ce lien : l'ambulancier cocherait
/// « Article 80 » et saisirait les champs du transport standard. Ces tests épinglent les trois
/// réponses possibles — un type, aucun type, ou pas de réponse du tout — parce qu'elles ne veulent
/// pas dire la même chose.
/// </para>
/// </summary>
public class EffectiveContractTypeResolverTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";
    private static readonly Guid Mission = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001");

    private static string Context(string? code) => $$"""
    {
      "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
      "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
      "contextOrderId": {{(code is null ? "null" : "2")}},
      "contextOrderCode": {{(code is null ? "null" : $"\"{code}\"")}},
      "locked": false,
      "availableContextOrders": []
    }
    """;

    /// <summary>
    /// La garantie qui rend le drapeau réversible : désarmé, il ne coûte <b>rien</b> — pas même
    /// l'appel réseau. Un drapeau qui ralentirait le chemin chaud en position « off » ne pourrait
    /// pas être livré en avance de la bascule, ce qui est pourtant sa seule raison d'être.
    /// </summary>
    [Fact]
    public void Drapeau_desarme_le_resolveur_ne_se_prononce_pas_et_n_appelle_pas_l_ERP()
    {
        var handler = new StubHandler(Context("ART80"));

        var resolved = Build(handler, armed: false).Resolve(Mission);

        resolved.Should().BeNull("le type vit encore dans MOB_JOB_CONTRACT, on ne s'en mêle pas");
        handler.Calls.Should().Be(0, "désarmé, le chemin du détail mission ne gagne aucun appel réseau");
    }

    [Fact]
    public void Type_Order_traduit_vers_le_type_Vector_par_le_code()
    {
        var resolved = Build(new StubHandler(Context("ART80")), armed: true).Resolve(Mission);

        resolved.Should().NotBeNull();
        resolved!.Value.ContractTypeId.Should().Be(4,
            "ART80 porte l'id 4 côté Vector alors qu'il porte le 2 côté Order — on passe par le code");
    }

    /// <summary>
    /// STANDARD n'existe pas au catalogue Order : une mission en CPAM doit retrouver le jeu
    /// d'attributs du transport standard. C'est l'alias d'OC-4, lu dans l'autre sens.
    /// </summary>
    [Fact]
    public void CPAM_retrouve_le_type_STANDARD_par_alias()
    {
        var resolved = Build(new StubHandler(Context("CPAM")), armed: true).Resolve(Mission);

        resolved!.Value.ContractTypeId.Should().Be(1);
    }

    /// <summary>
    /// Le catalogue Order compte sept types, celui de Vector deux. Une mission en « Centre 15 » n'a
    /// pas de jeu d'attributs Vector : attributs communs seuls, plutôt que ceux d'un type voisin.
    /// </summary>
    [Fact]
    public void Type_Order_sans_equivalent_Vector_donne_aucun_type()
    {
        var resolved = Build(new StubHandler(Context("CENTRE15")), armed: true).Resolve(Mission);

        resolved.Should().NotBeNull("c'est une réponse, pas une abstention");
        resolved!.Value.ContractTypeId.Should().BeNull();
    }

    [Fact]
    public void Type_non_renseigne_cote_Order_donne_aucun_type()
    {
        var resolved = Build(new StubHandler(Context(null)), armed: true).Resolve(Mission);

        resolved.Should().NotBeNull();
        resolved!.Value.ContractTypeId.Should().BeNull();
    }

    /// <summary>
    /// ⚠️ Une panne réseau est une <b>abstention</b>, pas un « aucun type ». La différence se voit à
    /// l'écran : abstention = le formulaire d'avant la bascule ; « aucun type » retirerait des
    /// champs que l'ambulancier est peut-être en train de remplir.
    /// </summary>
    [Fact]
    public void Panne_de_l_ERP_est_une_abstention_pas_un_formulaire_vide()
    {
        var resolved = Build(new StubHandler(status: HttpStatusCode.InternalServerError), armed: true)
            .Resolve(Mission);

        resolved.Should().BeNull();
    }

    /// <summary>
    /// Le résolveur ne sert à rien s'il n'est pas écouté : ce test descend jusqu'au jeu d'attributs
    /// réellement composé, et vérifie qu'une réponse « aucun type » ne laisse que les attributs
    /// communs — sans repli sur le premier type actif.
    /// </summary>
    [Fact]
    public void L_overlay_suit_le_resolveur_jusqu_aux_attributs_servis()
    {
        var ctx = NewContext();
        SeedAttributes(ctx);

        var avecType = new JobAttributeOverlayRepository(ctx, new FixedResolver(new EffectiveContractType(4)))
            .BuildContractType(Mission, new Dictionary<string, IEnumerable<string>>());
        avecType.Attributs.Keys.Should().BeEquivalentTo("COMMENT", "ART80_REF");

        var sansType = new JobAttributeOverlayRepository(ctx, new FixedResolver(new EffectiveContractType(null)))
            .BuildContractType(Mission, new Dictionary<string, IEnumerable<string>>());
        sansType.Attributs.Keys.Should().BeEquivalentTo("COMMENT");
    }

    /// <summary>
    /// Le garde-fou de la bascule : un résolveur absent ou abstenu doit laisser le comportement
    /// d'aujourd'hui strictement intact — c'est ce qui rend le drapeau réversible.
    /// </summary>
    [Fact]
    public void Resolveur_abstenu_rend_le_comportement_historique()
    {
        var ctx = NewContext();
        SeedAttributes(ctx);
        ctx.JobContracts.Add(new MOB_JOB_CONTRACT { JCT_MISSION_ID = Mission, JCT_CONTRACT_ID = 4 });
        ctx.SaveChanges();

        var abstenu = new JobAttributeOverlayRepository(ctx, new FixedResolver(null))
            .BuildContractType(Mission, new Dictionary<string, IEnumerable<string>>());

        abstenu.Id.Should().Be(4);
        abstenu.Attributs.Keys.Should().Contain("ART80_REF",
            "MOB_JOB_CONTRACT reprend la main quand le résolveur ne se prononce pas");
    }

    // ── Harnais ─────────────────────────────────────────────────────────────────

    private static MobileDbContext NewContext()
        => new(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"effective-{Guid.NewGuid()}").Options);

    /// <summary>Catalogue Vector réel (relevé en base le 2026-08-24) : STANDARD=1, ART80=4.</summary>
    private static void SeedContractTypes(MobileDbContext ctx)
    {
        ctx.ContractTypes.AddRange(
            new MOB_CONTRACT_TYPE { CTT_ID = 1, CTT_CODE = "STANDARD", CTT_DISPLAY = "Transport standard", CTT_ACTIVE = true },
            new MOB_CONTRACT_TYPE { CTT_ID = 4, CTT_CODE = "ART80", CTT_DISPLAY = "Article 80", CTT_ACTIVE = true });
        ctx.SaveChanges();
    }

    private static void SeedAttributes(MobileDbContext ctx)
    {
        SeedContractTypes(ctx);
        ctx.ContractAttributes.AddRange(
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 1, CAT_NAME = "COMMENT", CAT_LABEL = "Commentaire", CAT_FIELD_TYPE = "text", CAT_INDEX = 10, CAT_IS_GLOBAL = true },
            new MOB_CONTRACT_ATTRIBUTE { CAT_ID = 2, CAT_NAME = "ART80_REF", CAT_LABEL = "Référence Article 80", CAT_FIELD_TYPE = "text", CAT_INDEX = 20, CAT_IS_GLOBAL = false });
        ctx.ContractAttributeContracts.Add(
            new MOB_CONTRACT_ATTRIBUTE_CONTRACT { CAC_ATTRIBUTE_ID = 2, CAC_CONTRACT_ID = 4 });
        ctx.SaveChanges();
    }

    private static OrderEffectiveContractTypeResolver Build(StubHandler handler, bool armed)
    {
        var ctx = NewContext();
        SeedContractTypes(ctx);

        return new OrderEffectiveContractTypeResolver(
            ctx,
            new HttpErpReadApiClient(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                                     NullLogger<HttpErpReadApiClient>.Instance),
            new ContextOrderOptions { UseOrderCatalog = armed },
            NullLogger<OrderEffectiveContractTypeResolver>.Instance);
    }

    private sealed class FixedResolver : IEffectiveContractTypeResolver
    {
        private readonly EffectiveContractType? _value;
        public FixedResolver(EffectiveContractType? value) => _value = value;
        public EffectiveContractType? Resolve(Guid missionId) => _value;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;

        public StubHandler(string body = "", HttpStatusCode status = HttpStatusCode.OK)
        {
            _body = body;
            _status = status;
        }

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }
}
