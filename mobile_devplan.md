# 📱 CaSoft.Erp.USVector — Plan de développement

> Feature : reconnecter l'API mobile terrain (ambulanciers ↔ régulation) à l'ERP en
> développement, après perte de la base legacy `BD_REGULATION_prod` (piratage).
> Le **schéma** legacy n'est pas perdu (récupérable depuis les entités EF scaffoldées de
> `MobApp.Data.ef`), seule la **donnée** l'est.

> Solution : `USVector.sln` (`E:\VB_Projects\CaSoft.Erp\CaSoft.Erp.MobileApp`).
> Historique : ce plan reprend `mobile_reconnect_devplan.md` (ex-repo Orders), désormais porté ici.

---

## 0. État d'avancement

| Livré | Détail |
|---|---|
| ✅ Squelette de solution | `USVector.sln` + 5 projets Clean Architecture, **build vert**, projets vides. Références in-process vers `Orders.Application` / `Orders.Infrastructure` câblées dans `CaSoft.Erp.USVector.Infrastructure`. |
| ✅ **MOB-0** (2026-06-06) | BD dédiée **`BD_ERP_MOBILE_APP`** créée sur `192.168.1.109,1440` (ErpAccount = db_owner). 3 tables MVP livrées + exécutées (`MOB_001_Initial.sql`). Entités scaffoldées Database First (`scripts/scaffold-tables.ps1`) + `MobileDbContext` manuel (Fluent API). Build vert. |
| ✅ **MOB-1** (2026-06-06) | Portage legacy complet : Framework (51 vb, copié de `UrgenceSanteSolution_V2/0-Framework` → projet `CaSoft.Erp.USVector.Framework`), Domain (28 vb), Contracts (2 vb), Application (116 vb), connecteurs `GpsGate.Connector` + `EmergencyPlatformConnector` (Sirus inclus), 16 controllers + Program.cs. 11 stubs repos (`NotImplementedStubs.cs`, Infrastructure). Secrets GpsGate/Sirus **externalisés** en config (`__SET_VIA_ENV__`). Build vert solution, **API démarre et expose les 25 routes du contrat legacy** (vérifié via swagger.json). |
| ✅ **MOB-2** (2026-06-07) | DI `MobileDbContext` (`ConnectionStrings:MobileDb`, user-secrets en dev). Repos BD Mobile réels : `SignatureRepository` (`MOB_SIGNATURE`), `JobTimeRepository` (`MOB_MISSION_STATE`), `SessionRepository` (`MOB_SESSION`, port `ISessionRepository` ajouté côté Application pour MOB-4/12). Timeline branchée via `IJobRepository.GetJobTime/SaveJobTime` (délégation + **création paresseuse** de la ligne au premier geste ambulancier). TODO legacy résolu : `DELETE api/Signature` réellement câblé. **Validé en réel** contre `BD_ERP_MOBILE_APP` : PUT/GET/DELETE signature, PATCH/GET time (lignes vérifiées en base puis nettoyées). |
| ✅ **MOB-6** (2026-06-08) | Détail mission ERP-backed. `Repositories/Erp/JobRepository.cs` implémente `IJobRepository.GetJob` : compose `IMissionDetailQueryService.GetFullAsync` (mission + adresses pickup/dropoff résolues, MIS-2) + `IOrderQueryService.GetByIdAsync` (mode/sens/fréquence) + `IBeneficiaryQueryService.GetByIdAsync` (identité patient) → `ClJob` via builder. Mapping départ/arrivée en paragraphes (repli `Label` si jointure orpheline), DDN/âge/téléphone. Timeline déléguée au `JobTimeRepository` (délégation identique au stub conservée). `IsExist` câblé. `Save`/`UpdateCommande`/`Invoicing` → MOB-13. DI : stub remplacé. La chaîne `JobDetailController → ClGetJobUseCase → ClJobListCache → IJobRepository` était déjà câblée (aucune modif controller/use case). **Build solution vert.** **Réaligné 2026-06-10** : le refactor ERP de `ClEditOrderDtoOut` (DTO devenu imbriqué `{ order, beneficiary }`) avait cassé le build mobile ; `JobRepository` lit désormais mode/sens/fréquence/bénéficiaire via `order.Order.*` (`TransportModeId`/`HasReturn`/`Frequency`/`BeneficiaryId`). Build revert vert. |
| ✅ **MOB-7** (2026-06-08) | Statuts / temps. La timeline opérationnelle (`GET/PATCH api/time`, jalons ack/read/go/onsite/terminate en `MOB_MISSION_STATE`) reste pleinement fonctionnelle, désormais servie par le vrai `JobRepository` (délégation `GetJobTime/SaveJobTime` → `JobTimeRepository` préservée). **Projection statut fin→ERP différée** : le domaine `ClMission` d'Orders expose `Status` en lecture seule sans méthode de transition (le seul writer actuel est le défaut `ToDo`) ; en faire le mobile le premier writer est une décision ERP à cadrer (`orders_devplan`, module régulation), hors MVP. **Sirus : statu quo** (recâblage MOB-16). |
| ✅ **MOB-8** (2026-06-08) | Signature. CRUD `GET/PUT/PATCH/DELETE api/signature` déjà livré (MOB-2). Net-new : flag de présence `MI_SIGNATURE_EXISTS`. `ISignatureRepository` doté de `Exists(id)` / `ExistingFor(ids)` (clé seule, sans charger le base64). Détail : `JobRepository.GetJob` pose `IsSign` ← présence signature, exposé via `ClJobDetailModel.IsSign` + `ClJobDetailAdapter`. Liste : `ClJobListItemModel.SignatureExists` alimenté par overlay batch `MOB_SIGNATURE` dans `CrewRepository.FetchJobList` (1 requête). Ajouts additifs au DTO de sortie, contrat non cassé. **Build solution vert.** |
| 🛠️ **Correctifs runtime** (2026-07-05) | Mise en service réelle de la boucle sur le serveur de dev (IIS `/vector`, découplage 4a live). Bugs corrigés : **(1)** `OrdersApi:BaseUrl` sans `/` final → le segment `/order` était perdu à la résolution d'URI relative → **500** sur `joblist` ; slash forcé (`OrdersBaseUri`) + `ListCrewIdsAsync` tolère 404 (crews vide = joblist vide). **(2)** `http://localhost/order` inatteignable depuis le process `w3wp` (tombe sur un autre site IIS → 404) ; `OrdersApi__BaseUrl` re-pointé vers l'URL qui répond (override `web.config`, non écrasé par `deploy.ps1`). **(3)** Publication `win-x64` RID-specific → `Microsoft.Data.SqlClient` chargeait sa façade « PlatformNotSupported » (impl Windows dans `runtimes/win` ignorée) → **publication portable** (RID retiré de `IIS-DevServer.pubxml`) → SQL Server OK. **Validé de bout en bout** : joblist renvoie les missions réelles d'un personnel mappé `PER_KEYCLOAK_MAP`. |
| ✅ **« Mission vue » / « Bien reçu »** (2026-07-05) | Depuis la JobList, l'ambulancier signale « bien reçu » → l'icône disparaît. **Sémantique alignée sur la spec (§10)** : marqueur **« Mission vue »** (pas un acquittement distinct — cf. §5). `PATCH /api/joblist` → `ClMarkMissionSeenUseCase` pose `MST_READ_AT` (`ReadTime = Now`), **idempotent** (déjà vue = no-op, horodatage conservé). Flag joblist **`IsSeen`** (= `MST_READ_AT` non nul) masque l'icône. `JobTimeRepository.Save` upsert BD Mobile **+ projette `readAt` vers Orders.Api** (`PUT missions/{id}/operational`, TRF-5) → événement **`MissionSeen`**. Validé réel : `IsSeen` false→PATCH 200→true, idempotence OK, projection sans erreur. **Coordination** : champ joblist `IsAck`→**`IsSeen`** (UI) ; régulation affiche « vue à HH:MM » depuis **`read`**, en liste missions + historique (Orders.Api). |

