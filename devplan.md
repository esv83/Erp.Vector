# Plan de développement — Erp.Vector

> **Édition du** 2026-09-21 (soir) · **En production** `f000ad0` — **constaté par
> `GET /vector/api/version`** : commit, `Tree: clean`, environnement Production · **235 tests verts**
> · Dépôt `github.com/esv83/Erp.Vector` (`USVector.sln`) · Prod `\\192.168.1.112\prod_api\Vector.Api`
> (IIS `/vector`)
>
> ✅ **L'API sait désormais dire ce qu'elle est** : quel commit elle sert, depuis quel arbre, sur
> quelle base, et quels scripts de schéma cette base porte. Trois questions qui, jusqu'au 20/09,
> demandaient de lire un `.pdb` sur un partage — et dont deux incidents étaient nés.
>
> ⏳ **Ce qui reste ici est modeste ou attend une décision.** Le plus lourd est la protection des
> données du patient ; le plus bloquant n'est pas chez nous — voir « Ce qui attend ailleurs ».
>
> Ce document **se régénère** au prompt *« compact devplan »* et ne porte **que l'ouvert**. Le
> **pourquoi** vit dans [`decided.md`](decided.md), à lire avant de coder ; le **fait**, horodaté et
> constaté, dans [`delivered.md`](delivered.md).
>
> **Règle de travail** : neutre ou additif, jamais de rupture du contrat que consomme l'app web — elle
> n'est pas déployée en même temps que l'API.

**Cinq rubriques, communes à tous les dépôts de l'ERP.** Le format est partagé ; les données ne le
sont jamais.

## 📍 Point de reprise — 21/09 : ce qui tourne se demande au lieu de se déduire

**Publié à 09:11, et constaté autrement que d'habitude** : `api/version` a répondu `f000ad0`,
`Tree: clean`. Le schéma s'est annoncé seul au démarrage — *8 scripts sur 8, dernier `MOB_010`* — et
`deploy.ps1` sait désormais refuser une publication dont le **commit servi** n'est pas celui qu'on
vient de poser.

> ⚖️ **Ce que la journée a réparé n'est pas une panne, c'est une cécité.** Les 13 et 15/09, la
> production servait autre chose que ce que le partage portait, et aucun contrôle sur les fichiers ne
> pouvait le voir.

**Trois choses attendent d'être vues, pas d'être écrites** : un premier refus de conducteur en `WARN`,
la première passe de la facturation sous ce binaire, et le fichier de la sonde anonyme qui se créera
au premier appel. **Une attend une décision** : la lecture automatique des cartes, en place et inerte.

**Orders a accepté** *(21/09)* la lecture par lot que nous demandions — `POST /missions/batch-refs`,
200 identifiants, trois champs — et la range derrière la restriction des types de commande, qui est le
blocage que nous leur avions déclaré.

---
# 1. Fonctionnalités livrées

*Ce que le module sait faire **en production**, en une page. Le récit daté vit dans
[`delivered.md`](delivered.md), à jour au 21/09.*

| Domaine | Ce que le module apporte aujourd'hui |
|---|---|
| **Se connecter** | Compte d'entreprise · l'app retrouve seule l'équipage du jour et fait choisir quand il y en a plusieurs · missions visibles **30 min avant la prise de service** · un accès refusé **dit pourquoi et quoi faire** |
| **Confirmer sa prise de service** | L'app dit **qu'une confirmation attend**, même la veille, et l'ambulancier confirme **sans le courriel** |
| **Voir son travail du jour** | Missions **engagées** par la régulation · détail : patient, adresses, horaires, sens, service destinataire · affichage des lieux composé par le serveur |
| **Faire avancer la mission** | Cinq étapes horodatées, **annulables** · « mission vue » · signature · conducteur — **un refus dit pourquoi** · tout remonte à la régulation en quasi temps réel, un envoi en échec est rejoué |
| **Compléter le dossier** | Type de mission et informations de facturation servis par la régulation, pré-remplis et verrouillés quand la fiche patient les connaît · un refus arrive **avec son motif** · anomalies, documents, photos |
| **Photographier la carte mutuelle** | Depuis la mission · affichable par les écrans d'Orders et de la facturation · **lecture automatique en place, inerte** tant que la décision n'est pas prise |
| **Passer à la facturation** | Transfert automatique à la clôture · dossier **par lots de 200**, signatures par lots de 50 · dossier **gelé** après transfert |
| **Tenir debout** | Le terrain n'écrase jamais la donnée officielle · API fermée par défaut, jetons exigés · **délais et disjoncteur** sur les appels à Orders · publication **depuis `main` propre et poussé**, **commit servi vérifié** |
| **Se dire** | `api/version` : commit, **état de l'arbre au build**, environnement · `api/version/runtime` : base résolue, drapeaux, **état du schéma** |

