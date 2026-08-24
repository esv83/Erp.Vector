using System.Net;
using System.Text;
using System.Text.Json;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-2 — Écriture du <b>context de la mission</b> vers Orders.Api.
/// Épingle la route, le verbe, le corps envoyé, et surtout la **traduction des refus métier** :
/// verrou régulateur (409) et context non applicable (400) sont des issues normales du terrain,
/// pas des pannes — elles ne doivent jamais remonter en exception.
/// Contrat : <c>Erp.Order/note_vector_orderContext_mission.md</c> §2.
/// </summary>
public class ErpContextOrderWriteClientTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";

    [Fact]
    public async Task Envoie_un_PATCH_sur_la_route_de_la_mission_avec_le_choix_et_son_auteur()
    {
        var handler = new StubHandler(HttpStatusCode.NoContent);
        var client = Build(handler);
        var missionId = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001");

        await client.SetMissionContextOrderAsync(missionId, contextOrderId: 1, setBy: "amb-42");

        handler.LastMethod.Should().Be(HttpMethod.Patch);
        handler.LastUri!.ToString().Should().Be($"{BaseUrl}missions/{missionId}/contextOrder");

        // camelCase, comme tout le contrat Orders.Api.
        using var sent = JsonDocument.Parse(handler.LastBody!);
        sent.RootElement.GetProperty("contextOrderId").GetInt32().Should().Be(1);
        sent.RootElement.GetProperty("setBy").GetString().Should().Be("amb-42");
    }

    [Fact]
    public async Task Choix_accepte_renvoie_Applied()
    {
        var client = Build(new StubHandler(HttpStatusCode.NoContent));

        var outcome = await client.SetMissionContextOrderAsync(Guid.NewGuid(), 2);

        outcome.Should().Be(EnContextOrderWriteOutcome.Applied);
    }

    [Fact]
    public async Task Verrou_regulateur_renvoie_LockedByRegulator_sans_lever()
    {
        var client = Build(new StubHandler(HttpStatusCode.Conflict,
            """{"detail":"Context fixé par la régulation."}"""));

        var outcome = await client.SetMissionContextOrderAsync(Guid.NewGuid(), 2);

        outcome.Should().Be(EnContextOrderWriteOutcome.LockedByRegulator);
    }

    [Fact]
    public async Task Context_non_applicable_renvoie_NotApplicable_sans_lever()
    {
        var client = Build(new StubHandler(HttpStatusCode.BadRequest,
            """{"detail":"Context non applicable à cette commande."}"""));

        var outcome = await client.SetMissionContextOrderAsync(Guid.NewGuid(), 5);

        outcome.Should().Be(EnContextOrderWriteOutcome.NotApplicable);
    }

    [Fact]
    public async Task Mission_inconnue_renvoie_MissionNotFound_sans_lever()
    {
        var client = Build(new StubHandler(HttpStatusCode.NotFound));

        var outcome = await client.SetMissionContextOrderAsync(Guid.NewGuid(), 1);

        outcome.Should().Be(EnContextOrderWriteOutcome.MissionNotFound);
    }

    [Fact]
    public async Task Erreur_serveur_est_remontee()
    {
        var client = Build(new StubHandler(HttpStatusCode.InternalServerError, "boom"));

        var act = () => client.SetMissionContextOrderAsync(Guid.NewGuid(), 1);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static HttpErpWriteApiClient Build(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
               NullLogger<HttpErpWriteApiClient>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body = "")
        {
            _status = status;
            _body = body;
        }

        public Uri? LastUri { get; private set; }
        public HttpMethod? LastMethod { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            LastMethod = request.Method;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }
}
