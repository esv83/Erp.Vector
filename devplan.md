# Plan de développement — Erp.Vector

> **Édition du** 2026-10-04 · **En production** `4ff99ce` — **constaté par `GET /vector/api/version`**
> à 11:14 : commit, `Tree: clean`, environnement Production ; `.pdb` concordant ; schéma 8 sur 8 ·
> **254 tests verts** · Dépôt `github.com/esv83/Erp.Vector` (`USVector.sln`) · Prod
> `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`)
>
> ✅ **Une panne d'Orders se dit, au lieu de se taire en 500** — et la facturation peut lire Orders
> par lot : la route est en production chez eux depuis le 04/10, en 1.8.5.
>
> ⏳ **Ce qui reste ici est surtout à VOIR** : la première passe de la facturation sur la route par
> lot, le premier 503 sur une vraie panne. Le plus lourd reste la protection des données du patient ;
> le plus bloquant n'est pas chez nous — voir « Ce qui attend ailleurs ».
>
> Ce document **se régénère** au prompt *« compact devplan »* et ne porte **que l'ouvert**. Le
> **pourquoi** vit dans [`decided.md`](decided.md), à lire avant de coder ; le **fait**, horodaté et
> constaté, dans [`delivered.md`](delivered.md).
>
> **Règle de travail** : neutre ou additif, jamais de rupture du contrat que consomme l'app web — elle
> n'est pas déployée en même temps que l'API. **Et envers l'amont** : une route neuve se consomme avec
> un repli sur son absence.

**Cinq rubriques, communes à tous les dépôts de l'ERP.** Le format est partagé ; les données ne le
sont jamais.

## 📍 Point de reprise — 04/10 : ce qui se taisait se dit

**Le relevé de la production du 21/09 au 04/10** a trouvé ce qu'aucun contrôle ne cherchait :
**~2 600 requêtes du terrain en 500 brut**, dont l'écran d'entrée, pendant deux coupures
d'infrastructure (22/09, 02/10) et les publications d'Orders. Corrigé et publié le jour même — une
panne d'Orders rend désormais un **503 avec un message affichable**.

> ⚖️ **La coupure du 22/09 a duré 40 minutes et n'a été vue que douze jours plus tard**, en lisant le
> journal pour autre chose. Ce que le journal sait, personne ne le lui demande.

**Le même relevé a remonté chez Orders** 539 pannes SQL rendues en « 400 » : signalées à leur plan,
**corrigées dans la journée** (1.8.5 en production, 1.8.6 qui étend la règle à cent handlers).

**Par quoi reprendre** — lundi 05/10, après la passe de la facturation : la voir passer **par la
route par lot**, puis retirer le repli.

---
# 1. Fonctionnalités livrées

*Ce que le module sait faire **en production**, en une page. Le récit daté vit dans
[`delivered.md`](delivered.md), à jour au 04/10.*

| Domaine | Ce que le module apporte aujourd'hui |
|---|---|
| **Se connecter** | Compte d'entreprise · l'app retrouve seule l'équipage du jour et fait choisir quand il y en a plusieurs · missions visibles **30 min avant la prise de service** · un accès refusé **dit pourquoi et quoi faire** |
| **Confirmer sa prise de service** | L'app dit **qu'une confirmation attend**, même la veille, et l'ambulancier confirme **sans le courriel** |
| **Voir son travail du jour** | Missions **engagées** par la régulation · détail : patient, adresses, horaires, sens, service destinataire · affichage des lieux composé par le serveur |
| **Faire avancer la mission** | Cinq étapes horodatées, **annulables** · « mission vue » · signature · conducteur — **un refus dit pourquoi** · tout remonte à la régulation en quasi temps réel, un envoi en échec est rejoué |
| **Compléter le dossier** | Type de mission et informations de facturation servis par la régulation, pré-remplis et verrouillés quand la fiche patient les connaît · un refus arrive **avec son motif** · anomalies, documents, photos |
| **Photographier la carte mutuelle** | Depuis la mission · affichable par les écrans d'Orders et de la facturation · **lecture automatique en place, inerte** tant que la décision n'est pas prise |
| **Passer à la facturation** | Transfert automatique à la clôture · dossier **par lots de 200**, signatures par lots de 50 · **Orders lu par lot** quand il porte la route, mission par mission sinon · dossier **gelé** après transfert |
| **Tenir debout** | Le terrain n'écrase jamais la donnée officielle · API fermée par défaut, jetons exigés · **délais et disjoncteur** sur les appels à Orders — **vus sur de vraies pannes** · **Orders à terre → 503 et message**, plus de 500 brut · publication **depuis `main` propre et poussé**, **commit servi vérifié** — la garde **a refusé pour de vrai** le 04/10 |
| **Se dire** | `api/version` : commit, **état de l'arbre au build**, environnement · `api/version/runtime` : base résolue, drapeaux, **état du schéma** |

