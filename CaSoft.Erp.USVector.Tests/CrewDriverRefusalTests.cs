using System.Net;
using System.Text;
using System.Text.Json;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Dto;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// MOB-11 — Le refus d'Orders sur le conducteur arrive à l'ambulancier <b>avec son motif</b>.
/// <para>
/// Constaté le 2026-09-13 : désigner un conducteur après la fin de la vacation était refusé par Orders
/// avec une phrase claire, que Vector journalisait en ERROR puis remplaçait par
/// « Orders.Api PUT crews/…/driver → 400. ». L'ambulancier réessayait cinq fois en 35 s. Ces tests
/// épinglent les deux moitiés : le client HTTP ne lève plus sur un refus métier, et le motif traverse
/// jusqu'au corps du 400 mobile — même code qu'avant, seul le texte change (D14).
/// </para>
/// </summary>
public class CrewDriverRefusalTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";
    private const string VacationEnded =
        "La vacation s'est terminée le 13/09/2026 à 18:00 : on ne peut pas y désigner un conducteur après.";

    // ── Client HTTP ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Envoie_un_PUT_sur_la_route_de_l_equipage_avec_le_conducteur_et_la_date()
    {
        var handler = new StubHandler(HttpStatusCode.NoContent);
        var crewId = Guid.Parse("5c1e0000-0000-0000-0000-000000000001");
        var driverId = Guid.Parse("5c1e0000-0000-0000-0000-000000000002");

        var result = await Build(handler).SetCrewDriverAsync(crewId, driverId, new DateTime(2026, 9, 13, 17, 0, 0));

        result.Outcome.Should().Be(EnCrewDriverWriteOutcome.Applied);
        handler.LastMethod.Should().Be(HttpMethod.Put);
        handler.LastUri!.ToString().Should().Be($"{BaseUrl}crews/{crewId}/driver");
        using var sent = JsonDocument.Parse(handler.LastBody!);
        sent.RootElement.GetProperty("driverPersonnelId").GetGuid().Should().Be(driverId);
    }

    [Fact]
    public async Task Vacation_terminee_est_un_refus_avec_le_motif_d_Orders_sans_lever()
    {
        var client = Build(new StubHandler(HttpStatusCode.BadRequest,
            $$"""{"title":"Conducteur refusé","detail":"{{VacationEnded}}"}"""));

        var result = await client.SetCrewDriverAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);

        result.Outcome.Should().Be(EnCrewDriverWriteOutcome.Refused);
        result.Reason.Should().Be(VacationEnded);
    }

    [Fact]
    public async Task Conflit_est_un_refus()
    {
        var client = Build(new StubHandler(HttpStatusCode.Conflict, """{"detail":"Déjà conducteur."}"""));

        var result = await client.SetCrewDriverAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);

        result.Outcome.Should().Be(EnCrewDriverWriteOutcome.Refused);
        result.Reason.Should().Be("Déjà conducteur.");
    }

    [Fact]
    public async Task Equipage_inconnu_renvoie_CrewNotFound_sans_lever()
    {
        var client = Build(new StubHandler(HttpStatusCode.NotFound, """{"title":"Équipage introuvable"}"""));

        var result = await client.SetCrewDriverAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);

        result.Outcome.Should().Be(EnCrewDriverWriteOutcome.CrewNotFound);
        result.Reason.Should().Be("Équipage introuvable");
    }

    [Fact]
    public async Task Refus_au_corps_illisible_reste_un_refus_sans_motif()
    {
        var client = Build(new StubHandler(HttpStatusCode.BadRequest, "<html>proxy</html>"));

        var result = await client.SetCrewDriverAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);

        result.Outcome.Should().Be(EnCrewDriverWriteOutcome.Refused);
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task Erreur_serveur_est_remontee()
    {
        var client = Build(new StubHandler(HttpStatusCode.InternalServerError, "boom"));

        var act = () => client.SetCrewDriverAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── Cas d'usage, jusqu'à la réponse mobile ─────────────────────────────

    [Fact]
    public void Le_motif_d_Orders_devient_le_corps_du_400_mobile()
    {
        var (crew, driver) = CrewWithOneMember();
        var repo = new FakeCrews(crew, ClCrewDriverWriteResult.Refused(VacationEnded));

        var result = new ClSetDriverUseCase(new ClSetDriverCommand(crew.CrewId, driver.Id), repo).Handle();

        var http = result.ToActionResult().Should().BeOfType<BadRequestObjectResult>().Subject;
        http.Value.Should().Be(VacationEnded);
    }

    [Fact]
    public void Refus_sans_motif_retombe_sur_un_libelle_de_refus_pas_sur_une_panne()
    {
        var (crew, driver) = CrewWithOneMember();
        var repo = new FakeCrews(crew, ClCrewDriverWriteResult.Refused("   "));

        var result = new ClSetDriverUseCase(new ClSetDriverCommand(crew.CrewId, driver.Id), repo).Handle();

        result.IsFail.Should().BeTrue();
        result.InnerError!.ErrorText.Should().Be(ClSetDriverUseCase.RefusalFallback);
    }

    [Fact]
    public void Conducteur_enregistre_renvoie_Ok()
    {
        var (crew, driver) = CrewWithOneMember();
        var repo = new FakeCrews(crew, ClCrewDriverWriteResult.Applied());

        var result = new ClSetDriverUseCase(new ClSetDriverCommand(crew.CrewId, driver.Id), repo).Handle();

        result.IsSucces.Should().BeTrue();
        repo.Updated.Should().BeTrue();
    }

    // ── Outillage ───────────────────────────────────────────────────────────

    private static (ClCrew Crew, ClEmployee Driver) CrewWithOneMember()
    {
        var driver = new ClEmployee(Guid.NewGuid(), "Jean", "Martin");
        var crew = new ClCrew(Guid.NewGuid(), new List<ClEmployee> { driver }, new DateTime(2026, 9, 13, 8, 0, 0),
            new ClVehicle(Guid.Empty, string.Empty, new ClKilometers()));
        return (crew, driver);
    }

    private static HttpErpWriteApiClient Build(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
               NullLogger<HttpErpWriteApiClient>.Instance);

    private sealed class FakeCrews : ICrewRepository
    {
        private readonly ClCrew _crew;
        private readonly ClCrewDriverWriteResult _write;

        public FakeCrews(ClCrew crew, ClCrewDriverWriteResult write)
        {
            _crew = crew;
            _write = write;
        }

        public bool Updated { get; private set; }

        public ClCrew GetCrew(Guid gCrewID) => _crew;
        public ClCrewDriverWriteResult Update(ClCrew crew)
        {
            Updated = true;
            return _write;
        }

        public bool IsEmployeeInCrew(Guid gCrewID, Guid gEmployeeId) => throw new NotSupportedException();
        public ClLogDriverModel GetCrewDriver(Guid gVehicleID) => throw new NotSupportedException();
        public List<ClJobListItemModel> FetchJobList(Guid gCrewId) => throw new NotSupportedException();
        public List<ClJobListItemModel> FetchJobList(IReadOnlyCollection<Guid> gCrewIds) => throw new NotSupportedException();
        public List<ClInstructionListItemModel> FetchInstructionList(Guid gCrewId) => throw new NotSupportedException();
        public void AckInstruction(int instructionId) => throw new NotSupportedException();
        public List<Guid> GetCrewIdList(DateOnly id) => throw new NotSupportedException();
    }

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
