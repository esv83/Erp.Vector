# Plan de développement — Erp.Vector

> **Édition du** 2026-09-19 · **En production** `dc30612` *(`main`, publié le 15/09 à 18:31, constaté
> par sourcelink le 19/09)* · **172 tests verts** · Dépôt `github.com/esv83/Erp.Vector`
> (`USVector.sln`) · Prod `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`)
>
> 🔴 **Le 15/09, la production a tourné cinq heures sur une branche au lieu de `main`** — cinquième
> publication de ce qui n'était pas prévu. La capture de carte mutuelle a disparu *(17 échecs sur 8
> équipages)*, le correctif de clôture du 13/09 avec elle. Rien dans `deploy.ps1` ne l'empêche
> ⇒ *itération 2*.
>
> ✅ **La facturation présente son jeton depuis le 19/09 à 11:48** : plus un seul appel anonyme de sa
> part. Les quatre routes ouvertes pour elle — dont l'image de la carte mutuelle, donnée de santé —
> peuvent se fermer ⇒ *itération 1*.
>
> Ce document se régénère sur demande — prompt **« compact devplan »** — et il est **relevé sur l'état
> réel du dépôt, de la production et des modules voisins**. **Il ne porte que l'ouvert** : le livré,
> daté, vit dans [`delivered.md`](delivered.md).
>
> **Règle de travail** : on code neutre ou additif, jamais de rupture du contrat consommé par l'app
> web — elle n'est pas déployée en même temps que l'API (D14).

**Cinq rubriques, communes à tous les dépôts de l'ERP.** Le format est partagé ; les données ne le
sont jamais.

## 📍 Point de reprise — 19/09, la sonde a répondu

**La sonde posée le 15/09 sur les routes ouvertes sans jeton a servi dès sa première semaine.** Du 16
au 18/09, elle a compté jusqu'à **7 662 appels anonymes par jour** — tous de la facturation, depuis
`192.168.1.112`. Depuis le 19/09 à 11:48, **zéro** : la facturation passe avec son jeton.

> ⚖️ **Elle dit aussi ce que la fermeture ne cassera pas.** L'app mobile tire déjà la signature et
> l'image de la carte **avec son propre jeton** ; la politique prévue admet les deux. Un seul appel
> sans jeton venu de l'app en quatre jours *(18/09, et il a reçu un 404)*.

**La facturation engage B5** *(19/09)* : Vector est devenu son goulot — ~40 appels par seconde au
plus, 12,8 s sur 18,3 s d'acquisition. Deux itérations neuves *(13 et 14)*.

**La confirmation de prise de service depuis l'app est adoptée** : 40 à 90 consultations par jour,
9 confirmations du 15 au 18/09. **La carte mutuelle arrive** : 26 captures du 13 au 15/09, 13 le 16/09.

⚠️ **Le `nlog.config` du serveur n'est pas celui du dépôt** *(constaté le 19/09)* : la sonde écrit
dans le journal général faute de son fichier dédié. La mesure reste lisible ; la configuration a dérivé
sans que rien le signale.

---
# 1. Fonctionnalités livrées

*Ce que le module sait faire **en production**, en une page. Le récit de chaque livraison vit dans
[`delivered.md`](delivered.md), **à jour au 19/09**. Ce qui est écrit mais pas déployé n'est pas ici.*

| Domaine | Ce que le module apporte aujourd'hui |
|---|---|
| **Se connecter** | L'ambulancier entre avec son **compte d'entreprise** · l'app retrouve seule son ou ses équipages du jour et lui fait choisir celui qu'il occupe · ses missions sont visibles **30 minutes avant sa prise de service** · quand l'accès est refusé, **le message dit pourquoi et quoi faire** — équipage pas encore composé, service pas encore ouvert, service clôturé |
| **Confirmer sa prise de service** | L'app lui dit **qu'une confirmation l'attend**, même la veille, et il confirme **sans passer par le courriel** — utile quand le message n'est jamais arrivé |
| **Voir son travail du jour** | La liste des missions **engagées** par la régulation · le détail : patient, adresses, horaires, sens, service destinataire · l'affichage des lieux est composé par le serveur, identique partout |
| **Faire avancer la mission** | Cinq étapes horodatées, **annulables** · « mission vue », dont la régulation voit l'heure · signature du patient · choix du conducteur · **tout remonte à la régulation en quasi temps réel**, et un envoi en échec est rejoué sans jamais bloquer la saisie |
| **Compléter le dossier** | Type de mission et informations de facturation, **servis par la régulation** : les champs dépendent du type, ceux que la fiche patient connaît arrivent **remplis et verrouillés** · un refus arrive **avec son motif**, dit pour le terrain · anomalies, documents et photos |
| **Photographier la carte mutuelle** | Depuis la mission, avec ses quatre champs · la régulation et la facturation peuvent **l'afficher** sur leurs écrans |
| **Passer à la facturation** | Une mission clôturée par le régulateur devient transférable d'elle-même · la facturation tire **un dossier unique** et les pièces à la demande · une fois transmis, **le dossier est gelé** côté terrain |
| **Tenir debout** | **Le terrain n'écrase jamais la donnée officielle** · l'API est fermée par défaut, chaque exception nommée et justifiée · Vector présente son identité de service à la régulation |

