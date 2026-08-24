# 🚑 Vector — devplan terrain

> **Périmètre** : la chaîne complète du terrain — l'app ambulancier reconnectée à l'ERP, les saisies
> faites en mission (attributs, anomalies, documents, signature), et le transfert du dossier vers la
> comptabilité.
>
> **Ce document remplace** `mobile_devplan.md` (socle MOB-0..16), `MOB-13_devplan.md` (attributs /
> contrat) et `TRANSFER_devplan.md` (transfert terrain→compta), fusionnés ici le **2026-08-24**.
> Les fonctions abandonnées en cours de route ne sont plus décrites : elles sont listées au **§5**
> avec leur motif, pour ne pas être ré-instruites.
>
> **Solution** : `USVector.sln` — dépôt `github.com/esv83/Erp.Vector`, branche `main`.
> **En service** : dev et **production** (`\\192.168.1.112\{dev_api,prod_api}\Vector.Api`, IIS `/vector`).
> **Vue d'ensemble tous chantiers** : [`devplan.md`](devplan.md) (tableau de bord). Ce plan-ci est la
> source de détail des trois lots ci-dessus.

| | Sens |
|---|---|
| 🟢 | livré, vérifié |
| ⏳ | à faire, rien ne bloque |
| ⛔ | bloqué (décision, contenu métier, ou livrable d'un autre module) |
| ⚪ | différé V2 |
| ⚠️ | dette / garde-fou |

---

## 1. Ce qui est livré

*Section volontairement non technique : ce que l'ambulancier, le régulateur et la facturation ont
réellement gagné. Le détail d'implémentation des points livrés vit dans le code et l'historique git.*

### 1.1 L'application terrain est reconnectée à l'ERP

La base historique ayant été perdue, l'API mobile a été reconstruite sur l'ERP **sans toucher au
contrat consommé par le terminal** : mêmes routes, mêmes formats. L'application n'a eu qu'à être
re-pointée. Les données de référence (missions, équipages, véhicules, personnel, patients) viennent
de l'ERP ; tout ce qui est propre au terrain vit dans une base dédiée à Vector. L'API est déployée
sur les serveurs de dev et de production et sert le trafic réel depuis le 2 août 2026.

Vector ne touche plus aux bases des autres modules : il dialogue avec eux **par leurs API**. Un
chantier en cours côté ERP ne casse plus l'app mobile.

### 1.2 L'ambulancier se connecte avec son compte d'entreprise

Le login déclaratif d'origine a été remplacé par l'**identification Keycloak** : l'ambulancier
s'authentifie avec son compte, l'application retrouve seule le ou les équipages dont il fait partie
ce jour-là, et ne lui montre que les missions de ces équipages — la mission d'un autre équipage est
refusée. Un compte non rattaché à un ambulancier reçoit un message explicite l'invitant à contacter
la régulation. Validé de bout en bout le 5 juillet 2026 ; validation réelle des jetons vérifiée en
production le 2 août 2026.

Ses missions lui sont visibles **30 minutes avant sa prise de service**, pour qu'il prépare sa
journée avant de démarrer sa vacation.

### 1.3 Il voit son plan de travail et le fait avancer

- La **liste des missions du jour**, avec patient, mode de transport, lieux et horaires.
- Le **détail d'une mission** : identité et coordonnées du patient, adresses de départ et
  d'arrivée résolues, horaires, sens et fréquence du transport.
- Le marqueur **« mission vue »** : d'un geste, l'ambulancier signale qu'il a pris connaissance de
  la mission ; l'icône disparaît et la régulation voit l'heure de prise de connaissance.
- Les **cinq jalons de progression** de la mission (vue, en route, sur place, terminée…),
  horodatés au fil de la course.
- La **signature du patient**, avec un indicateur de présence visible dès la liste.

Chaque geste terrain est **projeté vers la régulation** en temps quasi réel : le statut
administratif de la mission s'y met à jour tout seul. Cette synchronisation est **garantie** — les
envois qui échouent sont mis en attente et rejoués automatiquement, sans jamais bloquer la saisie de
l'ambulancier.

### 1.4 Il complète le dossier depuis le terrain

- **Attributs de mission** : commentaires, coordonnées du patient, type de contrat et champs de
  facturation, présentés sous forme de **formulaire dynamique** (le jeu de champs dépend du type
  retenu, chaque champ portant son libellé, son type de saisie et ses options).
- **Anomalies** constatées en mission (téléphone, adresse, patient, impossibilité…) : elles ne
  bloquent rien, elles partent avec le dossier et c'est la facturation qui arbitre.
- **Documents et photos** rattachés à la mission.
- **Carte mutuelle** du patient (plan dédié : [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md)).

Principe constant : **le terrain n'écrase jamais la donnée officielle de l'ERP.** Ces saisies sont
une couche terrain, déclarative, que la comptabilité relit et corrige.

### 1.5 Le dossier part en comptabilité

- Une mission **clôturée par le régulateur** devient automatiquement transférable.
- La facturation dispose d'une **file des missions à transférer** — donc de la visibilité sur ce qui
  reste en attente.
- Elle récupère **un paquet unique et versionné** rassemblant tout l'enrichissement terrain de la
  mission (jalons, signature, attributs, mutuelle, documents, anomalies), et tire les **pièces
  jointes à la demande**.
- Une fois la mission transférée, **le dossier est gelé côté terrain** : toute tentative de
  modification est refusée avec un message explicite.
- Un **repère de fraîcheur** accompagne le paquet : tant que la mission n'est pas transférée, la
  facturation sait si le terrain a modifié quelque chose depuis son dernier tirage.

### 1.6 Repères de validation

| Lot | Preuve |
|---|---|
| Socle mobile (MOB-0..3, 5..8, 11) | contrat legacy exposé (25 routes), joblist/détail/signature/timeline validés sur missions réelles |
| Login Keycloak (MOB-4a) | validé bout-en-bout 2026-07-05, jetons réellement validés en prod 2026-08-02 |
| Attributs / contrat (MOB-13.1→13.11) | 11 tests, validé en BD le 2026-06-14, déployé dev |
| Transfert, lot Orders (TRF-1..4) | schéma appliqué 2026-06-22, build vert |
| Transfert, lot Vector (TRF-5..11) | 24 tests, gel et paquet consolidé livrés |
| Synchro régulation garantie | file d'attente de projection + worker (anti-rafale 5 s, retry) |
| Migrations BD Mobile | **toutes appliquées** — vérifié le 2026-08-24 sur `BD_ERP_MOBILE_APP` (13 tables `MOB_*`, dont anomalies, documents, mutuelle, file de projection) |

---

## 2. Décisions structurantes (toujours en vigueur)

| # | Décision |
|---|---|
| D1 | **Contrat mobile préservé** : on remplace l'implémentation des repositories, pas les routes ni les DTO. Les ajouts sont additifs. |
| D2 | **Séparation officiel ↔ terrain** : la donnée terrain est déclarative et non fiable par construction ; elle n'écrase jamais l'ERP. |
| D3 | **Accès aux autres modules par API HTTP uniquement** (Vector est en posture DMZ). Base propre à Vector sur le LAN, derrière firewall. Conforme à la spec DMZ ; le durcissement événementiel est une option V2 (§4.4). |
| D4 | **Identités de référence en Guid** (équipage / véhicule / personnel), alignées sur l'ERP. |
| D5 | **Grain de transfert = la mission**, avec le rattachement à sa commande conservé dans le paquet. |
| D6 | **Transfert automatique** des missions clôturées ; la facturation contrôle via la file des non-transférées. Pas de validation régulateur préalable : la facturation agrège et corrige. |
| D7 | **Gel au transfert**, pas à la clôture : tant que la mission est seulement transférable, le terrain peut encore corriger. |
| D8 | **La compta tire les octets** (signature, photos, documents) depuis Vector.Api — pas de stockage partagé. |
| D9 | **Anomalies non bloquantes** : transférées comme donnée, arbitrées en facturation. |
| D10 | **Temps réel régulateur = persistance + polling** au MVP ; le push est V2. |
| D11 | **`Closed` reste la main du régulateur** : le mobile n'écrit jamais la clôture administrative. |
| D12 | **Photos et documents en base** en V1 ; sortie vers un stockage fichier planifiée V2. |

---

## 3. Ce qui reste — détail technique

### 3.1 ⏳ OC — Bascule du « contrat » vers le **ContextOrder** (Order = source de vérité)

**Le plus gros reste à faire, et il périme une partie du livré MOB-13.** Le référentiel de type de
mission a migré côté Order (OC-9). Vector doit devenir consommateur ; son catalogue autonome et son
magasin d'attributs deviennent des doublons.

**État constaté (2026-08-24)** : aucune occurrence de `contextOrder` dans le code Vector — la
bascule n'est pas commencée. Côté Order tout est en place : tables `ORD_ORDER_CONTEXT`,
`_ATTRIBUTE`, `_ATTR_LINK`, `_ATTR_OPTION`, `_VALUE`, `_ASSIGNMENT`, `_AGENCE`, `_MODE` présentes en
BD, endpoints livrés, scripts `038` et `040` joués.
**Sources** : [`Erp.Order/note_vector_orderContext_mission.md`](../Erp.Order/note_vector_orderContext_mission.md)
(intégrateur Vector, §7 pour les attributs) et
[`note_web_alexandre_vector_type_mission.md`](note_web_alexandre_vector_type_mission.md) (contrat UI web).

À implémenter :

1. **Client HTTP** (`ErpApi/`, DTO miroir + `IErpReadApiClient` / `IErpWriteApiClient`) :
   - `GET /missions/{missionId}/contextOrder` → `{ contextOrderId, contextOrderCode,
     contextOrderDisplay, locked, availableContextOrders[] }`. La liste est **déjà filtrée**
     (agence + mode de la commande) : ne pas re-filtrer côté Vector.
   - `PATCH /missions/{missionId}/contextOrder` `{ contextOrderId, setBy }` → 204. L'origine `Field`
     est imposée par l'endpoint. Erreurs à propager : **409** (verrou régulateur), **400** (type non
     applicable ou inactif), **404**.
