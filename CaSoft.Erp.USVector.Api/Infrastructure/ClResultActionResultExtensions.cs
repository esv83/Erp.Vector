using CaSoft.Framework;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// Pont Result → HTTP : mappe un <see cref="ClResult{T}"/> en <see cref="ActionResult"/>, à l'identique
/// de <c>ClWebApiPresenter</c> (parité stricte, non-régression) :
/// <list type="bullet">
///   <item>succès + valeur non nulle → <c>200</c> Ok(valeur)</item>
///   <item>succès + valeur nulle, ou erreur <c>NotFound</c> → <c>404</c></item>
///   <item>autre erreur → <c>400</c> BadRequest(message)</item>
/// </list>
/// Permet à un contrôleur de consommer directement un use case migré : <c>useCase.Handle().ToActionResult()</c>.
/// </summary>
public static class ClResultActionResultExtensions
{
    public static ActionResult ToActionResult<T>(this ClResult<T> result)
    {
        if (result.IsSucces)
            return result.Value is null ? new NotFoundResult() : new OkObjectResult(result.Value);

        if (result.InnerError is ClError { IsNotFound: true })
            return new NotFoundResult();

        return new BadRequestObjectResult(result.InnerError?.ErrorText ?? "Erreur inconnue.");
    }
}