**MVP boucle ambulancier complété (login MOB-4 mis à part).** Prochaine étape : **MOB-4** (Login, reporté — `ILoginRepository` + résolution équipage ERP via `ICrewQueryService`, token en `MOB_SESSION`) puis **MOB-9** (déploiement sous-app IIS `/mobile` + bascule app mobile + retrait `WebApi`).

**Notes de portage MOB-1** (divergences framework V2 comblées) :
- `ClBusinessListBase(Of E)` (arité 1) ajoutée + `Synchronyze` reconstitué ; `Merge` de `ClBusinessBase` décommenté ; `IUseCaseResponse(Of T)`/`ClUseCaseResponse(Of T)` recréés (+ alias `Result`) ; `ClEntityBase.Id` restauré (`Shadows` sur les Id typés des entités).
- Namespaces renommés : `CaSoft.MobileApp.Domaine`→`CaSoft.Erp.USVector.Domain`, `CaSoft.MobileApp.Business`→`CaSoft.Erp.USVector.Application`, `CaSoft.MobileApp.Modeles`→`CaSoft.Erp.USVector.Contracts`, `CaSoft.Framework.Business[.Framework]`→`CaSoft.Framework`, `WebApi`→`CaSoft.Erp.USVector.Api`.
- Écartés (morts/couplés DAL legacy) : `ClReadJobCommand.cs`, `ClConfig.cs` (IdentityServer4), `WeatherForecast.cs`, injection `BD_REGULATION_PRODContext` du `SignatureController`. Mapping `ClContactModel.ToJobBeneficiary` recréé en extension Application.
- 4 warnings BC42105 assumés (corps legacy commentés — réimplémentation MOB-6/MOB-10/Sirus).

> Connection string dev : `Server=192.168.1.109,1440;Database=BD_ERP_MOBILE_APP;User Id=ErpAccount;Password=***;TrustServerCertificate=True` — clé `ConnectionStrings:MobileDb` (user-secrets / env pool IIS).

---

## 1. Contexte & analyse de l'existant

### Solution legacy `E:\VB_Projects\MobileApp` (Clean Architecture, .NET 8)

| Projet legacy | Langage | Rôle | Sort dans la reconnexion |
|---|---|---|---|
| `WebApi` (MobApp.API) | C# | API REST mobile (~35 endpoints, 13 controllers) | **Remplacée** par `CaSoft.Erp.USVector.Api` |
| `MobApp.Application` | VB | Use cases + services + **interfaces repos** | **Porté** → `CaSoft.Erp.USVector.Application` (on garde les interfaces) |
| `MobApp.Domaine` | VB | Entités métier (`ClJob`, `ClCrew`, `ClVehicle`…) | **Porté** tel quel → `CaSoft.Erp.USVector.Domain` |
| `MobApp.Modeles` | VB | DTOs = **contrat mobile** | **Porté** tel quel → `CaSoft.Erp.USVector.Contracts` (préserve le contrat) |
| `MobApp.Data` + `MobApp.Data.ef` | VB/C# | Repos + `BD_REGULATION_PRODContext` (legacy) | **Remplacés** → `CaSoft.Erp.USVector.Infrastructure` |
| `EmergencyPlatformConnector` / `GpsGate.Connector` / `Sirus.Connector` | VB | Géoloc (GpsGate REST) + régulation (Sirus UDP) | **Portés tels quels** (statu quo) |

### Le point de raccordement

Les controllers dépendent d'**interfaces** (`IJobRepository`, `ICrewRepository`,
`ILoginRepository`, `ISignatureRepository`, `IJobTimeRepository`, `IContactRepository`,
`IInvoicingRepository`, `ILogRepository`, `ILogAnalyzeRepository`) implémentées contre le
DbContext legacy. **On ne remplace que l'implémentation de ces interfaces** : controllers,
domaine, services, DTOs et contrat mobile restent intacts.

### Le gap fonctionnel (ce que l'ERP ne fournit PAS encore)

