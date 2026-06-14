using Microsoft.AspNetCore.Mvc;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Api.Infrastructure;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobDetailController : Controller
    {
        [HttpGet("{gJobId}")]
        public IActionResult GetDetail(
            Guid gJobId,
            [FromServices] IJobCache jobCache,
            [FromServices] IMobileIdentityResolver identity)
        {
            // MOB-4a : autorisation depuis le token Keycloak (remplace AutorizeJob).
            var sub = User.GetKeycloakSubject();
            if (sub is null)
                return Unauthorized("Authentification Keycloak requise.");

            var personnelId = identity.ResolvePersonnelId(sub.Value);
            if (personnelId is null)
                return StatusCode(403, "Compte non rattaché à un personnel. Contactez la régulation.");

            // Le personnel ne voit que les missions de ses crews.
            if (!identity.IsMissionAccessible(personnelId.Value, gJobId))
                return StatusCode(403, "Mission hors de vos équipages.");

            ClGetJobUseCase useCase = new ClGetJobUseCase(gJobId, jobCache);
            return new ClUseCaseHandler(useCase).Execute();
        }
    }
}
