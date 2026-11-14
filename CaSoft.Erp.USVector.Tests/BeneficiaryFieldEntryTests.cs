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
/// J-144 / contact — les saisies de l'équipage sur le patient, relayées à la bordure d'Order.
/// <para>
/// Ce que ces tests protègent : le bon chemin de la bordure, <c>setBy</c> posé par l'API (jamais par
/// le front), les refus du contrat rendus en issue métier <b>avec le motif d'Order</b>, et une panne
/// qui lève — pour devenir 503 — au lieu de se déguiser en refus.
/// </para>
/// </summary>
public class BeneficiaryFieldEntryTests
{
    private const string EdgeUrl = "https://api.urgencesante.net/publicaccess/";
    private static readonly Guid Mission = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000002");
    private const string Sub = "0b6c2a4e-1111-2222-3333-444455556666";

    [Fact]
    public async Task L_identite_part_vers_la_bordure_avec_le_sub_de_l_ambulancier()
    {
        var handler = new StubHandler(HttpStatusCode.NoContent);

        var result = await Build(handler).RecordIdentityAsync(Mission, new ClBeneficiaryIdentityDtoIn
        {
            LastName = "Martin", FirstName = "Paul", BirthDate = "1965-03-02", Gender = 1
        }, Sub, default);

        result.Outcome.Should().Be(EnBeneficiaryEntryOutcome.Applied);
        handler.Uri!.ToString().Should().Be($"{EdgeUrl}vector/missions/{Mission}/beneficiary/identity");
        using var sent = JsonDocument.Parse(handler.Body!);
        sent.RootElement.GetProperty("lastName").GetString().Should().Be("Martin");
        sent.RootElement.GetProperty("birthDate").GetString().Should().Be("1965-03-02");
        sent.RootElement.GetProperty("gender").GetInt32().Should().Be(1);
        sent.RootElement.GetProperty("setBy").GetString().Should().Be(Sub);
    }

    [Fact]
    public async Task Le_contact_part_vers_la_bordure()
    {
        var handler = new StubHandler(HttpStatusCode.NoContent);

        var result = await Build(handler).RecordContactAsync(Mission,
            new ClBeneficiaryContactDtoIn { Phone = "06 12 34 56 78", Email = null }, Sub, default);

        result.Outcome.Should().Be(EnBeneficiaryEntryOutcome.Applied);
        handler.Uri!.ToString().Should().Be($"{EdgeUrl}vector/missions/{Mission}/beneficiary/contact");
        using var sent = JsonDocument.Parse(handler.Body!);
        sent.RootElement.GetProperty("phone").GetString().Should().Be("06 12 34 56 78");
        sent.RootElement.GetProperty("setBy").GetString().Should().Be(Sub);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, EnBeneficiaryEntryOutcome.Invalid)]
    [InlineData(HttpStatusCode.Forbidden, EnBeneficiaryEntryOutcome.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, EnBeneficiaryEntryOutcome.NotFound)]
    [InlineData(HttpStatusCode.Conflict, EnBeneficiaryEntryOutcome.Locked)]
    public async Task Les_refus_d_Order_remontent_en_issue_metier_avec_son_motif(HttpStatusCode status, EnBeneficiaryEntryOutcome expected)
    {
        var handler = new StubHandler(status, """{ "title": "Refus", "detail": "la mission du 10/11/2026 n'accepte plus de saisie du terrain" }""");

        var result = await Build(handler).RecordIdentityAsync(Mission, new ClBeneficiaryIdentityDtoIn { LastName = "Martin" }, Sub, default);

        result.Outcome.Should().Be(expected);
        result.Reason.Should().Be("la mission du 10/11/2026 n'accepte plus de saisie du terrain");
    }

    /// <summary>
    /// 503 de la bordure (Order injoignable) : lève avec son statut, que le gestionnaire commun rend
    /// en 503. Un 401 (notre jeton de service refusé) lève aussi : ce n'est pas un refus du terrain.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Une_panne_ou_un_jeton_refuse_levent(HttpStatusCode status)
    {
        var act = () => Build(new StubHandler(status)).RecordContactAsync(Mission,
            new ClBeneficiaryContactDtoIn { Email = "paul.martin@exemple.fr" }, Sub, default);

        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(status);
    }

    private static HttpBeneficiaryFieldEntryService Build(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri(EdgeUrl) }, NullLogger<HttpBeneficiaryFieldEntryService>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body = "")
        {
            _status = status;
            _body = body;
        }

        public Uri? Uri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Uri = request.RequestUri;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/problem+json") };
        }
    }
}