### Les réserves à ne pas perdre de vue

- ⚠️ **Deux routes restent anonymes** : l'affichage de la carte mutuelle par les écrans amont, une
  balise `<img src>` ne portant pas de jeton — *voir « Fonctionnalités envisagées en Vn »*.
- ⚠️ **Un ambulancier rattaché depuis Employee seul n'entre pas** : 8 membres sur 244 *(13/09, non
  remesuré)* — *voir « Ce qui attend ailleurs »*.
- ⚠️ **Ouvrir l'app avant que l'équipage soit composé échoue** — 23 min d'attente médiane
  *(juillet-août)*. Le message dit quoi faire.
- ⚠️ **Tous les types de mission sont proposés partout**, faute de règle d'applicabilité. Le
  catalogue en porte **11** et non 7 : relevé par Orders le 21/09, les deux tables de restriction
  étant **toujours vides**. Le défaut n'a pas seulement duré, il s'est élargi.
- ⚠️ **Un n° de sécurité sociale mal tapé part en facturation** : aucun module ne le corrige, et
  l'écran ne le fait pas relire *(demandé au dev web le 26/08)*.

---
# 2. Code bloqué, manquant ou en attente

*Classé par itération de session de codage, **du plus simple au plus complexe**. **L'ordre est un ordre
de difficulté, pas de priorité.** Chaque itération porte entre crochets son **ancienne référence** :
d'autres dépôts la citent encore. **Dans une référence croisée, on nomme la rubrique** — les numéros ne
survivent pas à une réorganisation.*

## Itération 1 — Constater ce qui vient d'être publié *[ex-F1, B2, B8]*

| | |
|---|---|
| **Nature** | Constats et mesures — aucun code |
| **Effort** | Au fil de l'eau, puis une petite session pour les mesures |
| **Bloqué par** | **Les événements eux-mêmes** : ils arrivent quand ils arrivent |

**À voir passer** — le code est en production depuis le 21/09 à 09:11 :
- **un refus de conducteur** : `WARN` avec la phrase d'Orders, et le motif dans le corps du 400 ;
- **une passe de la facturation** : ses lots de paquets et de signatures sous ce binaire, sans
  401/403, avec les délais et le disjoncteur en place ;
- **le fichier `usvector-surface-anonyme-*.log`**, qui se créera au premier appel anonyme — seules les
  deux routes d'affichage de la carte en produisent encore ;
- **un refus de la garde de publication** : autre branche, ou `main` non poussé — jamais éprouvé en
  réel ;
