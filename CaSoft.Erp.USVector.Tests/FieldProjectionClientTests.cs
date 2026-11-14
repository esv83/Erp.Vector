using System.Net;
using System.Text;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// DR-001 — <c>PUT missions/{id}/field</c> : un refus métier d'Order (400/409) remonte en issue
/// définitive, pour que l'outbox l'abandonne ; seule une panne lève et se relance.
/// </summary>
public class FieldProjectionClientTests
{
    private static readonly Guid Mission = Guid.Parse("745c9f76-9ba1-41d6-a873-b9c0e9868b11");

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task Un_refus_d_Order_est_une_issue_pas_une_exception(HttpStatusCode status)
        => (await Build(status).ProjectFieldAsync(Mission, true, "REFUS", DateTime.UtcNow))
            .Should().Be(EnOperationalProjectionOutcome.Refused);

    [Fact]
    public async Task Une_mission_inconnue_reste_MissionNotFound()
        => (await Build(HttpStatusCode.NotFound).ProjectFieldAsync(Mission, false, null, null))
            .Should().Be(EnOperationalProjectionOutcome.MissionNotFound);

    [Fact]
    public async Task Une_panne_leve_pour_etre_relancee()
    {
        var act = () => Build(HttpStatusCode.ServiceUnavailable).ProjectFieldAsync(Mission, false, null, null);
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static HttpErpWriteApiClient Build(HttpStatusCode status)
        => new(new HttpClient(new StubHandler(status)) { BaseAddress = new Uri("https://api.urgencesante.net/order/") },
               NullLogger<HttpErpWriteApiClient>.Instance);

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent("""{ "title": "Refus", "detail": "motif inconnu" }""", Encoding.UTF8, "application/problem+json")
            });
    }
}
