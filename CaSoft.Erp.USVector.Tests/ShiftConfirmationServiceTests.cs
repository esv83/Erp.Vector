using System.Net;
using System.Text;
using System.Text.Json;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Confirmation de prise de service depuis l'application, relayée vers Orders.Api.
/// <para>
/// Ce que ces tests protègent : <b>aucun appel ne vise la délivrance du lien</b> (elle tuerait celui du
/// courriel), l'identité envoyée est celle que le contrôleur a résolue, et les refus d'Order reviennent
/// en issue métier — jamais en exception.
/// </para>
/// </summary>
public class ShiftConfirmationServiceTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";
    private static readonly Guid Personnel = Guid.Parse("81cc3fd1-c2e9-4a40-b798-68da7f29b907");
    private static readonly Guid Crew = Guid.Parse("d1f0e2a3-0000-0000-0000-000000000001");
    private static readonly Guid Request = Guid.Parse("5e5e5e5e-0000-0000-0000-000000000002");

    [Fact]
    public async Task Lit_ce_qui_attend_sur_la_route_du_personnel()
    {
        var handler = new StubHandler(HttpStatusCode.OK, $$"""
        [ { "requestId": "{{Request}}", "crewId": "{{Crew}}", "crewLabel": "A601",
            "proposedLocalTime": "2026-09-16T07:00:00", "status": 1, "sentAtUtc": "2026-09-15T18:00:00" } ]
        """);

        var mine = await Build(handler).GetMineAsync(Personnel, default);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.ToString().Should().Be($"{BaseUrl}personnel/{Personnel}/shift-confirmations/pending");
        mine.HasPending.Should().BeTrue();
        mine.Pending.Should().ContainSingle();
        mine.Pending[0].RequestId.Should().Be(Request);
        mine.Pending[0].CrewId.Should().Be(Crew);
        mine.Pending[0].CrewLabel.Should().Be("A601");
        mine.Pending[0].ProposedLocalTime.Should().Be(new DateTime(2026, 9, 16, 7, 0, 0));
    }

    [Fact]
    public async Task Rien_n_attend_rend_HasPending_faux()
    {
        var mine = await Build(new StubHandler(HttpStatusCode.OK, "[]")).GetMineAsync(Personnel, default);

        mine.HasPending.Should().BeFalse();
        mine.Pending.Should().BeEmpty();
    }

    /// <summary>
    /// Vector livré avant la route d'Order : l'application ne doit pas échouer à son lancement pour une
    /// fonctionnalité qui n'est pas encore servie.
    /// </summary>
    [Fact]
    public async Task Route_absente_cote_Order_rend_rien_a_confirmer_sans_lever()
    {
        var mine = await Build(new StubHandler(HttpStatusCode.NotFound)).GetMineAsync(Personnel, default);

        mine.HasPending.Should().BeFalse();
    }

    [Fact]
    public async Task Une_panne_d_Order_leve_pour_que_le_controleur_rende_503()
    {
        var agir = () => Build(new StubHandler(HttpStatusCode.InternalServerError)).GetMineAsync(Personnel, default);

        await agir.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Confirme_par_la_route_de_la_demande_avec_le_personnel_resolu()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            """{ "firstName": "Luc", "proposedLocalTime": "2026-09-16T07:00:00", "confirmed": true, "alreadyConfirmed": false }""");

        var result = await Build(handler).ConfirmAsync(Crew, Request, Personnel, default);

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.ToString().Should().Be($"{BaseUrl}crews/{Crew}/shift-confirmations/{Request}/confirm");
        handler.LastUri!.ToString().Should().NotContain("/link", "délivrer un lien tuerait celui du courriel");

        using var sent = JsonDocument.Parse(handler.LastBody!);
        sent.RootElement.GetProperty("personnelId").GetGuid().Should().Be(Personnel);

        result.Outcome.Should().Be(EnShiftConfirmationConfirmOutcome.Confirmed);
        result.ProposedLocalTime.Should().Be(new DateTime(2026, 9, 16, 7, 0, 0));
    }

    [Fact]
    public async Task Deja_confirmee_rend_AlreadyConfirmed()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            """{ "firstName": "Luc", "proposedLocalTime": "2026-09-16T07:00:00", "confirmed": true, "alreadyConfirmed": true }""");

        var result = await Build(handler).ConfirmAsync(Crew, Request, Personnel, default);

        result.Outcome.Should().Be(EnShiftConfirmationConfirmOutcome.AlreadyConfirmed);
    }

    [Fact]
    public async Task Demande_d_un_autre_rend_NotFound_sans_lever()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound,
            """{ "title": "Ressource introuvable", "detail": "Demande de confirmation introuvable." }""");

        var result = await Build(handler).ConfirmAsync(Crew, Request, Personnel, default);

        result.Outcome.Should().Be(EnShiftConfirmationConfirmOutcome.NotFound);
    }

    [Fact]
    public async Task Un_refus_d_Order_remonte_avec_son_motif()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest,
            """{ "title": "Requête invalide", "detail": "Cette prise de service a déjà été validée par le régulateur." }""");

        var result = await Build(handler).ConfirmAsync(Crew, Request, Personnel, default);

        result.Outcome.Should().Be(EnShiftConfirmationConfirmOutcome.Refused);
        result.Reason.Should().Be("Cette prise de service a déjà été validée par le régulateur.");
    }

    private static HttpShiftConfirmationService Build(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
               NullLogger<HttpShiftConfirmationService>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body = "")
        {
            _status = status;
            _body = body;
        }

        public HttpMethod? LastMethod { get; private set; }
        public Uri? LastUri { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastMethod = request.Method;
            LastUri = request.RequestUri;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }
}
