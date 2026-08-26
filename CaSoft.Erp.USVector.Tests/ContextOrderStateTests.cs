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

    private static string Payload(bool locked, int? contextOrderId, string? origin = null) => $$"""
    {
      "missionId": "9f3ca1b2-0000-0000-0000-000000000001",
      "orderId":   "1a2bc3d4-0000-0000-0000-000000000002",
      "contextOrderId": {{(contextOrderId?.ToString() ?? "null")}},
      "contextOrderCode": "CENTRE15",
      "contextOrderDisplay": "Centre 15",
      "locked": {{(locked ? "true" : "false")}},
      "origin": {{(origin is null ? "null" : $"\"{origin}\"")}},
      "availableContextOrders": []
    }
    """;

    /// <summary>
    /// Le cas demandé, désormais servi tel quel par Orders.Api (<c>Order OC-24</c>) : la régulation
    /// propose, l'ambulancier garde la main. Verrou et provenance sont deux champs indépendants,
    /// pas un booléen déguisé.
    /// </summary>
    [Fact]
    public async Task Pose_par_la_regulation_mais_modifiable()
    {
        var state = await ReadState(Payload(locked: false, contextOrderId: 4, origin: "Regulator"));

        state!.Locked.Should().BeFalse("l'ambulancier peut encore changer le type");
        state.Origin.Should().Be("Regulator", "mais la valeur affichée vient de la régulation");
        state.ContextOrderDisplay.Should().Be("Centre 15");
    }

    [Fact]
    public async Task Context_impose_par_la_regulation_est_verrouille()
    {
        var state = await ReadState(Payload(locked: true, contextOrderId: 4, origin: "Regulator"));

        state!.Locked.Should().BeTrue();
        state.Origin.Should().Be("Regulator");
    }

    [Fact]
    public async Task Context_choisi_par_le_terrain_est_attribue_au_terrain()
    {
        var state = await ReadState(Payload(locked: false, contextOrderId: 4, origin: "Field"));

        state!.Locked.Should().BeFalse();
        state.Origin.Should().Be("Field");
    }

    /// <summary>
    /// Repli : une instance d'Orders.Api antérieure à OC-24 ne sert pas <c>origin</c>. On le déduit
    /// alors du verrou, ce qui rend l'ordre de déploiement indifférent — Vector livré en premier
    /// continue de fonctionner et bascule tout seul quand Order suit.
    /// </summary>
    [Theory]
    [InlineData(true, 4, "Regulator")]
    [InlineData(false, 4, "Field")]
    [InlineData(false, null, null)]
    public async Task Sans_origin_servi_la_provenance_est_deduite_du_verrou(bool locked, int? id, string? expected)
    {
        var state = await ReadState(Payload(locked, id));

        state!.Origin.Should().Be(expected);
    }

    [Fact]
    public async Task Aucun_context_pose_ne_designe_aucune_provenance()
    {
        var state = await ReadState(Payload(locked: false, contextOrderId: null, origin: null));

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

    private static async Task<ClContextOrderStateDtoOut?> ReadState(string payload)
        => await BuildService(new StubHandler(HttpStatusCode.OK, payload))
            .GetAsync(Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001"), CancellationToken.None);

    private static ContextOrderStateQueryService BuildService(StubHandler handler)
        => new(new HttpErpReadApiClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
            NullLogger<HttpErpReadApiClient>.Instance));


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
