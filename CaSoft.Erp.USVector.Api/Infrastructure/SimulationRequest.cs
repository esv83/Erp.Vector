using CaSoft.Erp.USVector.Infrastructure.Simulation;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>Section <c>Simulation</c> de la configuration : le mode formation.</summary>
public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    /// <summary>
    /// Ouvre le mode formation : un ambulancier authentifié qui envoie <see cref="SimulationRequest.HeaderName"/>
    /// travaille sur un jeu de missions fictives. Coupé, l'en-tête est refusé (409) plutôt qu'ignoré — sinon
    /// l'ambulancier se croirait en formation sur ses vraies missions.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Développement de l'app seulement : des personas sans compte Keycloak (<c>api/simulation/personas</c>).
    /// Sans effet si <see cref="Enabled"/> est faux ; refusé au démarrage en Production.
    /// </summary>
    public bool AnonymousPersonas { get; set; }

    /// <summary>Heure locale à partir de laquelle la purge de la nuit efface les saisies des jours passés.</summary>
    public int PurgeHour { get; set; } = 2;

    public bool PersonasEnabled => Enabled && AnonymousPersonas;
}

/// <summary>Une requête est-elle en mode formation ?</summary>
public static class SimulationRequest
{
    /// <summary>En-tête posé par l'app sur chaque appel quand l'ambulancier a choisi le mode formation.</summary>
    public const string HeaderName = "X-Vector-Simulation";

    /// <summary>Claim posé sur le principal d'un persona (<see cref="VectorJwtBearerEvents"/>).</summary>
    public const string PersonaClaim = "vector_simulation";

    /// <summary>Préfixe du jeton d'un persona : <c>Bearer simulation-persona-{n}</c>.</summary>
    public const string PersonaTokenPrefix = "simulation-persona-";

    public static bool HasTrainingHeader(HttpContext? http)
        => http is not null && http.Request.Headers.TryGetValue(HeaderName, out var value)
           && (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

    /// <summary>L'ambulancier a demandé le mode formation, et le serveur l'ouvre.</summary>
    public static bool IsTrainingRequested(HttpContext? http, SimulationOptions options)
        => options.Enabled && HasTrainingHeader(http);

    public static bool IsPersona(HttpContext? http) => http?.User.FindFirst(PersonaClaim) is not null;

    /// <summary>Formation demandée ou persona : la requête est cantonnée au jeu de formation.</summary>
    public static bool IsSimulated(HttpContext? http, SimulationOptions options)
        => IsTrainingRequested(http, options) || IsPersona(http);

    public static bool IsSimulated(Guid id) => ClSimulationIds.IsSimulated(id);
}
