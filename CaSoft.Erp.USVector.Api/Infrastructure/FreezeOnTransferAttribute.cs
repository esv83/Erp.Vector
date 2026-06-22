using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// TRF-7 — Gel terrain : interdit l'édition d'une mission déjà transférée en facturation.
/// À poser sur les actions d'édition mission-scoped ; <paramref name="routeKey"/> nomme le
/// paramètre de route portant l'identifiant de mission (par défaut <c>gJobId</c>, le contrat
/// legacy où « job » = mission). Renvoie 409 si la mission est Transférée (2) ou Facturée (3).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FreezeOnTransferAttribute : Attribute, IFilterFactory
{
    private readonly string _routeKey;

    public FreezeOnTransferAttribute(string routeKey = "gJobId") => _routeKey = routeKey;

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new MissionEditFreezeFilter(_routeKey, serviceProvider.GetRequiredService<IErpReadApiClient>());

    private sealed class MissionEditFreezeFilter : IAsyncActionFilter
    {
        // Statuts gelés : 2=Transféré, 3=Facturé.
        private const int Transferred = 2;
        private const int Billed = 3;

        private readonly string _routeKey;
        private readonly IErpReadApiClient _erp;

        public MissionEditFreezeFilter(string routeKey, IErpReadApiClient erp)
        {
            _routeKey = routeKey;
            _erp = erp;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.RouteData.Values.TryGetValue(_routeKey, out var raw)
                && Guid.TryParse(raw?.ToString(), out var missionId))
            {
                var status = await _erp.GetMissionTransferStatusAsync(missionId, context.HttpContext.RequestAborted);
                if (status is Transferred or Billed)
                {
                    context.Result = new ObjectResult(new
                    {
                        Message = "Mission transférée en facturation : édition close."
                    })
                    {
                        StatusCode = StatusCodes.Status409Conflict
                    };
                    return;
                }
            }

            await next();
        }
    }
}
