using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// Saisie des valeurs d'attributs d'une mission.
    /// <para>
    /// Les valeurs vont dans Order, au niveau de la <b>commande</b> : ce que l'aller renseigne, le
    /// retour le voit. Le corps ne change pas (couples nom/valeur), mais l'écriture est <b>tout ou
    /// rien</b> et peut refuser — <b>409</b> si elle modifie un champ verrouillé (date de naissance
    /// ou NIR déjà connus, bon de transport déjà coché), <b>400</b> si une valeur est invalide.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class JobEditController : Controller
    {
        private readonly IContextOrderAttributeService _attributes;

        public JobEditController(IContextOrderAttributeService attributes) => _attributes = attributes;

        [HttpPatch("{gJobId}")]
        [FreezeOnTransfer]
        public async Task<IActionResult> PatchEditableJob(
            Guid gJobId, List<ClAttributValueModel> Values, CancellationToken ct)
        {
            // setBy : le « sub » Keycloak de l'ambulancier, tracé tel quel côté Order — même choix
            // que pour le type de mission, et pour la même raison : ne pas ajouter un maillon
            // d'identité, donc un mode d'échec, sur un chemin d'écriture.
            var setBy = User.GetKeycloakSubject()?.ToString();

            var outcome = await _attributes.SaveValuesAsync(gJobId, Values, setBy, ct);

            return outcome switch
            {
                // On renvoie ce qui a été soumis : le lot est accepté en entier ou pas du tout,
                // donc l'écho est exact.
                EnContextOrderValuesOutcome.Applied => Ok(Values),
                EnContextOrderValuesOutcome.FieldLocked =>
                    Conflict("Champ verrouillé : cette information est déjà enregistrée et ne peut plus être modifiée."),
                EnContextOrderValuesOutcome.Invalid =>
                    BadRequest("Saisie refusée : au moins une valeur est invalide, rien n'a été enregistré."),
                EnContextOrderValuesOutcome.MissionNotFound => NotFound($"Mission {gJobId} introuvable."),
                _ => BadRequest("Saisie refusée.")
            };
        }
    }
}
