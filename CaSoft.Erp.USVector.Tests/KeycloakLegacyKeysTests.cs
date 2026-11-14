using CaSoft.Erp.USVector.Api.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Itération 11 — un serveur qui pose encore les anciennes clés (web.config, environnement) garde son
/// réglage : elles l'emportent, recopiées sous les noms du socle, au lieu d'un ClientId absent.
/// </summary>
public class KeycloakLegacyKeysTests
{
    private static ConfigurationManager Config(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(values);
        return configuration;
    }

    [Fact]
    public void Sans_ancienne_cle_rien_ne_change()
    {
        var configuration = Config(new() { ["Keycloak:ClientId"] = "us-ambulance" });

        KeycloakLegacyKeys.Apply(configuration).Should().BeEmpty();
        configuration["Keycloak:ClientId"].Should().Be("us-ambulance");
    }

    [Fact]
    public void L_ancienne_audience_devient_le_ClientId()
    {
        var configuration = Config(new() { ["Keycloak:ClientId"] = "us-ambulance", ["Keycloak:Audience"] = "autre-mobile" });

        KeycloakLegacyKeys.Apply(configuration).Should().ContainSingle();
        configuration["Keycloak:ClientId"].Should().Be("autre-mobile", "la clé posée à la main l'emporte");
    }

    [Fact]
    public void Une_ancienne_liste_remplace_la_nouvelle_en_entier()
    {
        var configuration = Config(new()
        {
            ["Keycloak:FrontClientIds:0"] = "us-regulation",
            ["Keycloak:FrontClientIds:1"] = "us-certification",
            ["Keycloak:ScreenAzp:0"] = "us-regulation"
        });

        KeycloakLegacyKeys.Apply(configuration);

        ClKeycloakCallers.ReadScreenAzp(configuration).Should().Equal("us-regulation");
    }
}
