using System.Net;
using System.Text;
using System.Text.Json;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Relais de la sélection du contexte vers Orders.Api.
/// <para>
/// Ce que ces tests protègent : <b>aucune écriture ne part sur un identifiant qu'on n'a pas
/// proposé</b>. Les deux catalogues n'ont jamais partagé leurs ids — <c>4</c> désignait « Article
/// 80 » côté Vector et désigne « Centre 15 » côté Order — et un client resté sur une liste périmée
/// enregistrerait un type pour un autre, sans que rien ne le signale jusqu'à la facturation.
/// </para>
/// </summary>
public class ContextOrderSelectionTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";
    private static readonly Guid Mission = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001");

    /// <summary>Catalogue Order tel que servi en production : ART80 y porte l'id 2, Centre 15 le 4.</summary>
    private const string ContextPayload = """
    {
      "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
      "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
      "contextOrderId": null,
      "locked": false,
      "origin": null,
      "availableContextOrders": [
        { "id": 1, "code": "CPAM",     "display": "CPAM",       "index": 10 },
        { "id": 2, "code": "ART80",    "display": "Article 80", "index": 20 },
        { "id": 4, "code": "CENTRE15", "display": "Centre 15",  "index": 40 }
      ]
    }
    """;

    /// <summary>
    /// Le cas nominal : l'identifiant vient de la liste qu'on a servie, il circule tel quel jusqu'à
    /// l'ERP. Le traduire serait aujourd'hui une faute — il est déjà dans le bon espace.
    /// </summary>
    [Fact]
    public async Task Relaie_l_identifiant_recu_sans_le_traduire()
    {
        var handler = new RoutingHandler(ContextPayload);

        var outcome = await Build(handler).SelectAsync(Mission, contextOrderId: 2, "amb-42", default);

        outcome.Should().Be(EnContextOrderSelectionOutcome.Applied);
        handler.PatchUri!.ToString().Should().Be($"{BaseUrl}missions/{Mission}/contextOrder");

        using var sent = JsonDocument.Parse(handler.PatchBody!);
        sent.RootElement.GetProperty("contextOrderId").GetInt32().Should().Be(2);
        sent.RootElement.GetProperty("setBy").GetString().Should().Be("amb-42");
    }

    /// <summary>
    /// ⚠️ Le filet de la bascule, et la raison d'être de ce service. Un client resté sur l'ancienne
    /// liste poste « 4 » en croyant dire « Article 80 » ; ici la commande ne propose pas le 4. Le
    /// choix est refusé <b>sans écriture</b>, au lieu d'enregistrer « Centre 15 » sur une mission
    /// réelle.
    /// </summary>
    [Fact]
    public async Task Refuse_un_identifiant_absent_des_types_proposes()
    {
        const string sansCentre15 = """
        {
          "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
          "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
          "contextOrderId": null,
          "locked": false,
          "origin": null,
          "availableContextOrders": [
            { "id": 1, "code": "CPAM",  "display": "CPAM",       "index": 10 },
            { "id": 2, "code": "ART80", "display": "Article 80", "index": 20 }
          ]
        }
        """;
        var handler = new RoutingHandler(sansCentre15);

        var outcome = await Build(handler).SelectAsync(Mission, contextOrderId: 4, null, default);

        outcome.Should().Be(EnContextOrderSelectionOutcome.NotApplicable);
        handler.PatchUri.Should().BeNull("aucune écriture ne doit partir sur un id étranger à la liste servie");
    }

    [Fact]
    public async Task Mission_inconnue_cote_ERP_ne_declenche_aucune_ecriture()
    {
        var handler = new RoutingHandler(getStatus: HttpStatusCode.NotFound);

        var outcome = await Build(handler).SelectAsync(Mission, contextOrderId: 4, null, default);

        outcome.Should().Be(EnContextOrderSelectionOutcome.MissionNotFound);
        handler.PatchUri.Should().BeNull();
    }

    /// <summary>
    /// Les refus d'Order sont des cas métier, pas des pannes : ils remontent en issue typée et le
    /// contrôleur les traduit en 409 / 400 / 404. Personne ne lève.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Conflict, EnContextOrderSelectionOutcome.LockedByRegulator)]
    [InlineData(HttpStatusCode.BadRequest, EnContextOrderSelectionOutcome.NotApplicable)]
    [InlineData(HttpStatusCode.NotFound, EnContextOrderSelectionOutcome.MissionNotFound)]
    public async Task Les_refus_de_l_ERP_remontent_en_issue_metier(
        HttpStatusCode status, EnContextOrderSelectionOutcome expected)
    {
        var handler = new RoutingHandler(ContextPayload, patchStatus: status);

        var outcome = await Build(handler).SelectAsync(Mission, contextOrderId: 2, null, default);

        outcome.Should().Be(expected);
    }

    private static ContextOrderSelectionService Build(RoutingHandler handler)
        => new(
            new HttpErpReadApiClient(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                                     NullLogger<HttpErpReadApiClient>.Instance),
            new HttpErpWriteApiClient(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                                      NullLogger<HttpErpWriteApiClient>.Instance));

    /// <summary>Répond au GET du context et enregistre le PATCH — ou son absence.</summary>
    private sealed class RoutingHandler : HttpMessageHandler
    {
        private readonly string _getBody;
        private readonly HttpStatusCode _getStatus;
        private readonly HttpStatusCode _patchStatus;

        public RoutingHandler(string getBody = "",
                              HttpStatusCode getStatus = HttpStatusCode.OK,
                              HttpStatusCode patchStatus = HttpStatusCode.NoContent)
        {
            _getBody = getBody;
            _getStatus = getStatus;
            _patchStatus = patchStatus;
        }

        public Uri? PatchUri { get; private set; }
        public string? PatchBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Patch)
            {
                PatchUri = request.RequestUri;
                PatchBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                return new HttpResponseMessage(_patchStatus) { Content = new StringContent("", Encoding.UTF8, "application/json") };
            }

            return new HttpResponseMessage(_getStatus)
            {
                Content = new StringContent(_getBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