### Les réserves à ne pas perdre de vue

- ⚠️ **Deux routes restent anonymes** : l'affichage de la carte mutuelle par les écrans amont. **Zéro
  appel du 21/09 au 04/10** — *voir « Protéger les données du patient »*.
- ⚠️ **Le message du 503 n'atteint l'ambulancier que si l'app l'affiche** : demandé au dev web le
  04/10 — *voir « Ce qui attend ailleurs »*.
- ⚠️ **Un ambulancier rattaché depuis Employee seul n'entre pas** : 8 membres sur 244 *(13/09, non
  remesuré)*.
- ⚠️ **Ouvrir l'app avant que l'équipage soit composé échoue** — 23 min d'attente médiane
  *(juillet-août)*. Le message dit quoi faire.
- ⚠️ **Tous les types de mission sont proposés partout** : 11 au catalogue, les deux tables de
  restriction **vides** *(relevé chez Orders le 04/10)*.
- ⚠️ **Un n° de sécurité sociale mal tapé part en facturation** : aucun module ne le corrige, et
  l'écran ne le fait pas relire *(demandé au dev web le 26/08)*.

---
# 2. Code bloqué, manquant ou en attente

*Classé par itération de session de codage, **du plus simple au plus complexe**. **L'ordre est un ordre
de difficulté, pas de priorité.** Chaque itération porte entre crochets son **ancienne référence** :
d'autres dépôts la citent encore. **Dans une référence croisée, on nomme la rubrique.***

## Itération 1 — Constater ce qui vient d'être publié *[ex-F1, B2, B8]*

| | |
|---|---|
| **Nature** | Constats et mesures — aucun code |
| **Effort** | Un relevé lundi, puis au fil de l'eau |
| **Bloqué par** | **Les événements eux-mêmes** |

**À voir passer** — `4ff99ce` est en production depuis le 04/10 à 11:14 :
- **la passe de la facturation par la route par lot** : plus aucun `/missions/{id}/full` pendant les
  lots, **aucune ligne « Orders ne porte pas batch-refs »** — Orders sert la 1.8.5 depuis le 04/10 ;
- **un 503 sur une vraie panne** : `WARN … Orders indisponible, 503 rendu` à la place d'exceptions non
  gérées — la prochaine coupure, ou la publication de la 1.8.6 d'Orders ;
- **un refus de conducteur** : `WARN` avec la phrase d'Orders — **jamais vu** (344 changements en 200
  du 21/09 au 04/10) ;
