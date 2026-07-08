using Microsoft.AspNetCore.Mvc;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Contracts;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignatureController : Controller
    {
        // Le DbContext legacy (BD_REGULATION_PROD) injecté ici n'était pas utilisé :
        // toutes les actions passent par ISignatureRepository ([FromServices]).

        [HttpGet("{gJobId}")]
        public ActionResult<ClSignatureGetModel> GetSignature(Guid gJobId, [FromServices] ISignatureRepository getSignatureRepository)
        {
            if (!ClAutorizationCommand.AutorizeJob(Request, gJobId))
            {
                return BadRequest("Autorisation refusée");
            }

            ClGetSignatureUseCase GetSignatureUseCase = new ClGetSignatureUseCase(gJobId, getSignatureRepository);
            // Use case migré au Result pattern : consommé via le pont Result→ActionResult.
            return GetSignatureUseCase.Handle().ToActionResult();
        }

        // Enregistrement de la signature (verbe unique côté contrat : POST).
        // Idempotent (relation 1:1 mission) : si une signature existe déjà — re-signature
        // ou double envoi du front — on met à jour au lieu de renvoyer une 400 (violation
        // de clé primaire sur MOB_SIGNATURE).
        [HttpPost("{gJobId}")]
        [FreezeOnTransfer]
        public ActionResult PostSignature(Guid gJobId, ClSignatureGetModel signatureModel, [FromServices] ISignatureRepository repository)
        {
            try
            {
                if (repository.Exists(gJobId))
                    repository.Update(gJobId, signatureModel.Data);
                else
                    repository.Insert(gJobId, signatureModel.Data);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{gJobId}")]
        [FreezeOnTransfer]
        public ActionResult PatchSignature(Guid gJobId, ClSignatureGetModel signatureModel, [FromServices] ISignatureRepository repository)
        {
            //Update les infos de la mission
            try
            {
                repository.Update(gJobId, signatureModel.Data);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{gJobId}")]
        [FreezeOnTransfer]
        public ActionResult DeleteSignature(Guid gJobId, [FromServices] ISignatureRepository Repository)
        {
            try
            {
                // TODO legacy résolu en MOB-2 : suppression réellement câblée sur MOB_SIGNATURE.
                Repository.Delete(gJobId, string.Empty);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
