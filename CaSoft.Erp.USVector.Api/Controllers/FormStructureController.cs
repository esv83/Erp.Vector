using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// Structure du formulaire d'attributs d'une mission : les champs à afficher et leur valeur.
    /// <para>
    /// Servie par Order — définitions <b>et</b> valeurs, au niveau de la commande. Deux propriétés
    /// s'ajoutent au modèle historique (<c>IsReadOnly</c>, <c>ReadOnlyReason</c>) et portent le
    /// verrou <b>par champ</b> : celui qui fige une date de naissance déjà connue sans figer le
    /// reste du formulaire.
    /// </para>
    /// <para>
    /// La route reste indexée par <c>jobId</c> : le mobile ne passe jamais le contexte, c'est celui
    /// de la commande qui commande le jeu de champs.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FormStructureController : Controller
    {
        private readonly IContextOrderAttributeService _attributes;

        public FormStructureController(IContextOrderAttributeService attributes) => _attributes = attributes;

        [HttpGet("{gJobId}")]
        public async Task<IActionResult> GetDetail(Guid gJobId, CancellationToken ct)
        {
            var fields = await _attributes.GetFormStructureAsync(gJobId, ct);

            // Mission inconnue d'Order : 404 explicite plutôt qu'un formulaire vide, qui se lirait
            // « cette mission n'a aucun champ à saisir ».
            return fields is null ? NotFound() : Ok(fields);
        }
    }
}
