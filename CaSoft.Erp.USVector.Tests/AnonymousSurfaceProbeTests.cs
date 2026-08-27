using System.Net;
using System.Reflection;
using CaSoft.Erp.USVector.Api.Controllers;
using CaSoft.Erp.USVector.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// <b>DEC-6 · E1</b> — la sonde mesure-t-elle ce qu'on croit qu'elle mesure ?
///
/// <para><b>Ce que ces tests protègent.</b> La sonde décide seule de ce qu'elle observe, en lisant
/// les métadonnées de l'endpoint. C'est ce qui la rend automatique — une route anonyme ajoutée
/// demain est mesurée sans que personne y pense — et c'est aussi ce qui la rend silencieuse quand
/// elle se trompe : une sonde qui n'observe rien produit un fichier vide, et un fichier vide se lit
/// exactement comme « personne n'appelle ». C'est la conclusion que E3 utilise pour fermer une
/// route.</para>
///
/// <para>D'où le premier test, qui est le seul qui compte vraiment : la surface observée est
/// <b>exactement</b> celle que <see cref="AnonymousSurfaceTests"/> déclare justifiée, diagnostic
/// exclu. Les deux listes sont bâties par le même chemin de réflexion, donc elles ne peuvent pas
/// diverger en silence.</para>
/// </summary>
public class AnonymousSurfaceProbeTests
{
    /// <summary>
    /// Les six ouvertures que la mesure doit couvrir : les quatre qui ne subsistent que faute de
    /// <c>DEC-6</c>, et les deux ouvertes <b>par décision</b> pour les écrans amont — celles-là ne se
    /// referment pas avec ce chantier, mais on veut savoir qui les tire, précisément parce que la
    /// dernière fois la réponse a changé en un jour.
    /// </summary>
    private static readonly string[] SurfaceMesuree =
    {
        "DocumentController.GetContent",
        "FieldDataController.Get",
        "MutuelleCardController.GetCurrentImage",
        "MutuelleCardController.GetImage",
        "MutuelleCardController.ListPresence",
        "SignatureController.GetSignature"
    };

    [Fact]
    public void La_sonde_couvre_exactement_la_surface_anonyme_hors_diagnostic()
    {
        OuverturesMesurees().Should().BeEquivalentTo(SurfaceMesuree,
            "une ouverture non mesurée produit un fichier vide, et un fichier vide se lit comme "
            + "« personne n'appelle » — c'est sur ce constat que E3 ferme une route");
    }

    /// <summary>
    /// `WhoAmI` doit pouvoir répondre « pas de jeton » : c'est son rôle, et le diagnostic est
    /// appelé par les développeurs. Les mesurer noierait le signal sous notre propre trafic.
    /// </summary>
    [Fact]
    public void Les_ouvertures_de_diagnostic_ne_sont_pas_mesurees()
    {
        AnonymousSurfaceProbe.ActionMesuree(Point<AuthController>(nameof(AuthController.WhoAmI)))
            .Should().BeNull();

        AnonymousSurfaceProbe.ActionMesuree(Point<DiagController>(nameof(DiagController.Page)))
            .Should().BeNull();
    }

    [Fact]
    public void Une_ouverture_mesuree_est_nommee_controleur_point_action()
    {
        AnonymousSurfaceProbe.ActionMesuree(Point<FieldDataController>(nameof(FieldDataController.Get)))
            .Should().Be("FieldDataController.Get");
    }

    /// <summary>
    /// La sonde n'a pas à journaliser le trafic authentifié : ce serait une ligne par requête
    /// mobile, et la mesure ne se lirait plus.
    /// </summary>
    [Fact]
    public void Une_route_protegee_n_est_pas_mesuree()
    {
        AnonymousSurfaceProbe.ActionMesuree(Point<FieldDataController>(nameof(FieldDataController.Get), anonyme: false))
            .Should().BeNull();
    }