2. **`GET api/Contract/{jobId}`** : passe d'un tableau plat à un objet
   `{ locked, contextOrderId, contextOrders[] }`. **Changement de contrat web → à coordonner avec
   Alexandre avant livraison** ; repli possible : garder le tableau et ajouter
   `GET api/Contract/{jobId}/state` pour le `locked`. Renommage `/api/Contract` → `/api/ContextOrder`
   à trancher dans la même passe.
3. **`POST api/Contract/{jobId}`** : n'écrit plus `MOB_JOB_CONTRACT`, relaie le `PATCH` Order.
   Supprimer la règle « défaut = premier context actif » : « non renseigné » devient un état valide.
4. **Attributs** : `GET /missions/{id}/contextOrder/form-structure` et
   `PATCH /missions/{id}/contextOrder/values` remplacent `JobAttributeOverlayRepository.BuildContractType`
   et `.Save`. Le DTO de champ est le miroir de `ClMobileAppFieldModel`, plus **deux champs additifs à
   exposer** : `isReadOnly` (verrou **par champ**, à ne pas confondre avec `locked` qui gèle le type)
   et `readOnlyReason`. Écriture **tout ou rien** : un champ invalide fait échouer le lot (400).
5. **Règles métier portées par Order, à respecter côté API/UI Vector** :
   - `DDN` / `NIR` pré-remplis depuis la fiche bénéficiaire et **verrouillés dès qu'ils sont
     connus** ; une saisie sur fiche vide alimente la fiche. DDN en ISO (date future refusée) ; NIR
     à clé de contrôle vérifiée et **non corrigeable une fois posé** — le faire relire à la saisie.
   - `PMT` / `BT` (prescription, bon de transport) vivent **au niveau commande** : l'aller et le
     retour partagent la case, scellée dès qu'elle est cochée (409 si on tente de la décocher).
   - `locked` gèle **le choix du type**, pas la saisie des attributs.
