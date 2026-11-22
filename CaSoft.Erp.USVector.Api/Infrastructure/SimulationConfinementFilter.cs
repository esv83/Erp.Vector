using System.Collections;
using System.Reflection;
using CaSoft.Erp.USVector.Api.Controllers;
using CaSoft.Erp.USVector.Infrastructure.Simulation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// Cantonne une requête en mode formation au jeu de formation. Filtre global, appliqué à toutes les actions.
/// <para>
/// Pourquoi il faut : une quarantaine de routes ne vérifient pas que la mission appartient à l'équipage —
/// seule l'authentification les garde. Sans ce filtre, un persona (sans compte) lirait une vraie mission
/// dont il connaît l'identifiant, et un stagiaire pourrait signer une vraie mission par erreur.
/// </para>
/// <para>
/// Règle : tout identifiant reçu (route, requête, corps — un niveau de propriétés) doit être dans la plage
/// réservée. Exceptions : une carte ou un document, dont l'identifiant naît en base, sont admis s'ils
/// appartiennent à un patient ou une mission fictifs. La plateforme d'urgence (Sirus, GpsGate) est fermée :
/// un statut de formation n'a rien à y faire.
/// </para>
/// </summary>
public sealed class SimulationConfinementFilter : IAsyncActionFilter
{
    private static readonly Type[] ClosedControllers = { typeof(EmergencyPlatformController), typeof(DiagController) };

    private readonly SimulationOptions _options;

    public SimulationConfinementFilter(SimulationOptions options) => _options = options;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        if (SimulationRequest.HasTrainingHeader(http) && !_options.Enabled)
        {
            context.Result = new ObjectResult("Le mode formation n'est pas ouvert sur ce serveur.") { StatusCode = StatusCodes.Status409Conflict };
            return;
        }

        if (!SimulationRequest.IsSimulated(http, _options))
        {
            await next();
            return;
        }

        if (context.ActionDescriptor is ControllerActionDescriptor action && ClosedControllers.Contains(action.ControllerTypeInfo.AsType()))
        {
            context.Result = Refuse("Cette fonction n'est pas disponible en mode formation.");
            return;
        }

        foreach (var (name, value) in context.ActionArguments)
        {
            var refusal = await CheckAsync(name, value, http);
            if (refusal is not null)
            {
                context.Result = Refuse(refusal);
                return;
            }
        }

        await next();
    }

    private static async Task<string?> CheckAsync(string name, object? value, HttpContext http)
    {
        if (value is Guid id && id != Guid.Empty && !ClSimulationIds.IsSimulated(id))
        {
            // Carte et document naissent en base, hors de la plage : on regarde à qui ils appartiennent.
            var isOwned = name switch
            {
                "cardId" => await Store(http).IsSimulatedCardAsync(id, http.RequestAborted),
                "documentId" => await Store(http).IsSimulatedDocumentAsync(id, http.RequestAborted),
                _ => false
            };
            return isOwned ? null : OutOfScope(name, id);
        }

        var outsider = GuidsIn(value).FirstOrDefault(g => g != Guid.Empty && !ClSimulationIds.IsSimulated(g));
        return outsider == Guid.Empty ? null : OutOfScope(name, outsider);
    }

    /// <summary>Les identifiants d'un argument de corps : liste d'identifiants, ou propriétés d'un objet (un niveau).</summary>
    internal static IEnumerable<Guid> GuidsIn(object? value)
    {
        switch (value)
        {
            case null or string or Guid:
                yield break;
            case IEnumerable<Guid> ids:
                foreach (var g in ids) yield return g;
                yield break;
            case IEnumerable:
                yield break;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum) yield break;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
            {
                if (property.GetValue(value) is Guid g) yield return g;
            }
            else if (typeof(IEnumerable<Guid>).IsAssignableFrom(property.PropertyType) && property.GetValue(value) is IEnumerable<Guid> list)
            {
                foreach (var g in list) yield return g;
            }
        }
    }

    private static ClSimulationMobileStore Store(HttpContext http) => http.RequestServices.GetRequiredService<ClSimulationMobileStore>();

    private static string OutOfScope(string name, Guid id)
        => $"Mode formation : {name} {id} n'appartient pas au jeu de formation.";

    private static ObjectResult Refuse(string message) => new(message) { StatusCode = StatusCodes.Status403Forbidden };
}
