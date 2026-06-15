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

        /// <summary>Dépose une photo de carte mutuelle (champ de formulaire <c>file</c>).</summary>
        [HttpPost("beneficiaries/{beneficiaryId:guid}/mutuelle-card")]
        public async Task<IActionResult> Upload(
            Guid beneficiaryId,
            [FromForm] IFormFile file,
            [FromQuery] Guid? crewId,
            [FromQuery] Guid? missionId)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Fichier image manquant.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var command = new ClUploadMutuelleCardCommand(
                beneficiaryId, ms.ToArray(), file.ContentType, crewId, missionId);

            return new ClUseCaseHandler(new ClUploadMutuelleCardUseCase(command, _repository)).Execute();
        }

        /// <summary>Métadonnées de la carte courante du bénéficiaire (sans le binaire).</summary>
        [HttpGet("beneficiaries/{beneficiaryId:guid}/mutuelle-card")]
        public IActionResult GetCurrent(Guid beneficiaryId)
        {
            var card = _repository.GetCurrent(beneficiaryId);
            return card is null ? NotFound() : Ok(card.ToDtoOut());
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
