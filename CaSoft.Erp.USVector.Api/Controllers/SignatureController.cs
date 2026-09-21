using Microsoft.AspNetCore.Authorization;
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

        // Les octets de la signature, annoncés par le paquet terrain (ImageUrl) : tirés par la
        // facturation avec son jeton de service, et affichés par l'app avec le sien — D8. Anonyme
        // jusqu'au 19/09 faute de DEC-6. Le POST juste dessous reste réservé à l'app.
        [Authorize(Policy = ClKeycloakCallers.ServiceOrMobilePolicy)]
        [HttpGet("{gJobId}")]
        public ActionResult<ClSignatureGetModel> GetSignature(Guid gJobId, [FromServices] ISignatureRepository getSignatureRepository)
        {
            ClGetSignatureUseCase GetSignatureUseCase = new ClGetSignatureUseCase(gJobId, getSignatureRepository);
            // Use case migré au Result pattern : consommé via le pont Result→ActionResult.
            return GetSignatureUseCase.Handle().ToActionResult();
        }

        /// <summary>Plafond d'un lot : ~42 Ko par image, soit ~2 Mo par réponse.</summary>
        public const int MaxSignaturesParLot = 50;

        /// <summary>Corps de <c>POST api/missions/signatures</c>.</summary>
        public sealed class SignatureBatchQuery
        {
            public List<Guid>? MissionIds { get; set; }
        }

        /// <summary>
        /// B5 — Signatures de plusieurs missions en un appel (19/09, demande de la facturation). Une
        /// entrée par mission demandée : <c>Found</c> avec l'image (même valeur que
        /// <c>GET api/Signature/{id}</c>), ou <c>NotFound</c>. Au-delà de
        /// <see cref="MaxSignaturesParLot"/>, l'appelant découpe.
        /// </summary>
        // Route absolue : sous api/Signature, « batch » concurrencerait POST {gJobId}.
        [Authorize(Policy = ClKeycloakCallers.ServiceOrMobilePolicy)]
        [HttpPost("~/api/missions/signatures")]
        public IActionResult GetMany([FromBody] SignatureBatchQuery query, [FromServices] ISignatureQueryService signatures)
        {
            var ids = query?.MissionIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();

            if (ids.Count > MaxSignaturesParLot)
                return BadRequest($"Trop de signatures demandées ({ids.Count}) : maximum {MaxSignaturesParLot} par appel.");

            return Ok(signatures.ReadMany(ids));
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