- **la suite [`Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http)**, à rejouer entièrement
  une fois, identifiants en main.

**À comprendre**, relevés le 04/10 et non examinés :
- **988 `403` sur `JobDetail`** et 345 sur `Joblist` en 14 jours — l'accès refusé normal (hors
  fenêtre, autre équipage) ou un écran qui insiste ?
- **51 démarrages du pool en 14 jours**, avec des clés de protection éphémères à chacun — recyclage
  sur inactivité, probablement.
- **Pas de passe de la facturation le jeudi 24/09.**

**À mesurer chez Orders** : les **étapes de mission vides** (~3 883 annoncées avant leur repli du
23/07). Vector ne les journalise pas : c'est un comptage dans leur base.

## Itération 2 — Retirer le repli de la lecture par lot 🆕

| | |
|---|---|
| **Nature** | Échafaudage à démonter |
| **Effort** | Une courte session |
| **Bloqué par** | **Une passe de la facturation vue sur la route par lot** — voir « Constater ce qui vient d'être publié » |

Le repli mission par mission ne sert que tant qu'Orders ne porte pas `batch-refs` ; la 1.8.5 est en
production. Retirer `ReadRefsOneByOneAsync`, le **corps par défaut** de
`IErpReadApiClient.GetMissionBatchRefsAsync`, et le traitement 404/405 du client — une route absente
redevient une panne. Les doubles de test implémentent alors la méthode.

## Itération 3 — L'adresse publique de la recette *[relevé le 21/09]*

| | |
|---|---|
| **Nature** | Un contrôle à moitié armé |
| **Effort** | Une minute, dès que l'URL est connue |
| **Bloqué par** | **L'URL de la recette** — les chemins évidents rendent 404 *(21/09, non revérifié)* |

`deploy.ps1` lit `api/version` après la copie ; l'adresse est **vide pour la recette**, le contrôle
y est sauté. Renseigner `VectorVersionUrl` dans `IIS-DevServer.pubxml`.

## Itération 4 — Dire l'heure qu'il est *[ex-E2]*

| | |
|---|---|
| **Nature** | Données ambiguës transmises à la facturation |
| **Effort** | Une session, **à coordonner avec la facturation** |
| **Bloqué par** | Rien |

Les étapes sont en UTC **sans le déclarer** ; l'heure de signature est écrite **en heure locale**
(`SignatureRepository.cs:34`, `:43`). Même motif à examiner dans `ClMarkMissionSeenUseCase` et
`ClSetDriverUseCase`. **Fin visée** : tout en UTC, fuseau **déclaré** dans le contrat du paquet.

## Itération 5 — Les dettes de forme qui restent *[ex-G2, G5]*

| | |
|---|---|
| **Nature** | Hygiène — aucun changement de contrat |
| **Effort** | Plusieurs petites sessions, à piocher |
| **Bloqué par** | Rien |

- **Nommage des DTO** en `…DtoIn` / `…DtoOut` — aucun impact JSON, des centaines de références.
- **Pont synchrone/asynchrone** (`.GetAwaiter().GetResult()`) sur liste, détail, identité, conducteur :
  à défaire en remontant l'asynchrone jusqu'aux cas d'usage.
- **`IResultUseCase` est synchrone** : les cas d'usage asynchrones n'implémentent aucune interface.
- **`EnableRetryOnFailure` sur notre `UseSqlServer`** (`Program.cs`) : absorberait les micro-coupures,
  pas les 40 min du 22/09 — à poser en regardant les écritures en transaction.
- **Les `catch (HttpRequestException)` de `ShiftConfirmationController`** sont devenus redondants avec
  le gestionnaire global : à retirer, en gardant leur message.

⚖️ **Les alias de compatibilité restent** : leur sort est en rubrique « Fonctionnalités envisagées
en Vn », conditionné au front — *cf. [`decided.md`](decided.md)*.

## Itération 6 — Le kilométrage dans le dossier transmis *[ex-E1, MOB-10]*

| | |
|---|---|
| **Nature** | Champ attendu par la facturation, toujours vide |
| **Effort** | Une session si le km véhicule suffit ; **plusieurs** s'il faut un relevé par mission |
| **Bloqué par** | 🔴 **Un arbitrage avec la facturation** |

Le kilométrage appartient à l'équipage et au véhicule, pas à la mission. Km du véhicule, ou relevé
début/fin par mission (table, saisie mobile, paquet) ?

## Itération 7 — Activer la lecture automatique des cartes *[ex-F2, P3]*

| | |
|---|---|
| **Nature** | Fonctionnalité en place, **inerte** |
| **Effort** | Poser une clé ; puis l'écran de validation, **côté web** |
| **Bloqué par** | 🔴 **Une décision d'exploitation** : activer, c'est envoyer une **donnée de santé** au fournisseur du modèle |

Tout est publié et dort. **Ce que la décision engage** : ~12 cartes par jour, de l'ordre du centime
par carte, et l'image qui sort du réseau. **Ce que ça rapporterait** *(20/09)* : le code AMC n'est
saisi que **41 fois sur 85**. Il faudra ensuite l'**écran de validation** côté web
([`note_web_alexandre_carte_mutuelle_ocr.md`](note_web_alexandre_carte_mutuelle_ocr.md)).

## Itération 8 — Positions et statuts des véhicules *[ex-F3, MOB-16]*

| | |
|---|---|
| **Nature** | Connecteurs portés, jamais recâblés |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | Rien de connu — non réexaminé depuis juillet |

GpsGate (positions, REST) et Sirus (statuts, UDP) sont injectés mais ne servent à rien.

## Itération 9 — Protéger les données du patient *[ex-G7, P4]*

| | |
|---|---|
| **Nature** | RGPD — dette assumée |
| **Effort** | Plusieurs sessions, un seul lot |
| **Bloqué par** | Rien |

Documents, carte mutuelle et anomalies servis par une API exposée : **rétention et purge** (3 ans),
chiffrement au repos, **fermeture des deux routes d'affichage de la carte**, **audit des accès**.
🆕 **La fermeture a maintenant sa mesure** : **zéro appel** à ces deux routes du 21/09 au 04/10 — la
règle « fermer sur une mesure » est satisfaite pour la fenêtre relevée ; reste à confirmer auprès
d'Orders et de la facturation que leurs écrans ne les appellent plus. ⚠️ Si la lecture automatique
est activée, ce lot porte aussi le sort de l'image envoyée au modèle.

## Itération 10 — Ce qui attend ailleurs *[ex-A3, B, C1, C3, E4, F4]*

*Rien à coder ici tant que l'autre partie n'a pas bougé. Non revérifié à cette édition sauf mention,
et **c'est dit**.*

| Entrée | Qui doit bouger | Dernier relevé | En deux mots |
|---|---|---|---|
| **Règle d'applicabilité des types** *[B9]* | Orders + décision métier | **04/10** | **11 types proposés partout**, les deux tables de restriction vides. Première de leur plan, **par priorité** |
| **Afficher le message du 503** 🆕 | dev web | **04/10** | [`note_web_alexandre_503_regulation_indisponible.md`](note_web_alexandre_503_regulation_indisponible.md) — au moins sur l'écran d'entrée. Neutre s'il ne le fait pas : rien ne casse, le message se perd |
| **Exiger un jeton du terrain** | Orders | **04/10** | Rien ne l'en empêche de notre côté. ⚠️ **`POST /missions/batch-refs` répond 200 sans jeton** en production |
| **Rattachement des comptes** *[C1]* | Orders, Identity, **RH** | 13/09 | Vector lit `PER_KEYCLOAK_MAP`, que l'écran d'Employee n'alimente pas. Débloqué par la bascule d'Orders sur le carnet d'Identity, elle-même bloquée par **273 personnels sans fiche Employee**. En attendant : [consigne](docs/auth/consigne-rattachement-ambulancier.md) **à transmettre à la régulation et à la RH** |
| **Écrans : motif d'un champ grisé, relecture du NIR, bouton *Réessayer*, motif du refus de conducteur, validation des cartes lues** | dev web | 21/09 | Cinq demandes, toutes contractualisées : [`note_ui_alex.md`](note_ui_alex.md), [`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md), [`note_web_alexandre_carte_mutuelle_ocr.md`](note_web_alexandre_carte_mutuelle_ocr.md), [routes retirées](note_web_alexandre_routes_retirees.md) |
| **Composer les équipages avant la prise de service** *[C3]* | 🔴 régulation — décision | 13/09 | Sans quoi l'accès anticipé de 30 min ne sert à rien. ⚠️ Le filtre d'appartenance (`MobileIdentityResolver.cs:35`) est **volontaire** |
| **`REFERENCE` et `URGENT`** *[B10]* | décision métier | 19/09 | Absents du catalogue ; tout le reste est servi |
| **`Billed` : l'écrire, ou retirer le palier** *[B4, E4]* | 🔴 décision | 13/09 | La facturation est en lecture seule par décision de son module |
| **Tests du transfert côté Orders** *[B6]* · **Relance des missions terminées non clôturées** *[B7]* | Orders | 13/09 | Aucun filet sur la dérivation du statut ; des dossiers n'arrivent jamais en facturation |
| **Présence : qui est connecté** *[F4]* | 🔴 décision + cadrage **RH/RGPD** | 13/09 | Spec sans code : [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md) |
| **La cause des coupures des 22/09 et 02/10** 🆕 | exploitation / réseau | **04/10** | SQL, Orders et DNS tombent **ensemble** : ce n'est pas un module. Hors de ce dépôt ; signalé ici pour qu'on ne le cherche pas dans le code |