- **la suite [`Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http)**, à rejouer entièrement une
  fois, identifiants en main : c'est le seul moyen de vérifier aussi **ce qui doit être refusé**.

**À mesurer** : les **étapes de mission vides** (~3 883 annoncées avant le repli d'Orders livré le
23/07 — constater qu'elles ont disparu) et les **adresses « non structurées »**, journalisées une par
une, jamais comptées. 🟢 La base **est joignable depuis le poste de dev**.

## Itération 2 — L'adresse publique de la recette *[relevé le 21/09]*

| | |
|---|---|
| **Nature** | Un contrôle à moitié armé |
| **Effort** | Une minute, dès que l'URL est connue |
| **Bloqué par** | **L'URL de la recette**, que je n'ai pas trouvée (les chemins évidents rendent 404) |

`deploy.ps1` lit `api/version` après la copie pour vérifier **le commit servi**. L'adresse vient du
profil de publication : renseignée pour la production, **vide pour la recette** — le contrôle y est
donc sauté, et le script le dit à chaque passage. Renseigner `VectorVersionUrl` dans
`IIS-DevServer.pubxml`.

## Itération 3 — Dire l'heure qu'il est *[ex-E2]*

| | |
|---|---|
| **Nature** | Données ambiguës transmises à la facturation |
| **Effort** | Une session, **à coordonner avec la facturation** |
| **Bloqué par** | Rien |

Les étapes sont en UTC **sans le déclarer** ; l'heure de signature est écrite **en heure locale**
(`SignatureRepository.cs:34`, `:43`). Même motif à examiner dans `ClMarkMissionSeenUseCase` et
`ClSetDriverUseCase`. **Fin visée** : tout en UTC, fuseau **déclaré** dans le contrat du paquet.

## Itération 4 — Les dettes de forme qui restent *[ex-G2, G5]*

| | |
|---|---|
| **Nature** | Hygiène — aucun changement de contrat |
| **Effort** | Plusieurs petites sessions, à piocher |
| **Bloqué par** | Rien |

Le code mort est parti le 21/09 ; ce qui suit demande du temps, pas du courage :
- **Nommage des DTO** en `…DtoIn` / `…DtoOut` — aucun impact JSON, des centaines de références.
- **Pont synchrone/asynchrone** (`.GetAwaiter().GetResult()`) sur liste, détail, identité, conducteur :
  à défaire en remontant l'asynchrone jusqu'aux cas d'usage, pas au chausse-pied.
- **`IResultUseCase` est synchrone** : les cas d'usage asynchrones n'implémentent aucune interface.

⚖️ **Les alias de compatibilité restent** et ne se retirent pas ici : leur sort est en rubrique
« Fonctionnalités envisagées en Vn », conditionné à la confirmation du front — *cf.
[`decided.md`](decided.md), 21/09*.

## Itération 5 — Le kilométrage dans le dossier transmis *[ex-E1, MOB-10]*

| | |
|---|---|
| **Nature** | Champ attendu par la facturation, toujours vide |
| **Effort** | Une session si le km véhicule suffit ; **plusieurs** s'il faut un relevé par mission |
| **Bloqué par** | 🔴 **Un arbitrage avec la facturation** |

Le kilométrage appartient à l'équipage et au véhicule, pas à la mission. Km du véhicule, ou relevé
début/fin par mission (table, saisie mobile, paquet) ?

## Itération 6 — Activer la lecture automatique des cartes *[ex-F2, P3]*

| | |
|---|---|
| **Nature** | Fonctionnalité en place, **inerte** |
| **Effort** | Poser une clé ; puis l'écran de validation, **côté web** |
| **Bloqué par** | 🔴 **Une décision d'exploitation** : activer, c'est envoyer une **donnée de santé** au fournisseur du modèle |

Tout est publié et dort : file, worker, appel en sortie structurée, colonnes de proposition. Sans clé,
le worker ne démarre pas. **Ce que la décision engage** : ~12 cartes par jour, de l'ordre du centime
par carte, et l'image qui sort du réseau. **Si elle ne doit pas sortir**, seule l'implémentation du
port change — le reste tient.

**Ce que ça rapporterait**, mesuré le 20/09 : le code AMC n'est saisi que **41 fois sur 85**. Ensuite,
l'**écran de validation** côté web
([`note_web_alexandre_carte_mutuelle_ocr.md`](note_web_alexandre_carte_mutuelle_ocr.md)) — sans lui,
une proposition n'atteint jamais un opérateur.

## Itération 7 — Positions et statuts des véhicules *[ex-F3, MOB-16]*

| | |
|---|---|
| **Nature** | Connecteurs portés, jamais recâblés |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | Rien de connu — non réexaminé depuis juillet |

GpsGate (positions, REST) et Sirus (statuts, UDP) sont injectés mais ne servent à rien.

## Itération 8 — Protéger les données du patient *[ex-G7, P4]*

| | |
|---|---|
| **Nature** | RGPD — dette assumée |
| **Effort** | Plusieurs sessions, un seul lot |
| **Bloqué par** | Rien |

Documents, carte mutuelle et anomalies servis par une API exposée : **rétention et purge** (3 ans),
chiffrement au repos, **fermeture des deux routes d'affichage de la carte**, **audit des accès**.
⚠️ Si la lecture automatique est activée, ce lot porte aussi le sort de l'image envoyée au modèle.

## Itération 9 — Ce qui attend ailleurs *[ex-A3, B, C1, C3, E4, F4]*

*Rien à coder ici tant que l'autre partie n'a pas bougé. Non revérifié à cette édition sauf mention,
et **c'est dit**.*

| Entrée | Qui doit bouger | Dernier relevé | En deux mots |
|---|---|---|---|
| **Règle d'applicabilité des types** *[B9]* | Orders + décision métier | 21/09 | **11 types proposés partout** — quatre de plus qu'au relevé d'août, les deux tables de restriction toujours vides. **Orders la place devant tout le reste** parce que nous nous y déclarons bloqués |
| **Lecture groupée « mission → commande → bénéficiaire »** *[B5, suite]* | Orders | 21/09 | ✅ **Acceptée et inscrite à leur plan** : `POST /missions/batch-refs`, 200 identifiants, `{ missionId, orderId, beneficiaryId }`, ligne absente si la mission est inconnue. Une session chez eux, rangée après la règle des types. C'est tout ce qui reste de notre part dans l'acquisition de la facturation (6,8 s par journée) |
| **Exiger un jeton du terrain** | Orders | 21/09 | Plus rien ne l'en empêche : nos **quatre** clients portent le jeton, le dernier depuis le 21/09 |
| **Rattachement des comptes** *[C1]* | Orders, Identity, **RH** | 13/09 | Vector lit `PER_KEYCLOAK_MAP`, que l'écran d'Employee n'alimente pas. Débloqué par la bascule d'Orders sur le carnet d'Identity, elle-même bloquée par **273 personnels sans fiche Employee**. En attendant : [consigne](docs/auth/consigne-rattachement-ambulancier.md) **à transmettre à la régulation et à la RH** |
| **Écrans : motif d'un champ grisé, relecture du NIR, bouton *Réessayer*, motif du refus de conducteur, validation des cartes lues** | dev web | 21/09 | Cinq demandes, toutes contractualisées : [`note_ui_alex.md`](note_ui_alex.md), [`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md), [`note_web_alexandre_carte_mutuelle_ocr.md`](note_web_alexandre_carte_mutuelle_ocr.md), [routes retirées](note_web_alexandre_routes_retirees.md) |
| **Composer les équipages avant la prise de service** *[C3]* | 🔴 régulation — décision | 13/09 | Sans quoi l'accès anticipé de 30 min ne sert à rien. ⚠️ Le filtre d'appartenance (`MobileIdentityResolver.cs:35`) est **volontaire** |
| **`REFERENCE` et `URGENT`** *[B10]* | décision métier | 19/09 | Absents du catalogue ; tout le reste est servi |
| **`Billed` : l'écrire, ou retirer le palier** *[B4, E4]* | 🔴 décision | 13/09 | La facturation est en lecture seule par décision de son module |
| **Tests du transfert côté Orders** *[B6]* · **Relance des missions terminées non clôturées** *[B7]* | Orders | 13/09 | Aucun filet sur la dérivation du statut ; des dossiers n'arrivent jamais en facturation |
| **Présence : qui est connecté** *[F4]* | 🔴 décision + cadrage **RH/RGPD** | 13/09 | Spec sans code : [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md) |

