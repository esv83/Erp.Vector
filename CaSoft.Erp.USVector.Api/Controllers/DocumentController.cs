using System.Linq;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// TRF-10 — Documents/photos terrain d'une mission (spec §14) : dépôt (multipart), liste des
    /// métadonnées, récupération du binaire. Stockage BD Mobile ; transférés dans le paquet field-data.
    /// </summary>
    [Route("api")]
    [ApiController]
    public class DocumentController : Controller
    {
        private readonly IDocumentRepository _repository;

        public DocumentController(IDocumentRepository repository) => _repository = repository;

        /// <summary>
        /// Corps multipart du dépôt de document. Regroupe les champs de formulaire dans un seul
        /// modèle : requis par SwaggerGen (un <see cref="IFormFile"/> ne peut pas être un paramètre
        /// <c>[FromForm]</c> à plat aux côtés d'autres champs). Binding insensible à la casse →
        /// les champs <c>file</c> / <c>category</c> restent compatibles avec le client.
        /// </summary>
        public sealed class UploadDocumentForm
        {
            public IFormFile? File { get; set; }
            public int Category { get; set; }
        }

        /// <summary>Dépose un document (champ de formulaire <c>file</c> + <c>category</c>). 409 si mission transférée.</summary>
        [HttpPost("missions/{gJobId:guid}/documents")]
        [FreezeOnTransfer("gJobId")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(
            Guid gJobId,
            [FromForm] UploadDocumentForm form,
            [FromQuery] Guid? crewId)
        {
            var file = form.File;
            if (file is null || file.Length == 0)
                return BadRequest("Fichier manquant.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var command = new ClUploadDocumentCommand(
                gJobId, ms.ToArray(), file.ContentType, file.FileName, form.Category, crewId);

            return new ClUseCaseHandler(new ClUploadDocumentUseCase(command, _repository)).Execute();
        }

        /// <summary>Métadonnées des documents de la mission (du plus récent au plus ancien).</summary>
        [HttpGet("missions/{gJobId:guid}/documents")]
        public IActionResult List(Guid gJobId)
            => Ok(_repository.ListByMission(gJobId).Select(d => d.ToDtoOut()));

        /// <summary>Octets d'un document (Content-Type d'origine).</summary>
        [HttpGet("documents/{documentId:guid}/content")]
        public IActionResult GetContent(Guid documentId)
        {
            var doc = _repository.GetById(documentId);
            if (doc?.Content is null || doc.Content.Length == 0)
                return NotFound();

            var contentType = string.IsNullOrWhiteSpace(doc.ContentType)
                ? "application/octet-stream"
                : doc.ContentType;
            return File(doc.Content, contentType);
        }
    }
}
