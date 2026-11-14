using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    /// <summary>
    /// L'équipage complète le patient de sa mission (J-144, contact) — relais vers la bordure d'Order.
    /// <para>
    /// <b>204</b> enregistré · <b>400</b> valeur mal formée · <b>403</b> hors équipage ou mission de plus
    /// de deux jours (définitif) · <b>404</b> mission ou patient introuvable · <b>409</b> (identité) déjà
    /// tranchée par la régulation · <b>503</b> Order injoignable, à rejouer. Le corps d'un refus est
    /// <b>le motif d'Order</b>, en texte simple, à afficher tel quel — comme <c>JobEdit</c>.
    /// </para>
    /// <para>
    /// <c>setBy</c> est le <c>sub</c> du jeton de l'ambulancier, jamais une valeur du corps : c'est
    /// sur lui qu'Order juge l'appartenance à l'équipage.
    /// </para>
    /// </summary>
    [Route("api/missions/{gJobId:guid}/beneficiary")]
    [ApiController]
    public class MissionBeneficiaryController : Controller
    {
        private readonly IBeneficiaryFieldEntryService _entries;

        public MissionBeneficiaryController(IBeneficiaryFieldEntryService entries) => _entries = entries;

        /// <summary>Identité du patient d'une commande née anonyme (nom, prénom, date de naissance, sexe).</summary>
        [HttpPut("identity")]
        public async Task<IActionResult> RecordIdentity(Guid gJobId, [FromBody] ClBeneficiaryIdentityDtoIn input, CancellationToken ct)
        {
            var setBy = User.GetKeycloakSubject()?.ToString();
            if (setBy is null) return Unauthorized("Jeton sans identifiant d'ambulancier : la saisie ne peut pas lui être attribuée.");
            return ToActionResult(await _entries.RecordIdentityAsync(gJobId, input, setBy, ct));
        }

        /// <summary>Téléphone et/ou e-mail ajoutés au patient de la mission.</summary>
        [HttpPut("contact")]
        public async Task<IActionResult> RecordContact(Guid gJobId, [FromBody] ClBeneficiaryContactDtoIn input, CancellationToken ct)
        {
            var setBy = User.GetKeycloakSubject()?.ToString();
            if (setBy is null) return Unauthorized("Jeton sans identifiant d'ambulancier : la saisie ne peut pas lui être attribuée.");
            return ToActionResult(await _entries.RecordContactAsync(gJobId, input, setBy, ct));
        }

        private IActionResult ToActionResult(ClBeneficiaryEntryResult result) => result.Outcome switch
        {
            EnBeneficiaryEntryOutcome.Applied => NoContent(),
            EnBeneficiaryEntryOutcome.Invalid => BadRequest(result.Reason ?? "Saisie refusée : valeur invalide, rien n'a été enregistré."),
            EnBeneficiaryEntryOutcome.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                result.Reason ?? "Saisie refusée — la régulation a été prévenue de compléter la fiche."),
            EnBeneficiaryEntryOutcome.NotFound => NotFound(result.Reason ?? "Mission ou patient introuvable."),
            EnBeneficiaryEntryOutcome.Locked => Conflict(result.Reason ?? "Identité verrouillée : la régulation l'a déjà établie."),
            _ => BadRequest(result.Reason ?? "Saisie refusée.")
        };
    }
}
