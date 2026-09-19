using System.Reflection;
using CaSoft.Erp.USVector.Api.Controllers;
using CaSoft.Erp.USVector.Api.Infrastructure;
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
    /// Les quatre routes que la facturation tire en serveur-à-serveur. <b>Anonymes jusqu'au
    /// 2026-09-19</b>, faute d'authentification de service (DEC-6) ; fermées le jour où la facturation
    /// a présenté son jeton. Elles admettent désormais le service <b>ou</b> l'app — pas l'anonyme.
    /// </summary>
    private static readonly string[] FermeesAvecDec6 =
    {
        "DocumentController.GetContent",      // octets d'un document (D8)
        "FieldDataController.Get",            // paquet terrain
        "MutuelleCardController.GetImage",    // ⚠️ donnée de santé (D8)
        "SignatureController.GetSignature"    // octets de la signature (D8)
    };

    /// <summary>
    /// Ouvertes pour les <b>écrans</b> d'Order et de la facturation, qui affichent la carte mutuelle
    /// du patient. Justification distincte de la précédente, et il faut la garder distincte : une
    /// balise <c>&lt;img src&gt;</c> ne portera <b>jamais</b> de jeton, donc ces deux-là ne se
    /// referment pas en donnant un compte de service à un module — elles se referment le jour où ces
    /// écrans passent à un <c>fetch</c> authentifié (DEVPLAN_2, 🔴 H1).
    /// <para>
    /// Ce qu'elles divulguent est borné : les octets d'une photo, et « ce bénéficiaire en a une ».
    /// Ni nom de mutuelle, ni code AMC — le sondage de présence les tait délibérément.
    /// </para>
    /// </summary>
    private static readonly string[] OuverturesPourLesEcransAmont =
    {
        "MutuelleCardController.GetCurrentImage",  // ⚠️ donnée de santé — <img src> côté Order
        "MutuelleCardController.ListPresence"      // présence seule, pour une liste de bénéficiaires
    };

    /// <summary>
    /// Outils de diagnostic : l'un doit pouvoir <i>dire</i> pourquoi l'authentification échoue,
    /// l'autre est déjà fermé par configuration.
    /// </summary>
    private static readonly string[] OuverturesDeDiagnostic =
    {
        "AuthController.WhoAmI",              // doit pouvoir répondre « pas de jeton » — c'est son rôle
        "DiagController.*"                    // 404 hors dev/staging (Diagnostics:Enabled)
    };

    private static IEnumerable<string> SurfaceAttendue
        => OuverturesPourLesEcransAmont.Concat(OuverturesDeDiagnostic);

    [Fact]
    public void La_surface_anonyme_est_exactement_celle_qui_est_justifiee()
    {
        SurfaceAnonyme().Should().BeEquivalentTo(SurfaceAttendue,
            "toute route anonyme supplémentaire expose de la donnée à qui connaît un identifiant ; "
            + "l'ajouter doit être un choix argumenté, pas un effet de bord");
    }

    /// <summary>
    /// Fermées ne suffit pas : sans leur politique, ces routes retomberaient sur la politique de repli,
    /// qui n'admet que l'app — et la facturation recevrait des 403. Chacune doit porter
    /// <see cref="ClKeycloakCallers.ServiceOrMobilePolicy"/>, et rien d'anonyme.
    /// </summary>
    [Fact]
    public void Les_routes_de_la_facturation_exigent_un_jeton_de_service_ou_mobile()
    {
        foreach (var route in FermeesAvecDec6)
        {
            var (controleur, action) = (route.Split('.')[0], route.Split('.')[1]);
            var methode = Controleurs().Single(c => c.Name == controleur)
                .GetMethod(action, BindingFlags.Public | BindingFlags.Instance)!;

            methode.GetCustomAttribute<AllowAnonymousAttribute>().Should().BeNull(route);
            methode.GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy)
                .Should().Contain(ClKeycloakCallers.ServiceOrMobilePolicy, route);
        }
    }

    /// <summary>
    /// Les ouvertures pour les écrans amont sont **comptées à part**, et doivent le rester : les
    /// confondre avec les précédentes ferait croire qu'un compte de service les refermera, alors
    /// qu'elles attendent une décision de front (H1). Deux, pas plus — la carte mutuelle et rien
    /// d'autre.
    /// </summary>
    [Fact]
    public void Les_ouvertures_pour_les_ecrans_amont_sont_au_nombre_de_deux()
    {
        SurfaceAnonyme().Intersect(OuverturesPourLesEcransAmont)
            .Should().HaveCount(2, "seule la carte mutuelle est consultable depuis les écrans amont");
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
