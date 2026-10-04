using System.Net;
using System.Text.Json;
using CaSoft.Erp.USVector.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Orders ne répond pas → 503 affichable, jamais un 500 brut. Du 21/09 au 04/10, ~2 600 requêtes
/// sont tombées en 500 faute de ce gestionnaire, dont l'écran d'entrée de l'ambulancier.
/// <para>
/// Les tests de pipeline montent le <b>même ordre</b> que <c>Program.cs</c> (gestionnaire, puis CORS) :
/// l'app web appelle depuis une autre origine, et un 503 sans en-tête CORS serait vu par le navigateur
/// comme une erreur réseau — le message n'arriverait jamais.
/// </para>
/// </summary>
public class OrdersUnavailableExceptionHandlerTests
{
    public static TheoryData<Exception> Pannes => new()
    {
        new BrokenCircuitException("The circuit is now open"),
        new TimeoutRejectedException("The operation didn't complete within the allowed timeout"),
        new HttpRequestException("Hôte inconnu."),
        new HttpRequestException("Orders.Api GET x → 503.", null, HttpStatusCode.ServiceUnavailable),
        new HttpRequestException("Orders.Api GET x → 500.", null, HttpStatusCode.InternalServerError),
        new HttpRequestException("Orders.Api GET x → 408.", null, HttpStatusCode.RequestTimeout)
    };

    public static TheoryData<Exception> PasDesPannes => new()
    {
        // Un 4xx d'Orders qu'aucun appelant n'a traité est un défaut : « réessayez » serait faux.
        new HttpRequestException("Orders.Api GET x → 400.", null, HttpStatusCode.BadRequest),
        new HttpRequestException("Orders.Api GET x → 409.", null, HttpStatusCode.Conflict),
        new InvalidOperationException("bogue")
    };

    [Theory]
    [MemberData(nameof(Pannes))]
    public void Une_panne_d_Orders_est_reconnue(Exception exception)
        => OrdersUnavailableExceptionHandler.IsOrdersUnavailable(exception).Should().BeTrue();

    [Theory]
    [MemberData(nameof(PasDesPannes))]
    public void Un_refus_ou_un_bogue_n_est_pas_une_panne(Exception exception)
        => OrdersUnavailableExceptionHandler.IsOrdersUnavailable(exception).Should().BeFalse();

    [Fact]
    public async Task Disjoncteur_ouvert_rend_503_affichable_avec_CORS()
    {
        using var client = await Pipeline(new BrokenCircuitException("The circuit is now open"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/crew/mine");
        request.Headers.Add("Origin", "https://app.example");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(OrdersUnavailableExceptionHandler.RetryAfterSeconds));
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle("*");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detail").GetString().Should().Be(OrdersUnavailableExceptionHandler.Detail);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(503);
    }

    [Fact]
    public async Task Un_bogue_reste_un_500()
    {
        using var client = await Pipeline(new InvalidOperationException("bogue"));

        var response = await client.GetAsync("/api/crew/mine");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Headers.RetryAfter.Should().BeNull();
    }

    /// <summary>Même câblage que <c>Program.cs</c>, avec une route qui lève l'exception donnée.</summary>
    private static async Task<HttpClient> Pipeline(Exception exception)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<OrdersUnavailableExceptionHandler>();
        builder.Services.AddCors();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseCors(o => o.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
        app.MapGet("/api/crew/mine", (HttpContext _) => { throw exception; });

        await app.StartAsync();
        return app.GetTestClient();
    }
}
