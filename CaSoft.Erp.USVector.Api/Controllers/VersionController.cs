using System.Data.Common;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Schema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers;

/// <summary>
/// G8 — « Quel code tourne, et sur quelle base ? », en une requête.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ce que ça répare.</b> Quatre publications incohérentes entre le 25/08 et le 15/09, et un
/// correctif joué sur la mauvaise base Orders sans une erreur : à chaque fois, la réponse existait
/// quelque part — un `.pdb` à lire sur un partage, un fichier de configuration sur le serveur — mais
/// jamais à portée de question. Deux routes, deux publics.
/// </para>
/// <para>
/// <b>Le partage des deux.</b> <c>GET api/version</c> est **anonyme** et ne dit que ce qu'un binaire
/// assume de lui-même : version, commit, état de l'arbre, environnement. C'est la convention du parc
/// (Orders sert le sien de la même façon), et c'est ce qui permet de constater une publication depuis
/// n'importe où, sans jeton. <c>GET api/version/runtime</c> **exige un jeton** : il nomme la base et
/// les réglages en vigueur, qui n'ont rien à faire sur l'internet public.
/// </para>
/// </remarks>
[ApiController]
[Route("api/version")]
public class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;

    public VersionController(IWebHostEnvironment env, IConfiguration cfg)
    {
        _env = env;
        _cfg = cfg;
    }

    /// <summary>Version, commit, état de l'arbre au build, environnement.</summary>
    // ⛔ Anonyme, et c'est délibéré : une publication doit pouvoir se constater sans jeton — c'est
    // précisément quand quelque chose cloche qu'on n'en a pas sous la main. Ne divulgue ni donnée
    // métier, ni réglage : un identifiant de commit d'un dépôt privé, et rien d'autre.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        Service = BuildInfo.Service,
        BuildInfo.Version,
        BuildInfo.Commit,
        BuildInfo.ShortCommit,
        BuildInfo.Tree,
        BuildInfo.BuildUtc,
        Environment = _env.EnvironmentName,
        TimestampUtc = DateTimeOffset.UtcNow
    });

    /// <summary>La même chose, plus la base résolue et les drapeaux en vigueur.</summary>
    /// <remarks>
    /// Aucun secret ne sort : de la chaîne de connexion, on ne rend que le <b>serveur</b> et le
    /// <b>nom de la base</b> — jamais l'utilisateur ni le mot de passe —, et des réglages on ne rend
    /// que l'état (posé / absent), jamais la valeur.
    /// </remarks>
    [Authorize(Policy = ClKeycloakCallers.ServiceOrMobilePolicy)]
    [HttpGet("runtime")]
    public async Task<IActionResult> GetRuntime([FromServices] SchemaJournal schema, CancellationToken ct)
    {
        var (server, database) = ResolveDatabase(_cfg.GetConnectionString("MobileDb"));
        // G4 — l'état du schéma se relit à chaque appel : entre le démarrage et maintenant, un script
        // a pu être joué. Le message ne porte jamais le détail SQL (il nomme le compte de connexion).
        var etat = await schema.ReadAsync(ct);

        return Ok(new
        {
            Service = BuildInfo.Service,
            BuildInfo.Version,
            BuildInfo.Commit,
            BuildInfo.ShortCommit,
            BuildInfo.Tree,
            BuildInfo.BuildUtc,
            Environment = _env.EnvironmentName,
            TimestampUtc = DateTimeOffset.UtcNow,
            Database = new
            {
                Server = server,
                Name = database,
                Schema = new
                {
                    etat.UpToDate,
                    Statement = etat.Statement(),
                    LastApplied = etat.LastApplied,
                    Applied = etat.Applied.Select(a => new { a.ScriptId, a.AppliedAt, a.Origin }),
                    Missing = etat.Missing,
                    Unknown = etat.Unknown
                }
            },
            Flags = new
            {
                KeycloakEnabled = _cfg.GetValue("Keycloak:Enabled", false),
                KeycloakAuthority = _cfg["Keycloak:Authority"],
                MobileAzp = _cfg["Keycloak:Audience"],
                ServiceAzp = ClKeycloakCallers.ReadServiceAzp(_cfg),
                DisableValidation = _cfg.GetValue("Keycloak:DisableValidation", false),
                DiagnosticsEnabled = _cfg.GetValue("Diagnostics:Enabled", false),
                OrdersApi = _cfg["OrdersApi:BaseUrl"],
                // L'état, pas le secret : c'est ce qui manquait le 13/09, quand deux variables du
                // web.config écrites sans leur chevron ouvrant laissaient le compte de service inerte
                // sans que rien ne le signale.
                ServiceAccountConfigured = !string.IsNullOrWhiteSpace(_cfg["OrdersApi:ServiceAccount:ClientId"])
                    && !string.IsNullOrWhiteSpace(_cfg["OrdersApi:ServiceAccount:ClientSecret"])
                    && _cfg["OrdersApi:ServiceAccount:ClientSecret"] != "__SET_VIA_ENV__"
            }
        });
    }

    /// <summary>
    /// Serveur et nom de base d'une chaîne de connexion, <b>sans jamais rendre d'identifiants</b>.
    /// Une chaîne illisible rend deux valeurs nulles : dire « je ne sais pas » vaut mieux que lever.
    /// </summary>
    internal static (string? Server, string? Database) ResolveDatabase(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return (null, null);

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            return (Value(builder, "Server", "Data Source", "Addr", "Address"),
                    Value(builder, "Database", "Initial Catalog"));
        }
        catch (ArgumentException)
        {
            return (null, null);
        }
    }

    private static string? Value(DbConnectionStringBuilder builder, params string[] keys)
        => keys.Where(builder.ContainsKey)
            .Select(k => builder[k]?.ToString())
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
