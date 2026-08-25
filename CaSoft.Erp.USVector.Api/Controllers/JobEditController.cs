using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// MOB-13 — Saisie des valeurs d'attributs d'une mission.
    /// <para>
    /// OC-5 — Sous drapeau, les valeurs vont dans Order au lieu de <c>MOB_JOB_ATTRIBUTE_VALUE</c>.
    /// Le corps ne change pas (couples nom/valeur), mais l'écriture devient <b>tout ou rien</b> et
    /// peut refuser : <b>409</b> si elle modifie un champ verrouillé (date de naissance ou NIR déjà
    /// connus, bon de transport déjà coché), <b>400</b> si une valeur est invalide.
    /// </para>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class JobEditController : Controller
    {
        private readonly IJobCache _jobCache;
        private readonly IJobRepository _jobRepository;
        private readonly IContextOrderAttributeService _attributes;
        private readonly ContextOrderOptions _options;

        public JobEditController(
            [FromServices] IJobCache jobCache,
            IJobRepository jobRepository,
            IContextOrderAttributeService attributes,
            ContextOrderOptions options)
        {
            _jobCache = jobCache;
            _jobRepository = jobRepository;
            _attributes = attributes;
            _options = options;
        }

        [HttpPatch("{gJobId}")]
        [FreezeOnTransfer]
        public async Task<IActionResult> PatchEditableJob(
            Guid gJobId, List<ClAttributValueModel> Values, CancellationToken ct)
        {
            if (!_options.UseOrderAttributes)
            {
                var cmd = new ClUpdateJobEditCommand(gJobId, Values);
                return new ClUpdateJobEditUseCase(cmd, _jobCache, _jobRepository).Handle().ToActionResult();
            }

            // setBy : le « sub » Keycloak de l'ambulancier, tracé tel quel côté Order — même choix
            // que pour le type de mission, et pour la même raison : ne pas ajouter un maillon
            // d'identité, donc un mode d'échec, sur un chemin d'écriture.
            var setBy = User.GetKeycloakSubject()?.ToString();

            var outcome = await _attributes.SaveValuesAsync(gJobId, Values, setBy, ct);

            return outcome switch
            {
                // On renvoie ce qui a été soumis, comme le faisait le chemin historique : le lot est
                // accepté en entier ou pas du tout, donc l'écho est exact.
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
