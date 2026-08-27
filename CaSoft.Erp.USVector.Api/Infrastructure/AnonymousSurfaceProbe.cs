using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// <b>DEC-6 · E1</b> — sonde de la surface anonyme : <i>qui appelle réellement</i> les routes que
/// l'API sert sans jeton (DEVPLAN_2 §6, étape E1).
/// <para>
/// <b>Pourquoi mesurer avant de fermer.</b> Le plan a fermé deux routes sur le motif « aucun
/// consommateur prouvé ». Ce motif a tenu <b>un jour</b> : le lendemain, l'écran de capture de la
/// carte mutuelle passait en production et deux navigateurs tiraient l'image. « Aucun consommateur »
/// est un constat <b>daté</b>, pas une propriété — d'où cette sonde, qui dit qui appelle
/// <b>au moment où l'on ferme</b>, et non à la date où le plan a été écrit.
/// </para>
/// <para>
/// <b>Ce qu'elle observe</b> : toute action <c>[AllowAnonymous]</c> <b>sauf</b> les ouvertures de
/// diagnostic (<see cref="HorsMesure"/>). La liste n'est pas recopiée ici : l'appartenance se lit
/// dans les métadonnées de l'endpoint, donc une route anonyme ajoutée demain est mesurée sans que
/// personne y pense. <c>AnonymousSurfaceProbeTests</c> vérifie que cette lecture rend exactement la
/// surface que <c>AnonymousSurfaceTests</c> déclare justifiée.
/// </para>
/// <para>
/// <b>Lecture seule</b> : la sonde ne lit ni le corps, ni la réponse, et ne peut pas changer l'issue
/// d'une requête. Elle journalise dans le <c>finally</c> pour qu'une exception en aval ne fasse pas
/// disparaître la ligne — un appelant qui provoque un 500 est précisément celui qu'on veut voir.
/// </para>
/// <para>
/// ⚠️ <b>À enregistrer après <c>UseAuthentication</c></b> : sans cela <c>azp</c> est toujours vide,
/// et la mesure ne saurait pas distinguer un appel nu d'un appel déjà porteur d'un jeton.
/// </para>
/// </summary>
public sealed class AnonymousSurfaceProbe
{
    /// <summary>
    /// Journal dédié, routé vers son propre fichier par <c>nlog.config</c> : la mesure se compte à
    /// la ligne, et n'a pas à être extraite du bruit applicatif.
    /// </summary>
    public const string LoggerName = "Vector.SurfaceAnonyme";

    /// <summary>
    /// Ouvertures de <b>diagnostic</b>, hors mesure. Elles ne se referment pas avec <c>DEC-6</c> :
    /// <c>WhoAmI</c> doit pouvoir répondre « pas de jeton » — c'est son rôle — et <c>DiagController</c>
    /// rend 404 hors dev/staging. Les mesurer noierait le signal sous les appels des développeurs.
    /// <para>
    /// Le suffixe <c>.*</c> désigne un contrôleur anonyme au complet, dans la même notation que
    /// <c>AnonymousSurfaceTests</c>.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> HorsMesure = new(StringComparer.Ordinal)
    {
        "AuthController.WhoAmI",
        "DiagController.*"
    };

    private readonly RequestDelegate _suivant;
    private readonly ILogger _journal;

    public AnonymousSurfaceProbe(RequestDelegate suivant, ILoggerFactory fabrique)
    {
        _suivant = suivant;
        _journal = fabrique.CreateLogger(LoggerName);
    }

    public async Task InvokeAsync(HttpContext contexte)
    {
        var action = ActionMesuree(contexte.GetEndpoint());

        try
        {
            await _suivant(contexte);
        }
        finally
        {
            if (action is not null)
            {
                Journaliser(contexte, action);
            }
        }
    }

    /// <summary>
    /// Cette action anonyme fait-elle partie de la mesure ? Public et sans dépendance au pipeline
    /// HTTP, pour que le test puisse poser la même question par réflexion sur les contrôleurs.
    /// </summary>
    public static bool EstMesuree(string controleur, string action)
        => !HorsMesure.Contains($"{controleur}.*")
           && !HorsMesure.Contains($"{controleur}.{action}");

    /// <summary>
    /// Nom <c>Contrôleur.Action</c> si l'endpoint est une ouverture mesurée, <c>null</c> sinon —
    /// donc pour toute route protégée, toute ouverture de diagnostic, et tout endpoint qui n'est pas
    /// une action MVC (Swagger, fichiers statiques).
    /// </summary>
    public static string? ActionMesuree(Endpoint? point)
    {
        if (point?.Metadata.GetMetadata<IAllowAnonymous>() is null)
        {
            return null;
        }

        if (point.Metadata.GetMetadata<ControllerActionDescriptor>() is not { } descripteur)
        {
            return null;
        }

        // Le nom du TYPE (« DocumentController »), pas le nom MVC (« Document ») : c'est la notation
        // d'AnonymousSurfaceTests, et les deux listes doivent pouvoir se comparer sans traduction.
        var controleur = descripteur.ControllerTypeInfo.Name;
        var action = descripteur.MethodInfo.Name;

        return EstMesuree(controleur, action) ? $"{controleur}.{action}" : null;
    }

    /// <summary>
    /// Une ligne, champs séparés et toujours dans le même ordre — un champ vide vaut <c>-</c> plutôt
    /// que rien, sans quoi les colonnes se décalent et le comptage devient un travail manuel.
    /// </summary>
    private void Journaliser(HttpContext contexte, string action)
    {
        var requete = contexte.Request;

        _journal.LogInformation(
            "{Action}|{Methode} {Chemin}|statut={Statut}|ip={Ip}|agent={Agent}|referer={Referer}|auth={Auth}|azp={Azp}",
            action,
            requete.Method,
            requete.Path.Value ?? "-",
            contexte.Response.StatusCode,
            // IIS in-process : l'IP vue ici est celle de l'appelant, pas celle d'un proxy. Si un
            // relais s'intercale un jour, c'est `X-Forwarded-For` qu'il faudra lire.
            contexte.Connection.RemoteIpAddress?.ToString() ?? "-",
            Entete(requete, "User-Agent"),
            Entete(requete, "Referer"),
            contexte.HasAuthorizationHeader() ? "present" : "absent",
            contexte.User.FindFirst("azp")?.Value ?? "-");
    }

    private static string Entete(HttpRequest requete, string nom)
    {
        var valeur = requete.Headers[nom].ToString();
        return string.IsNullOrWhiteSpace(valeur) ? "-" : valeur;
    }
}
