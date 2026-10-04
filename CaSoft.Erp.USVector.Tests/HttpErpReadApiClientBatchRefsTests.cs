using System.Net;
using System.Text;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// <c>POST /missions/batch-refs</c> côté client. Ce qu'il faut tenir : une route ABSENTE (Orders avant
/// 1.8.5 — la production répondait 405 le 04/10) n'est pas une panne, elle déclenche le repli ; une
/// panne, elle, reste une panne.
/// </summary>
public class HttpErpReadApiClientBatchRefsTests
{
    private sealed class OrdersStub : HttpMessageHandler
    {
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string Body = "[]";
        public HttpRequestMessage? Request;
        public string? RequestBody;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(Status) { Content = new StringContent(Body, Encoding.UTF8, "application/json") };
        }
    }

    private static HttpErpReadApiClient Client(OrdersStub orders)
        => new(new HttpClient(orders) { BaseAddress = new Uri("https://orders.test/order/") },
            NullLogger<HttpErpReadApiClient>.Instance);

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    public async Task Une_route_absente_rend_null_pour_le_repli(HttpStatusCode status)
    {
        var orders = new OrdersStub { Status = status };

        var refs = await Client(orders).GetMissionBatchRefsAsync(new[] { Guid.NewGuid() });

        refs.Should().BeNull();
    }

    [Fact]
    public async Task Une_panne_reste_une_panne()
    {
        var orders = new OrdersStub { Status = HttpStatusCode.ServiceUnavailable };

        var act = () => Client(orders).GetMissionBatchRefsAsync(new[] { Guid.NewGuid() });

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Le_contrat_du_21_09_est_respecte_a_l_aller_et_au_retour()
    {
        var mission = Guid.NewGuid();
        var order = Guid.NewGuid();
        var orders = new OrdersStub
        {
            Body = $$"""[{"missionId":"{{mission}}","orderId":"{{order}}","beneficiaryId":null}]"""
        };

        var refs = await Client(orders).GetMissionBatchRefsAsync(new[] { mission });

        orders.Request!.Method.Should().Be(HttpMethod.Post);
        orders.Request.RequestUri!.ToString().Should().Be("https://orders.test/order/missions/batch-refs");
        orders.RequestBody.Should().Be($$"""{"missionIds":["{{mission}}"]}""");
        refs.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new ErpMissionBatchRefDto { MissionId = mission, OrderId = order, BeneficiaryId = null });
    }
}
