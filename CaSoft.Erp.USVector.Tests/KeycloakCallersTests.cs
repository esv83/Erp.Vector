using System.Security.Claims;
using CaSoft.Erp.USVector.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// C2 — Un jeton de la facturation est accepté à l'authentification, mais n'ouvre que les routes
/// serveur-à-serveur : jamais les routes du terrain.
/// </summary>
public class KeycloakCallersTests
{
    private const string Mobile = "us-ambulance";
    private const string Facturation = "erp-billinggateway-api";
    private static readonly string[] Services = { Facturation };

    private static ClaimsPrincipal Caller(string? azp)
        => azp is null
            ? new ClaimsPrincipal(new ClaimsIdentity())   // non authentifié
            : new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClKeycloakCallers.AzpClaim, azp), new Claim("sub", Guid.NewGuid().ToString()) },
                authenticationType: "Bearer"));

    private static async Task<bool> Allowed(ClaimsPrincipal user, AuthorizationPolicy policy)
    {
        var services = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var result = await services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, null, policy);
        return result.Succeeded;
    }

    [Theory]
    [InlineData(Mobile, true)]
    [InlineData(Facturation, true)]
    [InlineData("un-autre-client", false)]
    [InlineData(null, false)]
    public void L_authentification_accepte_le_mobile_et_les_services_declares(string? azp, bool accepted)
        => ClKeycloakCallers.IsAccepted(azp, Mobile, Services).Should().Be(accepted);

    [Fact]
    public void Sans_service_declare_seul_le_mobile_est_accepte()
        => ClKeycloakCallers.IsAccepted(Facturation, Mobile, Array.Empty<string>()).Should().BeFalse();

    [Fact]
    public async Task Le_jeton_de_la_facturation_n_ouvre_pas_les_routes_du_terrain()
        => (await Allowed(Caller(Facturation), ClKeycloakCallers.MobileOnly(Mobile))).Should().BeFalse();

    [Fact]
    public async Task Le_jeton_mobile_ouvre_les_routes_du_terrain()
        => (await Allowed(Caller(Mobile), ClKeycloakCallers.MobileOnly(Mobile))).Should().BeTrue();

    [Fact]
    public async Task Sans_jeton_rien_ne_s_ouvre()
    {
        (await Allowed(Caller(null), ClKeycloakCallers.MobileOnly(Mobile))).Should().BeFalse();
        (await Allowed(Caller(null), ClKeycloakCallers.ServiceOrMobile(Mobile, Services))).Should().BeFalse();
    }

    [Theory]
    [InlineData(Facturation, true)]
    [InlineData(Mobile, true)]
    [InlineData("un-autre-client", false)]
    public async Task Les_routes_serveur_a_serveur_acceptent_le_service_ou_le_mobile(string azp, bool allowed)
        => (await Allowed(Caller(azp), ClKeycloakCallers.ServiceOrMobile(Mobile, Services))).Should().Be(allowed);

    [Fact]
    public void La_liste_des_services_ignore_les_vides_et_les_espaces()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:ServiceAzp:0"] = " erp-billinggateway-api ",
                ["Keycloak:ServiceAzp:1"] = "",
                ["Keycloak:ServiceAzp:2"] = "erp-billinggateway-api"
            })
            .Build();

        ClKeycloakCallers.ReadServiceAzp(configuration).Should().Equal(Facturation);
    }

    [Fact]
    public void Sans_section_aucun_service()
        => ClKeycloakCallers.ReadServiceAzp(new ConfigurationBuilder().Build()).Should().BeEmpty();
}