### Les réserves à ne pas perdre de vue

- 🔴 **Quatre routes répondent encore sans jeton**, dont l'image de la carte mutuelle — plus pour
  longtemps *(itération 1)*. Deux autres, pour les écrans de la régulation et de la facturation,
  resteront ouvertes plus longtemps *(rubrique 3)*.
- ⚠️ **Un ambulancier rattaché depuis l'écran d'Employee seul n'entre pas** : 8 membres d'équipage sur
  244 sans compte reconnu *(13/09 — non remesuré)*. Une consigne transitoire existe *(itération 20)*.
- ⚠️ **Ouvrir l'app avant que la régulation ait composé l'équipage échoue** — 23 min d'attente
  médiane, 9 cas au-delà d'une heure *(mesuré sur juillet-août)*. Le message dit désormais quoi faire.
- ⚠️ **Les 7 types de mission sont proposés partout**, faute de règle d'applicabilité *(25/08)*.
- ⚠️ **Un n° de sécurité sociale mal tapé part en facturation** : aucun module ne permet de le
  corriger, et l'écran ne le fait pas relire *(demandé au dev web le 26/08)*.

---
# 2. Code bloqué, manquant ou en attente

*Classé par itération de session de codage, **du plus simple au plus complexe**. Une itération = une
session de travail réaliste. **L'ordre est un ordre de difficulté, pas de priorité** — on prend ce qui
rentre dans le temps disponible. Chaque itération porte entre crochets son **ancienne référence** :
d'autres dépôts et documents la citent encore.*

> ⚖️ **Les deux premières sont aussi les plus urgentes**, et ce n'est pas un hasard : l'une ferme une
> donnée de santé, l'autre empêche de publier autre chose que ce qu'on croit. Toutes deux sont
> petites parce que leur préalable est déjà fait.

## Itération 1 — Fermer les quatre routes de la facturation *[ex-C2, DEC-6]* 🟡 *codée le 19/09 — reste à publier*

| | |
|---|---|
| **Nature** | Sécurité — une donnée de santé et le dossier terrain complet lisibles par qui connaît un identifiant |
| **Effort** | Fait — reste la publication, puis le constat |
| **Bloqué par** | **La publication** |

Le paquet terrain, la signature, les documents et l'image de la carte mutuelle répondaient sans jeton
**uniquement parce que la facturation n'en avait pas**. Elle en a un depuis le 19/09 à 11:48.

**Condition vérifiée avant de coder** — la sonde, le 19/09 jusqu'à 15:54 : **zéro `auth=absent`** sur
la journée. Facturation avec son jeton (615 paquets terrain, 362 signatures après 11:48), app avec le
sien (signature, image de carte), personne sur `documents/content`.

**Codé** : `[AllowAnonymous]` → `[Authorize(Policy = ClKeycloakCallers.ServiceOrMobilePolicy)]` sur les
quatre routes. `AnonymousSurfaceTests` : la surface anonyme ne compte plus que les deux routes
d'affichage de la carte (M9) et le diagnostic ; un test fige que les quatre routes portent la politique
— sans elle, elles retomberaient sur la politique de repli, qui n'admet que l'app, et la facturation
recevrait des 403. La sonde ne mesure plus que les deux routes d'affichage. 180 tests verts.

| Route | Admet désormais |
|---|---|
| `GET api/missions/{id}/field-data` | la facturation ou l'app, avec jeton |
| `GET api/Signature/{id}` | idem |
| `GET api/documents/{id}/content` | idem |
| `GET api/mutuelle-card/{id}/image` | idem |

**Fin** : après publication, aucun 401/403 sur ces routes dans le journal — ni de la facturation
(`192.168.1.112`), ni de l'app (`192.168.1.113`). ⚠️ **Le jour où la facturation tirera les documents
ou l'image de carte**, elle devra y poser son jeton comme sur les deux autres.

