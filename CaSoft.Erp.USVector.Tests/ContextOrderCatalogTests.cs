using System.Net;
using System.Text;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-3b — La liste des types de mission servie depuis le catalogue Order.
/// <para>
/// Ce que ces tests protègent : la <b>forme</b> rendue au mobile ne bouge pas (D14), le <b>défaut
/// automatique disparaît</b>, et une panne de l'ERP ne fait pas ressurgir des identifiants qui ne
/// veulent plus rien dire.
/// </para>
/// </summary>
public class ContextOrderCatalogTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";
    private static readonly Guid Mission = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001");

    /// <summary>Catalogue Order tel que servi en production, volontairement non trié par index.</summary>
    private const string ContextPayload = """
    {
      "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
      "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
      "contextOrderId": 2,
      "contextOrderCode": "ART80",
      "contextOrderDisplay": "Article 80",
      "locked": false,
      "origin": "Field",
      "availableContextOrders": [
        { "id": 4, "code": "CENTRE15", "display": "Centre 15",  "index": 40 },
        { "id": 1, "code": "CPAM",     "display": "CPAM",       "index": 10 },
        { "id": 2, "code": "ART80",    "display": "Article 80", "index": 20 }
      ]
    }
    """;

    /// <summary>
    /// Le cœur d'OC-3b : ce sont les ids <b>Order</b> qui sont servis, dans l'ordre d'Order, et le
    /// type effectif de la mission est le seul marqué.
    /// </summary>
    [Fact]
    public async Task Sert_le_catalogue_Order_trie_par_index()
    {
        var choices = await Build(new StubHandler(ContextPayload)).GetChoicesAsync(Mission, default);

        choices.Select(c => c.Id).Should().Equal(new[] { 1, 2, 4 }, "le tri d'affichage appartient à Order");
        choices.Select(c => c.Display).Should().Equal("CPAM", "Article 80", "Centre 15");
        choices.Single(c => c.IsSelected).Id.Should().Be(2, "la mission porte ART80, qui vaut 2 côté Order");
    }

    /// <summary>
    /// La règle « défaut = premier type actif » disparaît : un type non renseigné est un état
    /// valide. La pré-sélection faisait passer un défaut technique pour un choix de l'ambulancier —
    /// et c'est ce faux choix qui partait en facturation.
    /// </summary>
    [Fact]
    public async Task Sans_type_pose_aucun_item_n_est_preselectionne()
    {
        var sansType = ContextPayload
            .Replace("\"contextOrderId\": 2,", "\"contextOrderId\": null,")
            .Replace("\"contextOrderCode\": \"ART80\",", "\"contextOrderCode\": null,");

        var choices = await Build(new StubHandler(sansType)).GetChoicesAsync(Mission, default);

        choices.Should().HaveCount(3);
        choices.Should().OnlyContain(c => !c.IsSelected);
    }

    /// <summary>
    /// Le verrou porte sur la mission, pas sur le type : il est reporté sur chaque item pour que le
    /// front grise la liste sans second appel.
    /// </summary>
    [Fact]
    public async Task Le_verrou_est_reporte_sur_chaque_item()
    {
        var verrouille = ContextPayload.Replace("\"locked\": false", "\"locked\": true");

        var choices = await Build(new StubHandler(verrouille)).GetChoicesAsync(Mission, default);

        choices.Should().NotBeEmpty().And.OnlyContain(c => c.Locked);
    }

    /// <summary>
    /// ⚠️ Le test qui garde la bascule honnête. Avant OC-3b, une panne de l'ERP laissait servir la
    /// liste locale ; après, ce repli écrirait des ids Vector là où le POST attend des ids Order —
    /// 4 vaut ART80 ici, CENTRE15 là-bas. Une liste vide vaut mieux qu'une liste qui ment.
    /// </summary>
    [Fact]
    public async Task Panne_de_l_ERP_donne_une_liste_vide_et_non_le_catalogue_local()
    {
        var choices = await Build(new StubHandler(status: HttpStatusCode.InternalServerError))
            .GetChoicesAsync(Mission, default);

        choices.Should().BeEmpty();
    }

    [Fact]
    public async Task Mission_inconnue_d_Order_donne_une_liste_vide()
    {
        var choices = await Build(new StubHandler(status: HttpStatusCode.NotFound))
            .GetChoicesAsync(Mission, default);

        choices.Should().BeEmpty();
    }

    private static ContextOrderCatalogService Build(StubHandler handler)
        => new(new HttpErpReadApiClient(
                   new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                   NullLogger<HttpErpReadApiClient>.Instance),
               NullLogger<ContextOrderCatalogService>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;

        public StubHandler(string body = "", HttpStatusCode status = HttpStatusCode.OK)
        {
            _body = body;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
    }
}