6. **`FieldDataReader`** : le bloc `attributes` de `field-data` doit venir d'Order. Le paquet maigrit
   mais **ne disparaît pas** (horaires, signature, documents, anomalies, mutuelle restent servis par
   Vector).
7. **Dépréciation** : `MOB_CONTRACT_TYPE` / `_ATTRIBUTE` / `_ATTRIBUTE_CONTRACT` / `_ATTRIBUTE_OPTION`,
   `MOB_JOB_CONTRACT`, `MOB_JOB_ATTRIBUTE_VALUE`, `JobAttributeOverlayRepository` et ses 11 tests.
   **Donnée en dev au 2026-08-24** : `MOB_JOB_ATTRIBUTE_VALUE` = **2 132 lignes**,
   `MOB_JOB_CONTRACT` = **0 ligne** (aucun type jamais sélectionné ; seed = `STANDARD` + `ART80`).
   → **décider** : abandon pur (hypothèse retenue, ce sont des données de test) ou reprise vers
   `ORD_ORDER_CONTEXT_VALUE`.
8. **Nettoyage** : retirer `JobRepository.UpdateCommande` et `JobRepository.Invoicing`
   (`NotImplementedException`), ainsi que `InvoicingRepositoryStub` et `AttributsRepositoryStub`
   (`NotImplementedStubs.cs`) et leur enregistrement DI — le `PATCH contextOrder` les remplace.