**Ensuite** : Orders pourra exiger un jeton du terrain — son plan l'attend de Vector, et les trois
clients de Vector portent le jeton depuis l'itération 8.

## Itération 2 — Rendre impossible une publication hors de `main` *[ex-G8, partie 1]* 🟡 *codée le 19/09 — reste la première publication*

| | |
|---|---|
| **Nature** | Garde de déploiement — **cinquième récidive** |
| **Effort** | Fait — reste à l'éprouver sur une vraie publication |
| **Bloqué par** | **Rien** |

Trois publications incohérentes le 25/08, une depuis un arbre non commité le 13/09, une depuis une
branche le 15/09 — **cinq heures** sans la capture mutuelle ni le correctif de clôture. Chaque fois,
la publication a réussi sans un signal.

**Codé dans `deploy.ps1`** :
- **Garde de dépôt en PROD**, avant tout effet (ni app_offline, ni confirmation) : branche `main`, arbre
  propre **fichiers non suivis compris** (le SDK les compile), `HEAD` **égal** à `origin/main` — ni en
  retard, ni en avance : un commit non poussé ne se reproduit pas. `-Force` ne la saute pas. En DEV,
  simple information. `-CheckOnly` : la garde seule, rien n'est publié.
- **Après copie** : le `.pdb` publié doit annoncer le commit publié (sourcelink), et **tous** les
  `appsettings*.json` **et `nlog.config`** doivent être identiques au dépôt — seul `appsettings.json`
  l'était.

**Cause du `nlog.config` périmé, trouvée** : le SDK Web range `*.config` en `Content`, copié à la
publication en `PreserveNewest` — la copie se fie aux horodatages et peut sauter le fichier. Le csproj
force désormais `CopyToPublishDirectory="Always"` sur `nlog.config` et `appsettings*.json` ; vérifié
par une publication locale sur une cible plus récente et périmée. ⚠️ L'ancien `None Update` du csproj
était **sans effet** sur ce fichier.

**Éprouvé** : refus d'un arbre modifié, information en DEV, copie forcée, lecture du commit dans le
`.pdb`. **Pas encore** : le refus d'une autre branche, d'un `main` non poussé, et le chemin qui passe
— à voir à la prochaine publication, qui recopiera aussi le bon `nlog.config`.

> ⚖️ **La garde vaut mieux que la discipline** : Orders a constaté la même chose sur ses tags. Ce qui
> est vérifié par une machine tient ; ce qui repose sur un geste se perd.

## Itération 3 — Remettre la documentation d'équerre *[ex-G6]* ✅ *close le 19/09 — sort du plan à la prochaine édition*

| | |
|---|---|
| **Nature** | Documentation fausse — et elle oriente du travail |
| **Effort** | Fait, sans code |
| **Bloqué par** | — |

| Document | Ce qui a été corrigé, et ce qui l'établit |
|---|---|
| `README.md` | Réécrit : accès **HTTP** à Orders (aucune référence de projet dans les `.csproj`), IIS `/vector`, authentification, déploiement. Il décrivait l'accès in-process, `/mobile` et « MOB-4 reporté » |
| `docs/deploiement/configuration-keycloak-iis.md` | `Authority`/`Audience` **lus dans la configuration** (`Program.cs:28-60`, KC-1) — la « limitation connue » et le placeholder sont retirés ; `Audience` et `ServiceAzp` ajoutés au récapitulatif |
| `BUG_DISPLAY.MD` §6, §8 | DET-1 en production ; fraîcheur des coordonnées sans objet (Orders, 06/09). Restent les deux vérifications d'exploitation : mission **retour**, lieu **non référencé** |
| `endPoint.md` §5 | `engagedOnly` **honoré par Orders depuis le 15/07** (`b194c3c`, `CrewsEndpoints.cs:500`) |
| `refactor_result_pattern.md`, `plan_correctif_vector_fallback_snapshot.md` | Marqués **terminés**, avec leurs commits |
| `VECTOR_ORDERS_DECOUPLING_devplan.md` | DEC-6 fait dans les deux sens ; attente `assignedCrewId` sans objet (`ListMissionsAsync` n'a plus d'appelant) ; contrat consommé à jour |
| `MUTUELLE_CARD_devplan.md` | Compacté : le livré renvoie à `delivered.md`, « transmettre la note » retiré, `M9` distingue la route qui se ferme avec DEC-6 des deux qui attendent P4 |
| Liens vers `Erp.Order` | `note_vector_orderContext_mission.md` et `note_front_jules_order_context.md` sont dans `Erp.Order/archive/` : liens repointés (3 commentaires de code, 1 note). `feature_order_context_devplan.md` n'existe plus nulle part ; plus aucun lien n'y mène |

