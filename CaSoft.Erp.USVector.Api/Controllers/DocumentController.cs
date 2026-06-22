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

        /// <summary>Dépose un document (champ de formulaire <c>file</c> + <c>category</c>). 409 si mission transférée.</summary>
        [HttpPost("missions/{gJobId:guid}/documents")]
        [FreezeOnTransfer("gJobId")]
        public async Task<IActionResult> Upload(
            Guid gJobId,
            [FromForm] IFormFile file,
            [FromForm] int category,
            [FromQuery] Guid? crewId)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Fichier manquant.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var command = new ClUploadDocumentCommand(
                gJobId, ms.ToArray(), file.ContentType, file.FileName, category, crewId);

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
