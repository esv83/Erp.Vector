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
            var card = _repository.GetCurrentMetadata(beneficiaryId);
            return card is null ? NotFound() : Ok(card.ToDtoOut());
        }

        /// <summary>Nombre maximal de bénéficiaires sondés en un appel.</summary>
        /// <remarks>
        /// La route est ouverte : sans plafond, un corps de cent mille identifiants ferait travailler
        /// la base pour rien, sans qu'aucun compte soit en jeu. 500 couvre très largement une page
        /// d'écran — au-delà, c'est l'appelant qui pagine, et il sait le faire.
        /// </remarks>
        public const int MaxBeneficiairesParLot = 500;

        /// <summary>Corps du sondage par lot.</summary>
        public sealed class MutuelleCardPresenceQuery
        {
            public List<Guid>? BeneficiaryIds { get; set; }
        }

        /// <summary>
        /// Pour une liste de bénéficiaires, ceux qui portent une carte — avec la date de la plus
        /// récente et l'URL de son image. Les autres sont <b>absents</b> de la réponse.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ⛔ <b>Ouvert, comme l'image qu'il annonce.</b> Un écran qui affiche une pastille « carte
        /// disponible » ne peut pas la conditionner à un appel authentifié quand l'image elle-même
        /// s'affiche par une balise <c>&lt;img&gt;</c> sans jeton : ce serait fermer la serrure en
        /// laissant la porte. Ce que la réponse divulgue est borné à *« ce bénéficiaire a une
        /// photo »* — ni nom de mutuelle, ni code AMC — et reste protégé par le même identifiant non
        /// énumérable que l'image. Se referme avec DEC-6, en même temps qu'elle.
        /// </para>
        /// <para>
        /// <b>POST et non GET</b> : une liste de plusieurs centaines de Guid dépasse la longueur d'URL
        /// admise par IIS. L'appel reste une lecture — il n'écrit rien et peut être rejoué.
        /// </para>
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("mutuelle-card/presence")]
        public IActionResult ListPresence([FromBody] MutuelleCardPresenceQuery query)
        {
            var ids = query?.BeneficiaryIds?.Where(id => id != Guid.Empty).ToList() ?? new List<Guid>();

            if (ids.Count > MaxBeneficiairesParLot)
                return BadRequest($"Trop de bénéficiaires demandés ({ids.Count}) : maximum {MaxBeneficiairesParLot} par appel.");

            // Lot vide : une liste vide, pas un 400. Un écran sans ligne à afficher n'est pas une
            // erreur d'appelant, et le lui dire l'obligerait à traiter un cas de plus.
            if (ids.Count == 0) return Ok(new List<ClMutuelleCardPresenceDtoOut>());

            var presences = _repository.ListPresence(ids)
                .Select(p => p.ToDtoOut())
                .ToList();

            return Ok(presences);
        }

        /// <summary>
        /// Octets de la carte <b>courante</b> du bénéficiaire — l'URL stable, qui suit les nouvelles
        /// captures. <c>404</c> s'il n'en a aucune.
        /// </summary>
        /// <remarks>
        /// ⛔ Ouverte pour la même raison que <see cref="GetImage"/>, et une de plus : les écrans
        /// d'Order et de la facturation l'affichent par une balise <c>&lt;img src&gt;</c>, qui ne
        /// portera **jamais** de jeton. ⚠️ Donnée de santé — se referme avec DEC-6, et ce jour-là ces
        /// écrans devront passer par un <c>fetch</c> authentifié.
        /// <para>
        /// Indexée par bénéficiaire et non par carte : l'appelant connaît son patient, pas l'identifiant
        /// d'une photo qu'il n'a pas prise. Un identifiant de carte l'obligerait à relire des
        /// métadonnées avant chaque affichage, et deviendrait périmé à la capture suivante.
        /// </para>
        /// </remarks>
        [AllowAnonymous]
        [HttpGet("beneficiaries/{beneficiaryId:guid}/mutuelle-card/image")]
        public IActionResult GetCurrentImage(Guid beneficiaryId)
            => ServirImage(_repository.GetCurrentImage(beneficiaryId));

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
            => ServirImage(_repository.GetImage(cardId));

        /// <summary>Octets et type d'origine, ou 404. Repli MIME sur un binaire sans type déclaré.</summary>
        private IActionResult ServirImage(ClMutuelleCardImage? image)
        {
            if (image?.Bytes is null || image.Bytes.Length == 0)
                return NotFound();

            var contentType = string.IsNullOrWhiteSpace(image.ContentType)
                ? "application/octet-stream"
                : image.ContentType;

            return File(image.Bytes, contentType);
        }
    }
}