---
# 3. Fonctionnalités envisagées en Vn

*Non engagé, non planifié — avec la condition qui les remettrait sur la table.*

| Sujet | En deux mots | Ce qui la rouvrirait |
|---|---|---|
| **Retirer les alias de compatibilité** *[G2]* | `IsAck` (alias de `IsSeen`), champs historiques du détail, `SelectedDriver` jamais nul, champs typés des lieux. **Conservés délibérément** | **La confirmation du front, champ par champ** : l'écran lit `IsSeen`, les libellés, `PickupLocation`/`DropoffLocation`, l'affichage piloté serveur. Contrat : [`note_ui_alex.md`](note_ui_alex.md) |
| **Fermer les routes d'affichage de la carte** *[M9]* | Image courante et présence restent anonymes : une balise `<img src>` ne porte pas de jeton. **Aucun appel constaté** depuis le 15/09 | Les écrans d'Orders et de la facturation passent à un appel authentifié — sinon, le lot RGPD *(« Protéger les données du patient »)* |
| **Lots en parallèle pour la facturation** | ~3 s de gain, 16 appels simultanés vers Orders — **refusé le 19/09** | La lecture groupée chez Orders, qui rend la question sans objet |
| **Base Vector dédiée** *[Vd-1]* | Seul jalon DMZ non conditionné à la V2 | Pertinent dès maintenant ; personne ne l'a porté |
| **Accès anticipé à cheval sur minuit** *[CREW-2]* | Correctif connu | Les vacations de nuit ne sont pas concernées *(02/08)* |
| **Durcissement DMZ événementiel, push temps réel** *[Vd-2 à Vd-8]* | [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md) | Une exigence d'exposition, ou le polling qui ne suffit plus |
| **Photos hors SQL, masquage** *[Vd-6, Vd-5]* | NIR partiel, équipage retour | Le volume, ou le lot RGPD |
| **Contrats partagés avec Orders** *[4b]* | Écart JSON assumé | Une rupture de contrat constatée |
| **Repère de fraîcheur du dossier** *[E5]* | `updatedAt` est servi, personne ne s'en sert | Un besoin de resynchronisation |
| **Éviction ciblée du cache d'identité, mode hors ligne, géolocalisation avancée, renommage `USVector` → `Vector`** | — | Une demande |

