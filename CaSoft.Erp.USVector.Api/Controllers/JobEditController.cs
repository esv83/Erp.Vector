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
    /// <para>
    /// Le corps d'un refus porte <b>le motif d'Order</b> — « clé de contrôle du numéro de sécurité
    /// sociale incorrecte » —, reformulé pour le terrain quand il nomme un système auquel
    /// l'ambulancier n'a pas accès (cf. <c>ModTerrainLockWording</c>). C'est du texte simple, comme
    /// avant : <b>à afficher tel quel</b>. Sans cet affichage, un refus se lit sur le terrain comme
    /// une saisie qui ne s'enregistre pas, et l'ambulancier recommence sans jamais savoir ce qui
    /// coince.
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

            var result = await _attributes.SaveValuesAsync(gJobId, Values, setBy, ct);

            // Le corps du refus est le motif d'Order dès qu'il en donne un : lui seul nomme le champ
            // et la règle (« clé de contrôle du numéro de sécurité sociale incorrecte », « fiche
            // maîtrisée par le référentiel AidesNSoft »). Les phrases ci-dessous ne sont que le repli
            // pour un refus muet — elles décrivent la famille, pas la cause, et c'est précisément ce
            // qui faisait lire un refus comme une perte de saisie.
            // Le format ne change pas (texte simple, comme depuis OC-5) : le mobile n'a rien à
            // reparser, il a seulement une phrase à afficher.
            return result.Outcome switch
            {
                // On renvoie ce qui a été soumis : le lot est accepté en entier ou pas du tout,
                // donc l'écho est exact.
                EnContextOrderValuesOutcome.Applied => Ok(Values),
                EnContextOrderValuesOutcome.FieldLocked =>
                    Conflict(Motif(result, ModTerrainLockWording.FicheLocked)),
                EnContextOrderValuesOutcome.Invalid =>
                    BadRequest(Motif(result, "Saisie refusée : au moins une valeur est invalide, rien n'a été enregistré.")),
                EnContextOrderValuesOutcome.MissionNotFound =>
                    NotFound(Motif(result, $"Mission {gJobId} introuvable.")),
                _ => BadRequest(Motif(result, "Saisie refusée."))
            };
        }

        /// <summary>Motif d'Order s'il en a donné un, sinon le libellé de repli.</summary>
        private static string Motif(ClContextOrderValuesResult result, string repli)
            => result.HasReason ? result.Reason : repli;
    }
}
