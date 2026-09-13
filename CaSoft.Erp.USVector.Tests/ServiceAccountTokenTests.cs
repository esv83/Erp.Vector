using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// C2 — Jeton de service vers Orders.Api : inerte sans configuration, un seul jeton tant qu'il est
/// valide, oublié sur un 401, et jamais bloquant quand le realm ne répond pas.
/// </summary>
public class ServiceAccountTokenTests
{
    private sealed class RealmStub : HttpMessageHandler
    {
        public int Calls;
        public HttpStatusCode Status = HttpStatusCode.OK;
        public int ExpiresIn = 300;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            var body = $"{{\"access_token\":\"jeton-{Calls}\",\"expires_in\":{ExpiresIn}}}";
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class OrdersStub : HttpMessageHandler
    {
        public AuthenticationHeaderValue? LastAuthorization;
        public HttpStatusCode Status = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastAuthorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(Status));
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 13, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static ServiceAccountOptions Configured() => new()
    {
        TokenEndpoint = "https://sso/realms/delesse/protocol/openid-connect/token",
        ClientId = "us-vector",
        ClientSecret = "secret"
    };

    private static ServiceAccountTokenProvider Provider(RealmStub realm, ServiceAccountOptions options, TimeProvider? clock = null)
        => new(new HttpClient(realm), options, NullLogger<ServiceAccountTokenProvider>.Instance, clock);

    private static HttpClient Orders(ServiceAccountTokenProvider tokens, OrdersStub orders)
        => new(new ServiceAccountTokenHandler(tokens, NullLogger<ServiceAccountTokenHandler>.Instance) { InnerHandler = orders })
        {
            BaseAddress = new Uri("https://api/order/")
        };

    [Fact]
    public async Task Sans_compte_de_service_l_appel_part_sans_jeton_et_le_realm_n_est_pas_sollicite()
    {
        var realm = new RealmStub();
        var orders = new OrdersStub();

        await Orders(Provider(realm, new ServiceAccountOptions()), orders).GetAsync("missions");

        orders.LastAuthorization.Should().BeNull();
        realm.Calls.Should().Be(0);
    }

    [Fact]
    public void Le_secret_de_remplacement_vaut_non_configure()
    {
        var options = Configured();
        options.ClientSecret = ServiceAccountOptions.Placeholder;
        options.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Le_jeton_est_pose_en_Bearer()
    {
        var orders = new OrdersStub();

        await Orders(Provider(new RealmStub(), Configured()), orders).GetAsync("missions");

        orders.LastAuthorization!.Scheme.Should().Be("Bearer");
        orders.LastAuthorization.Parameter.Should().Be("jeton-1");
    }

    [Fact]
    public async Task Un_seul_jeton_tant_qu_il_est_valide()
    {
        var realm = new RealmStub();
        var tokens = Provider(realm, Configured());

        (await tokens.GetTokenAsync()).Should().Be("jeton-1");
        (await tokens.GetTokenAsync()).Should().Be("jeton-1");
        realm.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Le_jeton_est_renouvele_avant_son_expiration()
    {
        var realm = new RealmStub { ExpiresIn = 300 };   // marge de 60 s : valable 240 s
        var clock = new ManualClock();
        var tokens = Provider(realm, Configured(), clock);

        await tokens.GetTokenAsync();
        clock.Now = clock.Now.AddSeconds(239);
        (await tokens.GetTokenAsync()).Should().Be("jeton-1");
        clock.Now = clock.Now.AddSeconds(2);
        (await tokens.GetTokenAsync()).Should().Be("jeton-2");
    }

    [Fact]
    public async Task Un_401_d_Orders_fait_oublier_le_jeton()
    {
        var realm = new RealmStub();
        var orders = new OrdersStub { Status = HttpStatusCode.Unauthorized };
        var client = Orders(Provider(realm, Configured()), orders);

        await client.GetAsync("missions");
        await client.GetAsync("missions");

        realm.Calls.Should().Be(2);
        orders.LastAuthorization!.Parameter.Should().Be("jeton-2");
    }

    [Fact]
    public async Task Un_realm_indisponible_ne_bloque_pas_l_appel_a_Orders()
    {
        var orders = new OrdersStub();

        var response = await Orders(Provider(new RealmStub { Status = HttpStatusCode.InternalServerError }, Configured()), orders)
            .GetAsync("missions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        orders.LastAuthorization.Should().BeNull();
    }
}
