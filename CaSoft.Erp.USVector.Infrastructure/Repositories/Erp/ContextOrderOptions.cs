namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// OC-3b — Le drapeau qui arme la bascule du référentiel de type de mission vers Order.
///
/// <para><b>Pourquoi un drapeau et pas un remplacement sec.</b> Les trois préalables de la bascule
/// ne sont pas dans ce dépôt : le déploiement d'<c>Order OC-28</c> (sans lui, l'API en service
/// verrouille encore 20 missions sur 25), l'affichage du cadenas côté web, et la levée des ids en
/// dur du front. Le code peut donc être écrit, testé et livré bien avant de pouvoir servir. Le
/// drapeau permet exactement cela — partir en production <b>inerte</b>, comme l'ont fait OC-1, OC-2
/// et OC-4 avant lui — puis s'armer par un fichier d'environnement, et se désarmer de même si la
/// bascule se passe mal : un retour arrière par configuration, sans redéploiement, donc <b>sans la
/// coupure d'API</b> qu'impose <c>app_offline.htm</c>.</para>
///
/// <para><b>Ce que l'armement change, en un mot</b> : la liste et l'écriture du type de mission
/// cessent de passer par <c>MOB_CONTRACT_TYPE</c> / <c>MOB_JOB_CONTRACT</c> et passent par Order.
/// Le contrat mobile — routes, verbes, forme des réponses — ne bouge pas (D14) ; seuls les
/// <b>identifiants</b> servis changent d'espace, ce qui est précisément ce que le front doit avoir
/// cessé de coder en dur avant qu'on arme.</para>
///
/// <para>⚠️ <b>À supprimer avec le second chemin</b>, une fois la bascule acquise partout : un
/// drapeau qui survit à sa transition devient un mode d'exécution que plus personne ne teste.</para>
/// </summary>
public sealed class ContextOrderOptions
{
    /// <summary>Section de configuration correspondante.</summary>
    public const string SectionName = "ContextOrder";

    /// <summary>
    /// <c>false</c> (défaut) — la liste et l'écriture restent sur le catalogue Vector : comportement
    /// strictement identique à celui d'avant OC-3b.
    /// <para>
    /// <c>true</c> — Order devient la source : <c>GET api/Contract/{jobId}</c> sert
    /// <c>availableContextOrders</c> (déjà filtré agence/mode), <c>POST</c> relaie le <c>PATCH</c>
    /// et peut désormais répondre <b>409</b> (verrou) et <b>400</b> (non applicable) là où l'appel
    /// réussissait toujours, et le jeu d'attributs de <c>FormStructure</c> suit le type effectif lu
    /// chez Order.
    /// </para>
    /// </summary>
    public bool UseOrderCatalog { get; init; }

    /// <summary>
    /// OC-5 — <c>false</c> (défaut) : le formulaire d'attributs et les valeurs saisies restent en BD
    /// Mobile. <c>true</c> : <c>FormStructure</c> et <c>JobEdit</c> passent par Order, qui devient
    /// aussi l'autorité sur le verrou <b>par champ</b> (DDN/NIR connus, PMT/BT scellés) — et
    /// <c>PATCH api/JobEdit</c> peut désormais répondre <b>409</b>.
    /// <para>
    /// <b>Suppose <see cref="UseOrderCatalog"/>.</b> Les deux endpoints d'attributs d'Order résolvent
    /// eux-mêmes mission → context effectif : armer les attributs sans avoir armé le type ferait
    /// choisir un type d'un côté et afficher les champs d'un autre. Le démarrage refuse cette
    /// combinaison plutôt que de la laisser produire un écran incohérent en production.
    /// </para>
    /// <para>
    /// L'inverse est légitime et constitue l'étape intermédiaire voulue : le type vient d'Order, les
    /// attributs suivent plus tard, une fois la première bascule observée.
    /// </para>
    ///
    /// <para>⛔ <b>Ne pas armer avant OC-7.</b> Le paquet terrain (<c>field-data</c>) compose encore
    /// son bloc <c>attributes</c> depuis le magasin Vector. Une fois ce drapeau armé, les valeurs
    /// saisies partent chez Order et ce magasin cesse d'être alimenté : le paquet continuerait de se
    /// construire sans erreur, mais <b>vide de ces valeurs</b>, et la facturation ne verrait rien
    /// venir. C'est un silence, pas une panne — donc à traiter avant, pas après.</para>
    ///
    /// <para>Ce n'est volontairement pas un garde-fou de démarrage : OC-7 est un changement de code,
    /// pas une clé de configuration, et rien au démarrage ne sait dire s'il est fait.</para>
    /// </summary>
    public bool UseOrderAttributes { get; init; }
}