    /// <summary>
    /// Swagger et les fichiers statiques sont anonymes et ne sont pas des actions MVC : sans ce
    /// filtre, le fichier de mesure porterait une ligne par ouverture de la page Swagger.
    /// </summary>
    [Fact]
    public void Un_endpoint_qui_n_est_pas_une_action_mvc_n_est_pas_mesure()
    {
        var swagger = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowAnonymousAttribute()),
            "swagger");

        AnonymousSurfaceProbe.ActionMesuree(swagger).Should().BeNull();
    }

    [Fact]
    public void Aucun_endpoint_n_est_mesure_hors_route()
    {
        AnonymousSurfaceProbe.ActionMesuree(null).Should().BeNull();
    }

    // ── La sonde écrit-elle vraiment ? ──────────────────────────────────────────

    /// <summary>
    /// Le filtre peut être juste et la ligne ne jamais partir. Comme un fichier vide se lit comme
    /// « personne n'appelle », on vérifie le geste, pas seulement la décision.
    /// </summary>
    [Fact]
    public async Task Une_requete_sur_une_ouverture_mesuree_produit_une_ligne()
    {
        var journal = new JournalDeTest();
        var sonde = new AnonymousSurfaceProbe(_ => Task.CompletedTask, journal);

        var contexte = new DefaultHttpContext();
        contexte.SetEndpoint(Point<FieldDataController>(nameof(FieldDataController.Get)));
        contexte.Request.Method = "GET";
        contexte.Request.Path = "/api/missions/8f0d1e2c-0000-0000-0000-000000000001/field-data";
        contexte.Request.Headers["User-Agent"] = "BillingGateway/1.0";
        contexte.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.50");

        await sonde.InvokeAsync(contexte);

        journal.Lignes.Should().ContainSingle().Which.Should()
            .Contain("FieldDataController.Get")
            .And.Contain("192.168.1.50")
            .And.Contain("BillingGateway/1.0")
            .And.Contain("auth=absent");
    }

    /// <summary>
    /// Le nom du journal n'est pas cosmétique : c'est lui que la règle <c>nlog.config</c> route vers
    /// le fichier de mesure. Le changer sans toucher la config enverrait la sonde dans le journal
    /// applicatif, où elle serait illisible.
    /// </summary>
    [Fact]
    public async Task La_ligne_part_dans_le_journal_dedie()
    {
        var journal = new JournalDeTest();
        var sonde = new AnonymousSurfaceProbe(_ => Task.CompletedTask, journal);

        var contexte = new DefaultHttpContext();
        contexte.SetEndpoint(Point<SignatureController>(nameof(SignatureController.GetSignature)));

        await sonde.InvokeAsync(contexte);

        journal.Categorie.Should().Be("Vector.SurfaceAnonyme")
            .And.Be(AnonymousSurfaceProbe.LoggerName);
    }

    /// <summary>
    /// Un appelant qui provoque une erreur est précisément celui qu'on veut voir : la ligne est
    /// écrite dans le <c>finally</c>, donc elle survit à une exception en aval.
    /// </summary>
    [Fact]
    public async Task La_ligne_survit_a_une_exception_en_aval()
    {
        var journal = new JournalDeTest();
        var sonde = new AnonymousSurfaceProbe(
            _ => throw new InvalidOperationException("panne en aval"), journal);

        var contexte = new DefaultHttpContext();
        contexte.SetEndpoint(Point<DocumentController>(nameof(DocumentController.GetContent)));

        var appel = async () => await sonde.InvokeAsync(contexte);

        await appel.Should().ThrowAsync<InvalidOperationException>();
        journal.Lignes.Should().ContainSingle().Which.Should().Contain("DocumentController.GetContent");
    }

    [Fact]
    public async Task Une_requete_protegee_ne_produit_aucune_ligne()
    {
        var journal = new JournalDeTest();
        var sonde = new AnonymousSurfaceProbe(_ => Task.CompletedTask, journal);

        var contexte = new DefaultHttpContext();
        contexte.SetEndpoint(Point<FieldDataController>(nameof(FieldDataController.Get), anonyme: false));

        await sonde.InvokeAsync(contexte);

        journal.Lignes.Should().BeEmpty();
    }

    // ── Réflexion et harnais ────────────────────────────────────────────────────

    /// <summary>
    /// Même chemin de réflexion que <see cref="AnonymousSurfaceTests"/> — contrôleurs de
    /// l'assemblage, actions anonymes — puis la question posée à la sonde elle-même.
    /// </summary>
    private static List<string> OuverturesMesurees()
    {
        var mesurees = new List<string>();

        var controleurs = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var controleur in controleurs)
        {
            var classeAnonyme = controleur.GetCustomAttribute<AllowAnonymousAttribute>() is not null;

            var actions = controleur
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName
                            && (classeAnonyme || m.GetCustomAttribute<AllowAnonymousAttribute>() is not null));

            mesurees.AddRange(actions
                .Where(m => AnonymousSurfaceProbe.EstMesuree(controleur.Name, m.Name))
                .Select(m => $"{controleur.Name}.{m.Name}"));
        }

        return mesurees.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    private static Endpoint Point<T>(string action, bool anonyme = true) where T : ControllerBase
    {
        var descripteur = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(T).GetTypeInfo(),
            MethodInfo = typeof(T).GetMethod(action, BindingFlags.Public | BindingFlags.Instance)!,
            ControllerName = typeof(T).Name,
            ActionName = action
        };

        var metadonnees = anonyme
            ? new EndpointMetadataCollection(descripteur, new AllowAnonymousAttribute())
            : new EndpointMetadataCollection(descripteur);

        return new Endpoint(_ => Task.CompletedTask, metadonnees, $"{typeof(T).Name}.{action}");
    }

    /// <summary>Capture ce que la sonde écrit, et sous quel nom de journal.</summary>
    private sealed class JournalDeTest : ILoggerFactory, ILogger
    {
        public List<string> Lignes { get; } = new();

        public string? Categorie { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            Categorie = categoryName;
            return this;
        }

        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex,
                                Func<TState, Exception?, string> formatter)
            => Lignes.Add(formatter(state, ex));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }
    }
}
