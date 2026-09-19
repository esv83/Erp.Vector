using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Identity.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// C2 — Jeton de service vers Orders.Api, posé par le gestionnaire du paquet
/// <c>CaSoft.Identity.Client</c> à travers <see cref="ServiceAccountTokenRegistration"/>.
/// <para>
/// Ces tests montent le <b>câblage réel</b> — l'extension, la fabrique de clients, le gestionnaire du
/// paquet — et ne remplacent que les deux bouts du fil : le realm et Orders. Le risque n'est plus dans
/// le gestionnaire, testé chez Identity, mais dans la traduction de la configuration de Vector : un
/// compte mal traduit rendrait le jeton muet sans une erreur.
/// </para>
/// </summary>
public class ServiceAccountTokenTests
{
    private sealed class RealmStub : HttpMessageHandler
    {
        public int Calls;
        public HttpStatusCode Status = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            var body = $"{{\"access_token\":\"jeton-{Calls}\",\"expires_in\":300}}";
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

    private static ServiceAccountOptions Configured() => new()
    {
        TokenEndpoint = "https://sso/realms/delesse/protocol/openid-connect/token",
        ClientId = "erp-vector-api",
        ClientSecret = "secret"
    };

    /// <summary>Le client « Orders » tel que Program.cs le câble, les deux bouts du fil remplacés.</summary>
    private static HttpClient Orders(ServiceAccountOptions account, OrdersStub orders, RealmStub realm)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("orders", c => c.BaseAddress = new Uri("https://api/order/"))
            .AddVectorServiceAccountToken(account)
            .ConfigurePrimaryHttpMessageHandler(() => orders);
        services.AddHttpClient(ClServiceAccountTokenProvider.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => realm);

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("orders");
    }

    [Fact]
    public async Task Sans_compte_de_service_l_appel_part_sans_jeton_et_le_realm_n_est_pas_sollicite()
    {
        var realm = new RealmStub();
        var orders = new OrdersStub();

        await Orders(new ServiceAccountOptions(), orders, realm).GetAsync("missions");

        orders.LastAuthorization.Should().BeNull();
        realm.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Le_secret_de_remplacement_laisse_le_gestionnaire_inerte_meme_avec_un_point_de_jeton()
    {
        // Le cas du poste de dev : TokenEndpoint est toujours déduit de Keycloak:Authority, le secret
        // reste « __SET_VIA_ENV__ ». Le paquet, lui, s'active sur le seul TokenEndpoint.
        var realm = new RealmStub();
        var orders = new OrdersStub();
        var account = Configured();
        account.ClientSecret = ServiceAccountOptions.Placeholder;

        await Orders(account, orders, realm).GetAsync("missions");

        orders.LastAuthorization.Should().BeNull();
        realm.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Le_jeton_est_pose_en_Bearer_et_reutilise_tant_qu_il_est_valide()
    {
        var realm = new RealmStub();
        var orders = new OrdersStub();
        var client = Orders(Configured(), orders, realm);

        await client.GetAsync("missions");
        await client.GetAsync("missions");

        orders.LastAuthorization!.Scheme.Should().Be("Bearer");
        orders.LastAuthorization.Parameter.Should().Be("jeton-1");
        realm.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Un_401_d_Orders_fait_oublier_le_jeton()
    {
        var realm = new RealmStub();
        var orders = new OrdersStub { Status = HttpStatusCode.Unauthorized };
        var client = Orders(Configured(), orders, realm);

        await client.GetAsync("missions");
        await client.GetAsync("missions");

        realm.Calls.Should().Be(2);
        orders.LastAuthorization!.Parameter.Should().Be("jeton-2");
    }

    [Fact]
    public async Task Un_realm_indisponible_ne_bloque_pas_l_appel_a_Orders()
    {
        var orders = new OrdersStub();

        var response = await Orders(Configured(), orders, new RealmStub { Status = HttpStatusCode.InternalServerError })
            .GetAsync("missions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        orders.LastAuthorization.Should().BeNull();
    }

    [Fact]
    public void La_traduction_porte_le_compte_de_Vector_dans_la_section_du_paquet()
    {
        var account = Configured();
        account.TokenRenewalMarginSeconds = 90;

        var settings = ServiceAccountTokenRegistration.ToIdentityClientSettings(account);

        settings.Should().Contain("Identity:TokenEndpoint", account.TokenEndpoint)
            .And.Contain("Identity:ClientId", "erp-vector-api")
            .And.Contain("Identity:ClientSecret", "secret")
            .And.Contain("Identity:TokenRenewalMarginSeconds", "90")
            .And.NotContainKey("Identity:Scope");
    }
}
