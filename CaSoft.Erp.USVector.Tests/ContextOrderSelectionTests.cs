using System.Net;
using System.Text;
using System.Text.Json;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-4 — Relais de la sélection terrain vers Orders.Api.
/// <para>
/// Ce que ces tests protègent avant tout, c'est la <b>correspondance des identifiants</b> : le
/// catalogue Vector et le catalogue Order ne partagent pas leurs ids, et l'id <c>4</c> vaut
/// <c>ART80</c> ici, <c>CENTRE15</c> là-bas. Relayer l'entier reçu écrirait un type pour un autre
/// sur une mission réelle — le premier test épingle exactement cela.
/// </para>
/// </summary>
public class ContextOrderSelectionTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";
    private static readonly Guid Mission = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001");

    /// <summary>Catalogue Order tel que servi en production : ART80 y porte l'id 2, pas 4.</summary>
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
    /// Le cœur d'OC-4 : l'ambulancier coche « Article 80 » (id <b>4</b> côté Vector), l'ERP doit
    /// recevoir l'id <b>2</b>. Un relais naïf enverrait 4, c'est-à-dire « Centre 15 ».
    /// </summary>
    [Fact]
    public async Task Traduit_l_id_Vector_vers_l_id_Order_par_le_code()
    {
        var handler = new RoutingHandler(ContextPayload);
        var outcome = await Build(handler).SelectAsync(Mission, contractTypeId: 4, "amb-42", default);

        outcome.Should().Be(EnContextOrderSelectionOutcome.Applied);
        handler.PatchUri!.ToString().Should().Be($"{BaseUrl}missions/{Mission}/contextOrder");

        using var sent = JsonDocument.Parse(handler.PatchBody!);
        sent.RootElement.GetProperty("contextOrderId").GetInt32()
            .Should().Be(2, "ART80 porte l'id 2 côté Order, alors qu'il porte le 4 côté Vector");
        sent.RootElement.GetProperty("setBy").GetString().Should().Be("amb-42");
    }

    /// <summary>
    /// STANDARD n'a aucun équivalent dans le catalogue Order. On refuse plutôt que d'écrire un type
    /// approchant : un mauvais type part en facturation sans que personne ne le voie passer.
    /// </summary>
    [Fact]
    public async Task Type_sans_equivalent_cote_Order_est_refuse()
    {
        var handler = new RoutingHandler(ContextPayload);
        var outcome = await Build(handler).SelectAsync(Mission, contractTypeId: 1, null, default);

        outcome.Should().Be(EnContextOrderSelectionOutcome.NotApplicable);
        handler.PatchUri.Should().BeNull("aucune écriture ne doit partir sur un type non traduit");
    }

    [Fact]
    public async Task Type_inconnu_du_catalogue_Vector_est_refuse()
    {
        var handler = new RoutingHandler(ContextPayload);
        var outcome = await Build(handler).SelectAsync(Mission, contractTypeId: 99, null, default);

        outcome.Should().Be(EnContextOrderSelectionOutcome.NotApplicable);
        handler.PatchUri.Should().BeNull();
    }

    [Fact]
    public async Task Mission_inconnue_cote_ERP_ne_declenche_aucune_ecriture()
    {
        var handler = new RoutingHandler(getStatus: HttpStatusCode.NotFound);
        var outcome = await Build(handler).SelectAsync(Mission, contractTypeId: 4, null, default);

        outcome.Should().Be(EnContextOrderSelectionOutcome.MissionNotFound);
        handler.PatchUri.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, EnContextOrderSelectionOutcome.LockedByRegulator)]
    [InlineData(HttpStatusCode.BadRequest, EnContextOrderSelectionOutcome.NotApplicable)]
    [InlineData(HttpStatusCode.NotFound, EnContextOrderSelectionOutcome.MissionNotFound)]
    public async Task Les_refus_de_l_ERP_remontent_en_issue_metier(HttpStatusCode status, EnContextOrderSelectionOutcome expected)
    {
        var handler = new RoutingHandler(ContextPayload, patchStatus: status);

        var outcome = await Build(handler).SelectAsync(Mission, contractTypeId: 4, null, default);

        outcome.Should().Be(expected);
    }

    private static ContextOrderSelectionService Build(RoutingHandler handler)
    {
        var ctx = new MobileDbContext(new DbContextOptionsBuilder<MobileDbContext>()
            .UseInMemoryDatabase($"selection-{Guid.NewGuid()}").Options);

        // Catalogue Vector réel (relevé en base le 2026-08-24) : STANDARD=1, ART80=4.
        ctx.ContractTypes.AddRange(
            new MOB_CONTRACT_TYPE { CTT_ID = 1, CTT_CODE = "STANDARD", CTT_DISPLAY = "Transport standard", CTT_ACTIVE = true },
            new MOB_CONTRACT_TYPE { CTT_ID = 4, CTT_CODE = "ART80", CTT_DISPLAY = "Article 83", CTT_ACTIVE = true });
        ctx.SaveChanges();

        return new ContextOrderSelectionService(
            ctx,
            new HttpErpReadApiClient(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                                     NullLogger<HttpErpReadApiClient>.Instance),
            new HttpErpWriteApiClient(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                                      NullLogger<HttpErpWriteApiClient>.Instance));
    }

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
