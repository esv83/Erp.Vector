using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IMissionBeneficiaryQueryService _beneficiaries;

        public MutuelleCardController(IMutuelleCardRepository repository, IMissionBeneficiaryQueryService beneficiaries)
        {
            _repository = repository;
            _beneficiaries = beneficiaries;
        }

        /// <summary>
        /// Dépose une photo de carte mutuelle depuis une mission (champ de formulaire <c>file</c>).
        /// Le patient est résolu côté serveur (mission → commande → bénéficiaire), la mission tracée
        /// d'office. 404 si la mission est introuvable ou sans patient.
        /// </summary>
        /// <remarks>
        /// Route à utiliser par l'app : elle n'a jamais reçu l'identifiant du patient, ce qui rendait
        /// la route par bénéficiaire inatteignable (aucune carte capturée en production au 27/08/2026).
        /// </remarks>
        [HttpPost("missions/{missionId:guid}/mutuelle-card")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadForMission(
            Guid missionId,
            [FromForm] UploadMutuelleCardForm form,
            [FromQuery] Guid? crewId,
            CancellationToken ct)
        {
            var file = form.File;
            if (file is null || file.Length == 0)
                return BadRequest("Fichier image manquant.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var command = new ClUploadMissionMutuelleCardCommand(missionId, ms.ToArray(), file.ContentType, crewId);
            var result = await new ClUploadMissionMutuelleCardUseCase(command, _beneficiaries, _repository).HandleAsync(ct);
            return result.ToActionResult();
        }

        /// <summary>
        /// Carte courante du patient de la mission (sans le binaire). 404 si la mission n'a pas de
        /// patient ou si le patient n'a encore aucune carte — cas normal au premier transport.
        /// </summary>
        [HttpGet("missions/{missionId:guid}/mutuelle-card")]
        public async Task<IActionResult> GetCurrentForMission(Guid missionId, CancellationToken ct)
        {
            var result = await new ClGetMissionMutuelleCardUseCase(missionId, _beneficiaries, _repository).HandleAsync(ct);
            return result.ToActionResult();
        }

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
        // ⛔ Octets de la carte mutuelle, annoncés par le paquet terrain (ImageUrl) et tirés par la
        // facturation sans jeton — D8. ⚠️ Donnée de santé : c'est l'ouverture la plus sensible des
        // quatre, et la première à refermer avec DEC-6 (§3.C2).
        [AllowAnonymous]
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
