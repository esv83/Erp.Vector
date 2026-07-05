using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// P1 — Carte mutuelle d'un bénéficiaire : dépôt de la photo (multipart), métadonnées, image.
    /// Stockage BD Mobile ; champs extraits (OCR/IA) renseignés ultérieurement (P3).
    /// </summary>
    [Route("api")]
    [ApiController]
    public class MutuelleCardController : Controller
    {
        private readonly IMutuelleCardRepository _repository;

        public MutuelleCardController(IMutuelleCardRepository repository) => _repository = repository;

        /// <summary>
        /// Corps multipart du dépôt de carte mutuelle. L'<see cref="IFormFile"/> est porté par un
        /// modèle <c>[FromForm]</c> (SwaggerGen ne sait pas générer un <c>IFormFile</c> en paramètre
        /// <c>[FromForm]</c> à plat). Binding insensible à la casse → champ <c>file</c> compatible.
        /// </summary>
        public sealed class UploadMutuelleCardForm
        {
            public IFormFile? File { get; set; }
        }

        /// <summary>Dépose une photo de carte mutuelle (champ de formulaire <c>file</c>).</summary>
        [HttpPost("beneficiaries/{beneficiaryId:guid}/mutuelle-card")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(
            Guid beneficiaryId,
            [FromForm] UploadMutuelleCardForm form,
            [FromQuery] Guid? crewId,
            [FromQuery] Guid? missionId)
        {
            var file = form.File;
            if (file is null || file.Length == 0)
                return BadRequest("Fichier image manquant.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var command = new ClUploadMutuelleCardCommand(
                beneficiaryId, ms.ToArray(), file.ContentType, crewId, missionId);

            return new ClUploadMutuelleCardUseCase(command, _repository).Handle().ToActionResult();
        }

        /// <summary>Métadonnées de la carte courante du bénéficiaire (sans le binaire).</summary>
        [HttpGet("beneficiaries/{beneficiaryId:guid}/mutuelle-card")]
        public IActionResult GetCurrent(Guid beneficiaryId)
        {
            var card = _repository.GetCurrent(beneficiaryId);
            return card is null ? NotFound() : Ok(card.ToDtoOut());
        }

        /// <summary>
        /// Renseigne/corrige manuellement les champs mutuelle d'une carte (avant OCR, P2).
        /// Saisie humaine → statut <c>validated</c>.
        /// </summary>
        [HttpPatch("mutuelle-card/{cardId:guid}")]
        public IActionResult SetFields(Guid cardId, [FromBody] ClMutuelleFieldsDtoIn fields)
        {
            var command = new ClSetMutuelleFieldsCommand(cardId, fields);
            return new ClSetMutuelleFieldsUseCase(command, _repository).Handle().ToActionResult();
        }

        /// <summary>Octets de l'image d'une carte (Content-Type d'origine).</summary>
        [HttpGet("mutuelle-card/{cardId:guid}/image")]
        public IActionResult GetImage(Guid cardId)
        {
            var card = _repository.GetById(cardId);
            if (card?.Image is null || card.Image.Length == 0)
                return NotFound();

            var contentType = string.IsNullOrWhiteSpace(card.ContentType)
                ? "application/octet-stream"
                : card.ContentType;
            return File(card.Image, contentType);
        }
    }
}
