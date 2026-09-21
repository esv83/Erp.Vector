using System.Net;
using CaSoft.Erp.USVector.Api.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// DEC-7 — Ce qui empêche un appel à Orders de pendre, et ce qui l'empêche de s'acharner.
/// <para>
/// Ces tests montent le <b>vrai</b> câblage (fabrique de clients + pipeline de résilience) et ne
/// remplacent qu'Orders. Ce qu'ils protègent : <b>une panne se retente, une réponse ne se retente
/// pas</b> — plusieurs lectures s'appuient sur le 404 (équipage inconnu, mission hors du lot), et les
/// refus métier portent leur motif.
/// </para>
/// </summary>
public class OrdersResilienceTests
{
    /// <summary>Orders simulé : compte les essais, et rend ce qu'on lui a dit.</summary>
    private sealed class OrdersStub : HttpMessageHandler
    {
        public int Calls;
        public HttpStatusCode Status = HttpStatusCode.OK;
        public TimeSpan Delay = TimeSpan.Zero;
        public Exception? Throws;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            if (Throws is not null) throw Throws;
            if (Delay > TimeSpan.Zero) await Task.Delay(Delay, ct);
            return new HttpResponseMessage(Status);
        }
    }

    /// <summary>Réglages minuscules : ces tests mesurent le comportement, pas la patience.</summary>
    private static HttpClient Client(OrdersStub orders, params (string Key, string Value)[] reglages)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{OrdersResilience.SectionName}:AttemptTimeoutSeconds"] = "1",
            [$"{OrdersResilience.SectionName}:TotalTimeoutSeconds"] = "3",
            [$"{OrdersResilience.SectionName}:RetryDelayMs"] = "1",
            [$"{OrdersResilience.SectionName}:BreakerSamplingSeconds"] = "5",
        }.Concat(reglages.Select(r => new KeyValuePair<string, string?>($"{OrdersResilience.SectionName}:{r.Key}", r.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value))).Build();

        var services = new ServiceCollection();
        services.AddHttpClient("orders", c => c.BaseAddress = new Uri("https://api/order/"))
            .AddOrdersResilience(configuration)
            .ConfigurePrimaryHttpMessageHandler(() => orders);

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("orders");
    }

    // ── Ce qui se retente ────────────────────────────────────────────────────

    [Fact]
    public async Task Une_panne_d_Orders_est_retentee()
    {
        var orders = new OrdersStub { Status = HttpStatusCode.InternalServerError };

        await Client(orders, ("MaxRetries", "2")).GetAsync("missions");

        orders.Calls.Should().Be(3, "l'essai initial, puis deux nouvelles tentatives");
    }

    [Fact]
    public async Task Une_coupure_reseau_est_retentee()
    {
        var orders = new OrdersStub { Throws = new HttpRequestException("connexion refusée") };

        var act = () => Client(orders, ("MaxRetries", "1")).GetAsync("missions");

        await act.Should().ThrowAsync<Exception>();
        orders.Calls.Should().Be(2);
    }

    // ── Ce qui ne se retente pas ─────────────────────────────────────────────

    /// <summary>
    /// 🔴 Le 404 est une RÉPONSE, pas une panne : `ListCrewIdsAsync` en fait une liste vide, et le lot
    /// de paquets une mission `NotFound`. Le retenter tripleraient la charge sans rien changer.
    /// </summary>
    [Fact]
    public async Task Un_404_n_est_jamais_retente()
    {
        var orders = new OrdersStub { Status = HttpStatusCode.NotFound };

        var reponse = await Client(orders).GetAsync("crews/inconnu");

        reponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        orders.Calls.Should().Be(1);
    }

    /// <summary>Un refus métier porte son motif : le rejouer effacerait ce motif derrière une attente.</summary>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task Un_refus_metier_n_est_jamais_retente(HttpStatusCode refus)
    {
        var orders = new OrdersStub { Status = refus };

        var reponse = await Client(orders).PutAsync("crews/x/driver", new StringContent("{}"));

        reponse.StatusCode.Should().Be(refus);
        orders.Calls.Should().Be(1);
    }

    // ── Ce qui coupe ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sans délai explicite, un appel pendait <b>100 secondes</b> — et depuis les lots, un seul appel
    /// qui pend retient 200 paquets.
    /// </summary>
    [Fact]
    public async Task Un_appel_qui_pend_est_coupe_bien_avant_les_cent_secondes()
    {
        var orders = new OrdersStub { Delay = TimeSpan.FromSeconds(30) };
        var debut = DateTime.UtcNow;

        var act = () => Client(orders, ("AttemptTimeoutSeconds", "1"), ("TotalTimeoutSeconds", "2"),
                               ("MaxRetries", "0")).GetAsync("missions");

        await act.Should().ThrowAsync<Exception>();
        DateTime.UtcNow.Subtract(debut).Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Une_reponse_normale_passe_sans_delai_ni_tentative_supplementaire()
    {
        var orders = new OrdersStub();

        var reponse = await Client(orders).GetAsync("missions");

        reponse.StatusCode.Should().Be(HttpStatusCode.OK);
        orders.Calls.Should().Be(1);
    }
}
