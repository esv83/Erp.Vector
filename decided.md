# Erp.Vector — ce qui est décidé

> **Ce document s'ajoute, il ne se régénère pas.** C'est sa différence avec `DEVPLAN.md`,
> réécrit de zéro à chaque *« compact devplan »* : une décision tranchée et un piège payé
> n'ont aucune raison d'être repassés sous une plume tous les deux jours.

| Document | Contenu | Régime |
|---|---|---|
| `DEVPLAN.md` | ce qui **reste** — en attente, bloqué, envisagé, décidé et pas fait | **régénéré** |
| `decided.md` *(ici)* | **pourquoi** c'est ainsi, et ce qu'on a déjà payé pour l'apprendre | **ajouté** |
| `delivered.md` | ce qui est **fait**, horodaté et **constaté** plutôt qu'annoncé | **ajouté** |

🔴 **À lire avant d'écrire du code dans ce dépôt.** Ce n'est pas de la documentation
d'ambiance : c'est ce qui évite de rejouer un arbitrage déjà rendu et de repayer une erreur
déjà payée.

**Mis à jour au prompt « compact devplan »** : ce qu'une édition a tranché ou appris
descend ici *avant* que le plan ne soit réécrit.

---

# 4. Décisions tranchées — ne pas les rejouer

*Les décisions appliquées vivent dans [`delivered.md`](delivered.md) §4 — **celles du 19/09 au §4.3** :
fermer sur une mesure, lots par liste d'identifiants, pas plus de 8 appels vers Orders, le paquet
partagé sans toucher la configuration, publier depuis `main` seulement. Ce qui suit est la façon dont
ce plan se tient.*

