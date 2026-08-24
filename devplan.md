# 📱 devplan — Vector (module terrain ambulanciers)

> **Plan unique du module.** Il sert à trois choses : savoir **ce que le module fait**, savoir **ce
> qui reste**, et **ne pas rejouer les décisions déjà tranchées**.
>
> **§1 est écrit sans vocabulaire technique** — il doit rester lisible par quelqu'un qui ne lit pas
> le code. **§3 est écrit pour celui qui va coder** : noms de tables, de routes et de fichiers
> compris. Une fonctionnalité livrée quitte §3 et va enrichir §1. Ce qui a été abandonné en route est
> listé au **§6**, avec son motif, pour ne pas être ré-instruit.
>
> **Statut** : 🟡 en service, chantiers en cours — boucle ambulancier livrée (hors écran de
> rattachement Keycloak), chaîne terrain→facturation livrée et consommée, bascule du référentiel de
> contexte à faire.
> **Prod** : `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`) — trafic servi, jetons Keycloak
> réellement validés depuis le 2026-08-02.
> **Dépôt** : `github.com/esv83/Erp.Vector` (`USVector.sln`) · **Dernière mise à jour** : 2026-08-24.
>
> *Ce document fusionne l'ancien tableau de bord et les plans `mobile_devplan`, `MOB-13_devplan`,
> `TRANSFER_devplan` (2026-08-24). Restent séparés, avec leur propre plan : la carte mutuelle, le
> découplage HTTP, le refactor Result, et la spec fonctionnelle (§7).*