---
# 3. Fonctionnalités envisagées en Vn

*Non engagé, non planifié — avec la condition qui les remettrait sur la table.*

| Sujet | En deux mots | Ce qui la rouvrirait |
|---|---|---|
| **Retirer les alias de compatibilité** *[G2]* | `IsAck` (alias de `IsSeen`), champs historiques du détail, `SelectedDriver` jamais nul, champs typés des lieux. **Conservés délibérément** | **La confirmation du front, champ par champ**. Contrat : [`note_ui_alex.md`](note_ui_alex.md) |
| **Base Vector dédiée** *[Vd-1]* | Seul jalon DMZ non conditionné à la V2 | Pertinent dès maintenant ; personne ne l'a porté |
| **Accès anticipé à cheval sur minuit** *[CREW-2]* | Correctif connu | Les vacations de nuit ne sont pas concernées *(02/08)* |
| **Durcissement DMZ événementiel, push temps réel** *[Vd-2 à Vd-8]* | [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md) | Une exigence d'exposition, ou le polling qui ne suffit plus |
| **Photos hors SQL, masquage** *[Vd-6, Vd-5]* | NIR partiel, équipage retour | Le volume, ou le lot RGPD |
| **Contrats partagés avec Orders** *[4b]* | Écart JSON assumé | Une rupture de contrat constatée |
| **Repère de fraîcheur du dossier** *[E5]* | `updatedAt` est servi, personne ne s'en sert | Un besoin de resynchronisation |
| **Alerter sur les 500 et les coupures** | La coupure du 22/09 n'a été vue que douze jours après. Le journal sait compter ; personne ne le lui demande | Une deuxième coupure découverte en retard |
| **Éviction ciblée du cache d'identité, mode hors ligne, géolocalisation avancée, renommage `USVector` → `Vector`** | — | Une demande |