| Besoin mobile (legacy) | État ERP | Conséquence |
|---|---|---|
| Statut opérationnel **fin** (PC, en route, sur place, en charge, à destination, dispo) | `ORD_MISSION.MIS_STATUS` = 4 statuts **admin** grossiers (ToDo/InProgress/Done/Closed) | Timeline opérationnelle → **BD Mobile** |
| Timestamps mission (ack/read/go/onsite/terminate) | Absent | → BD Mobile |
| Signature (base64) | Absent | → BD Mobile |
| Kilométrage saisi | Absent | → BD Mobile |
| Logs mécaniques + analyses | Absent | → BD Mobile (post-MVP) |
| Attributs de facturation dynamiques (`T_FACTURE_*`) | Absent (pas de modèle contrat dynamique) | → différé (post-MVP) |
| Token de session équipage (`EQ_TOKEN`) | Absent | → BD Mobile (table session) |

---

## 2. Architecture cible (décisions arrêtées)

```
┌─────────────┐   HTTPS (contrat inchangé : mêmes routes + DTOs)
│  App Mobile │ ───────────────────────────────────────────────┐
│ (terrain)   │                                                 │
└─────────────┘                                                 ▼
                                              ┌────────────────────────────────┐
                                              │  CaSoft.Erp.USVector.Api          │
                                              │  ASP.NET Core 8 — sous-app IIS  │
                                              │  /mobile du gateway ERP         │
                                              │  (controllers + domaine + DTOs  │
                                              │   portés de MobApp.*)           │
                                              └───────┬───────────────┬─────────┘
                          in-process (réf. projet)    │               │  EF (BD dédiée)
                  ┌────────────────────────────────────┘               ▼
                  ▼                                          ┌────────────────────┐
        ┌──────────────────────┐                            │  BD Mobile (MOB_*)  │
        │ Orders.Application /  │  (lecture données de        │  sessions/tokens,   │
        │ Orders.Infrastructure│   référence : missions,     │  timeline statuts,  │
        │ (ERP)                │   crew, véhicules,          │  signature, km,     │
        └──────────────────────┘   personnel, bénéficiaires) │  logs mécaniques    │
                                                             └────────────────────┘
        ┌──────────────────────┐
        │ Sirus (UDP régul.)   │  ← connecteurs portés tels quels (statu quo)
        │ GpsGate (géoloc REST)│
        └──────────────────────┘
```

**Décisions :**
1. **`CaSoft.Erp.USVector.Api`** (ASP.NET Core 8) **remplace** `WebApi`. Même contrat
   (routes + DTOs) → l'app mobile est **re-pointée**, zéro changement côté terminal.
2. Accès données de référence ERP **in-process** : `CaSoft.Erp.USVector.Infrastructure` référence
   directement `Orders.Application` / `Orders.Infrastructure`. Pas de hop HTTP, pas de couplage à `Orders.Api`.
3. Données purement mobiles → **BD Mobile dédiée** (DbContext `MobileDbContext`, schéma `MOB_*`),
   référençant les entités ERP par id.
4. **Sirus + GpsGate : statu quo** — connecteurs portés, recâblés en DI, non réécrits.
5. v1 = **MVP boucle ambulancier** : login → liste → détail → statuts/temps → signature.

---

## 2bis. Architecture cible DMZ (enrichissements — `spec_architecture_vector_mission_dmz.md`, 2026-07)

Contrainte structurante : **Vector est exposé aux mobiles (posture DMZ)** et **n'accède jamais directement aux bases des autres modules** (`OrderDb`/`CrewDb`/`CertificationDb`/`BillingDb`).

**Précision retenue (2026-07)** : Vector accède aux **APIs HTTP** des autres modules **et** à sa **propre base `DB_VECTOR` (sur le LAN)** **à travers un firewall très sécurisé**. L'isolation repose donc sur le **firewall + accès API-only** (jamais d'accès direct aux bases des autres modules), pas sur une interdiction de toute connexion vers le LAN. ⇒ **Le découplage 4a actuel (Vector ⇄ `Orders.Api` en HTTP) est CONFORME et retenu.** Le schéma strict de la spec (projections poussées + Outbox/bridge + RabbitMQ) devient une **option de durcissement V2**, pas le V1.

Principe donnée (inchangé) : **`DB_VECTOR` = vérité terrain déclarative, non fiable** (corrigeable/ignorable, comparée aux données certifiées).