### 3.2 ⛔ MOB-4b — Écran de rattachement compte Keycloak ↔ ambulancier

Aujourd'hui le rattachement se fait **par INSERT SQL manuel** dans `PER_KEYCLOAK_MAP`. C'est le seul
maillon manuel de la chaîne d'authentification : sans lui, un ambulancier ne reçoit aucune mission.

**Ce qui a changé depuis la rédaction initiale — l'hôte de l'écran est à re-trancher** :
- Les **endpoints Orders sont livrés** : `GET` / `PUT` / `DELETE /personnel/{id}/keycloak` et
  `GET /keycloak/users` (`Orders.Api/Endpoints/PersonnelEndpoints.cs`, `KeycloakUsersEndpoints.cs`).
  Le point 1 du plan initial est donc fait.
- Le **module Identity revendique désormais cette correspondance** : la reprise de
  `PER_KEYCLOAK_MAP` a été jouée le **23/08/2026** (146 pivots, 105 correspondances), et Orders doit
  à terme appeler Identity au lieu d'écrire sa table (cf. `Erp.Identity/DEVPLAN.md` §2.1).
- L'écran Siège envisagé comme hôte (`UcEmployeeKeycloakAccount`) **n'existe plus que dans
  `Archives/`** : l'extension prévue est caduque.