---
# 4. Décisions tranchées

**Dans [`decided.md`](decided.md)**, qui s'ajoute et ne se régénère pas. 🔴 *À lire avant d'écrire du
code.* L'édition du 04/10 y a versé sept lignes : le 503 et le 500, Polly hors de
`HttpRequestException`, le gestionnaire et CORS, le repli sur l'absence d'une route amont, le 4xx
d'amont qui se lit avant d'être cru, la sonde muette, et le journal de production lu en agrégats.

---
# 5. Journal des livraisons

**Dans [`delivered.md`](delivered.md)**, horodaté et constaté. Dernières entrées : la publication de
`4ff99ce` et les constats du 21/09 au 04/10 ; trois incidents ajoutés en fin de document.

---

## Annexe — documents voisins

| Doc | Ce qu'il apporte |
|---|---|
| [`decided.md`](decided.md) | **Le pourquoi** : décisions tranchées, pièges déjà payés — à lire avant de coder |
| [`delivered.md`](delivered.md) | **Le fait** : journal daté et constaté, configuration, incidents |
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Le besoin et le vocabulaire |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Carte mutuelle |
| [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md) | Projection du terrain vers Orders |
| [`TRACABILITE_SAISIES_VECTOR_EXPORT.md`](TRACABILITE_SAISIES_VECTOR_EXPORT.md) | Des saisies aux 91 colonnes de facturation |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Authentification de service, résilience |
| [`endPoint.md`](endPoint.md) | Ce que Vector attend d'Orders.Api |
| [`CaSoft.Erp.USVector.Api/Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http) | La suite de vérification, à rejouer après chaque publication |
| `note_web_alexandre_*.md`, `note_ui_alex.md`, `docs/ui-web/*` | Ce qui est promis au dev web |

**Fin du document**
