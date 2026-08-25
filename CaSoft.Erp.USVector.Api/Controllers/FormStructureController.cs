using CaSoft.Framework;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// MOB-13 — Structure du formulaire d'attributs d'une mission : les champs à afficher et leur
    /// valeur courante.
    /// <para>
    /// OC-5 — La <b>source</b> bascule vers Order sous drapeau. Le contrat ne change pas : même
    /// route, même liste de champs, même parsing côté front. Deux propriétés s'ajoutent
    /// (<c>IsReadOnly</c>, <c>ReadOnlyReason</c>) et portent le verrou <b>par champ</b> — celui qui
    /// fige une date de naissance déjà connue sans figer le reste du formulaire.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FormStructureController : Controller
    {
        private readonly ContextOrderOptions _options;
        private readonly IContextOrderAttributeService _attributes;

        public FormStructureController(ContextOrderOptions options, IContextOrderAttributeService attributes)
        {
            _options = options;
            _attributes = attributes;
        }

        [HttpGet("{gJobId}")]
        public async Task<IActionResult> GetDetail(
            Guid gJobId, [FromServices] IJobRepository repository, CancellationToken ct)
        {
            if (_options.UseOrderAttributes)
            {
                var fields = await _attributes.GetFormStructureAsync(gJobId, ct);
                // Mission inconnue d'Order : 404 explicite plutôt qu'un formulaire vide, qui se
                // lirait comme « cette mission n'a aucun champ à saisir ».
                return fields is null ? NotFound() : Ok(fields);
            }

            return new ClGetJobEditFormStructureUseCase(gJobId, repository).Handle().ToActionResult();
        }
    }
}