### Ce qui relève du module Vector (périmètre de ce plan)
- **`DB_VECTOR` (LAN, derrière firewall)** — base propre de Vector (aujourd'hui `BD_ERP_MOBILE_APP` → à renommer/reloger en `DB_VECTOR`). Contient les données terrain : statuts saisis, données admin, réf. documents, historique terrain. *Option durcissement V2* : tables `VectorMissionProjection` / `VectorOutboxMessage` / `VectorInboxMessage`.
- **Lecture missions** : via les **APIs HTTP** des modules (`Orders.Api`…) à travers le firewall (**4a, en place**). *Option V2* : projection locale `VectorMissionProjection` poussée par un `VectorPublicationWorker` (cache/résilience). Champs utiles : MissionId, CrewId, type, adresses affichables, heure prévue, mode, patient (selon masquage), `SensitiveDataMaskingMode`, `CanEditFieldData`, `AccessUntilStatus`.
- **Remontées terrain** : via les **APIs HTTP d'écriture** des modules à travers le firewall (`PUT missions/{id}/operational`, driver… — **TRF-5, en place**). *Option V2* : `VectorOutboxMessage` transactionnel + `VectorDmzBridgeWorker` (pull LAN) + RabbitMQ + Inbox (asynchrone/résilience/idempotence).
- **Statuts terrain V1** : Mission vue → En route → Sur place → Patient pris en charge → Arrivé destination → **Disponible** (clôture terrain). Retours arrière OK, sauts OK, pas de doublon du même statut.
- **Données admin terrain** : téléphone, NIR (masqué partiel), mutuelle, prescription récupérée/manquante, **photo carte mutuelle**, consignes retour — historisées, non fiables, ne modifient pas la commande.
- **Photo carte mutuelle** : **hors base SQL** — staging temporaire DMZ + référence `VectorDocumentStaging`, transfert contrôlé vers stockage interne par le bridge, purge DMZ.
- **Visibilité équipage retour** : calculée **côté interne** puis **projetée** ; Vector n'affiche que l'autorisé (ne recalcule pas les droits métier).
- **Rétention** : purge auto à **3 ans**.
- **Événements Vector→LAN** : MissionSeen, CrewEnRoute, CrewOnScene, PatientPickedUp, PickupLocationDeparted, DestinationReached, PatientDroppedOff, CrewAvailable, PrescriptionCollected/Missing, FieldAdministrativeDataProvided, MutualCardPhotoUploaded, ReturnInstructionProvided.
- **Événements LAN→Vector (consommés)** : MissionPublishedToVector, MissionUpdatedForVector, MissionRemovedFromVector, MissionAccessClosedForVector, ReturnCrewDataAvailable, VectorVisibilityRulesChanged.
- **Découpage projet cible** : `CaSoft.Erp.Vector.{Domain,Application,Infrastructure,Api,Worker}` + `CaSoft.Erp.Vector.DmzBridge.Worker` (côté LAN).

### Hors périmètre Vector (modules internes LAN — pour info)
`Order/Mission`, `Crew`, `Certification`, `Billing`, `Integration` : Vector ne les touche pas ; il émet/consomme des événements et reçoit des projections. SignalR régulation est **alimenté depuis le LAN** (état consolidé, ≤ 1 min, **jamais** de donnée patient dans les notifications).

### Delta vs état actuel & points à arbitrer
| Sujet | Actuel (MOB-*) | Cible DMZ (spec) | Action |
|---|---|---|---|
| Accès données ERP | Vector appelle **`Orders.Api` en HTTP** (lecture missions/crews, écriture operational/driver) — 4a | **APIs HTTP à travers firewall** (pas d'accès direct aux bases des autres modules) | ✅ **Conforme — retenu.** Projections/Outbox = durcissement V2 optionnel |
| Base | `BD_ERP_MOBILE_APP` (MOB_*) sur SQL interne partagé | **`DB_VECTOR`** propre à Vector (LAN, derrière firewall) | Renommer/reloger en `DB_VECTOR` (base dédiée) |
| **« Mission vue »** (ex-ACK, tranché 2026-07-05) | `MST_READ_AT`, flag `IsSeen`, `MissionSeen` | Spec §10 « Mission vue » | ✅ **Aligné** (acquittement distinct abandonné) |
| **Photo carte mutuelle** (livré P1/P2) | Stockée en **BD Mobile** | **Hors SQL** : staging DMZ → transfert interne → purge | ⚠️ Évolution : sortir la photo du SQL |
| Écritures régulation | `ProjectOperationalAsync` (PUT direct `Orders.Api`, TRF-5) | **API HTTP via firewall (conforme)** ; SignalR état consolidé alimenté LAN | ✅ Conserver ; Outbox = durcissement V2 |
| Nommage projets | `CaSoft.Erp.USVector.*` | `CaSoft.Erp.Vector.*` | Décision de nommage |
| Fiabilité donnée | implicite | **non fiable par défaut**, traçage du niveau de fiabilité | Formaliser (billing trace, audit qualité) |

> La roadmap **MOB-*** (**4a HTTP + `DB_VECTOR` derrière firewall**) reste le **socle retenu**. Le durcissement event-driven (projections poussées, Outbox/bridge, RabbitMQ, SignalR) est une **option V2 à cadrer séparément** (cf. Phase 3). Restent valables indépendamment du firewall : photo carte mutuelle **hors SQL** (§13), masquage/visibilité, purge 3 ans.

---

## 3. Mapping legacy → ERP (données de référence, lues in-process)

| Table legacy | Entité ERP | Notes de mapping |
|---|---|---|
| `T_MISSION_MI` | `ORD_MISSION` (+ `ORD_ORDER`) | Mission opérationnelle ; certains champs (mode, sens, rdv) côté `ORD_ORDER` |
| `T_COMMANDE_CDE` | `ORD_ORDER` | Commande / intention de transport |
| `T_EQUIPAGE_EQ` | `CRW_CREW` (+ `CRW_CREW_MEMBER`) | Équipage en service ; lat/long/position = géoloc (GpsGate), pas ERP |
| `T_VEHICULE_VEH` | `ORD_VEHICLE` | Véhicule / ambulance |
| `T_PERSONNEL_PER` | `PER_PERSONNEL` | Ambulancier |
| `T_CONDUITE_COND` | `CRW_DRIVER_ASSIGNMENT` | Conducteur affecté (existe déjà côté ERP) |
| `T_CONTACT_CONT` | `BEN_BENEFICIARY` | Patient / bénéficiaire |
| `T_FACTURE_TYPE_FTY` + attributs | **Aucun équivalent ERP** | Modèle contrat/facturation dynamique → différé (post-MVP) |

**Identifiants** : `MI_ID`/`ORD_ID` (Guid), `EQ_ID`→`CRW` (Guid côté ERP, vs `int` legacy →
table de correspondance si l'app mobile persiste des `int`), `VEH_ID`/`PER_ID` (int),
`CONT_ID`/`BEN_ID` (Guid). **Risque** : l'app mobile suppose `EQ_ID int` et `CrewId int` dans
le contrat (`ClCrewModel.CrewId: int`) alors que l'ERP utilise des Guid pour le crew → à
arbitrer en MOB-3 (table de correspondance `MOB_CREW_MAP` int↔Guid, ou exposer le Guid).

---

## 4. Itérations

### Phase 0 — Socle (sans logique data réelle)

| # | Itération | Taille | Détail |
|---|---|---|---|
| M0 | ✅ **MOB-0 — Cadrage & schéma BD Mobile** | S | **Livré 2026-06-06**. BD dédiée `BD_ERP_MOBILE_APP` + 3 tables (`MOB_001_Initial.sql`, idempotent, exécuté) : `MOB_SESSION` (token unique + 1 session active max/équipage), `MOB_MISSION_STATE` (timeline 5 jalons alignée `T_JOB_TIME`), `MOB_SIGNATURE` (1:1 mission). Database First : scaffold sélectif (`scripts/scaffold-tables.ps1`) → entités POCO + `MobileDbContext` manuel. |
| M1 | ✅ **MOB-1 — Portage du code legacy** | M | **Livré 2026-06-06**. Tout le legacy porté (`Domain`/`Contracts`/`Application`/connecteurs/controllers + framework `CaSoft.Erp.USVector.Framework`), DI bootstrap, repos stubbés (NotImplemented). Build vert, API démarrée, 25 routes du contrat legacy exposées. Cf. « Notes de portage MOB-1 » (§0). `Sirus.Connector` legacy était un projet vide — le code Sirus vit dans `EmergencyPlatformConnector/Sirus/`. |
| M2 | ✅ **MOB-2 — Repos BD Mobile + DI** | S | **Livré 2026-06-07**. DI `MobileDbContext` + repos réels session/state/signature (cf. §0). `api/Signature` et `api/Time` fonctionnels de bout en bout sur la BD Mobile. |

### Phase 1 — MVP boucle ambulancier

| # | Itération | Taille | Détail |
|---|---|---|---|
| M3 | ✅ **MOB-3 — Mapping de référence ERP** | M | **Livré 2026-06-07**. **MOB-3a** : arbitrage `CrewId` tranché → **toutes les identités de référence (crew/vehicle/personnel) en Guid côté mobile**, alignées sur l'ERP (CRW_ID/VEH_ID/PER_ID Guid). **Pas de `MOB_CREW_MAP`**. Bascule `Integer→Guid` : domaine (`ClCrew`/`ClVehicle`/`ClEmployee`), DTO contrat (`ClCrewModel`/`ClTokenDto`/`ClVehicleModel`/`ClSetDriverCommand`/`ClDriverModel`/`ClCrewListModel`/`ClLoginModel`), interfaces+use cases crew (`intKm`/`logId`/`instructionId` restent `int`), stubs C# + route params controllers. **MOB-3b** : `ErpReferenceMappings.cs` — méthodes d'extension consommant les **DTO publics d'Orders.Application** (pas les entités EF) : `ClVehicleDtoOut→ClVehicle`, `ClPersonnelDtoOut`/`ClCrewMemberDtoOut→ClEmployee`, `ClDriverAssignmentDtoOut→ClLastDriver`, `ClCrewDtoOut(+véhicule résolu)→ClCrew`. Mapping riche Job/Mission/Beneficiary porté par MOB-5/MOB-6 (forme cible exercée là). Build solution vert, aucune régression API (signature/time OK). |
| M4 | 🔄 **MOB-4 — Login Keycloak** (`api/login` refondu) | M | **Identification depuis Keycloak IMPLÉMENTÉE et validée en réel (2026-07-05)** : `sub` → `PER_ID` (`PER_KEYCLOAK_MAP`, Orders, via `GET personnel/by-keycloak/{sub}`) → **crew id actif** (`ResolveActiveCrewIds` / `ResolveActiveCrewId`) → **membres d'équipage avec PER_ID** (`GetCrewFullAsync`). Toute la résolution passe par `IMobileIdentityResolver` (`ResolvePersonnelId` / `ResolveActiveCrewIds` / `ResolveActiveCrewId` / `IsMissionAccessible`). `joblist`/`jobdetail`/`driver` dérivent/contrôlent le crew depuis le `sub` (**401** sans token, **403** si compte non rattaché, **404** si aucun équipage actif). **MOB-4a livré 2026-06-14** (API, build vert), **validé bout-en-bout 2026-07-05** (`sub 416adcc3 → PER_ID → 2 équipages → joblist`). Reste **MOB-4b** (écran admin de peuplement `PER_KEYCLOAK_MAP` — aujourd'hui insertion SQL manuelle). `AutorizeJob` neutralisé remplacé. Cf. §7. |
| M5 | ✅ **MOB-5 — Liste missions** (`GET api/joblist`, `PATCH`) | M | **Livré 2026-06-08**. **Accès ERP in-process câblé** (`AddOrdersInfrastructure` dans Program.cs, `OrdersDb` + `AddressApi:BaseUrl` en config/user-secret). `CrewRepository.FetchJobList` (mobile Infra) : missions du jour assignées via `IMissionQueryService` (filtre `AssignedCrewId`), `ClMissionListItemDtoOut→ClJobListItemModel`, flags `IsAck`/`IsTerminated` overlay depuis `MOB_MISSION_STATE` (1 requête). `FetchInstructionList`→vide (pas d'équivalent ERP). `PATCH` (marquer lu) → `ReadJob`→`JobTimeRepository` (déjà livré MOB-2). **Validé en réel** : 2 missions ERP mappées correctement (patient/mode/labels pickup-dropoff/horaire/RDV), overlay `IsAck` testé (insert state→true→nettoyé). Caveats : pont sync/async (`.GetAwaiter().GetResult()`, port legacy sync), filtre crew côté client (pas de filtre natif), `TransportType`/`TransportSens`/`IsSerial` absents du DTO léger → MOB-6. `GetCrew`/conducteur restaient NotImplemented (→ **résolus MOB-11, 2026-07-05**). |
| M6 | ✅ **MOB-6 — Détail mission** (`GET api/jobdetail`) | M | **Livré 2026-06-08**. `JobRepository.GetJob` : `IMissionDetailQueryService` (mission + adresses résolues) + `IOrderQueryService` (mode/sens/fréquence) + `IBeneficiaryQueryService` (patient) → `ClJob`. Facturation/attributs dynamiques différés (contrat minimal). Chaîne controller/use case/cache déjà câblée → seule l'impl. `IJobRepository` fournie. |
| M7 | ✅ **MOB-7 — Statuts / temps** (`GET/PATCH api/time`) | M | **Livré 2026-06-08**. Timeline ack/read/go/onsite/terminate en `MOB_MISSION_STATE` opérationnelle (délégation `JobRepository`→`JobTimeRepository`). Projection statut fin→`ORD_MISSION.MIS_STATUS` **différée** (pas de transition de statut côté domaine Orders ; à cadrer ERP). Sirus : statu quo (MOB-16). **MàJ 2026-07-05** : l'acquittement « Bien reçu » depuis la JobList (`PATCH /api/joblist`) posait par erreur `MST_READ_AT` au lieu de `MST_ACK_AT` → corrigé (`ClAckJobUseCase`, idempotent, projeté régulation) ; cf. ligne « ACK / Bien reçu » du suivi. |
| M8 | ✅ **MOB-8 — Signature** (`GET/PUT/PATCH/DELETE api/signature`) | S | **Livré 2026-06-08**. CRUD `MOB_SIGNATURE` (MOB-2) + flag `MI_SIGNATURE_EXISTS` : `ISignatureRepository.Exists/ExistingFor` → `ClJobDetailModel.IsSign` (détail) et `ClJobListItemModel.SignatureExists` (liste, overlay batch). |
| M9 | **MOB-9 — Déploiement + smoke tests** | S | Publish sous-app IIS `/mobile`. Re-pointer l'app mobile. Suite `.http` du contrat : login → joblist → jobdetail → time → signature. **Validation parité contrat** (mêmes routes/DTOs que `WebApi`). Retrait de l'ancienne `WebApi`. |

### Phase 2 — Reste de parité (post-MVP)

| # | Itération | Taille | Détail |
|---|---|---|---|
| M10 | **MOB-10 — Kilométrage** (`api/kilometers`) | S | `MOB_KM` + maj `ORD_VEHICLE` (dernier km) côté ERP. |
| M11 | ✅ **MOB-11 — Conducteur** (`api/driver`) | S | **Livré 2026-07-05** (API, build vert). **Découplé 4a** : détail équipage + désignation conducteur via **Orders.Api HTTP** (plus d'accès direct `CRW_DRIVER_ASSIGNMENT`). `CrewRepository.GetCrew`/`IsEmployeeInCrew` (**MOB-4**) + `Update` (MOB-11) implémentés : `GetCrewFullAsync`→`GET crews/{id}` (membres **avec PER_ID**, conducteur actif, véhicule, fenêtre de service) ; `SetCrewDriverAsync`→`PUT crews/{id}/driver`. Mapping `ErpCrewFullDto→ClCrew` (`ErpCrewMappings`). **`GET api/Driver` token-canonique** ajouté (équipage dérivé du `sub`, comme joblist ; `IMobileIdentityResolver.ResolveActiveCrewId` départage par fenêtre de service si plusieurs) ; compat `GET/POST api/Driver/{crewId}` conservée. `ClGetDriverUseCase` gère « aucun conducteur désigné » (plus de NRE). **🔴 Dépendance : 2 endpoints additifs Orders.Api à livrer** (`GET crews/{id}`, `PUT crews/{id}/driver`) — contrat dans **`endPoint.md`**. Hors périmètre (pas d'endpoint ERP correspondant) : `GetCrewDriver(vehicleId)`, `GetCrewIdList(date)`. |
| M12 | **MOB-12 — Fin de service** (`api/endofservice`) | S | Clôture `MOB_SESSION` + horodatage fiable. |
| M13 | **MOB-13 — Édition mission + facturation dynamique** (`api/jobedit`, `formstructure`, `referenceddata`) | **L** | Nécessite un **modèle contrat/attributs dynamiques** (le gros gap). Soit tables `MOB_CONTRACT_*`, soit nouveau module ERP. À cadrer séparément. |
| M14 | **MOB-14 — Logs mécaniques + analyses** (`api/mechaniclog`, `analyze`) | M | Tables `MOB_MECANIQUE_*` + `referenceddata` (acteurs, natures, contraintes). |
| M15 | **MOB-15 — Documents** (`api/document`) | S | Source PDF (ERP ou BD Mobile). |
| M16 | **MOB-16 — Recâblage connecteurs** (Sirus + GpsGate) | S | Brancher GpsGate (positions équipages) + Sirus (statuts véhicule) sur le nouveau flux DI. Statu quo fonctionnel. |

### Phase 3 — Durcissement DMZ event-driven (option V2, cf. §2bis)

**Option de durcissement** issue de `spec_architecture_vector_mission_dmz.md`. Le **socle retenu** reste le **4a HTTP + `DB_VECTOR` derrière firewall** ; ces jalons ne s'imposent que pour renforcer résilience/asynchronisme. **Vd-1 et Vd-6 restent pertinents indépendamment.**

| # | Itération | Détail |
|---|---|---|
| Vd-1 | **`DB_VECTOR` dédiée** | Base propre à Vector (LAN, derrière firewall), renommée depuis `BD_ERP_MOBILE_APP` ; secrets séparés. **Pertinent tout de suite.** |
| Vd-2 | *(option)* **Projection missions entrante** | `VectorMissionProjection` + `VectorPublicationWorker` (push LAN→Vector) en cache/résilience — sinon lecture live via `Orders.Api` (4a). |
| Vd-3 | *(option)* **Outbox + bridge** | `VectorOutboxMessage` transactionnel + `VectorDmzBridgeWorker` (pull LAN) + RabbitMQ + Inbox — sinon PUT direct `Orders.Api` (4a). |
| Vd-4 | **Statuts terrain event-driven** | Cycle `Mission vue`→`Disponible` via événements ; `MissionFieldStatusCurrent/History` côté OrderDb. |
| Vd-5 | **Données admin + masquage + visibilité** | `SensitiveDataMaskingMode`, NIR masqué, visibilité équipage retour **projetée** (calcul interne). |
| Vd-6 | **Photo carte mutuelle hors SQL** | Staging DMZ + `VectorDocumentStaging` + transfert interne + purge 3 ans. |
| Vd-7 | **SignalR régulation** | État consolidé (≤ 1 min), sans donnée patient, alimenté LAN. |
| Vd-8 | **Contrats d'événements** | `CaSoft.Erp.Integration.Contracts` (événements Vector↔LAN, spec §22). |

---

## 5. Sujets transverses / à trancher plus tard

- **✅ ACK → « Mission vue » (tranché 2026-07-05)** : aligné sur la spec (§10). Le « bien reçu » de la JobList = marqueur terrain **« Mission vue »** — `MST_READ_AT`, flag **`IsSeen`**, `ClMarkMissionSeenUseCase`, événement **`MissionSeen`**. L'« acquittement » distinct est abandonné (`MST_ACK_AT` laissé dormant). ⚠️ **Coordination UI** : le champ joblist `IsAck` devient **`IsSeen`**. ⚠️ **Orders.Api** : la régulation affiche « vue à HH:MM » depuis le champ **`read`** (plus `ack`).
- **⚠️ Photo carte mutuelle hors SQL (spec DMZ §13)** : livrée en BD Mobile (P1/P2), mais la cible impose un stockage **hors base** (staging DMZ + transfert interne + purge 3 ans). Cf. §2bis / Vd-6.
- **Authentification réelle** : aujourd'hui token Guid + auth commentée côté legacy. À aligner
  avec le P0 #1 de l'ERP (JWT, `orders_devplan.md`). Le MVP réactive a minima le contrôle par
  token de session. **Refondu en MOB-4 ci-dessous (login = Keycloak).**
- **Source de vérité du cycle opérationnel** : laissé en BD Mobile pour le MVP. Si l'ERP se dote
  un jour du module `/regulation` (orders_devplan #65), migrer la timeline `MOB_MISSION_STATE`
  vers l'ERP et faire de l'ERP la source unique du temps réel.
- **`CrewId` int (contrat mobile) ↔ Guid (ERP)** : arbitrage en MOB-3 (table de correspondance
  vs évolution du contrat). Décision impactant le contrat mobile → à valider avant MOB-4.
- **Facturation / attributs dynamiques** : aucun équivalent ERP. MOB-13 = chantier à part entière.
- **Secrets** : la connection string legacy et les credentials GpsGate/Sirus traînent en clair
  dans `appsettings.json` legacy → à externaliser (variables d'env du pool IIS) dès MOB-1.

---

## 6. Ordre de marche recommandé

1. **MOB-0 → MOB-2** (socle : code porté, build vert, BD Mobile prête).
2. **MOB-3** (mapping + arbitrage CrewId — débloque tout le reste).
3. **MOB-4 → MOB-8** (la boucle métier, dans l'ordre du workflow ambulancier).
4. **MOB-9** (déploiement + bascule de l'app mobile + retrait `WebApi`).
5. **Phase 2** au fil de l'eau, MOB-13 (facturation) à cadrer indépendamment.

---

## 7. MOB-4 — Login Keycloak : conception arrêtée (2026-06-14)

**Besoin urgent** : un ambulancier se connecte à **son compte Keycloak** et accède aux missions
des **crews dont il est membre**. Le même compte servira plus tard à d'autres espaces (congés,
acompte, notes de service…) — à ne pas bloquer, mais hors scope immédiat.

### Constat (le maillon manquant)
- Les ambulanciers ont un **compte Keycloak**, **non encore mappé** ni à un « user société » ni à
  un `PER_PERSONNEL` (= identité crew member, `PER_ID` Guid, OrdersDb).
- Une liaison Keycloak ↔ employé existe côté **Siège** (`RH_EMPLOYEE.KeyCloackId`, int,
  `BD_ERP_SIEGE`, via `ClLinkKeyCloackAccountHandler`) mais : (a) pas peuplée pour les ambulanciers,
  (b) pointe la mauvaise identité (RH int, autre base), (c) aucun pont `RH(int) ↔ PER(Guid)`.
- La chaîne crew→missions est **déjà faite** (MOB-5) : `PER_ID → CRW_CREW_MEMBER actif → CRW_CREW →
  FetchJobList`. Il ne manque que **`sub → PER_ID`** + le câblage JWT + l'autorisation.

### Principe directeur : découpler authN de la résolution d'identité
Introduire **une seule indirection** pour ne pas attendre l'unification RH/société :

```
IMobileIdentityResolver.ResolvePersonnel(sub) → PER_ID
```

- 1ʳᵉ implémentation = liaison minimale **détenue par Orders** : table
  `PER_KEYCLOAK_MAP { KeyCloakId (sub) PK → PER_ID FK }` (table, pas colonne → réversible, explicite,
  pas de re-scaffold). Exposée via `IPersonnelQueryService.GetByKeyCloakId(sub)`.
- Plus tard, on **re-pointe l'abstraction** vers l'identité société unifiée — **sans toucher** au
  code missions/crew/joblist.

### Track A — URGENT : accès aux missions
1. **JWT Keycloak** validé sur l'API mobile (authority/audience en config), extraction du `sub`.
2. **Liaison** `PER_KEYCLOAK_MAP` (Orders) + `IPersonnelQueryService.GetByKeyCloakId`.
3. **Résolution crew** : `sub → PER_ID → CRW_CREW_MEMBER actif à la date (fenêtre
   CRM_JOINED_AT/CRM_LEFT_AT) → CRW_CREW`. Un employé peut être dans 1..n crews → gérer le(s) crew(s)
   actif(s) du jour.
4. **Autorisation** : `joblist`/`jobdetail` **dérivent/contrôlent le crew depuis le `sub`**
   (fin de la confiance au `crewId` d'URL ; `IsEmployeeInCrew`). `AutorizeJob` (return true en dur)
   remplacé.
5. **Login** mobile = token Keycloak → résolution crew (plus de `GET/POST api/login` déclaratif ni
   de token Guid en `MOB_SESSION` comme source d'auth).

### Provisioning de la liaison — décidé : **écran admin de mapping**
Outil admin (régulateur/RH) associant chaque compte Keycloak à un `PER_PERSONNEL`, sur le modèle de
l'existant Siège (`UcEmployeeKeycloakAccount`, `IKeycloakDirectoryClient` listant les users
Keycloak — **réutilisable**), mais écrivant la liaison **côté Orders** (`PER_KEYCLOAK_MAP`).
Fiable, immédiat, contrôlé.

### Track B — PLUS TARD : identité société unifiée + espace personnel
Unifier `RH_EMPLOYEE` ↔ `PER_PERSONNEL` (le « user société »). Congés/acompte/notes de service se
branchent sur cette identité (territoire Siège). La liaison du Track A devient une projection de
l'identité unifiée → zéro rework des missions.

### État de livraison
**MOB-4a (API) — livré 2026-06-14, build solution vert.** Réalisé :
- `026_AddKeycloakMap.sql` (Orders) : table `PER_KEYCLOAK_MAP { KEYCLOAK_ID PK → PER_ID FK, UNIQUE(PER_ID) }`
  — **⚠️ à exécuter** sur `BD_ERP_SANITAIRE_DEV` (Database First). Entité + Fluent ajoutés (`OrdersDbContext`).
- `IPersonnelQueryService.GetPersonnelIdByKeyCloakIdAsync` + impl (`sub → PER_ID`).
- Port mobile `IMobileIdentityResolver` (`ResolvePersonnelId`, `ResolveActiveCrewIds`, `IsMissionAccessible`)
  + impl `MobileIdentityResolver` (Orders in-process : `IPersonnelQueryService` + `ICrewQueryService` filtrant
  `PersonnelId`+`VacationDate`, + `IMissionDetailQueryService` pour l'accès au détail).
- `ICrewRepository.FetchJobList(IReadOnlyCollection(Of Guid))` (union dédupliquée) + `ClGetJobListUseCase`
  porté sur liste de crews ; `CrewRepository` réécrit en union.
- `JobListController` / `JobDetailController` : crew dérivé du `sub` (helper `MobileCallerExtensions.GetKeycloakSubject`),
  401/403. Route `GET api/joblist` (canonique) + `GET api/joblist/{intCrewId}` (legacy, param ignoré).
- `Program.cs` : auth JWT Keycloak **gated par `Keycloak:Enabled`** (config `Authority`/`Audience`), DI du resolver.

À tester : exécuter le SQL, créer ≥1 ligne `PER_KEYCLOAK_MAP` à la main, activer `Keycloak:Enabled=true`
(+ Authority/Audience) et appeler `GET api/joblist` avec un token. Sans token → 401 ; `sub` non mappé → 403.

**🔴 TODO — MOB-4b : UI de mapping Keycloak ID ↔ Personnel ID** (peuple `PER_KEYCLOAK_MAP`
proprement, sans SQL manuel). Objectif : un admin (régulateur/RH) associe un compte Keycloak à un
`PER_PERSONNEL` (ambulancier) depuis une UI. Décisions déjà prises (cf. plus bas) : **étendre l'écran
Siège** + **réutiliser `IKeycloakDirectoryClient`**, mais **ownership de la liaison côté Orders**.

À implémenter :
1. **Endpoint Orders** (Orders.Api) — CRUD minimal de la liaison `PER_KEYCLOAK_MAP` :
   - `PUT /personnel/{perId}/keycloak` (body `{ keyCloakId }`) → upsert (unicité : 1 sub ↔ 1 perId,
     respecter `UQ_PER_KEYCLOAK_MAP_PER_ID` + PK `KEYCLOAK_ID` ; renvoyer 409 sur conflit).
   - `DELETE /personnel/{perId}/keycloak` → détacher.
   - `GET /personnel/{perId}/keycloak` (ou flag dans le détail personnel) → état courant de la liaison.
   - Côté Application : `IPersonnelKeycloakLinkService` (Link/Unlink/Get) + impl Infrastructure
     écrivant `PER_KEYCLOAK_MAP` (s'inspirer de `ClLinkKeyCloackAccountHandler` du module Siège).
2. **UI** — étendre `Siege.Contracts.UI.WinForm` (`UcEmployeeKeycloakAccount`) : à côté du lien
   RH_EMPLOYEE existant, ajouter un volet « rattachement ambulancier » qui :
   - liste les comptes Keycloak via `IKeycloakDirectoryClient` (réutilisé) ;
   - recherche/sélectionne un `PER_PERSONNEL` (via API Orders personnel) ;
   - persiste la liaison via l'endpoint Orders ci-dessus (pattern `ClSiegeContractsApiClient` → nouveau
     client Orders, ou client API Orders existant si présent) — **n'écrit pas directement dans OrdersDb**.
3. **Garde-fous UI** : afficher si un compte/personnel est déjà lié, empêcher double-rattachement,
   message clair sur conflit 409.

Hôte alternatif si l'écran Siège s'avère trop couplé : écran WinForms dédié côté admin Orders/Crew
(même endpoint). À trancher au démarrage de MOB-4b.

Limites connues MOB-4a (acceptées) : pont sync/async (`.GetAwaiter().GetResult()`, contrat legacy sync) ;
`IsMissionAccessible` refait un `GetFullAsync` (1 requête de plus que le détail) ; filtre crew côté client
(pas de filtre crew natif sur `IMissionQueryService`).

### Sous-décisions tranchées (2026-06-14)
- **Hôte de l'écran admin** : **étendre l'écran Siège** (`Siege.Contracts.UI.WinForm`,
  `UcEmployeeKeycloakAccount`) en y ajoutant le volet de liaison vers `PER_PERSONNEL`.
  *Conséquence d'archi* : l'ownership de la liaison reste **Orders** (`PER_KEYCLOAK_MAP`) → l'UI
  Siège la persiste via un **endpoint Orders dédié** (pattern client API, comme
  `ClSiegeContractsApiClient`), elle n'écrit pas directement dans OrdersDb.
- **Client annuaire Keycloak** : **réutiliser `IKeycloakDirectoryClient` de Siège** (listing des
  comptes). Dépendance assumée vers le composant Siège.
- **`sub` non mappé** : **403 « compte non rattaché »** côté API mobile (message diagnosticable,
  l'app invite à contacter la régulation).
- **Multi-crew le même jour** : **union des missions** de tous les crews actifs du jour
  (fenêtre `CRM_JOINED_AT`/`CRM_LEFT_AT`).

---

**Fin du document**
