using System.Net;
using System.Text;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// <c>POST /missions/batch-refs</c> côté client. Depuis le retrait du repli (22/11), une route absente
/// est une panne comme une autre : plus aucun statut n'est traduit en « absence ».
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
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Une_route_absente_ou_en_panne_est_une_panne(HttpStatusCode status)
    {
        var orders = new OrdersStub { Status = status };

        var act = () => Client(orders).GetMissionBatchRefsAsync(new[] { Guid.NewGuid() });

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(status);
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
