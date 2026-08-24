using System.Net;
using System.Text;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-1 — Lecture du <b>context de la mission</b> depuis Orders.Api.
/// Ces tests <b>épinglent le contrat HTTP</b> (route, casse JSON camelCase, types) : c'est la
/// contrepartie du découplage 4a, où les DTO sont recopiés côté Vector et peuvent dériver en
/// silence si Orders change sa réponse. Les charges utiles ci-dessous sont celles de la note
/// d'intégration <c>Erp.Order/note_vector_orderContext_mission.md</c>.
/// </summary>
public class ErpContextOrderClientTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";

    /// <summary>Réponse verrouillée : le régulateur a fixé « Centre 15 ».</summary>
    private const string LockedPayload = """
    {
      "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
      "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
      "contextOrderId": 4,
      "contextOrderCode": "CENTRE15",
      "contextOrderDisplay": "Centre 15",
      "locked": true,
      "availableContextOrders": [
        { "id": 4, "code": "CENTRE15",      "display": "Centre 15",         "index": 40 },
        { "id": 5, "code": "SECOURS_PISTE", "display": "Secours sur piste", "index": 50 }
      ]
    }
    """;

    /// <summary>Réponse libre : mission VSL programmée, aucun context posé.</summary>
    private const string UnsetPayload = """
    {
      "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
      "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
      "contextOrderId": null,
      "contextOrderCode": null,
      "contextOrderDisplay": null,
      "locked": false,
      "availableContextOrders": [
        { "id": 1, "code": "CPAM",  "display": "CPAM",       "index": 10 },
        { "id": 2, "code": "ART80", "display": "Article 80", "index": 20 }
      ]
    }
    """;

    [Fact]
    public async Task Appelle_la_route_contextOrder_de_la_mission()
    {
        var handler = new StubHandler(HttpStatusCode.OK, LockedPayload);
        var client = Build(handler);
        var missionId = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001");

        await client.GetMissionContextOrderAsync(missionId);

        // Le PathBase de l'IIS (…/order/) doit être conservé : l'URL est relative à la BaseAddress.
        handler.LastUri!.ToString()
            .Should().Be($"{BaseUrl}missions/{missionId}/contextOrder");
    }

    [Fact]
    public async Task Lit_le_context_effectif_le_verrou_et_le_selecteur()
    {
        var client = Build(new StubHandler(HttpStatusCode.OK, LockedPayload));

        var dto = await client.GetMissionContextOrderAsync(Guid.NewGuid());

        dto.Should().NotBeNull();
        dto!.ContextOrderId.Should().Be(4);
        dto.ContextOrderCode.Should().Be("CENTRE15");
        dto.ContextOrderDisplay.Should().Be("Centre 15");
        dto.Locked.Should().BeTrue();
        // Le sélecteur est renvoyé MÊME verrouillé : c'est `locked` qui gouverne l'éditabilité.
        dto.AvailableContextOrders.Should().HaveCount(2);
        dto.AvailableContextOrders[0].Code.Should().Be("CENTRE15");
        dto.AvailableContextOrders[0].Index.Should().Be(40);
    }

    [Fact]
    public async Task Context_non_renseigne_est_un_etat_valide()
    {
        var client = Build(new StubHandler(HttpStatusCode.OK, UnsetPayload));

        var dto = await client.GetMissionContextOrderAsync(Guid.NewGuid());

        // Pas de défaut auto : « non renseigné » ne doit pas être remplacé par le 1er de la liste.
        dto!.ContextOrderId.Should().BeNull();
        dto.Locked.Should().BeFalse();
        dto.AvailableContextOrders.Select(c => c.Id).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Mission_introuvable_renvoie_null_sans_lever()
    {
        var client = Build(new StubHandler(HttpStatusCode.NotFound, """{"detail":"Mission … introuvable."}"""));

        var dto = await client.GetMissionContextOrderAsync(Guid.NewGuid());

        dto.Should().BeNull();
    }

    [Fact]
    public async Task Erreur_serveur_est_remontee()
    {
        var client = Build(new StubHandler(HttpStatusCode.InternalServerError, "boom"));

        var act = () => client.GetMissionContextOrderAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static HttpErpReadApiClient Build(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
               NullLogger<HttpErpReadApiClient>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }
}