> 🪤 **Trouvé en corrigeant** : `AddressApi:BaseUrl` était configurée, listée comme « clé lue »… et
> **aucun code ne la lit** — les adresses arrivent résolues par Orders. Signalé dans le guide et dans
> `delivered.md` §6 ; son retrait rejoint les dettes de forme *(itération 12)*.

## Itération 4 — Dire à l'ambulancier pourquoi le conducteur est refusé *[ex-G9 du 13/09]* 🟡 *codée le 19/09 — reste à publier*

| | |
|---|---|
| **Nature** | Un refus clair d'Orders arrivait au terrain sous forme de panne |
| **Effort** | Fait — reste la publication, puis le constat |
| **Bloqué par** | **La publication** |

Désigner un conducteur après la fin de la vacation est refusé par Orders avec un motif lisible
*(« La vacation s'est terminée à 18:00 … »)*. Vector le journalisait en erreur, le perdait, et
répondait un message technique. L'ambulancier réessayait — **5 fois en 35 secondes** le 13/09.

**Codé** (181 tests verts, +9) : `SetCrewDriverAsync` rend un `CrewDriverWriteResult`
typé — 400/409 refus, 404 équipage inconnu, motif d'Orders conservé, journalisé en **WARN** ; seule
une panne (5xx, réseau) lève encore. Le port `ICrewRepository.Update` rend un
`ClCrewDriverWriteResult`, et `ClSetDriverUseCase` renvoie le motif **tel quel**, repli « Changement
de conducteur refusé par la régulation. ». **Même code 400** pour l'app, seul le texte change (D14) ;
le contrat du front est complété
([`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md)).

**Fin** : après publication, un refus de conducteur apparaît en `WARN` au journal (plus d'`ERROR`), et
le corps du 400 est la phrase d'Orders. ⚠️ Que l'**écran** l'affiche au lieu d'un message générique
dépend du dev web.

## Itération 5 — Trois mesures en production *[ex-F1, B2, B8]*

| | |
|---|---|
| **Nature** | Mesures — aucun code |
| **Effort** | Une petite session |
| **Bloqué par** | ⚠️ Un **compte de lecture** : `ErpAccount` est refusé depuis le poste de dev *(13/09 — non revérifié)*. À défaut, les journaux et le paquet terrain |

- **Taux de remplissage de la carte mutuelle** (cartes / missions) — c'est lui qui dira si l'extraction
  automatique *(itération 16)* vaut la peine.
- **Les étapes de mission vides** : ~3 883 annoncées tant qu'Orders n'avait pas son repli — il l'a
  depuis le 23/07. Constater qu'elles ont disparu.
- **Les adresses « non structurées »** : chaque cas est journalisé, personne n'a compté.

## Itération 6 — Un jeu de requêtes rejouables *[ex-G3, MOB-9]*

| | |
|---|---|
| **Nature** | Outil de vérification |
| **Effort** | Une session |
| **Bloqué par** | Rien |

Le fichier `.http` ne couvre ni la liste, ni les étapes, ni la signature *(13/09)*. **Fin** : connexion
→ liste → détail → étapes → signature, rejouables après chaque publication.

## Itération 7 — Des appels sortants qui ne pendent pas *[ex-D, DEC-7]*

| | |
|---|---|
| **Nature** | Robustesse |
| **Effort** | Une session |
| **Bloqué par** | Rien |

Les deux clients vers Orders n'ont **ni délai court, ni nouvelle tentative, ni disjoncteur** en
lecture — 100 secondes par défaut *(13/09)*. L'écriture est couverte par la file de projection.
⇒ Délai explicite, puis `AddStandardResilienceHandler`, **en gardant** la tolérance au 404.

## Itération 8 — Prendre le gestionnaire de jeton du paquet partagé *[ex-C2, reliquat]* 🟡 *codée le 19/09 — reste à publier*

| | |
|---|---|
| **Nature** | Doublon — une source de vérité au lieu de cinq |
| **Effort** | Fait — reste la publication, puis le constat |
| **Bloqué par** | **La publication** |

Le gestionnaire écrit ici le 13/09 avait été promu dans le paquet le jour même, mêmes règles.

**Codé** : `CaSoft.Identity.Client` **1.4.0** référencé par l'API ; `ServiceAccountTokenHandler` et
`ServiceAccountTokenProvider` **supprimés** ; les **trois** clients Orders passent par
`AddVectorServiceAccountToken`, qui appelle `AddCaSoftServiceAccountToken` du paquet. Le troisième,
la confirmation de prise de service, partait **sans** jeton de service : il serait tombé le jour où
Orders exigera un jeton du terrain.

> ⚖️ **La configuration ne bouge pas.** Le paquet lit la section `Identity` ; Vector garde
> `OrdersApi:ServiceAccount` et la **traduit**. Lire la section du paquet aurait imposé de retoucher à
> la main le `web.config` de production le jour de la publication — sans quoi le jeton disparaissait
> sans erreur. Et la traduction ne passe **que** un compte complet : le paquet s'active sur le seul
> `TokenEndpoint`, que Vector déduit toujours de `Keycloak:Authority` — un poste de dev au secret de
> remplacement aurait demandé un jeton, en erreur, à chaque appel.

**Éprouvé** : 6 tests sur le câblage réel (fabrique de clients + gestionnaire du paquet, realm et
Orders simulés) — inerte sans compte et avec le secret de remplacement, Bearer posé puis réutilisé,
oubli sur 401, realm indisponible non bloquant, traduction des réglages ; publication locale (DLL du
paquet présentes) ; démarrage de l'API en Development. 180 tests verts.

**Fin** : après publication, « JWT validé … azp=erp-vector-api » côté Orders, ou à défaut l'absence de
« Jeton de service indisponible » dans le journal de Vector.

## Itération 9 — Dire l'heure qu'il est *[ex-E2]*

| | |
|---|---|
| **Nature** | Données ambiguës transmises à la facturation |
| **Effort** | Une session, **à coordonner avec la facturation** |
| **Bloqué par** | Rien |

Les étapes sont en UTC **sans le déclarer** ; l'heure de signature est écrite **en heure locale**
(`SignatureRepository.cs:34`, `:43`). Même motif à examiner dans `ClMarkMissionSeenUseCase`,
`ClSetDriverUseCase`. **Fin** : tout en UTC, fuseau **déclaré** dans le contrat du paquet.

## Itération 10 — Savoir ce qui tourne *[ex-G8, partie 2]*

| | |
|---|---|
| **Nature** | Traçabilité d'exploitation |
| **Effort** | Une session |
| **Bloqué par** | Rien — l'itération 2 d'abord |

Le `.pdb` donne le commit, **pas l'état de l'arbre** ; rien ne dit la base à laquelle l'API parle. Un
correctif a déjà été joué sur la mauvaise base Orders sans erreur *(25/08)*. ⇒ Une route réservée
qui expose commit, drapeau « arbre modifié », environnement, **base résolue** (sans identifiants) et
drapeaux en vigueur.

## Itération 11 — Suivre les migrations SQL *[ex-G4]*

| | |
|---|---|
| **Nature** | Divergence de schéma — **a déjà coûté une journée sans données terrain** *(06/08)* |
| **Effort** | Une session, plus une réconciliation |
| **Bloqué par** | Rien |

Aucune table de suivi : prod et dev ont divergé en sens inverse. ⇒ Table de suivi (modèle
`__BillingGatewaySchema`) et **contrôle au démarrage**. À réconcilier : la migration du transfert est
`027` dans l'historique, `034` dans le dépôt.

## Itération 12 — Les dettes de forme *[ex-G2, G5]*

| | |
|---|---|
| **Nature** | Hygiène — aucun changement de contrat |
| **Effort** | Plusieurs petites sessions, à piocher |
| **Bloqué par** | Rien, sauf les alias *(le front)* |

- `ListMissionsAsync` n'a plus aucun appelant : à supprimer.
- `AddressApi:BaseUrl` : configurée, lue par aucun code — la retirer des `appsettings` *(19/09)*.
- Nommage des DTO en `…DtoIn` / `…DtoOut` — aucun impact JSON.
- Pont synchrone/asynchrone (`.GetAwaiter().GetResult()`) sur liste, détail et identité.
- `IResultUseCase` est synchrone : les cas d'usage asynchrones n'implémentent aucune interface.
- **Alias de compatibilité** (`IsAck`, champs historiques du détail, `SelectedDriver` jamais nul,
  champs typés des lieux) — retrait **sur confirmation du front uniquement**.

## Itération 13 — Le dossier terrain en lot *[ex-B5, E3]* 🆕 *engagée le 19/09 à la demande de la facturation*

| | |
|---|---|
| **Nature** | Performance — **Vector freine l'acquisition de la facturation** |
| **Effort** | Une à deux sessions |
| **Bloqué par** | **Rien** — une décision de forme, à prendre en codant |

**Mesuré par BillingGateway le 19/09**, recoupé dans le journal de Vector : Vector plafonne vers **40
appels par seconde**. Passer de 1 à 8 appels simultanés n'a multiplié son débit que par 2,4 — chaque
appel monte à ~180 ms. Vector pèse **12,8 s sur les 18,3 s** d'acquisition d'une journée. La facturation
reste à 8 appels simultanés pour ne pas charger une base qui sert aussi le terrain. *(La mesure de
14,7 s pour 284 missions, citée jusqu'ici, date d'avant l'ajout des signatures à sa boucle.)*

**Ce que coûte un paquet aujourd'hui** (`FieldDataReader.GetAsync`, relevé le 19/09) : **2 appels HTTP à
Orders** (mission, puis commande pour le bénéficiaire) et **6 lectures en base**, mission par mission.

1. **Gain immédiat, indépendant du lot** : `_signature.Fetch(missionId)?.DateTime` charge **l'image
   entière** (~42 Ko) pour n'en lire que la date. Même défaut que celui corrigé sur la carte mutuelle le
   26/08 : une projection des seules métadonnées.
2. **La lecture groupée** — une requête `IN` par silo au lieu de six par mission. **Forme convenue
   avec la facturation le 19/09** : une **liste d'identifiants** de missions (`POST`, plafonnée, sur le
   modèle de `mutuelle-card/presence`) — elle les tient déjà de `for-export`, et une période aurait
   obligé Vector à demander la journée à Orders. **Ses deux exigences fermes** : retrouver chaque
   paquet **par son `MissionId`**, et distinguer **« inconnu de Vector »** (son 404 actuel, qui produit
   une note à l'écran) de **« en erreur »**. Le plafond est à fixer ici et à lui communiquer ; elle
   découpe de son côté. Reste l'aller-retour vers Orders pour la commande et le bénéficiaire : à
   grouper s'il existe une lecture groupée côté Orders, à vérifier avant de coder.
3. **Même politique que les routes fermées** à l'itération 1 : la facturation ou l'app, avec jeton.

**Ce que la facturation lit dans le paquet** *(confirmé le 19/09)* : `Timeline` et `Signature`
(`Exists`, `ImageUrl`, date). `Attributes`, `null` depuis le 13/09, est sans effet chez elle — son code
le tolère ; le lot n'a pas à le porter.

**Fin** : la journée de la facturation s'acquiert en quelques appels, et sa part Vector est remesurée.

## Itération 14 — Les images de signature en lot *[ex-B5, suite]* 🆕

| | |
|---|---|
| **Nature** | Performance — l'autre moitié de la demande, que B5 ne couvrait pas |
| **Effort** | Une session |
| **Bloqué par** | **L'itération 13** — même forme d'appel, à réutiliser |

**144 à 217 signatures par journée, ~42 Ko chacune, 6 à 9 Mo** — une requête par mission aujourd'hui.
La règle D8 ne bouge pas : les octets restent chez Vector, la facturation les tire. Seule change la
granularité : **une liste d'identifiants, paginée** (de l'ordre de 50 images par réponse), pour ne pas
fabriquer une réponse de 9 Mo. **Format convenu le 19/09 : JSON, image en base64** — la facturation la
stocke déjà ainsi (colonne `C61`) ; +33 % sur le fil, accepté. Mêmes exigences qu'à l'itération 13 :
chaque image retrouvée par son `MissionId`, « sans signature » distinct de « en erreur ».

## Itération 15 — Le kilométrage dans le dossier transmis *[ex-E1, MOB-10]*

| | |
|---|---|
| **Nature** | Champ attendu par la facturation, toujours vide |
| **Effort** | Une session si le km véhicule suffit ; **plusieurs** s'il faut un relevé par mission |
| **Bloqué par** | 🔴 **Un arbitrage avec la facturation** |

Le kilométrage appartient à l'équipage et au véhicule, pas à la mission. Km du véhicule, ou relevé
début/fin par mission (table, saisie mobile, paquet) ?

## Itération 16 — Lire la carte mutuelle automatiquement *[ex-F2, P3]*

| | |
|---|---|
| **Nature** | Fonctionnalité neuve |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | **La mesure du remplissage** *(itération 5)* — inutile si aucune carte n'arrive |

Extraction **asynchrone** par un modèle de vision, quatre champs proposés avec leur confiance, puis
**validation humaine** — jamais d'écriture aveugle. À cadrer : où tourne l'appel (DMZ ou LAN), le coût
par carte. Rappel : même validés, ces champs **n'alimentent pas** la colonne mutuelle de facturation.

## Itération 17 — La fin de service *[ex-F3, MOB-12]*

| | |
|---|---|
| **Nature** | Code hérité qui vise la mauvaise cible |
| **Effort** | Un recadrage, puis une ou deux sessions |
| **Bloqué par** | **Le recadrage** |

Le contrôleur vise une session mobile qui n'est plus la source d'authentification : la clôture doit
viser **la vacation côté Orders** — dont la règle est que **le régulateur** connaît l'heure de fin.

## Itération 18 — Positions et statuts des véhicules *[ex-F3, MOB-16]*

| | |
|---|---|
| **Nature** | Connecteurs portés, jamais recâblés |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | Rien de connu — non réexaminé depuis juillet |

GpsGate (positions, REST) et Sirus (statuts, UDP) sont injectés mais ne servent à rien.

## Itération 19 — Protéger les données du patient *[ex-G7, P4]*

| | |
|---|---|
| **Nature** | RGPD — dette assumée |
| **Effort** | Plusieurs sessions, un seul lot |
| **Bloqué par** | Rien ; gagne à suivre l'itération 1 |

Documents, carte mutuelle et anomalies servis par une API exposée : **rétention et purge** (3 ans),
chiffrement au repos, accès fin à l'image de la carte, **audit des accès**.

## Itération 20 — Ce qui attend ailleurs *[ex-A3, B, C1, C3, E4, F4]*

*Rien à coder ici tant que l'autre partie n'a pas bougé. Non revérifié à cette édition sauf mention,
et **c'est dit**.*

| Entrée | Qui doit bouger | Dernier relevé | En deux mots |
|---|---|---|---|
| **Écran : dire pourquoi un champ est grisé, faire relire le n° de sécurité sociale** *[A3]* | dev web | 26/08 | L'API envoie déjà le motif du verrou ; le NIR n'est **jamais** corrigeable après coup |
| **Écran : bouton *Réessayer* au sélecteur** *[C3]* | dev web | 13/09 | Contrat : [`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md). Note des [routes retirées](note_web_alexandre_routes_retirees.md) à transmettre aussi |
| **Composer les équipages avant la prise de service** *[C3]* | 🔴 régulation — décision | 13/09 | Sans quoi l'accès anticipé de 30 min ne sert à rien. ⚠️ Le filtre d'appartenance (`MobileIdentityResolver.cs:35`) est **volontaire** |
| **Rattachement des comptes** *[C1]* | Orders, Identity, **RH** | 13/09 | Vector lit `PER_KEYCLOAK_MAP`, que l'écran d'Employee n'alimente pas. Débloqué par la bascule d'Orders sur le carnet d'Identity, elle-même bloquée par **273 personnels sans fiche Employee**. En attendant : [consigne](docs/auth/consigne-rattachement-ambulancier.md) **à transmettre à la régulation et à la RH** |
| **Règle d'applicabilité des types** *[B9]* | Orders *(itération « Restreindre un type »)* + décision métier | 19/09 | Les 7 types proposés partout. Orders l'a placée en tête par priorité **parce que Vector s'y déclare bloqué** |
| **`REFERENCE` et `URGENT`** *[B10]* | décision métier | 19/09 | Absents du catalogue ; tout le reste est servi. Reconstater sur le terrain avant de clore |
| **`Billed` : l'écrire, ou retirer le palier** *[B4, E4]* | 🔴 décision | 13/09 | La facturation est en lecture seule par décision de son module |
| **Tests du transfert côté Orders** *[B6]* | Orders | 13/09 | Aucun filet sur la dérivation du statut et les gardes du transfert |
| **Relance des missions terminées non clôturées** *[B7]* | Orders | 13/09 | Des dossiers n'arrivent jamais en facturation |
| **Présence : qui est connecté** *[F4]* | 🔴 décision + cadrage **RH/RGPD** | 13/09 | Spec sans code : [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md). Définir « connecté », choisir la topologie |
| **Signaler à la facturation** que ses « 401 » du 27/08 portaient sur deux routes alors absentes | nous | 19/09 | Elles sont en service, anonymes, depuis le 15/09 |

---
# 3. Fonctionnalités envisagées en Vn

*Non engagé, non planifié — avec la condition qui les remettrait sur la table.*

| Sujet | En deux mots | Ce qui la rouvrirait |
|---|---|---|
| **Fermer les routes d'affichage de la carte** *[M9]* | L'image courante et la présence restent anonymes : une balise `<img src>` ne porte pas de jeton. **Aucun appel constaté** en 4 jours | Les écrans d'Order et de la facturation passent à un appel authentifié |
| **Base Vector dédiée** *[Vd-1]* | Seul jalon DMZ non conditionné à la V2 | Pertinent dès maintenant ; personne ne l'a porté |
| **Accès anticipé à cheval sur minuit** *[CREW-2]* | Correctif connu | Les vacations de nuit ne sont pas concernées *(décision du 02/08)* |
| **Durcissement DMZ événementiel, push temps réel** *[Vd-2 à Vd-4, Vd-7, Vd-8]* | [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md) | Une exigence d'exposition, ou le polling qui ne suffit plus |
| **Photos hors SQL, masquage** *[Vd-6, Vd-5]* | NIR partiel, équipage retour | Le volume, ou l'itération 19 |
| **Contrats partagés avec Orders** *[4b]* | Écart JSON assumé | Une rupture de contrat constatée |
| **Repère de fraîcheur du dossier** *[E5]* | `updatedAt` est servi, personne ne s'en sert | Un besoin de resynchronisation |
| **Éviction ciblée du cache d'identité, mode hors ligne, géolocalisation avancée, renommage `USVector` → `Vector`** | — | Une demande |

---
# 4. Décisions tranchées — ne pas les rejouer

*Les décisions appliquées vivent dans [`delivered.md`](delivered.md) §4 — structurantes (D1-D15) et
bascule du contexte. **Aucune décision nouvelle à cette édition.** Ce qui suit est la façon dont ce
plan se tient.*

| Date | Décision, et pourquoi |
|---|---|
| 2026-08-24 | **D14 — on code neutre ou additif.** L'app web n'est pas déployée avec l'API |
| 2026-09-13 | **Le livré sort du plan et entre, daté, dans `delivered.md`** au prompt « compact devplan » |
| 2026-09-19 | **Une attente envers l'amont se vérifie chez l'amont avant d'être reconduite.** Ce plan a attendu d'Orders pendant **huit semaines** un repli livré le 23/07 |
| 2026-09-19 | **Une entrée se cite par son titre**, l'ancienne référence entre crochets : les numéros d'itération changent d'une édition à l'autre |
| 2026-09-19 | **Une édition qui n'a pas tout revérifié le dit**, avec la date du dernier relevé |

---
# 5. Journal des livraisons

*Le journal daté vit dans [`delivered.md`](delivered.md) §2, les incidents au §8.*

## Ce qui a quitté la rubrique 2 à cette édition

| Entrée sortie | Pourquoi |
|---|---|
| **Keycloak et BillingGateway pour l'authentification de service** *[C2, étapes 1 à 3]* | ✅ Client créé, jeton posé, constaté le 19/09 à 11:48. Reste la fermeture *(itération 1)* |
| **Repli sur le snapshot `ORD_ORDER`** *[B2]* | ✅ Livré chez Orders le 23/07. Reste une mesure *(itération 5)* |
| **Attributs « rattachés à rien »** *[B10]* | 🟡 Tous servis sauf `REFERENCE` et `URGENT` *(itération 20)* |
| **Fraîcheur des coordonnées** *[G6]* | ✅ Orders remplace le numéro d'adresse à l'édition depuis le 06/09 |
| **Transmettre la note carte mutuelle au dev web** *[F1]* | ✅ Sans objet : l'app capture depuis le 13/09 |

**Restructuration** : le plan passe des chapitres par thème (A à H) aux itérations par difficulté,
sur le format d'Orders. Les anciennes références restent entre crochets.

---

## Annexe — documents voisins

| Doc | Ce qu'il apporte |
|---|---|
| [`delivered.md`](delivered.md) | Ce que le module fait, journal daté, décisions, configuration, pistes retirées, incidents |
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Le besoin et le vocabulaire |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Carte mutuelle (itérations 5, 16) |
| [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md) | Projection du terrain vers Orders |
| [`TRACABILITE_SAISIES_VECTOR_EXPORT.md`](TRACABILITE_SAISIES_VECTOR_EXPORT.md) | Des saisies Vector aux 91 colonnes de facturation |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Authentification de service, résilience (itérations 1, 7) |
| [`endPoint.md`](endPoint.md) | Ce que Vector attend d'Orders.Api |
| [`docs/auth/diag-404-second-membre-equipage.md`](docs/auth/diag-404-second-membre-equipage.md) | Diagnostic du sélecteur |
| `note_web_alexandre_*.md`, `note_ui_alex.md`, `docs/ui-web/*` | Ce qui est promis au dev web |

**Fin du document**
