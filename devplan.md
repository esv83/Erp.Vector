# 📱 devplan — CaSoft.Erp.USVector (module mobile terrain)

> **Statut global** : 🟡 En cours — MVP boucle ambulancier **livré** (hors UI login MOB-4b) · Transfert terrain→compta livré côté Orders+Vector, Certification à faire · Result pattern Vague 1 livrée.
> **Prod** : `\\192.168.1.112\prod_api\Vector.Api` — déployée le **2026-08-02 13:06** depuis `main` (`0e76a70`), trafic servi et JWT validés dans la foulée.
> **Dépôt** : `github.com/esv83/Erp.Vector` (`USVector.sln`) · **Dernière mise à jour** : 2026-08-24.
> **2026-08-24** : `mobile_devplan.md`, `MOB-13_devplan.md` et `TRANSFER_devplan.md` sont fusionnés dans
> [`TERRAIN_devplan.md`](TERRAIN_devplan.md) (détail des trois lots + liste des pistes abandonnées).
>
> _Devplan **unifié** du module Vector — synthèse des 8 docs de conception (voir §5). Organisé **par statut**
> (le tableau de bord §1 donne la vue d'ensemble), puis par module (§2). Convention : Clean Architecture,
> nommage `Cl`/`I`/`En`, DTO `…DtoIn`/`…DtoOut`, use cases `Handle() → ClResult(Of T)`, tables `MOB_*`._

### Légende des statuts
| | Sens |
|---|---|
| 🟢 **Livré** | codé, testé, vérifié (build vert + tests) |
| 🟡 **En cours** | démarré, non terminé |
| ⛔ **Bloqué / en attente** | dépend d'une décision, d'un contenu métier ou d'un livrable d'un autre module |
| ⏳ **À faire** | planifié, non démarré |
| ⚪ **Différé (V2)** | hors périmètre MVP |
| ⚠️ **Dette / garde-fou** | à traiter (dont bloquant avant prod) |

---

## 1. Tableau de bord (par statut)

### 🟢 Livré (vérifié)
| Réf | Élément | Module | Preuve |
|---|---|---|---|
| MOB-0 | BD Mobile + 3 tables (SESSION / MISSION_STATE / SIGNATURE) | Socle | schéma en BD |
| MOB-1 | Portage legacy : 16 controllers, **25 routes**, domaine/DTO | Socle | build vert |
| MOB-2 | Repositories réels (signature/time validés en réel) | Socle | validé BD réelle |
| MOB-3a/3b | Identités en **Guid** + `ErpReferenceMappings` | Socle | — |
| MOB-5 | Joblist terrain | Socle | validé 2 missions réelles |
| MOB-6 / MOB-7 / MOB-8 | Détail mission · timeline 5 jalons · signature + présence | Socle | validé réel |
| MOB-11 | Conducteur (découplé 4a) | Socle | — |
| MOB-4a | Login Keycloak `sub → PER_ID → crews` | Auth | **validé bout-en-bout 2026-07-05** |
| DEC T1-T6 | Découplage HTTP 4a (DTO miroir + clients) + endpoint `by-keycloak/{sub}` | Découplage | **isolation de build prouvée** |
| — | Cache identité (`CachingMobileIdentityResolver`, TTL 30 min) | Auth | mesuré : chemin chaud 2 appels → ~0 |
| Result V1 | 31 use cases migrés en Result pattern | Application | **31 tests** + smoke, parité HTTP |
| 13.1-13.11 | Édition attributs / contrat (overlay BD Mobile) | MOB-13 | **11 tests**, validé BD 2026-06-14 |
| MUTUELLE P1+P2 | Carte mutuelle : capture/stockage + restitution mobile | Mutuelle | **16 tests** (2026-06-15) |
| TRF-1..4 | Orders : `ORD_MISSION_OPERATIONAL` + statut transfert + endpoints | Transfert | SQL appliqué 2026-06-22, build vert |
| TRF-5..10 | Vector : écriture avancement, gel 409, anomalies, documents, `field-data` | Transfert | **24 tests** |
| DET-1 / DET-2 | Champ `Service` dédié · affichage pickup/dropoff **piloté serveur** | Lieux | DET-2 sur `feat/vector-service-location`, 12 tests |
| CREW-1 | **Accès anticipé 30 min** avant la prise de service (missions visibles avant de démarrer la vacation) | Auth | `ClCrew.EarlyAccessMinutes`, 3 tests · **en prod 2026-08-02** |
| DEP-1 | **Cible PROD de `deploy.ps1`** (profil `IIS-ProdServer`) + garde-fous : confirmation explicite, pré-vol d'accessibilité du partage | Déploiement | publication réelle vérifiée 2026-08-02 |

### 🟡 En cours
| Réf | Élément | Détail |
|---|---|---|
| MOB-4b | UI de mapping Keycloak ↔ Personnel | aujourd'hui **insertion SQL manuelle** dans `PER_KEYCLOAK_MAP`. Les **endpoints Orders sont livrés** (`GET/PUT/DELETE /personnel/{id}/keycloak`, `GET /keycloak/users`) ; il manque l'écran, dont **l'hôte est à re-trancher** : le module **Identity** possède désormais la correspondance (reprise jouée 23/08/2026) et l'écran Siège envisagé n'existe plus qu'en `Archives/`. Cf. [`TERRAIN_devplan.md`](TERRAIN_devplan.md) §3.2 |
| Result V2 | Retrait du legacy (échafaudage presenter) | straggler `ClSetDriverUseCase` (31/32) puis suppression des types legacy |
| MOB-13 → OC | Bascule vers le référentiel **ContextOrder** (côté Order) | **Pas commencée** (aucune occurrence `contextOrder` dans le code Vector au 2026-08-24) ; le back Order est prêt. Vector devient consommateur ; `MOB_CONTRACT_*` + `MOB_JOB_ATTRIBUTE_VALUE` **dépréciées**. Plan détaillé : [`TERRAIN_devplan.md`](TERRAIN_devplan.md) §3.1 |

### ⛔ Bloqué / en attente (dépendance externe ou décision)
| Réf | Élément | Ce qui bloque |
|---|---|---|
| MUTUELLE P2 (compta) | Client HTTP tirant la carte à l'export | à faire **dans le module Certification** (hors mobile) |

> ~~MOB-13.2 (vrai catalogue métier)~~ **abandonné** : le catalogue passe côté Order (ContextOrder) — le seed provisoire ne sera pas complété.
> ~~SQL `MOB_003/004/005`~~ **résolu** : tables vérifiées présentes en BD le 2026-08-24 (§3).
> ~~Projection statut fin→ERP~~ **résolue** par TRF-2/3/5 (dérivation `MIS_STATUS` côté domaine Orders, poussée par Vector) ; seule la clôture `Closed` reste la main du régulateur.

### ⏳ À faire (planifié)
| Réf | Élément | Module |
|---|---|---|
| MOB-9 (résiduel) | Suite smoke `.http` de parité de contrat — déploiement fait, `WebApi` legacy disparue | Socle |
| MOB-10 | Kilométrage : arbitrer km véhicule (existant) vs relevé **par mission** attendu par la facturation | Socle |
| MOB-12 | Fin de service — **à re-cadrer** : `MOB_SESSION` n'est plus la source d'auth, viser la vacation Orders | Socle |
| MOB-14 | Logs mécaniques (controllers exposés sur stubs) | Socle |
| MOB-16 | Recâblage connecteurs Sirus / GpsGate | Socle |
| MUTUELLE P3 | OCR carte mutuelle (Claude vision + validation humaine) | Mutuelle |
| — | Tests xUnit Orders du transfert (dérivation statut, garde-fous) | Transfert |
| TRF-12..15 | Certification : découverte → tirage `field-data` → agrégation → `transfer-status` | Certification (autre module) |

### ⚪ Différé (V2 / hors MVP)
**CREW-2 — accès anticipé à cheval sur minuit** : la fenêtre CREW-1 ne franchit pas le changement de jour. `ResolveActiveCrewIds` interroge Orders sur la **date du jour** (`GET /crews?personnelId=&date=`) ; à 23:50, une vacation démarrant à 00:15 ne remonte donc pas. Correctif = élargir la requête à **J+1** quand `now + EarlyAccessMinutes` change de date (puis dédoublonner les crewIds). **Non prioritaire** : décision métier 2026-08-02 — les vacations de nuit ne sont pas concernées.

Mode **offline** (cache + sync différée) · **géoloc avancée** · **push SignalR** (remplace le polling, spec §15) · assembly partagé **`Orders.Contracts`** (4b, anti-drift JSON) · **DMZ V2** (`Vd-1..8`, RabbitMQ, `DB_VECTOR`, masquage NIR) · **éviction ciblée** du cache (`Invalidate(sub)`) · renommage `CaSoft.Erp.USVector.*` → `CaSoft.Erp.Vector.*`.

### ⚠️ Dette & garde-fous
| Réf | Point | Criticité |
|---|---|---|
| ~~**C6**~~ | ~~`Keycloak:DisableValidation=true`~~ — **résolu**, vérifié en prod le 2026-08-02 : `ASPNETCORE_ENVIRONMENT=Production` (web.config) → l'overlay `appsettings.Production.json` impose `Enabled=true` / `DisableValidation=false` / `RequireHttpsMetadata=true`, et les logs confirment une validation réelle (`JWT validé : sub=… iss=https://auth.ade-dev.fr/realms/delesse`) | 🟢 fermé |
| ~~**KC-1**~~ | ~~`Keycloak:Authority` / `:Audience` codés en dur~~ — **résolu 2026-08-02**. Trois valeurs étaient figées (Authority, Audience **et l'`azp` attendu**) : toutes viennent désormais de la config, placeholders supprimés. Ajout d'un **garde-fou au démarrage** (Authority absente ou restée au placeholder → refus de démarrer avec un message explicite, au lieu de 401 en série sans cause lisible) ; placé hors du callback `AddJwtBearer`, qui ne s'exécute qu'à la 1ʳᵉ requête. Parité avec l'ancien code vérifiée sur les 3 environnements. `deploy.ps1` vérifie en plus que `appsettings.json` a bien atterri (le binaire en dépend maintenant). | 🟢 fermé |
| ~~**DEP-2**~~ | ~~`appsettings.json` diverge entre dépôt et serveur (`AddressApi:BaseUrl`)~~ — **résolu 2026-08-02** : la base porte l'URL de référence (prod), comme `OrdersApi:BaseUrl` ; `Development` et `Staging` redirigent vers l'`Address.Api` locale (comportement du serveur de dev **inchangé**) ; `Production` garde la surcharge explicite. Dépôt et serveur vérifiés **identiques bit-à-bit**, et résolution effective par environnement validée avec le vrai moteur de config. | 🟢 fermé |
| RGPD P4 | Données de santé (documents/mutuelle/anomalies) servies par Vector.Api | durcissement rétention/chiffrement/audit |
| C1-C5 | Dette de compat MOB (`IsAck` alias, champs JobDetail legacy, filtre crew client…) | à retirer une fois l'UI basculée |
| SQL Orders | Migration transfert numérotée **`027`** (§2.1 TRANSFER) vs **`034`** ailleurs | 🔸 à réconcilier |

---

## 2. Détail par module

> Chaque module : à quoi ça sert + architecture essentielle + source faisant autorité. **Le statut détaillé est au §1.**

### 2.1 Socle technique & reconnexion ERP (MOB-0..16)
> Source : [`TERRAIN_devplan.md`](TERRAIN_devplan.md) §1.1-1.3, §3.4

Reconnecter l'API mobile à l'ERP après perte de la base legacy, **sans changer le contrat mobile** (25 routes + DTOs) : on ne remplace que l'implémentation des repositories. API ASP.NET Core 8 `CaSoft.Erp.USVector.Api` (remplace `WebApi`). « Mission vue » remplace l'ACK (`MST_READ_AT` + `IsSeen` + événement `MissionSeen`).
- **Tables** : `MOB_SESSION`, `MOB_MISSION_STATE` (timeline ack/read/go/onsite/terminate), `MOB_SIGNATURE`.
- **Repos** : `JobRepository`, `CrewRepository` (`FetchJobList`), `JobTimeRepository`, `SignatureRepository`, `SessionRepository`.
- **Externes** : Keycloak, GpsGate, Sirus, Orders.Api.

### 2.2 Spec fonctionnelle fondatrice
> Source : [`AppMobile_specifications.md`](AppMobile_specifications.md) — doc **fonctionnel** (le besoin, pas la technique)

Référentiel du besoin : **plan de travail versionné**, **accusé de réception** (4 statuts), **prise de connaissance tracée**, **statuts terrain** (`EN ROUTE`/`SUR PLACE`/`DISPONIBLE`), **champs modulaires**, signature/documents/anomalies, **notifications temps réel**, **isolation par équipage**, **séparation officiel↔terrain** (garde-fou : jamais d'écrasement).

### 2.3 Découplage Vector ↔ Orders (HTTP, 4a)
> Source : [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md)

Vector consomme `Orders.Api` en REST (DTO miroir `ErpApi/ErpReadDtos` + `IErpReadApiClient`), **plus de référence projet** Orders → un WIP Orders ne casse plus le build mobile. Contrat Orders en **camelCase**, enums en `int`. Seul ajout côté Orders : `GET /personnel/by-keycloak/{sub}`. **4b** (contrats partagés) différé (risque de drift assumé).

### 2.4 Authentification & identité
> Source : [`docs/auth/optimisation-chaine-authentification.md`](docs/auth/optimisation-chaine-authentification.md)

Chaîne : `JWT local → sub→PER_ID (PER_KEYCLOAK_MAP) → crews actifs → crewId ∈ crews`, chokepoint `CrewAccess.ResolvePersonnel`. **Deux caches** (`CachingMobileIdentityResolver`) : personnel long (TTL 30 min), crews court + lecture fraîche sur `GET /api/crew/mine`. **Claim `per_id` écarté** (turnover → non invalidable ; le cache HTTP s'invalide). Config `MobileIdentityCache:{PersonnelMinutes, ActiveCrewsMinutes}`.
> **CREW-1 — fenêtre d'accès (2026-08-02, en prod)** : `ClCrew.IsSelectableAt` ouvre l'équipage **`EarlyAccessMinutes` = 30 min avant `ServiceStart`**, pour consulter ses missions avant de démarrer sa vacation (les conditions *non clôturé* / *non obsolète 18 h* sont inchangées). Seul verrou horaire de la chaîne : `CrewAccess.Authorize` filtre par **date**, la joblist n'a aucun filtre d'heure. DTO : nouveau flag **`IsPending`** (`IsCurrent=false` tant que le service n'a pas commencé) — contrat web décrit dans [`note_web_alexandre_acces_anticipe_missions.md`](note_web_alexandre_acces_anticipe_missions.md). Effet de bord assumé : en double vacation, `RequiresSelection` passe à `true` 30 min plus tôt. Limite connue → **CREW-2** (§1, différé).
> **KC-1 (2026-08-02)** : realm, audience et `azp` attendu viennent tous de la config (plus rien en dur), avec refus de démarrer si l'`Authority` est vide ou au placeholder. L'audience n'est volontairement **pas** validée (Keycloak émet `aud=account`, aucun mapper sur le realm) : c'est l'**`azp`** qui est contrôlé dans `OnTokenValidated`, signature/issuer/expiration restant validés — d'où une seule clé `Keycloak:Audience` pour les deux usages.

### 2.5 Refactor Result pattern
> Source : [`refactor_result_pattern.md`](refactor_result_pattern.md) — branche `ImplementCaSoftFramework` (fusionnée dans main puis **supprimée 2026-07-15**)

Migration des use cases legacy (`Execute(presenter)`) vers `Handle() → ClResult(Of T)` + `IError` (NotFound→404), **non cassante** (Strangler Fig + pont). Vague 1 faite (31/32) ; Vague 2 = retrait de l'échafaudage (`ClUseCaseHandler`, `ClWebApiPresenter`, `ClUseCaseBase`…).

### 2.6 Édition attributs de mission / contrat (MOB-13)
> Source : [`TERRAIN_devplan.md`](TERRAIN_devplan.md) §1.4, §3.1

Édition des attributs (commentaires, tél/mail patient, **type de contrat + attributs facturation**) en **overlay BD Mobile, aucune écriture ERP**. Applicabilité **N..N** (`CAT_IS_GLOBAL` ou liaison contrat). Tables catalogue `MOB_CONTRACT_TYPE/ATTRIBUTE(_CONTRACT/_OPTION)` + overlay `MOB_JOB_CONTRACT`/`MOB_JOB_ATTRIBUTE_VALUE`. Endpoints `GET FormStructure`, `PATCH JobEdit`, `GET/POST Contract`.
> ⚠️ **Évolution** : le référentiel migre **côté Order** (`ContextOrder`/OC-9, verrou régulateur + filtrage agence/mode) → Vector devient consommateur, `MOB_CONTRACT_*` **dépréciées**. Cf. `Erp.Order/feature_order_context_devplan.md` + `Erp.Order/note_vector_orderContext_mission.md`.

### 2.7 Carte mutuelle
> Source : [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md)

Capture photo → stockage blob BD Mobile (`MOB_MUTUELLE_CARD`) → restitution à la facturation (**pivot code AMC**) → OCR IA (Claude vision, validation humaine, P3). Endpoints `POST/GET /api/beneficiaries/{id}/mutuelle-card`, `GET /api/mutuelle-card/{id}/image`, `PATCH …` (saisie manuelle P2). Restitution = décision **2b** (Certification tire en HTTP).

### 2.8 Transfert terrain → comptabilité (TRF-1..15)
> Source : [`TERRAIN_devplan.md`](TERRAIN_devplan.md) §1.5, §3.3

Cycle **Orders → Vector → Certification** : projection de l'avancement vers Orders (`PUT /missions/{id}/operational` → `ORD_MISSION_OPERATIONAL`, `MIS_STATUS` dérivé), statut de transfert `MIS_TRANSFER_STATUS` (`Transferable→Transferred→Billed`), **gel terrain** au transfert (`[FreezeOnTransfer]` → 409), paquet consolidé versionné **`GET /missions/{id}/field-data`** (timeline/signature/attributs/mutuelle/km/documents/anomalies). Compta **tire les octets** (pas de blob partagé). Temps réel = **polling** au MVP (push SignalR en V2).

---

## 3. Migrations SQL
| Base | Script | Contenu | État |
|---|---|---|---|
| Orders (`BD_ERP_SANITAIRE_DEV`) | `026_AddKeycloakMap.sql` | `PER_KEYCLOAK_MAP` | 🟢 appliqué |
| Orders | `034_AddMissionOperationalAndTransfer.sql` | `ORD_MISSION_OPERATIONAL` + `MIS_TRANSFER_STATUS` | 🟢 appliqué 2026-06-22 ⚠️ *(numéroté `027` dans TRANSFER §2.1)* |
| Mobile (`BD_ERP_MOBILE_APP`, **db_owner**) | `MOB_001_Initial.sql` | SESSION / MISSION_STATE / SIGNATURE | 🟢 appliqué |
| Mobile | `MOB_002_JobAttributes.sql` | catalogue contrat + overlay | 🟢 appliqué |
| Mobile | `MOB_003_MutuelleCard.sql` | carte mutuelle | 🟢 appliqué *(vérifié 2026-08-24)* |
| Mobile | `MOB_004_Anomaly.sql` | anomalies | 🟢 appliqué *(vérifié 2026-08-24)* |
| Mobile | `MOB_005_Document.sql` | documents | 🟢 appliqué *(vérifié 2026-08-24)* |
| Mobile | `MOB_006_OperationalOutbox.sql` | file de projection régulation | 🟢 appliqué *(vérifié 2026-08-24)* |

## 4. Configuration
- `ConnectionStrings:MobileDb` · `ConnectionStrings:OrdersDb` **(inutilisé depuis le découplage 4a)**.
- `OrdersApi:BaseUrl` (**slash final**, inclure le PathBase IIS) · `AddressApi:BaseUrl`.
- `Keycloak:{Enabled, Authority, Audience, DisableValidation, RequireHttpsMetadata}` — **`DisableValidation=false` en prod** (C6, fermé). Depuis KC-1 toutes ces clés sont **réellement lues** ; `Audience` sert aussi d'**`azp` attendu**. Une `Authority` vide ou au placeholder **empêche le démarrage** (hors `DisableValidation`).
- `MobileIdentityCache:{PersonnelMinutes=30, ActiveCrewsMinutes=15}` · secrets GpsGate/Sirus `__SET_VIA_ENV__`.

### 4.1 Règle de config des serveurs (dev / prod)
Trois couches, à ne pas confondre — c'est la source des régressions d'auth et de résolution d'adresses :
1. **`web.config`** (serveur, **manuel**, jamais régénéré : `IsTransformWebConfigDisabled=true`) → `ASPNETCORE_ENVIRONMENT` + secrets (`ConnectionStrings__*`, `Sirus__Host`, `GpsGate__*`). Survit à toute publication.
2. **`appsettings.json`** → **fait partie de la sortie de publication**, donc *écrasable* par un déploiement. Porte la **valeur de référence (celle de la prod)**, jamais une valeur de poste local : `OrdersApi:BaseUrl` et `AddressApi:BaseUrl` y pointent tous deux sur `api.urgencesante.net`.
3. **`appsettings.Development.json` / `.Staging.json` / `.Production.json`** → shippés **et** prioritaires sur la base : c'est là qu'on décrit les **déviations** par environnement (Keycloak activé, redirection vers l'`Address.Api` locale en dev).

> Règle : le défaut sûr est dans la base, les écarts dans l'overlay — **jamais** une valeur éditée « à la main sur le serveur », qui n'est protégée que par un hasard d'incrémentalité MSBuild (cause de DEP-2).
> Corollaire vérifié le 2026-08-02 : `Development` et `Staging` → `localhost:5100`, `Production` → `api.urgencesante.net`.

## 4.2 Déploiement
`.\deploy.ps1 dev` · `.\deploy.ps1 prod` (confirmation à taper ; `-Force` en non-interactif). Profils `IIS-DevServer` / `IIS-ProdServer` → `\\192.168.1.112\{dev_api,prod_api}\Vector.Api`.
Le script fait un **pré-vol** (partage accessible) avant de publier, puis vérifie l'horodatage bin↔UNC. Publication **portable** (pas de RID, sinon SqlClient casse). `app_offline.htm` est posé puis retiré → **coupure courte de l'API** à chaque publication.
Prérequis : `net use \\192.168.1.112\prod_api /user:192.168.1.112\DeployApi *`.

## 5. Sources consolidées
| Doc | Genre | Module |
|---|---|---|
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Spec fonctionnelle | Besoin/vocabulaire |
| [`TERRAIN_devplan.md`](TERRAIN_devplan.md) | Devplan consolidé | Socle (§2.1) · Attributs (§2.6) · Transfert (§2.8) — fusion de `mobile_devplan` + `MOB-13_devplan` + `TRANSFER_devplan` le 2026-08-24 |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Devplan | Découplage (§2.3) |
| [`docs/auth/optimisation-chaine-authentification.md`](docs/auth/optimisation-chaine-authentification.md) | Note conception | Auth (§2.4) |
| [`refactor_result_pattern.md`](refactor_result_pattern.md) | Devplan refactoring | Application (§2.5) |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Devplan | Mutuelle (§2.7) |

**Non consolidés ici** (autres genres) : contrats front (`note_web_alexandre_*.md`, `docs/ui-web/*`), contrat HTTP Orders (`endPoint.md`), déploiement (`docs/deploiement/*`), `BUG_DISPLAY.MD`, `README.md`.
