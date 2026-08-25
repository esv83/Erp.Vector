namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// Type de contrat Vector à retenir pour composer le formulaire d'une mission.
/// <para>
/// <see cref="ContractTypeId"/> à <c>null</c> n'est <b>pas</b> une absence de réponse : c'est la
/// réponse « aucun type ne s'applique », qui donne un formulaire réduit aux attributs « core ».
/// L'absence de réponse, elle, s'exprime par un <see cref="IEffectiveContractTypeResolver"/> qui
/// rend <c>null</c> tout court.
/// </para>
/// </summary>
public readonly record struct EffectiveContractType(int? ContractTypeId);

/// <summary>
/// OC-3b — Dit quel jeu d'attributs servir, quand le type de mission ne vit plus dans
/// <c>MOB_JOB_CONTRACT</c> mais chez Order.
///
/// <para><b>Le problème que ça résout.</b> Une fois la bascule armée, plus rien n'écrit
/// <c>MOB_JOB_CONTRACT</c> — or c'est de là que <c>FormStructure</c> tirait le type, donc le jeu de
/// champs à afficher. Sans ce résolveur, le formulaire se figerait sur le type par défaut quel que
/// soit le choix de l'ambulancier : il cocherait « Article 80 » et saisirait les champs du
/// transport standard, sans que rien ne le signale.</para>
///
/// <para><b>Le trajet</b> : type effectif lu chez Order → son <c>code</c> → code Vector équivalent
/// (<see cref="ContextOrderCodeAliases"/>) → ligne de <c>MOB_CONTRACT_TYPE</c> → son id, celui que
/// le catalogue d'attributs connaît. On passe par le <b>code</b> et jamais par l'id : les deux
/// espaces d'identifiants ne coïncident pas.</para>
///
/// <para><b>Tri-état volontaire</b> — les trois réponses ne veulent pas dire la même chose :</para>
/// <list type="bullet">
///   <item><description><c>null</c> — « je ne me prononce pas » : drapeau désarmé, ou ERP muet. Le
///   chemin historique reprend la main, donc le comportement d'aujourd'hui.</description></item>
///   <item><description><c>EffectiveContractType(null)</c> — « aucun type applicable » : la mission
///   n'a pas de type posé, ou son type n'a pas d'équivalent au catalogue Vector. Attributs « core »
///   seuls.</description></item>
///   <item><description><c>EffectiveContractType(id)</c> — le type Vector correspondant.</description></item>
/// </list>
///
/// <para>⚠️ <b>Composant de transition</b> : il disparaît avec OC-5, quand les attributs eux-mêmes
/// viendront d'Order et qu'il n'y aura plus de catalogue Vector à retrouver.</para>
/// </summary>
public interface IEffectiveContractTypeResolver
{
    /// <summary>
    /// Type Vector à utiliser pour la mission, ou <c>null</c> si ce résolveur ne se prononce pas.
    /// Ne lève jamais : une panne de l'ERP est une abstention, pas une erreur.
    /// </summary>
    EffectiveContractType? Resolve(Guid missionId);
}