---
# 4. Décisions tranchées

**Elles ont déménagé.** Depuis le 21/09, le *pourquoi* vit dans [`decided.md`](decided.md) — décisions
et pièges déjà payés, **qui s'ajoutent et ne se réécrivent jamais**. Ce plan-ci se régénère : y laisser
des arbitrages revenait à les repasser sous une plume tous les deux jours.

🔴 **`decided.md` se lit avant d'écrire du code dans ce dépôt.**

---
# 5. Journal des livraisons

*Le journal daté vit dans [`delivered.md`](delivered.md), les incidents en fin de document.*

## Ce qui a quitté la rubrique « Code bloqué, manquant ou en attente » à cette édition

| Entrée sortie | Pourquoi |
|---|---|
| **Savoir ce qui tourne** *[G8]* | ✅ En production le 21/09 : `api/version` répond, et `deploy.ps1` la lit |
| **Suivre les migrations SQL** *[G4]* | ✅ `MOB_009` joué ; 8 scripts sur 8, constaté au démarrage |
| **Des appels sortants qui ne pendent pas** *[D, DEC-7]* | ✅ En production ; reste à le voir sur une vraie panne — *« Constater ce qui vient d'être publié »* |
| **Un jeu de requêtes rejouables** *[G3]* | ✅ Écrit ; reste à le jouer une fois en entier — *idem* |
| **La liste des documents charge leurs contenus** · **La fin de service** *[MOB-12]* · **Le code mort** *[G2, G5]* | ✅ En production le 21/09 |
| **Lire la carte mutuelle automatiquement** *[F2, P3]* | 🟡 Reformulée en **« Activer la lecture automatique »** : le code est publié et inerte, il ne reste qu'une décision |

**Entrées neuves** : l'adresse publique de la recette, à renseigner ; et, côté Orders, la lecture
groupée **acceptée** — elle passe d'une demande à porter à une attente datée.

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