À trancher avant de coder : **l'écran vise Identity** (cohérent avec la cible) **ou** reste sur les
endpoints Orders le temps de la bascule. Dans les deux cas il faut : lister les comptes Keycloak,
rechercher un `PER_PERSONNEL`, persister via API (jamais d'écriture directe en base), et afficher les
garde-fous (compte déjà lié, personnel déjà lié, conflit 409). ⚠️ Tant que les trois emplacements du
rattachement coexistent, ils divergeront — et l'écart se verra le jour où un ambulancier ne recevra
plus ses missions.

### 3.3 ⛔ TRF-12..15 — Lot Certification (autre module)

Rien à faire côté Vector : les deux contrats sont livrés et stables. Reste à écrire dans
`CaSoft.Erp.SanitaryTrypCertification` :

| Ticket | Objet |
|---|---|
| TRF-12 | Worker de **découverte** : `GET /missions?transferStatus=Transferable&from=&to=` sur Orders.Api. |
| TRF-13 | Client HTTP **`GET api/missions/{id}/field-data`** sur Vector.Api : tirer le paquet puis les binaires via leurs `imageUrl` ; mapper vers le modèle Certification (pivot **AMC** pour la mutuelle). |
| TRF-14 | Agrégation / correction en facturation, puis `PUT /missions/{id}/transfer-status` (`Transferred` → `Billed`, transitions monotones). |
| TRF-15 | Re-synchro : mémoriser le `updatedAt` du paquet et re-tirer tant que la mission n'est pas `Transferred`. |

Forme du paquet `ClFieldEnrichmentDtoOut` (versionné, `schemaVersion` + `updatedAt` global) :
`missionId`, `orderId`, `timeline{ack,read,go,onsite,terminate}`, `signature{exists,signedAt,imageUrl}`,
`attributes{contractCode,values[]}` *(→ sera piloté par Order, §3.1.6)*,
`mutuelle{mutuelleName,amcCode,concentrateur,teletransmission,imageUrl,ocrStatus}`,
`kilometers{value}` *(null au MVP, §3.4)*, `documents[{id,category,contentType,imageUrl}]`,
`anomalies[{type,text,reportedAt}]`.

**Couplage assumé** : Certification dépend de Vector.Api **et** d'Orders.Api. Pas de médiateur neutre
au MVP.

### 3.4 ⏳ Reste du périmètre Vector

| Réf | Objet | Détail technique |
|---|---|---|
| MOB-9 (résiduel) | **Parité de contrat** | Le déploiement et le retrait de l'ancienne `WebApi` sont sans objet (API en prod sur IIS `/vector`, solution legacy `E:\VB_Projects\MobileApp` absente du disque). Reste à écrire la suite de smoke `.http` couvrant login → joblist → jobdetail → time → signature dans `CaSoft.Erp.Mobile.Api.http`, comme filet de non-régression du contrat. |
| MOB-10 | **Kilométrage** | Constat TRF-9 : le km est **équipage/véhicule-scoped** (`crew.Vehicle.SetKilometers`, persisté via `ICrewRepository`, exposé par `KilometersController`), il n'existe pas de km par mission — d'où `field-data.kilometers = null`. À arbitrer avec la facturation : lui suffit-il du km véhicule, ou faut-il un relevé début/fin **par mission** (nouvelle table + saisie mobile + alimentation du paquet) ? |
| MOB-12 | **Fin de service** | `EndOfServiceController` existe (`GET`/`POST`, `ClSetEndOfServiceUseCase`) mais **à re-cadrer** : `MOB_SESSION` n'est plus la source d'authentification depuis Keycloak, donc la clôture doit viser la **vacation d'équipage côté Orders**, pas la session mobile. Le `TODO` du controller (permissions de poster une date de fin depuis la régulation) reste ouvert. |
| MOB-14 | **Logs mécaniques + analyses** | `MecanicLogController` / `AnalyzeLogController` / `DataReferenceController` sont exposés mais reposent sur `LogRepositoryStub` et `LogAnalyzeRepositoryStub` (DI, `Program.cs`). À faire : tables `MOB_MECANIQUE_*`, référentiels (acteurs, natures, contraintes), repositories réels. |
| MOB-16 | **Connecteurs Sirus / GpsGate** | Portés et injectés, non recâblés fonctionnellement : positions d'équipage (GpsGate REST) et statuts véhicule (Sirus UDP). Secrets déjà externalisés (`__SET_VIA_ENV__`). |
| — | **Tests xUnit Orders du transfert** | Manquants côté module Orders : dérivation `MIS_STATUS` (go→InProgress, terminate→Done), pose automatique de `Transferable` à l'entrée en `Closed` (et reset au recul `Closed→Done` avant transfert), garde-fous monotones de `MarkTransferred` / `MarkBilled`. |
| — | **Relance de clôture** (différé de DEP-1) | Alerter les régulateurs des missions **terminées mais non clôturées** — sans quoi elles ne deviennent jamais transférables. Piste : requête/dashboard `?status=Done` côté régulation, ou push V2. |

### 3.5 ⚠️ Dette et garde-fous

| Réf | Point | Condition de suppression |
|---|---|---|
| C1 | **`IsAck`** conservé comme alias lecture seule de `IsSeen` (`ClJobListItemModel`) | UI web migrée sur `IsSeen` |
| C2 | **Champs JobDetail legacy** en parallèle des nouveaux (`Schedule`→`ScheduleLabel`, `TransportMode`/`TransportSens`→`TransportModeLabel`, `Departure`/`Arrival`→`PickupLocation`/`DropoffLocation`) | UI web basculée sur les nouveaux champs |
| C3 | **`SelectedDriver` jamais null** (`Guid.Empty` + `""` au lieu de `null`, `ClGetDriverUseCase`) | UI web garde-fou le `null` proprement |
| C4 | **Sur-rapatriement des missions + filtre équipage côté client** : Vector passe `assignedCrewId` mais Orders.Api l'ignore → toute la journée est téléchargée puis filtrée en mémoire (`CrewRepository.FetchJobList`, `HttpErpReadApiClient.ListMissionsAsync`) | Orders.Api honore `assignedCrewId` (cf. `endPoint.md` §4) — retirer le filtre client ou le garder en défense |
| C5 | **Annulation d'un jalon locale seulement** : Vector envoie le snapshot complet, Orders.Api traite `null = ignorer` → l'effacement d'un jalon ne remonte pas à la régulation | Orders.Api bascule en `null = effacé` (`endPoint.md` §3) |
| — | **Pont sync/async** (`.GetAwaiter().GetResult()`) sur les chemins joblist / jobdetail / identité — hérité du contrat legacy synchrone | refonte des interfaces legacy en async |
| — | **Numérotation SQL Orders** : la migration du transfert est référencée `027` dans l'historique et `034` dans le dépôt | réconcilier la numérotation |
| RGPD | Données de santé (documents, carte mutuelle, anomalies) servies par Vector.Api | durcissement rétention / chiffrement / audit — suit MUTUELLE_CARD P4 |

### 3.6 ⚪ Différé V2

- **Push temps réel** (SignalR / notifications) en remplacement du polling régulateur, alimenté côté LAN, état consolidé sans donnée patient.
- **Durcissement DMZ événementiel** (`Vd-2`, `Vd-3`, `Vd-4`, `Vd-7`, `Vd-8` de la spec) : projection de missions poussée, Outbox généralisée + bridge + RabbitMQ, contrats d'événements `CaSoft.Erp.Integration.Contracts`. *Le socle retenu reste l'API HTTP à travers firewall* — cf. [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md).
- **`Vd-1` — base `DB_VECTOR` dédiée** (renommage/relogement de `BD_ERP_MOBILE_APP`, secrets séparés) : **pertinent dès maintenant**, seul jalon DMZ non conditionné à la V2.
- **`Vd-6` — photos hors SQL** : sortir `MOB_MUTUELLE_CARD.MMC_IMAGE` et `MOB_DOCUMENT.DOC_CONTENT` vers un stockage fichier, base = référence + métadonnées, purge à 3 ans, migration des blobs existants.
- **`Vd-5` — masquage et visibilité** : `SensitiveDataMaskingMode`, NIR masqué partiel, visibilité de l'équipage retour calculée côté interne puis projetée.
- **Mode offline** (cache + synchronisation différée), géolocalisation avancée.
- **Renommage** `CaSoft.Erp.USVector.*` → `CaSoft.Erp.Vector.*`.

---

## 4. Où vit quoi

| Donnée | Emplacement | Autorité |
|---|---|---|
| Missions, commandes, équipages, véhicules, personnel, bénéficiaires | Orders (`BD_ERP_SANITAIRE_DEV`), lu par API HTTP | **Orders** |
| Jalons terrain détaillés, signature, anomalies, documents, carte mutuelle, file de projection | BD Mobile (`BD_ERP_MOBILE_APP`, tables `MOB_*`) | **Vector** |
| Avancement opérationnel projeté + statut de transfert | Orders (`ORD_MISSION_OPERATIONAL`, `MIS_TRANSFER_STATUS` / `MIS_TRANSFERRED_AT` / `MIS_BILLED_AT`) | **Orders** (Vector pousse, Certification écrit le statut) |
| Type de mission (context) et attributs de facturation | Orders (`ORD_ORDER_CONTEXT*`) — **cible**, bascule Vector à faire (§3.1) | **Orders** |
| Rattachement compte Keycloak ↔ ambulancier | `PER_KEYCLOAK_MAP` (Orders) → **cible : module Identity** (§3.2) | **Identity** |

Configuration sensible : `OrdersApi:BaseUrl` (**slash final obligatoire**, PathBase IIS inclus),
`AddressApi:BaseUrl`, `ConnectionStrings:MobileDb`, `Keycloak:{Enabled,Authority,Audience,…}`.
Règles de config par environnement et déploiement : [`devplan.md`](devplan.md) §4.

---

## 5. Retiré du plan — obsolète ou abandonné

*Conservé uniquement pour ne pas réinstruire ces pistes.*

| Ce qui a disparu | Motif |
|---|---|
| **Accès in-process aux projets Orders** (référence projet `Orders.Application` / `Orders.Infrastructure`) | Remplacé par la consommation d'`Orders.Api` en HTTP (découplage 4a) : isolation de build et posture DMZ. `ConnectionStrings:OrdersDb` est devenu inutilisé. |
| **Table de correspondance `MOB_CREW_MAP`** (équipage `int` ↔ `Guid`) et l'arbitrage associé | Tranché en MOB-3a : toutes les identités de référence passent en Guid côté mobile. La table n'a jamais existé. |
| **Accusé de réception distinct** (`MST_ACK_AT`, `ClAckJobUseCase`, flag `IsAck`) | Remplacé par le marqueur **« Mission vue »** (`MST_READ_AT`, `IsSeen`, événement `MissionSeen`), aligné sur la spec fonctionnelle. La colonne `MST_ACK_AT` reste dormante ; `IsAck` survit comme alias de compatibilité (C1). |
| **Login déclaratif `GET/POST api/login`** et le jeton Guid de `MOB_SESSION` comme source d'authentification | Remplacés par Keycloak (MOB-4a). |
| **Table `MOB_KM`** telle que planifiée en MOB-10 | Constat TRF-9 : le kilométrage est équipage/véhicule-scoped, il n'y a pas de km par mission. Le besoin réel reste à arbitrer (§3.4). |
| **Catalogue autonome de contrats** (`MOB_CONTRACT_*`) et **seed du vrai catalogue métier (MOB-13.2)** | Le référentiel passe côté Order (ContextOrder, filtrage agence/mode, verrou régulateur). Le seed provisoire `STANDARD` + `ART80` ne sera jamais complété. |
| **MOB-13.12 — purge des valeurs orphelines** au changement de contrat | Sans objet : le magasin concerné (`MOB_JOB_ATTRIBUTE_VALUE`) est déprécié par la bascule §3.1. Si l'overlay survivait comme cache local, la purge deviendrait un script ponctuel, pas une fonctionnalité. |
| **Interfaces legacy `IContractTypeRepository` / `IAttributsRepository` / `IInvoicingRepository`** et `JobRepository.UpdateCommande` / `.Invoicing` | Remplacées par le port ciblé `IJobAttributeOverlay`, lui-même remplacé par les endpoints ContextOrder. Stubs à retirer (§3.1.8). |
| **`FetchInstructionList` / `AckInstruction` / `GetCrewIdList(date)` / `GetCrewDriver(vehicleId)`** | Aucun équivalent ERP ; hors périmètre, laissés en `NotImplementedException`. |
| **Blocage « migrations `MOB_003/004/005` à exécuter avec un compte db_owner »** | Résolu : vérifié le 2026-08-24, toutes les tables existent en BD (mutuelle, anomalies, documents, plus `MOB_OPERATIONAL_OUTBOX`). |
| **« Projection du statut de fin vers l'ERP différée, faute de transition côté Orders »** | Résolu par TRF-2/3/5 : la dérivation `MIS_STATUS` existe côté domaine Orders et Vector la pousse. Seule la clôture `Closed` reste la main du régulateur (D11). |
| **MOB-15 — Documents « source PDF ERP »** | Livré autrement (TRF-10) : documents et photos stockés en BD Mobile, servis par Vector.Api. |
| **Extension de l'écran Siège `UcEmployeeKeycloakAccount`** comme hôte du mapping Keycloak | Le module Siège n'existe plus que dans `Archives/` et la correspondance passe au module Identity (§3.2). |
| **Historique de portage legacy** (divergences framework V2, namespaces renommés, fichiers écartés, warnings BC42105) | Portage terminé et validé ; l'information n'a plus de valeur opérationnelle — elle reste dans l'historique git. |
| **Schéma DMZ strict comme cible V1** (interdiction de toute connexion LAN, projections poussées obligatoires) | Précision de 2026-07 : Vector accède aux API et à sa propre base à travers un firewall. L'architecture actuelle est conforme ; le reste devient une option de durcissement V2 (§3.6). |

---

## 6. Sources fusionnées

| Document | Contenu repris |
|---|---|
| `mobile_devplan.md` | Socle MOB-0..16, login Keycloak, architecture, dette de compatibilité, DMZ |
| `MOB-13_devplan.md` | Attributs de mission, catalogue et overlay, sélection du contrat |
| `TRANSFER_devplan.md` | Décisions de transfert, schéma Orders, endpoints, gel, lots TRF-1..15 |

Documents voisins non fusionnés : [`devplan.md`](devplan.md) (tableau de bord tous chantiers),
[`AppMobile_specifications.md`](AppMobile_specifications.md) (spec fonctionnelle),
[`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md),
[`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md),
[`refactor_result_pattern.md`](refactor_result_pattern.md),
[`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md),
[`endPoint.md`](endPoint.md) (contrat HTTP attendu d'Orders.Api).

---

**Fin du document**
