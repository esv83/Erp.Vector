using System.Security.Claims;
using CaSoft.Erp.USVector.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Itération 11 — la validation du socle, et ce que Vector y ajoute. Ce qui est protégé : le refus d'un
/// azp inconnu reste celui du socle, et son motif arrive aux contrôleurs (CrewAccess le rend en 401
/// lisible) ; un appelant déclaré passe ; le mode dégradé ne refuse rien.
/// </summary>
public class VectorJwtBearerEventsTests
{
    private const string Mobile = "us-ambulance";

    private static (JwtBearerOptions Options, IServiceProvider Services) Build(bool disableValidation)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://auth.example/realms/test",
            ["Keycloak:ClientId"] = Mobile,
            ["Keycloak:AllowedAzp:0"] = "erp-billinggateway-api"
        }).Build();

        var services = new ServiceCollection().AddLogging();
        CaSoft.Framework.Security.ClKeycloakAuthenticationExtensions.AddCaSoftKeycloakJwtBearer(
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme), configuration);
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
            options => VectorJwtBearerEvents.Apply(options, disableValidation));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        return (options, provider);
    }

    private static async Task<TokenValidatedContext> Validate(bool disableValidation, string azp)
    {
        var (options, services) = Build(disableValidation);
        var http = new DefaultHttpContext { RequestServices = services };
        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        var context = new TokenValidatedContext(http, scheme, options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("azp", azp), new Claim("sub", "x") }, "Bearer"))
        };
        await options.Events.OnTokenValidated(context);
        return context;
    }

    [Fact]
    public async Task Un_azp_inconnu_est_refuse_par_le_socle_et_le_motif_arrive_aux_controleurs()
    {
        var context = await Validate(disableValidation: false, azp: "un-autre-client");

        context.Result?.Failure.Should().NotBeNull();
        context.HttpContext.GetJwtError().Should().Contain("un-autre-client").And.Contain(Mobile);
    }

    [Theory]
    [InlineData(Mobile)]
    [InlineData("erp-billinggateway-api")]
    public async Task Un_appelant_declare_passe(string azp)
    {
        var context = await Validate(disableValidation: false, azp);

        context.Result.Should().BeNull();
        context.HttpContext.GetJwtError().Should().BeNull();
    }

    [Fact]
    public async Task Le_mode_degrade_ne_refuse_aucun_azp()
        => (await Validate(disableValidation: true, azp: "un-autre-client")).Result.Should().BeNull();

    [Fact]
    public void Le_socle_garde_les_claims_bruts_et_ne_controle_pas_l_audience()
    {
        var (options, _) = Build(disableValidation: false);

        options.MapInboundClaims.Should().BeFalse("GetKeycloakSubject lit « sub » tel quel");
        options.TokenValidationParameters.ValidateAudience.Should().BeFalse("Keycloak émet « aud: account »");
        options.TokenValidationParameters.ValidIssuer.Should().Be("https://auth.example/realms/test");
    }
}
