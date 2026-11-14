namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// Itération 11 — les clés Keycloak de Vector prennent les noms du socle : <c>Audience</c> →
/// <c>ClientId</c>, <c>ServiceAzp</c> → <c>AllowedAzp</c>, <c>ScreenAzp</c> → <c>FrontClientIds</c>.
/// <para>
/// <c>appsettings.json</c> porte les nouveaux noms. Une ancienne clé encore posée ailleurs (web.config,
/// variable d'environnement) <b>l'emporte</b> et est recopiée sous son nouveau nom : un serveur réglé à
/// la main garde son comportement, au lieu de refuser de démarrer sur un <c>ClientId</c> absent. Elle
/// est signalée au journal, pour être renommée puis retirée.
/// </para>
/// </summary>
public static class KeycloakLegacyKeys
{
    private static readonly (string Old, string New)[] Renamed =
    {
        ("Keycloak:Audience", "Keycloak:ClientId"),
        ("Keycloak:ServiceAzp", "Keycloak:AllowedAzp"),
        ("Keycloak:ScreenAzp", "Keycloak:FrontClientIds")
    };

    /// <summary>Recopie les anciennes clés présentes ; rend leurs noms, pour le journal.</summary>
    public static IReadOnlyList<string> Apply(IConfigurationManager configuration)
    {
        var copied = new Dictionary<string, string?>();
        var found = new List<string>();

        foreach (var (oldKey, newKey) in Renamed)
        {
            var section = configuration.GetSection(oldKey);
            if (section.Value is { } single)
            {
                copied[newKey] = single;
            }
            else if (section.GetChildren().Any())
            {
                // Une liste : on remplace toute la liste, pas seulement les indices présents.
                foreach (var child in configuration.GetSection(newKey).GetChildren())
                    copied[$"{newKey}:{child.Key}"] = null;
                foreach (var child in section.GetChildren())
                    copied[$"{newKey}:{child.Key}"] = child.Value;
            }
            else
            {
                continue;
            }
            found.Add($"{oldKey} → {newKey}");
        }

        if (copied.Count > 0) configuration.AddInMemoryCollection(copied);
        return found;
    }
}