| | Sens |
|---|---|
| 🟢 | livré, vérifié |
| ⏳ | à faire, rien ne bloque |
| ⛔ | bloqué (décision, contenu métier, ou livrable d'un autre module) |
| ⚪ | différé V2 |
| ⚠️ | dette / garde-fou |

---

# 1. Ce que le module fait aujourd'hui

## 1.1 L'application terrain est reconnectée à l'ERP

Après la perte de la base historique, l'API mobile a été reconstruite sur l'ERP **sans toucher au
contrat consommé par le terminal** : mêmes routes, mêmes formats. L'application n'a eu qu'à être
re-pointée. Les données de référence — missions, équipages, véhicules, personnel, patients —
viennent de l'ERP ; tout ce qui est propre au terrain vit dans une base dédiée à Vector.

Vector ne touche plus aux bases des autres modules : **il dialogue avec eux par leurs API**. Un
chantier en cours ailleurs ne casse plus le build ni le déploiement de l'app mobile.

## 1.2 L'ambulancier se connecte avec son compte d'entreprise

Il s'authentifie avec son **compte Keycloak** ; l'application retrouve seule le ou les équipages dont
il fait partie ce jour-là, et ne lui montre que les missions de ces équipages — celle d'un autre
équipage est refusée. Un compte non rattaché reçoit un message explicite l'invitant à contacter la
régulation.

Ses missions lui sont visibles **30 minutes avant sa prise de service**, pour qu'il prépare sa
journée avant de démarrer sa vacation.

## 1.3 Il voit son plan de travail et le fait avancer

- La **liste des missions du jour** : patient, mode de transport, lieux, horaires.
- Le **détail d'une mission** : identité et coordonnées du patient, adresses de départ et d'arrivée
  résolues, horaires, sens et fréquence, service médical destinataire.
- Le marqueur **« mission vue »** : d'un geste il signale qu'il a pris connaissance de la mission ;
  l'icône disparaît et la régulation voit l'heure de prise de connaissance.
- Les **cinq jalons de progression**, horodatés au fil de la course.
- La **signature du patient**, avec un indicateur de présence visible dès la liste.
- Le **conducteur** de l'équipage, consultable et modifiable.

Chaque geste est **projeté vers la régulation** en temps quasi réel : le statut de la mission s'y met
à jour tout seul. Cette synchronisation est **garantie** — les envois en échec sont mis en attente et
rejoués automatiquement, sans jamais bloquer la saisie de l'ambulancier.

## 1.4 Il complète le dossier depuis le terrain

- **Attributs de mission** : commentaires, coordonnées du patient, type de contrat et champs de
  facturation, sous forme de **formulaire dynamique** — le jeu de champs dépend du type retenu.
- **Anomalies** constatées en mission : elles ne bloquent rien, elles partent avec le dossier et
  c'est la facturation qui arbitre.
- **Documents et photos** rattachés à la mission.
- **Carte mutuelle** du patient — photo et saisie des informations
  ([`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md)).

Principe constant : **le terrain n'écrase jamais la donnée officielle de l'ERP.** Ces saisies sont
une couche déclarative que la facturation relit et corrige.

## 1.5 Le dossier part en facturation

- Une mission **clôturée par le régulateur** devient automatiquement transférable.
- Les modules d'aval disposent d'une **file des missions à traiter** — donc de la visibilité sur ce
  qui reste en attente.
- Ils récupèrent **un paquet unique et versionné** rassemblant tout l'enrichissement terrain de la
  mission (jalons, signature, attributs, mutuelle, documents, anomalies) et tirent les **pièces
  jointes à la demande**. **Ce paquet est réellement consommé** : le module de facturation
  l'interroge mission par mission pour construire la journée transmise à AidesNSoft, et la
  certification des trajets s'appuie sur la même file.
- Une fois la mission transférée, **le dossier est gelé côté terrain** : toute tentative de
  modification est refusée avec un message explicite. Le gel s'arme quand la certification déclare la
  mission transférée.

## 1.6 Ce qui tient tout cela debout

- **Un contrat mobile préservé** malgré la reconstruction : les ajouts sont additifs, les anciens
  champs restent servis le temps que l'UI web bascule.
- **Une authentification à un seul point de passage**, avec cache : le chemin chaud ne refait plus
  d'appels réseau pour retrouver l'ambulancier et son équipage.
- **Un code applicatif homogène** : les cas d'usage renvoient un résultat typé plutôt que d'écrire
  dans un présentateur, ce qui rend les erreurs traduisibles en codes HTTP sans logique dans les
  contrôleurs.
- **Un déploiement outillé** : une commande pour la recette, une pour la production, avec
  confirmation à taper, vérification préalable du partage réseau et contrôle que ce qui est publié
  est bien arrivé.

## 1.7 Repères de validation

| Lot | Preuve |
|---|---|
| Socle mobile | contrat legacy exposé (25 routes) ; joblist, détail, signature, timeline validés sur missions réelles |
| Login Keycloak | validé bout-en-bout 2026-07-05 ; jetons réellement validés en prod 2026-08-02 |
| Accès anticipé 30 min | 3 tests · en prod depuis 2026-08-02 |
| Découplage HTTP | isolation de build prouvée (reconstruction sans Orders) |
| Result pattern, vague 1 | 31 cas d'usage migrés, 31 tests, parité HTTP vérifiée |
| Attributs / contrat | 11 tests, validé en base 2026-06-14 |
| Carte mutuelle | 16 tests (2026-06-15) |
| Lieux (champ service, affichage piloté serveur) | 12 tests |
| Transfert, côté Orders et Vector | 24 tests ; schéma appliqué 2026-06-22 |
| Consommation réelle du paquet terrain | mesurée le 2026-08-06 par le module de facturation : 284 missions acquises |

## 1.8 ⚠️ Livré mais pas encore exploité

- **La carte mutuelle n'est jamais remplie en production** (table vide au 23/08/2026) : la chaîne
  serveur fonctionne, mais aucune photo n'arrive. Voir `MUTUELLE_CARD_devplan.md` §3.1 — c'est en
  aval que ça coince, pas dans l'API.
- **Le kilométrage n'est pas transmis** avec le dossier terrain : la facturation attend ce champ pour
  activer son contrôle (§3.4).
- **Le repère de fraîcheur du paquet** (`updatedAt`) est servi mais aucun consommateur ne s'en sert
  encore : ils re-tirent à chaque construction. À garder, sans y investir davantage.

---

# 2. Décisions structurantes (toujours en vigueur)

| # | Décision |
|---|---|
| D1 | **Contrat mobile préservé** : on remplace l'implémentation des repositories, pas les routes ni les DTO. Les ajouts sont additifs. |
| D2 | **Séparation officiel ↔ terrain** : la donnée terrain est déclarative et non fiable par construction ; elle n'écrase jamais l'ERP. |
| D3 | **Accès aux autres modules par API HTTP uniquement** (posture DMZ), base propre à Vector sur le LAN derrière firewall. L'architecture actuelle est conforme ; le durcissement événementiel est une option V2. |
| D4 | **Identités de référence en Guid** (équipage / véhicule / personnel), alignées sur l'ERP. |
| D5 | **Grain de transfert = la mission**, avec le rattachement à sa commande conservé dans le paquet. |
| D6 | **Transfert automatique** des missions clôturées ; l'aval contrôle via la file des non-transférées. Pas de validation régulateur préalable : la facturation agrège et corrige. |
| D7 | **Gel au transfert**, pas à la clôture : tant que la mission est seulement transférable, le terrain peut encore corriger. |
| D8 | **L'aval tire les octets** (signature, photos, documents) depuis Vector.Api — pas de stockage partagé. |
| D9 | **Anomalies non bloquantes** : transférées comme donnée, arbitrées en facturation. |
| D10 | **Temps réel régulateur = persistance + polling** au MVP ; le push est V2. |
| D11 | **`Closed` reste la main du régulateur** : le mobile n'écrit jamais la clôture administrative. |
| D12 | **Photos et documents en base** en V1 ; sortie vers un stockage fichier planifiée V2. |
| D13 | **Le claim `per_id` dans le jeton est écarté** : un turnover le rendrait non invalidable. C'est le cache HTTP qui s'invalide, pas le jeton. |

---

# 3. Ce qui reste

## 3.1 ⏳ OC — Bascule du « contrat » vers le **ContextOrder** (Order = source de vérité)

**Le plus gros reste à faire, et il périme une partie du livré.** Le référentiel de type de mission a
migré côté Order (OC-9) ; Vector doit devenir consommateur, son catalogue autonome et son magasin
d'attributs deviennent des doublons.

**État constaté (2026-08-24)** : aucune occurrence de `contextOrder` dans le code Vector — la bascule
n'est pas commencée. Côté Order tout est en place : tables `ORD_ORDER_CONTEXT`, `_ATTRIBUTE`,
`_ATTR_LINK`, `_ATTR_OPTION`, `_VALUE`, `_ASSIGNMENT`, `_AGENCE`, `_MODE` présentes en base,
endpoints livrés, scripts `038` et `040` joués.
**Sources** : [`../Erp.Order/note_vector_orderContext_mission.md`](../Erp.Order/note_vector_orderContext_mission.md)
(côté intégrateur, §7 pour les attributs) et
[`note_web_alexandre_vector_type_mission.md`](note_web_alexandre_vector_type_mission.md) (contrat UI web).

1. **Client HTTP** (`ErpApi/`, DTO miroir + `IErpReadApiClient` / `IErpWriteApiClient`) :
   - `GET /missions/{missionId}/contextOrder` → `{ contextOrderId, contextOrderCode,
     contextOrderDisplay, locked, availableContextOrders[] }`. La liste est **déjà filtrée** (agence +
     mode de la commande) : ne pas re-filtrer côté Vector.
   - `PATCH /missions/{missionId}/contextOrder` `{ contextOrderId, setBy }` → 204. L'origine `Field`
     est imposée par l'endpoint. Erreurs à propager : **409** (verrou régulateur), **400** (type non
     applicable ou inactif), **404**.
2. **`GET api/Contract/{jobId}`** passe d'un tableau plat à un objet
   `{ locked, contextOrderId, contextOrders[] }`. **Changement de contrat web → à coordonner avec
   Alexandre avant livraison** ; repli possible : garder le tableau et ajouter
   `GET api/Contract/{jobId}/state` pour le `locked`. Renommage `/api/Contract` → `/api/ContextOrder`
   à trancher dans la même passe.
3. **`POST api/Contract/{jobId}`** n'écrit plus `MOB_JOB_CONTRACT` : il relaie le `PATCH` Order.
   Supprimer la règle « défaut = premier context actif » — « non renseigné » devient un état valide.
4. **Attributs** : `GET /missions/{id}/contextOrder/form-structure` et
   `PATCH /missions/{id}/contextOrder/values` remplacent `JobAttributeOverlayRepository.BuildContractType`
   et `.Save`. Le DTO de champ est le miroir de `ClMobileAppFieldModel`, plus **deux champs additifs à
   exposer** : `isReadOnly` (verrou **par champ**, à ne pas confondre avec `locked` qui gèle le type)
   et `readOnlyReason`. Écriture **tout ou rien** : un champ invalide fait échouer le lot (400).
5. **Règles métier portées par Order, à respecter côté API/UI Vector** :
   - `DDN` / `NIR` pré-remplis depuis la fiche bénéficiaire et **verrouillés dès qu'ils sont connus** ;
     une saisie sur fiche vide alimente la fiche. DDN en ISO (date future refusée) ; NIR à clé de
     contrôle vérifiée et **non corrigeable une fois posé** — le faire relire à la saisie.
   - `PMT` / `BT` (prescription, bon de transport) vivent **au niveau commande** : l'aller et le
     retour partagent la case, scellée dès qu'elle est cochée (409 si on tente de la décocher).
   - `locked` gèle **le choix du type**, pas la saisie des attributs.
6. **`FieldDataReader`** : le bloc `attributes` du paquet doit venir d'Order. Le paquet maigrit mais
   **ne disparaît pas** (horaires, signature, documents, anomalies, mutuelle restent servis par Vector).
7. **Dépréciation** : `MOB_CONTRACT_TYPE` / `_ATTRIBUTE` / `_ATTRIBUTE_CONTRACT` / `_ATTRIBUTE_OPTION`,
   `MOB_JOB_CONTRACT`, `MOB_JOB_ATTRIBUTE_VALUE`, `JobAttributeOverlayRepository` et ses 11 tests.
   **Donnée en base au 2026-08-24** : `MOB_JOB_ATTRIBUTE_VALUE` = **2 132 lignes**,
   `MOB_JOB_CONTRACT` = **0 ligne** (aucun type jamais sélectionné ; seed = `STANDARD` + `ART80`).
   → **décider** : abandon pur (hypothèse retenue, ce sont des données de test) ou reprise vers
   `ORD_ORDER_CONTEXT_VALUE`.
8. **Nettoyage** : retirer `JobRepository.UpdateCommande` et `JobRepository.Invoicing`
   (`NotImplementedException`), ainsi que `InvoicingRepositoryStub` et `AttributsRepositoryStub`
   (`NotImplementedStubs.cs`) et leurs enregistrements DI — le `PATCH contextOrder` les remplace.

## 3.2 ⛔ Écran de rattachement compte Keycloak ↔ ambulancier (ex-MOB-4b)

Aujourd'hui le rattachement se fait **par INSERT SQL manuel** dans `PER_KEYCLOAK_MAP`. C'est le seul
maillon manuel de la chaîne d'authentification : sans lui, un ambulancier ne reçoit aucune mission.

**L'hôte de l'écran est à re-trancher** :
- Les **endpoints Orders sont livrés** : `GET` / `PUT` / `DELETE /personnel/{id}/keycloak` et
  `GET /keycloak/users` (`Orders.Api/Endpoints/PersonnelEndpoints.cs`, `KeycloakUsersEndpoints.cs`).
- Le **module Identity possède désormais cette correspondance** : la reprise de `PER_KEYCLOAK_MAP` a
  été jouée le **23/08/2026** (146 pivots, 105 correspondances) et Orders doit à terme appeler
  Identity au lieu d'écrire sa table (`Erp.Identity/DEVPLAN.md` §2.1).
- L'écran Siège envisagé comme hôte (`UcEmployeeKeycloakAccount`) **n'existe plus que dans
  `Archives/`** : l'extension prévue est caduque.

À trancher avant de coder : **l'écran vise Identity** (cohérent avec la cible) **ou** reste sur les
endpoints Orders le temps de la bascule. Dans les deux cas : lister les comptes Keycloak, rechercher
un `PER_PERSONNEL`, persister **via API** (jamais d'écriture directe en base), afficher les
garde-fous (compte déjà lié, personnel déjà lié, conflit 409). ⚠️ Tant que les trois emplacements du
rattachement coexistent, ils divergeront — et l'écart se verra le jour où un ambulancier ne recevra
plus ses missions.

## 3.3 La chaîne vers la facturation — ce qui reste réellement

Le plan d'origine confiait toute l'aval à Certification. **La réalité livrée est un partage** :

| Qui | Ce qu'il fait | État |
|---|---|---|
| **Certification** | Découvre la file (`GET /missions?transferStatus=Transferable`, `ClCertifiableMissionProvider`) et **écrit le retour** (`PUT /missions/{id}/transfer-status` → `Transferred`, `ClOrderTransportStatusUpdater`) | 🟢 livré — c'est **ce write-back qui arme le gel** côté Vector |
| **BillingGateway** | Tire le paquet terrain (`ClVectorFieldDataClient` → `GET /api/missions/{id}/field-data`), agrège avec Orders et Certification, produit le fichier AidesNSoft | 🟢 livré |

Ce qui reste, côté Vector ou à arbitrer :

1. **⛔ `Billed` n'a aucun écrivain.** BillingGateway est **en lecture seule** (aucune écriture vers
   Orders ni Vector, par décision de son module). Le statut `Billed` de `MIS_TRANSFER_STATUS` reste
   donc théorique. À trancher : le faire poser par BillingGateway à la publication d'une journée, ou
   **retirer le palier** de l'énumération. En l'état il n'est ni faux ni utile.
2. **⏳ Endpoint `field-data` en lot.** Le paquet est **unitaire** : un appel HTTP par mission —
   mesuré **14,7 s pour 284 missions** côté facturation, dans un traitement déclenché par un clic.
   La demande est un `field-data` **par période**, à l'image de `for-export` côté Orders. Non engagé
   côté demandeur, mais c'est le seul vrai correctif au coût d'une journée.
3. **⏳ `Kilometers` dans le paquet** : aujourd'hui `null` (cf. §3.4). La facturation attend ce champ
   pour activer son contrôle « kilométrage absent » — une simple ligne de configuration chez elle.
4. **⚠️ Horodatages.** Les jalons Vector sont en **UTC mais ne le déclarent nulle part** (les
   consommateurs le devinent), et **`SIG_DATETIME` est écrit avec `DateTime.Now`**
   (`SignatureRepository.cs`, deux occurrences) — donc en heure locale, contrairement à tout le
   reste. À corriger et à déclarer explicitement dans le contrat du paquet.

## 3.4 ⏳ Reste du périmètre Vector

| Réf | Objet | Détail technique |
|---|---|---|
| MOB-9 (résiduel) | **Parité de contrat** | Déploiement et retrait de l'ancienne `WebApi` sans objet (API en prod sur IIS `/vector`, solution legacy `E:\VB_Projects\MobileApp` absente du disque). Reste à écrire la suite de smoke `.http` couvrant login → joblist → jobdetail → time → signature dans `CaSoft.Erp.Mobile.Api.http`, comme filet de non-régression. |
| MOB-10 | **Kilométrage** | Le km est **équipage/véhicule-scoped** (`crew.Vehicle.SetKilometers`, persisté via `ICrewRepository`, exposé par `KilometersController`) : il n'existe pas de km par mission, d'où `field-data.kilometers = null`. À arbitrer avec la facturation : le km véhicule suffit-il, ou faut-il un relevé début/fin **par mission** (table + saisie mobile + alimentation du paquet) ? |
| MOB-12 | **Fin de service** | `EndOfServiceController` existe (`GET`/`POST`, `ClSetEndOfServiceUseCase`) mais **à re-cadrer** : `MOB_SESSION` n'est plus la source d'authentification depuis Keycloak, la clôture doit viser la **vacation d'équipage côté Orders**. Le `TODO` du contrôleur (permissions de poster une date de fin depuis la régulation) reste ouvert. |
| MOB-14 | **Logs mécaniques + analyses** | `MecanicLogController` / `AnalyzeLogController` / `DataReferenceController` sont exposés mais reposent sur `LogRepositoryStub` et `LogAnalyzeRepositoryStub` (DI, `Program.cs`). À faire : tables `MOB_MECANIQUE_*`, référentiels (acteurs, natures, contraintes), repositories réels. |
| MOB-16 | **Connecteurs Sirus / GpsGate** | Portés et injectés, non recâblés fonctionnellement : positions d'équipage (GpsGate REST) et statuts véhicule (Sirus UDP). Secrets déjà externalisés (`__SET_VIA_ENV__`). |
| Result V2 | **Retrait de l'échafaudage** | Vague 1 faite (31/32) ; reste le straggler `ClSetDriverUseCase`, puis la suppression des types de transition (`ClUseCaseHandler`, `ClWebApiPresenter`, `ClUseCaseBase`…). Cf. [`refactor_result_pattern.md`](refactor_result_pattern.md). |
| Mutuelle P3 | **Extraction automatique** | Pipeline OCR (Claude vision + validation humaine) — statut en base, aucun service écrit. Détail : [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) §3.2. **Sans objet tant que la table reste vide** (§1.8). |
| — | **Tests xUnit Orders du transfert** | Manquants côté module Orders : dérivation `MIS_STATUS` (go→InProgress, terminate→Done), pose automatique de `Transferable` à l'entrée en `Closed` (et reset au recul `Closed→Done` avant transfert), garde-fous monotones de `MarkTransferred` / `MarkBilled`. |
| — | **Relance de clôture** | Alerter les régulateurs des missions **terminées mais non clôturées** — sans quoi elles ne deviennent jamais transférables. Piste : requête ou tableau de bord `?status=Done` côté régulation. |
| DEC-6 / DEC-7 | **Auth de service et résilience des appels sortants** | Détail : [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) §2.1-2.2. En résumé : aucun jeton posé sur les appels à Orders.Api, aucun timeout explicite ni retry sur le **chemin de lecture** (l'écriture est couverte par la file de projection). |

## 3.5 ⚠️ Dette et garde-fous

| Réf | Point | Condition de suppression |
|---|---|---|
| C1 | **`IsAck`** conservé comme alias lecture seule de `IsSeen` (`ClJobListItemModel`) | UI web migrée sur `IsSeen` |
| C2 | **Champs JobDetail legacy** en parallèle des nouveaux (`Schedule`→`ScheduleLabel`, `TransportMode`/`TransportSens`→`TransportModeLabel`, `Departure`/`Arrival`→`PickupLocation`/`DropoffLocation`) | UI web basculée sur les nouveaux champs |
| C3 | **`SelectedDriver` jamais null** (`Guid.Empty` + `""` au lieu de `null`, `ClGetDriverUseCase`) | UI web garde-fou le `null` proprement |
| C4 | **Sur-rapatriement des missions + filtre équipage côté client** : Vector passe `assignedCrewId` mais Orders.Api l'ignore → toute la journée est téléchargée puis filtrée en mémoire (`CrewRepository.FetchJobList`, `HttpErpReadApiClient.ListMissionsAsync`) | Orders.Api honore `assignedCrewId` ([`endPoint.md`](endPoint.md) §4) — retirer le filtre client ou le garder en défense |
| C5 | **Annulation d'un jalon locale seulement** : Vector envoie le snapshot complet, Orders.Api traite `null = ignorer` → l'effacement d'un jalon ne remonte pas à la régulation | Orders.Api bascule en `null = effacé` ([`endPoint.md`](endPoint.md) §3) |
| DET-4 | **Nommage des DTO** du contrat mobile non aligné sur `…DtoIn` / `…DtoOut` | refactor par lots, **aucun impact JSON** |
| DET-3 | **Adresses « non structurées »** : le repli mono-ligne de `ToJobLocation` signale une donnée non normalisée ou une référence orpheline côté ERP (WARNING journalisé) | correctif côté Orders / Address.Api ; surveiller les WARNING pour mesurer l'ampleur |
| — | **Pont sync/async** (`.GetAwaiter().GetResult()`) sur joblist / jobdetail / identité — hérité du contrat legacy synchrone | refonte des interfaces legacy en async |
| — | **Suivi des migrations absent** : `…/Sql/` n'a **aucune table de suivi de schéma**. Constat du module de facturation : `BD_ERP_MOBILE_APP` (prod) et `BD_VECTOR_MOB_APP` (dev) ont divergé **en sens inverse** (l'une avait `MOB_006` sans `MOB_004/005`, l'autre l'inverse) — résolu pour la production le 06/08/2026, **la cause demeure**. Symptôme : un 500 opaque et une journée sans données terrain | ajouter une table de suivi (modèle `__BillingGatewaySchema`) et un contrôle au démarrage |
| — | **Horodatage de signature en heure locale** (`DateTime.Now`) alors que le reste est UTC | corriger + déclarer le fuseau dans le contrat du paquet (§3.3.4) |
| RGPD P4 | Données de santé (documents, carte mutuelle, anomalies) servies par Vector.Api | durcissement rétention / chiffrement / audit |
| — | **Numérotation SQL Orders** : la migration du transfert est référencée `027` dans l'historique et `034` dans le dépôt | réconcilier |

## 3.6 ⚪ Différé (V2 / hors MVP)

- **CREW-2 — accès anticipé à cheval sur minuit** : la fenêtre de 30 min ne franchit pas le changement
  de jour. `ResolveActiveCrewIds` interroge Orders sur la **date du jour**
  (`GET /crews?personnelId=&date=`) ; à 23:50, une vacation démarrant à 00:15 ne remonte pas.
  Correctif : élargir la requête à **J+1** quand `now + EarlyAccessMinutes` change de date, puis
  dédoublonner les crewIds. **Non prioritaire** — décision métier du 2026-08-02 : les vacations de
  nuit ne sont pas concernées.
- **Push temps réel** (SignalR / notifications) en remplacement du polling régulateur, alimenté côté
  LAN, état consolidé sans donnée patient.
- **Durcissement DMZ événementiel** (`Vd-2`, `Vd-3`, `Vd-4`, `Vd-7`, `Vd-8`) : projection de missions
  poussée, Outbox généralisée + bridge + RabbitMQ, contrats d'événements
  `CaSoft.Erp.Integration.Contracts`. *Le socle retenu reste l'API HTTP à travers firewall* — cf.
  [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md).
- **`Vd-1` — base `DB_VECTOR` dédiée** (renommage/relogement, secrets séparés) : **pertinent dès
  maintenant**, seul jalon DMZ non conditionné à la V2.
- **`Vd-6` — photos hors SQL** : sortir `MOB_MUTUELLE_CARD.MMC_IMAGE` et `MOB_DOCUMENT.DOC_CONTENT`
  vers un stockage fichier, base = référence + métadonnées, purge à 3 ans, migration des blobs.
- **`Vd-5` — masquage et visibilité** : `SensitiveDataMaskingMode`, NIR masqué partiel, visibilité de
  l'équipage retour calculée côté interne puis projetée.
- **Assembly de contrats partagé `Orders.Contracts`** (option 4b) — anti-dérive du JSON.
- **Éviction ciblée du cache d'identité** (`Invalidate(sub)`), **mode offline** (cache + synchro
  différée), **géolocalisation avancée**.
- **Renommage** `CaSoft.Erp.USVector.*` → `CaSoft.Erp.Vector.*`.

---

# 4. Où vit quoi

| Donnée | Emplacement | Autorité |
|---|---|---|
| Missions, commandes, équipages, véhicules, personnel, bénéficiaires | Orders (`BD_ERP_SANITAIRE_DEV`), lu par API HTTP | **Orders** |
| Jalons terrain détaillés, signature, anomalies, documents, carte mutuelle, file de projection | Base Vector (`BD_ERP_MOBILE_APP`, tables `MOB_*`) | **Vector** |
| Avancement opérationnel projeté + statut de transfert | Orders (`ORD_MISSION_OPERATIONAL`, `MIS_TRANSFER_STATUS` / `MIS_TRANSFERRED_AT` / `MIS_BILLED_AT`) | **Orders** (Vector pousse, Certification écrit le statut) |
| Type de mission (context) et attributs de facturation | Orders (`ORD_ORDER_CONTEXT*`) — **cible**, bascule Vector à faire (§3.1) | **Orders** |
| Rattachement compte Keycloak ↔ ambulancier | `PER_KEYCLOAK_MAP` (Orders) → **cible : module Identity** (§3.2) | **Identity** |

## 4.1 Migrations SQL

| Base | Script | Contenu | État |
|---|---|---|---|
| Orders | `026_AddKeycloakMap.sql` | `PER_KEYCLOAK_MAP` | 🟢 appliqué |
| Orders | `034_AddMissionOperationalAndTransfer.sql` | `ORD_MISSION_OPERATIONAL` + `MIS_TRANSFER_STATUS` | 🟢 appliqué 2026-06-22 ⚠️ *(référencé `027` dans l'historique)* |
| Vector | `MOB_001_Initial.sql` | session / timeline / signature | 🟢 appliqué |
| Vector | `MOB_002_JobAttributes.sql` | catalogue contrat + overlay | 🟢 appliqué |
| Vector | `MOB_003_MutuelleCard.sql` | carte mutuelle | 🟢 appliqué |
| Vector | `MOB_004_Anomaly.sql` | anomalies | 🟢 appliqué |
| Vector | `MOB_005_Document.sql` | documents | 🟢 appliqué |
| Vector | `MOB_006_OperationalOutbox.sql` | file de projection régulation | 🟢 appliqué |

> État **vérifié le 2026-08-24** sur `BD_ERP_MOBILE_APP` (192.168.1.109) : les 13 tables `MOB_*` sont
> présentes. ⚠️ Rien ne garantit l'alignement des **autres** environnements — il n'existe aucune table
> de suivi de schéma (cf. §3.5).

---

# 5. Configuration & déploiement

- `ConnectionStrings:MobileDb` · `ConnectionStrings:OrdersDb` **(inutilisé depuis le découplage)**.
- `OrdersApi:BaseUrl` (**slash final obligatoire**, PathBase IIS inclus) · `AddressApi:BaseUrl`.
- `Keycloak:{Enabled, Authority, Audience, DisableValidation, RequireHttpsMetadata}` —
  **`DisableValidation=false` en prod**. Toutes ces clés sont réellement lues ; `Audience` sert aussi
  d'**`azp` attendu** (l'audience n'est volontairement pas validée : Keycloak émet `aud=account`, sans
  mapper sur le realm ; c'est l'`azp` qui est contrôlé dans `OnTokenValidated`, signature/issuer/
  expiration restant validés). Une `Authority` vide ou restée au placeholder **empêche le démarrage**,
  avec un message explicite — au lieu d'une série de 401 sans cause lisible.
- `MobileIdentityCache:{PersonnelMinutes=30, ActiveCrewsMinutes=15}` · secrets GpsGate/Sirus
  `__SET_VIA_ENV__`.

## 5.1 Règle de config des serveurs — trois couches à ne pas confondre

C'est la source des régressions d'authentification et de résolution d'adresses :

1. **`web.config`** (serveur, **manuel**, jamais régénéré : `IsTransformWebConfigDisabled=true`) →
   `ASPNETCORE_ENVIRONMENT` + secrets (`ConnectionStrings__*`, `Sirus__Host`, `GpsGate__*`). Survit à
   toute publication.
2. **`appsettings.json`** → **fait partie de la sortie de publication**, donc *écrasable* par un
   déploiement. Porte la **valeur de référence (celle de la prod)**, jamais une valeur de poste local.
3. **`appsettings.{Development,Staging,Production}.json`** → shippés **et** prioritaires sur la base :
   c'est là qu'on décrit les **déviations** par environnement.

> Règle : le défaut sûr est dans la base, les écarts dans l'overlay — **jamais** une valeur éditée à
> la main sur le serveur, qui n'est protégée que par un hasard d'incrémentalité MSBuild.
> Corollaire vérifié le 2026-08-02 : `Development` et `Staging` → `localhost:5100`, `Production` →
> `api.urgencesante.net`.

## 5.2 Déploiement

`.\deploy.ps1 dev` · `.\deploy.ps1 prod` (confirmation à taper ; `-Force` en non-interactif). Profils
`IIS-DevServer` / `IIS-ProdServer` → `\\192.168.1.112\{dev_api,prod_api}\Vector.Api`.
Le script fait un **pré-vol** (partage accessible) avant de publier, vérifie l'horodatage bin↔UNC et
que `appsettings.json` a bien atterri. Publication **portable** (pas de RID, sinon `SqlClient` charge
sa façade « PlatformNotSupported » et toute lecture SQL casse). `app_offline.htm` est posé puis retiré
→ **courte coupure de l'API** à chaque publication.
Prérequis : `net use \\192.168.1.112\prod_api /user:192.168.1.112\DeployApi *`.

---

# 6. Retiré du plan — obsolète ou abandonné

*Conservé uniquement pour ne pas réinstruire ces pistes.*

| Ce qui a disparu | Motif |
|---|---|
| **Accès in-process aux projets Orders** (références projet `Orders.Application` / `Orders.Infrastructure`) | Remplacé par la consommation d'`Orders.Api` en HTTP : isolation de build et posture DMZ. `ConnectionStrings:OrdersDb` est devenu inutilisé. |
| **Table de correspondance `MOB_CREW_MAP`** (équipage `int` ↔ `Guid`) et son arbitrage | Tranché : toutes les identités de référence passent en Guid côté mobile. La table n'a jamais existé. |
| **Accusé de réception distinct** (`MST_ACK_AT`, `ClAckJobUseCase`, flag `IsAck`) | Remplacé par le marqueur **« Mission vue »** (`MST_READ_AT`, `IsSeen`, événement `MissionSeen`), aligné sur la spec fonctionnelle. `MST_ACK_AT` reste dormante ; `IsAck` survit comme alias de compatibilité (C1). |
| **Login déclaratif `GET/POST api/login`** et le jeton Guid de `MOB_SESSION` comme source d'authentification | Remplacés par Keycloak. |
| **Table `MOB_KM`** telle que planifiée | Le kilométrage est équipage/véhicule-scoped ; il n'y a pas de km par mission. Le besoin réel reste à arbitrer (§3.4). |
| **Catalogue autonome de contrats** (`MOB_CONTRACT_*`) et **seed du vrai catalogue métier** | Le référentiel passe côté Order (ContextOrder, filtrage agence/mode, verrou régulateur). Le seed provisoire `STANDARD` + `ART80` ne sera jamais complété. |
| **Purge des valeurs d'attributs orphelines** au changement de contrat | Sans objet : le magasin concerné est déprécié par la bascule §3.1. S'il survivait comme cache local, la purge serait un script ponctuel, pas une fonctionnalité. |
| **Interfaces legacy `IContractTypeRepository` / `IAttributsRepository` / `IInvoicingRepository`** et `JobRepository.UpdateCommande` / `.Invoicing` | Remplacées par un port ciblé, lui-même remplacé par les endpoints ContextOrder. Stubs à retirer (§3.1.8). |
| **`FetchInstructionList` / `AckInstruction` / `GetCrewIdList(date)` / `GetCrewDriver(vehicleId)`** | Aucun équivalent ERP ; hors périmètre, laissés en `NotImplementedException`. |
| **Blocage « migrations `MOB_003/004/005` à exécuter avec un compte db_owner »** | Résolu : tables présentes en base (§4.1). |
| **« Projection du statut de fin vers l'ERP différée, faute de transition côté Orders »** | Résolue : la dérivation `MIS_STATUS` existe côté domaine Orders et Vector la pousse. Seule la clôture `Closed` reste la main du régulateur (D11). |
| **Documents « source PDF ERP »** | Livré autrement : documents et photos stockés côté Vector, servis par Vector.Api. |
| **Lot Certification TRF-12..15 tel que planifié** (découverte → tirage → agrégation → statut, tout chez Certification) | La réalité est un partage Certification / BillingGateway, largement livré (§3.3). Ce qui reste y est décrit. |
| **Restitution de la carte mutuelle « à faire côté Certification »** | Faite, et ailleurs : BillingGateway tire le bloc mutuelle via le paquet terrain. |
| **Extension de l'écran Siège `UcEmployeeKeycloakAccount`** comme hôte du mapping Keycloak | Le module Siège n'existe plus que dans `Archives/` et la correspondance passe au module Identity (§3.2). |
| **Historique de portage legacy** (divergences framework, namespaces renommés, fichiers écartés, warnings compilateur) | Portage terminé et validé ; l'information vit dans `git log`. |
| **Schéma DMZ strict comme cible V1** (interdiction de toute connexion LAN, projections poussées obligatoires) | Vector accède aux API et à sa propre base à travers un firewall : l'architecture actuelle est conforme, le reste devient une option de durcissement V2 (§3.6). |
| **Dette C6 (`Keycloak:DisableValidation=true`), KC-1 (Keycloak en dur), DEP-2 (`appsettings` divergent), DET-1 (champ service concaténé)** | Toutes résolues et vérifiées ; détail dans `git log`. |

---

# 7. Documents voisins

| Doc | Genre | Ce qu'il apporte |
|---|---|---|
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Spec fonctionnelle | Le besoin et le vocabulaire (plan de travail, statuts terrain, isolation par équipage, séparation officiel↔terrain) |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Devplan | Carte mutuelle : capture, restitution, OCR |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Devplan | Découplage HTTP : contrat consommé, auth de service, résilience |
| [`refactor_result_pattern.md`](refactor_result_pattern.md) | Devplan refactoring | Result pattern, vague 2 |
| [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md) | Spec architecture | Cible DMZ événementielle (option V2) |
| [`docs/auth/optimisation-chaine-authentification.md`](docs/auth/optimisation-chaine-authentification.md) | Note conception | Chaîne d'authentification et caches |
| [`endPoint.md`](endPoint.md) | Contrat HTTP | Ce que Vector attend d'Orders.Api |
| [`../Erp.Order/note_vector_orderContext_mission.md`](../Erp.Order/note_vector_orderContext_mission.md) | Note d'intégration | ContextOrder : endpoints, attributs, règles DDN/NIR/PMT/BT |
| `note_web_alexandre_*.md`, `docs/ui-web/*` | Contrats front | Ce qui est promis au dev web |
| `docs/deploiement/*`, `BUG_DISPLAY.MD`, `README.md` | Exploitation | — |

---

**Fin du document**
