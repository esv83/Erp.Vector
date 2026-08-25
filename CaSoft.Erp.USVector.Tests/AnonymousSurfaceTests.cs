using System.Reflection;
using CaSoft.Erp.USVector.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Fige la <b>surface anonyme</b> de l'API : la liste exacte de ce qui répond sans jeton.
///
/// <para><b>Pourquoi ce test existe.</b> Jusqu'au 2026-08-25, aucun contrôleur ne portait d'attribut
/// d'autorisation et il n'existait aucune politique globale : seuls les cinq endpoints passant par
/// <c>CrewAccess</c> étaient protégés, par leur code. Tout le reste répondait 200 à qui connaissait
/// un identifiant de mission — y compris la structure du formulaire, <b>valeurs comprises</b> : on a
/// relevé une date de naissance de patient servie sans authentification.</para>
///
/// <para>Personne n'avait ouvert ces routes : elles n'avaient jamais été fermées. C'est précisément
/// ce qu'un test doit empêcher de se reproduire — une exception doit être une <b>décision</b>, pas un
/// oubli. Depuis, la politique de repli protège par défaut ; ce test épingle les seules sorties.</para>
///
/// <para>⚠️ <b>S'il échoue, ne l'ajustez pas par réflexe.</b> Une entrée en plus dans la liste, c'est
/// une donnée de plus lisible par quiconque connaît un identifiant. Justifiez-la ici, ou retirez
/// l'<c>[AllowAnonymous]</c>.</para>
/// </summary>
public class AnonymousSurfaceTests
{
    /// <summary>
    /// Les quatre premières sont tirées par la facturation en serveur-à-serveur, sans jeton, faute
    /// d'authentification de service (DEC-6, devplan §3.C2) — c'est leur unique justification, et
    /// elle est temporaire. Les deux dernières sont des outils de diagnostic : l'une doit pouvoir
    /// <i>dire</i> pourquoi l'authentification échoue, l'autre est déjà fermée par configuration.
    /// </summary>
    private static readonly string[] SurfaceAttendue =
    {
        "AuthController.WhoAmI",              // doit pouvoir répondre « pas de jeton » — c'est son rôle
        "DiagController.*",                   // 404 hors dev/staging (Diagnostics:Enabled)
        "DocumentController.GetContent",      // octets d'un document — tirés par la facturation (D8)
        "FieldDataController.Get",            // paquet terrain — tiré par la facturation
        "MutuelleCardController.GetImage",    // ⚠️ donnée de santé — tirée par la facturation (D8)
        "SignatureController.GetSignature"    // octets de la signature — tirés par la facturation (D8)
    };

    [Fact]
    public void La_surface_anonyme_est_exactement_celle_qui_est_justifiee()
    {
        SurfaceAnonyme().Should().BeEquivalentTo(SurfaceAttendue,
            "toute route anonyme supplémentaire expose de la donnée à qui connaît un identifiant ; "
            + "l'ajouter doit être un choix argumenté, pas un effet de bord");
    }

    /// <summary>
    /// Le paquet terrain et les trois routes d'octets se referment ensemble, le jour où Vector saura
    /// présenter un jeton de service. Ce test le rappellera : quand DEC-6 sera fait, ces quatre
    /// entrées doivent disparaître de la liste.
    /// </summary>
    [Fact]
    public void Les_ouvertures_pour_la_facturation_sont_au_nombre_de_quatre()
    {
        SurfaceAnonyme()
            .Where(a => a.StartsWith("Document") || a.StartsWith("FieldData")
                     || a.StartsWith("MutuelleCard") || a.StartsWith("Signature"))
            .Should().HaveCount(4, "elles ne subsistent que faute d'authentification de service (DEC-6)");
    }

    /// <summary>
    /// Garde-fou de lecture : si l'assemblage n'expose aucun contrôleur, les deux tests ci-dessus
    /// passeraient en ne vérifiant rien.
    /// </summary>
    [Fact]
    public void Les_controleurs_sont_bien_visibles_par_reflexion()
    {
        Controleurs().Should().HaveCountGreaterThan(10);
    }

    // ── Réflexion ───────────────────────────────────────────────────────────────

    private static IEnumerable<Type> Controleurs()
        => typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    private static List<string> SurfaceAnonyme()
    {
        var surface = new List<string>();

        foreach (var controleur in Controleurs())
        {
            // Anonyme au niveau de la classe : toutes ses actions le sont, on ne détaille pas —
            // sinon la liste attendue changerait au moindre ajout d'action sur un outil de dev.
            if (controleur.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            {
                surface.Add($"{controleur.Name}.*");
                continue;
            }

            surface.AddRange(controleur
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName
                            && m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                .Select(m => $"{controleur.Name}.{m.Name}"));
        }

        return surface.OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