| Date | Décision, et pourquoi |
|---|---|
| 2026-08-24 | **D14 — on code neutre ou additif.** L'app web n'est pas déployée avec l'API |
| 2026-09-13 | **Le livré sort du plan et entre, daté, dans `delivered.md`** au prompt « compact devplan » |
| 2026-09-19 | **Une attente envers l'amont se vérifie chez l'amont avant d'être reconduite.** Ce plan a attendu d'Orders pendant **huit semaines** un repli livré le 23/07 |
| 2026-09-19 | **Une entrée se cite par son titre**, l'ancienne référence entre crochets |
| 2026-09-19 | **Une édition qui n'a pas tout revérifié le dit**, avec la date du dernier relevé |
| 2026-09-21 | **Le `.pdb` dit ce qui a été COPIÉ, `api/version` dit ce qui TOURNE.** Les deux se vérifient après une publication, et le second seul répond à « le serveur a-t-il redémarré dessus ? ». Le 13/09 puis le 15/09, la production a servi autre chose que ce que le partage portait — un contrôle sur les fichiers ne pouvait pas le voir |
| 2026-09-20 | **Un binaire doit savoir dire depuis quel ARBRE il a été construit**, pas seulement depuis quel commit. Le sourcelink donne le `HEAD` du build ; il ne dit pas si des fichiers non commités ont été compilés avec — c'est exactement ce qui a trompé le 13/09 |
| 2026-09-20 | **L'application ne migre JAMAIS la base au démarrage, et ne refuse jamais de démarrer pour un schéma en retard.** Elle constate et le dit — au journal et sur `api/version/runtime`. Migrer sans qu'on l'ait décidé, ou couper le terrain qui marche sur tout le reste, sont deux façons de transformer une gêne en panne |
| 2026-09-20 | **Un script de schéma annulé par un autre n'est plus ATTENDU.** `MOB_008` a supprimé ce que posaient `MOB_002` et `MOB_007` : rien dans la base ne peut plus témoigner de leur passage, et les réclamer ne ferait qu'un signal faux de plus. Inscrits sur une base ancienne, ils ne sont ni manquants ni inconnus |
| 2026-09-20 | **Une amorce de journal de schéma CONSTATE, elle ne déclare pas.** `MOB_009` n'inscrit un script que si ses objets sont dans la base, et marque la ligne `Origine = 'constat'`. Une base vide ne doit pas se déclarer à jour — c'est le pire des verdicts, puisqu'il rassure |
| 2026-09-20 | **La lecture automatique d'une carte PROPOSE, elle n'écrit jamais** (`M5`, tenu par le code) : les champs lus vivent dans des colonnes à part, et c'est la validation humaine qui recopie. Le terrain n'écrase jamais la donnée officielle (D2) — une machine encore moins |
| 2026-09-20 | **Une carte illisible n'est pas une panne** : le modèle a répondu, il n'a rien lu, la carte sort de la file. Seule une panne technique se retente, et pas indéfiniment — la file de projection a relancé 55 450 fois une mission qui ne reviendrait jamais |
| 2026-09-20 | **Activer un appel à un modèle sur une donnée de santé est une décision d'exploitation, pas de code.** Tant que la clé n'est pas posée, le worker ne démarre pas : le code peut donc être publié avant que la décision soit prise, sans rien changer au comportement |
| 2026-09-21 | **Ce qui n'est JAMAIS retenté vers Orders : le 404 et les refus métier.** Un 404 est une réponse — l'équipage inconnu rend une liste vide, la mission inconnue sort du lot en `NotFound` ; un 400 ou un 409 porte son motif. Retenter effacerait l'un et tripleraient l'autre |
| 2026-09-21 | **Un disjoncteur protège l'amont autant que nous.** Orders sert aussi la régulation : quand il tombe, relancer 200 appels de lot ne le relèvera pas. Même raison pour le refus du 19/09 de passer à 16 appels simultanés pour gagner 3 s |
| 2026-09-21 | **Les alias de compatibilité ne se retirent pas tant que le front s'en sert** (`IsAck`, champs historiques du détail, `SelectedDriver` jamais nul, champs typés des lieux). L'app web n'est pas déployée avec l'API (D14) : leur retrait se décide champ par champ, sur confirmation du front |
| 2026-09-20 | **Un garde-fou qui ne garde rien vaut moins que pas de garde-fou** : `AutorizeJob` lisait un jeton, le jetait et rendait toujours `true`. On le croyait en place. Retiré le 21/09 |
| 2026-09-21 | **Accusé d'un trafic qu'on ne produit pas, on RÉPOND EN LISTANT SES ROUTES.** Orders nous attribuait 33 767 appels anonymes ; l'énumération de ce que Vector appelle a montré que nous ne touchons jamais `/vehicles`, et a écarté l'hypothèse en un message. « Nous n'appelons jamais cette route » se vérifie, « ce n'est pas nous » ne se vérifie pas |
| 2026-09-20 | **Une route retirée plutôt qu'une règle métier recodée** : la fin de service déclarée par l'ambulancier contredisait la règle d'Orders (« le régulateur, jamais l'ambulancier »). Le code était en plus cassé et inutilisé — on l'a retiré au lieu de le réparer |
| 2026-10-04 | **Un amont à terre se rend en 503, avec un message affichable — un 4xx non traité reste un 500.** La panne dit « réessayez » ; un refus d'Orders qu'aucun appelant n'a pris en charge est un défaut, et « réessayez dans une minute » y serait un mensonge. Le gestionnaire distingue sur le **statut porté par l'exception** : les clients Orders le renseignent, il ne se devine pas dans un message |
| 2026-10-04 | 🪤 **`catch (HttpRequestException)` ne voit pas Polly.** Le disjoncteur lève `BrokenCircuitException`, le délai `TimeoutRejectedException` : ni l'une ni l'autre n'en dérive. `ShiftConfirmationController` promettait un 503 et rendait 500 depuis le 21/09 |
| 2026-10-04 | 🪤 **`UseExceptionHandler` vide la réponse avant d'écrire la sienne.** Les en-têtes CORS survivent parce qu'ils sont posés au démarrage de la réponse — **vérifié par un test de pipeline**, pas supposé. Sans eux, l'app web verrait une erreur réseau, jamais le message |
| 2026-10-04 | **Une route neuve chez l'amont se consomme avec un repli sur son ABSENCE (404/405), pour que l'ordre des publications soit indifférent.** C'est D14 étendue à l'amont. **Une panne n'est pas une absence** : relancer 200 lectures unitaires sur un Orders à terre ne le relèverait pas. Le repli se retire une fois la nouvelle version **constatée** en production |
| 2026-10-04 | **Un 4xx d'un amont se lit dans son `detail` avant d'être cru.** 539 « 400 Requête invalide » d'Orders étaient des pannes SQL — et notre règle « un 400 ne se retente pas » les traitait en refus métier. La règle est juste ; c'est le statut qui mentait. Signalé **au plan d'Orders**, pas contourné chez nous |
| 2026-10-04 | **Une sonde muette n'est pas une sonde en panne — mais ça se vérifie ailleurs qu'en elle.** Le fichier de la sonde anonyme n'existait pas : le journal principal a montré **zéro appel** aux routes mesurées. Sans ce recoupement, on aurait « réparé » une sonde saine |
| 2026-10-04 | **Le journal de production se lit en agrégats, et rien n'en est copié sur le poste** : il porte des données de patient. Compter, grouper par route et par statut, masquer identifiants et chiffres — pas extraire des lignes |
| 2026-10-04 | 🪤 **Git suit le plan sous `devplan.md`, en minuscules.** Sous Windows, `git add DEVPLAN.md` ne l'ajoute pas, **sans erreur** : le commit part sans le plan. Payé deux fois — le 21/09 (`966f014`) et le 04/10. Ajouter `devplan.md`, et relire `git show --stat` avant de pousser |
| 2026-10-04 | **Dans un cas d'usage, seul le refus métier devient un `Fail` ; une panne remonte.** C'est la règle d'Orders du 16/09, appliquée chez nous : 16 cas d'usage et un contrôleur rendaient toute exception en 400, avec le message de SQL Server — l'ambulancier était accusé d'une panne. Un refus attendu se teste explicitement (`If … Then Return Fail`), il ne se déduit pas d'une exception |
| 2026-10-04 | **Le pont synchrone et le contrat `IResultUseCase` n'étaient qu'une seule dette.** Rendre un repository asynchrone oblige à rendre asynchrones ses cas d'usage — on ne la découpe pas par couche, on la déroule **par chaîne** (identité, équipage, mission), chacune publiable seule |
| 2026-10-04 | **On garde `ClResult(Of T)` ; `ClApplicationResult` attendra.** Le `CLAUDE.md` global le prescrit, mais Vector n'y a aucun appui et la bascule réécrirait la traduction HTTP de tous les cas d'usage — un risque sur le contrat du front (D14) pour aucun gain du terrain. Écart **assumé et daté**, pas oublié |
| 2026-10-04 | **Un `catch` qui produit un format au contrat ne se retire pas, il s'aligne.** Le plan disait redondants ceux de `ShiftConfirmationController` ; ils rendent un 503 en texte que le front lit depuis le 15/09. Ils trient désormais comme le gestionnaire. *Une entrée du plan se vérifie dans le code avant d'être exécutée* |
| 2026-10-04 | **La convention `DtoIn` / `DtoOut` vise la frontière UI ↔ Application, pas le format d'un amont.** Les `Erp*Dto` d'Infrastructure décrivent Orders ; les renommer n'apprendrait rien à personne |
| 2026-10-04 | 🪤 **Un cache enregistré par requête et consulté une fois par requête ne cache rien.** `ClJobListCache` et `ClCrewListCache` donnaient l'illusion d'une économie d'appels à Orders. Retirés ; le vrai cache du chemin chaud est celui de l'identité, et il est testé |
| 2026-10-04 | **La carte mutuelle reste chez Vector, et les écrans viennent la lire.** Elle naît sur le terrain et y est déjà tenue (historique, OCR, M5/M7) ; Erp.Document n'existe pas, et Orders n'a aucun champ mutuelle sur un patient dont la fiche est souvent tenue par AidesNSoft. Images en SQL : ~2 Go/an, une purge plutôt qu'un déménagement |
| 2026-10-04 | **Un écran entre par un azp dédié, en lecture, sur une liste de routes figée par un test — jamais par la politique de repli.** Accepter son jeton à l'authentification lui ouvrirait sinon les formulaires et le NIR. `Keycloak:ScreenAzp` vide = rien de changé, donc publiable avant de connaître les clients |
| 2026-10-04 | **Une nouvelle photo hérite des champs validés de la précédente, et le dit.** Les effacer à chaque photo faisait disparaître la mutuelle du dossier alors que la carte n'avait pas changé ; les reprendre sans le dire aurait présenté une donnée d'hier comme lue aujourd'hui. Ce n'est pas une écriture aveugle (M5) : c'est ce qu'un humain a validé, marqué à revérifier |
| 2026-10-04 | 🪤 **Une colonne lue par EF se crée en base AVANT la publication.** L'application ne migre jamais (20/09) : publier le binaire de `MOB_011` avant le script ferait échouer toute lecture de carte. Le schéma se déclare en retard au démarrage, il ne se répare pas tout seul |

---
