using System.Net;
using System.Text;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-3a — Verrou et provenance du <b>context de la mission</b> rendus lisibles par le terrain.
/// <para>
/// L'enjeu épinglé ici est la distinction entre <c>Locked</c> (l'ambulancier a-t-il la main ?) et
/// <c>Origin</c> (d'où vient la valeur affichée ?). Le cas qui compte est le troisième test :
/// <b>posé par la régulation mais modifiable</b>. Il n'est pas encore atteignable via Orders.Api —
/// c'est l'objet d'<c>Order OC-24</c> — mais le contrat mobile doit savoir l'exprimer dès
/// maintenant, sans quoi le front devra changer une seconde fois.
/// </para>
/// </summary>
public class ContextOrderStateTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";

    private static string Payload(bool locked, int? contextOrderId) => $$"""
    {
      "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
      "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
      "contextOrderId": {{(contextOrderId?.ToString() ?? "null")}},
      "contextOrderCode": "CENTRE15",
      "contextOrderDisplay": "Centre 15",
      "locked": {{(locked ? "true" : "false")}},
      "availableContextOrders": []
    }
    """;

    [Fact]
    public async Task Context_verrouille_est_attribue_a_la_regulation()
    {
        var state = await ReadState(Payload(locked: true, contextOrderId: 4));

        state!.Locked.Should().BeTrue();
        state.Origin.Should().Be("Regulator");
        state.ContextOrderDisplay.Should().Be("Centre 15");
    }

    [Fact]
    public async Task Context_pose_et_libre_est_attribue_au_terrain()
    {
        var state = await ReadState(Payload(locked: false, contextOrderId: 4));

        state!.Locked.Should().BeFalse();
        state.Origin.Should().Be("Field");
    }

    /// <summary>
    /// Le cas demandé : la régulation propose, l'ambulancier garde la main. Tant qu'Order dérive
    /// <c>locked</c> de la provenance, ce couple ne peut pas <b>venir</b> de l'API — mais le DTO
    /// mobile doit pouvoir le porter, et l'UI s'y fier : verrou et provenance sont deux champs
    /// indépendants, pas un booléen déguisé.
    /// </summary>
    [Fact]
    public void Pose_par_la_regulation_mais_modifiable_est_exprimable_dans_le_contrat_mobile()
    {
        var state = new ClContextOrderStateDtoOut
        {
            Locked = false,
            Origin = "Regulator",
            ContextOrderId = 4,
            ContextOrderDisplay = "Centre 15"
        };

        state.Locked.Should().BeFalse("l'ambulancier peut encore changer le type");
        state.Origin.Should().Be("Regulator", "mais la valeur affichée vient de la régulation");
    }

    [Fact]
    public async Task Aucun_context_pose_ne_designe_aucune_provenance()
    {
        var state = await ReadState(Payload(locked: false, contextOrderId: null));

        state!.ContextOrderId.Should().BeNull();
        state.Origin.Should().BeNull();
    }

    [Fact]
    public async Task Mission_inconnue_cote_ERP_ne_donne_aucun_etat()
    {
        var service = BuildService(new StubHandler(HttpStatusCode.NotFound));

        var state = await service.GetAsync(Guid.NewGuid(), CancellationToken.None);

        state.Should().BeNull();
    }

    [Fact]
    public void Le_verrou_est_reporte_sur_chaque_item_du_selecteur()
    {
        var result = new ClListContractsUseCase(Guid.NewGuid(), new FakeOverlay(), locked: true).Handle();

        result.Value.Should().OnlyContain(c => c.Locked);
    }

    /// <summary>
    /// D14 — l'ajout est neutre : sans état connu, la liste sort exactement comme avant OC-3a.
    /// </summary>
    [Fact]
    public void Sans_verrou_connu_la_liste_reste_celle_d_avant()
    {
        var result = new ClListContractsUseCase(Guid.NewGuid(), new FakeOverlay()).Handle();

        result.Value.Should().OnlyContain(c => !c.Locked);
        result.Value.Select(c => c.Id).Should().Equal(1, 2);
        result.Value.Single(c => c.IsSelected).Id.Should().Be(1, "le premier actif reste le défaut");
    }

    private static async Task<ClContextOrderStateDtoOut?> ReadState(string payload)
        => await BuildService(new StubHandler(HttpStatusCode.OK, payload))
            .GetAsync(Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001"), CancellationToken.None);

    private static ContextOrderStateQueryService BuildService(StubHandler handler)
        => new(new HttpErpReadApiClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
            NullLogger<HttpErpReadApiClient>.Instance));

    private sealed class FakeOverlay : IJobAttributeOverlay
    {
        public ClContractType BuildContractType(Guid missionId, IDictionary<string, IEnumerable<string>> baselines)
            => new();

        public void Save(Guid m, ClContractType c, IDictionary<string, IEnumerable<string>> b) { }

        public IReadOnlyList<ClContractType> GetContracts() => new List<ClContractType>
        {
            new(1, "Standard", null!),
            new(2, "Article 80", null!)
        };

        public int? GetSelectedContractId(Guid m) => null;

        public void SelectContract(Guid m, int c) { }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body = "")
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
    }
}
