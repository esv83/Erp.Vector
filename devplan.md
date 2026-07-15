# 📱 devplan — CaSoft.Erp.USVector (module mobile terrain)

> **Statut global** : 🟡 En cours — MVP boucle ambulancier **livré** (hors UI login MOB-4b) · Transfert terrain→compta livré côté Orders+Vector, Certification à faire · Result pattern Vague 1 livrée.
> **Dépôt** : `github.com/esv83/Erp.Vector` (`USVector.sln`) · **Dernière mise à jour** : 2026-07-15.
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

### 🟡 En cours
| Réf | Élément | Détail |
|---|---|---|
| MOB-4b | UI de mapping Keycloak ↔ Personnel | aujourd'hui **insertion SQL manuelle** dans `PER_KEYCLOAK_MAP` ; manque l'écran + endpoints CRUD Orders |
| Result V2 | Retrait du legacy (échafaudage presenter) | straggler `ClSetDriverUseCase` (31/32) puis suppression des types legacy |
| MOB-13 → OC | Bascule vers le référentiel **ContextOrder** (côté Order) | Vector devient consommateur ; `MOB_CONTRACT_*` à **déprécier** |

### ⛔ Bloqué / en attente (dépendance externe ou décision)
| Réf | Élément | Ce qui bloque |
|---|---|---|
| MOB-13.2 | **Vrai catalogue métier** (types de contrat + attributs facturation) | attend le **contenu métier** (seed actuel = provisoire STANDARD/ART80) |
| MUTUELLE P2 (compta) | Client HTTP tirant la carte à l'export | à faire **dans le module Certification** (hors mobile) |
| SQL `MOB_003/004/005` | Migrations mutuelle / anomalies / documents | **à exécuter avec un compte db_owner** (`ErpAccount` n'a pas `CREATE TABLE`) |
| Projection statut fin→ERP | `terminate` → `ORD_MISSION.MIS_STATUS` | Orders n'expose **pas de transition** ; à cadrer côté module régulation |

### ⏳ À faire (planifié)
| Réf | Élément | Module |
|---|---|---|
| MOB-9 | Déploiement IIS `/mobile` + smoke `.http` + retrait `WebApi` | Socle |
| MOB-10 | Kilométrage (table dédiée) | Socle |
| MOB-12 | Fin de service | Socle |
| MOB-14 | Logs mécaniques | Socle |
| MOB-16 | Recâblage connecteurs Sirus / GpsGate | Socle |
| MOB-13.12 | Purge des valeurs orphelines (au transfert) | MOB-13 |
| MUTUELLE P3 | OCR carte mutuelle (Claude vision + validation humaine) | Mutuelle |
| — | Tests xUnit Orders du transfert (dérivation statut, garde-fous) | Transfert |
| TRF-12..15 | Certification : découverte → tirage `field-data` → agrégation → `transfer-status` | Certification (autre module) |

### ⚪ Différé (V2 / hors MVP)
Mode **offline** (cache + sync différée) · **géoloc avancée** · **push SignalR** (remplace le polling, spec §15) · assembly partagé **`Orders.Contracts`** (4b, anti-drift JSON) · **DMZ V2** (`Vd-1..8`, RabbitMQ, `DB_VECTOR`, masquage NIR) · **éviction ciblée** du cache (`Invalidate(sub)`) · renommage `CaSoft.Erp.USVector.*` → `CaSoft.Erp.Vector.*`.

### ⚠️ Dette & garde-fous
| Réf | Point | Criticité |
|---|---|---|
| **C6** | `Keycloak:DisableValidation=true` | 🔴 **passer `false` avant prod** |
| RGPD P4 | Données de santé (documents/mutuelle/anomalies) servies par Vector.Api | durcissement rétention/chiffrement/audit |
| C1-C5 | Dette de compat MOB (`IsAck` alias, champs JobDetail legacy, filtre crew client…) | à retirer une fois l'UI basculée |
| SQL Orders | Migration transfert numérotée **`027`** (§2.1 TRANSFER) vs **`034`** ailleurs | 🔸 à réconcilier |

---

## 2. Détail par module

> Chaque module : à quoi ça sert + architecture essentielle + source faisant autorité. **Le statut détaillé est au §1.**

### 2.1 Socle technique & reconnexion ERP (MOB-0..16)
> Source : [`mobile_devplan.md`](mobile_devplan.md)

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

### 2.5 Refactor Result pattern
> Source : [`refactor_result_pattern.md`](refactor_result_pattern.md) — branche `ImplementCaSoftFramework` (fusionnée dans main puis **supprimée 2026-07-15**)

Migration des use cases legacy (`Execute(presenter)`) vers `Handle() → ClResult(Of T)` + `IError` (NotFound→404), **non cassante** (Strangler Fig + pont). Vague 1 faite (31/32) ; Vague 2 = retrait de l'échafaudage (`ClUseCaseHandler`, `ClWebApiPresenter`, `ClUseCaseBase`…).

### 2.6 Édition attributs de mission / contrat (MOB-13)
> Source : [`MOB-13_devplan.md`](MOB-13_devplan.md)

Édition des attributs (commentaires, tél/mail patient, **type de contrat + attributs facturation**) en **overlay BD Mobile, aucune écriture ERP**. Applicabilité **N..N** (`CAT_IS_GLOBAL` ou liaison contrat). Tables catalogue `MOB_CONTRACT_TYPE/ATTRIBUTE(_CONTRACT/_OPTION)` + overlay `MOB_JOB_CONTRACT`/`MOB_JOB_ATTRIBUTE_VALUE`. Endpoints `GET FormStructure`, `PATCH JobEdit`, `GET/POST Contract`.
> ⚠️ **Évolution** : le référentiel migre **côté Order** (`ContextOrder`/OC-9, verrou régulateur + filtrage agence/mode) → Vector devient consommateur, `MOB_CONTRACT_*` **dépréciées**. Cf. `Erp.Order/feature_order_context_devplan.md` + `note_vector_orderContext_mission.md`.

### 2.7 Carte mutuelle
> Source : [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md)

Capture photo → stockage blob BD Mobile (`MOB_MUTUELLE_CARD`) → restitution à la facturation (**pivot code AMC**) → OCR IA (Claude vision, validation humaine, P3). Endpoints `POST/GET /api/beneficiaries/{id}/mutuelle-card`, `GET /api/mutuelle-card/{id}/image`, `PATCH …` (saisie manuelle P2). Restitution = décision **2b** (Certification tire en HTTP).

### 2.8 Transfert terrain → comptabilité (TRF-1..15)
> Source : [`TRANSFER_devplan.md`](TRANSFER_devplan.md)

Cycle **Orders → Vector → Certification** : projection de l'avancement vers Orders (`PUT /missions/{id}/operational` → `ORD_MISSION_OPERATIONAL`, `MIS_STATUS` dérivé), statut de transfert `MIS_TRANSFER_STATUS` (`Transferable→Transferred→Billed`), **gel terrain** au transfert (`[FreezeOnTransfer]` → 409), paquet consolidé versionné **`GET /missions/{id}/field-data`** (timeline/signature/attributs/mutuelle/km/documents/anomalies). Compta **tire les octets** (pas de blob partagé). Temps réel = **polling** au MVP (push SignalR en V2).

---

## 3. Migrations SQL
| Base | Script | Contenu | État |
|---|---|---|---|
| Orders (`BD_ERP_SANITAIRE_DEV`) | `026_AddKeycloakMap.sql` | `PER_KEYCLOAK_MAP` | 🟢 appliqué |
| Orders | `034_AddMissionOperationalAndTransfer.sql` | `ORD_MISSION_OPERATIONAL` + `MIS_TRANSFER_STATUS` | 🟢 appliqué 2026-06-22 ⚠️ *(numéroté `027` dans TRANSFER §2.1)* |
| Mobile (`BD_ERP_MOBILE_APP`, **db_owner**) | `MOB_001_Initial.sql` | SESSION / MISSION_STATE / SIGNATURE | 🟢 appliqué |
| Mobile | `MOB_002_JobAttributes.sql` | catalogue contrat + overlay | 🟢 appliqué |
| Mobile | `MOB_003_MutuelleCard.sql` | carte mutuelle | ⛔ à exécuter |
| Mobile | `MOB_004_Anomaly.sql` | anomalies | ⛔ à exécuter |
| Mobile | `MOB_005_Document.sql` | documents | ⛔ à exécuter |

## 4. Configuration
- `ConnectionStrings:MobileDb` · `ConnectionStrings:OrdersDb` **(inutilisé depuis le découplage 4a)**.
- `OrdersApi:BaseUrl` (**slash final**, inclure le PathBase IIS) · `AddressApi:BaseUrl`.
- `Keycloak:{Enabled, Authority, Audience, DisableValidation}` — **`DisableValidation=false` en prod (C6)**.
- `MobileIdentityCache:{PersonnelMinutes=30, ActiveCrewsMinutes=15}` · secrets GpsGate/Sirus `__SET_VIA_ENV__`.

## 5. Sources consolidées
| Doc | Genre | Module |
|---|---|---|
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Spec fonctionnelle | Besoin/vocabulaire |
| [`mobile_devplan.md`](mobile_devplan.md) | Devplan | Socle (§2.1) |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Devplan | Découplage (§2.3) |
| [`docs/auth/optimisation-chaine-authentification.md`](docs/auth/optimisation-chaine-authentification.md) | Note conception | Auth (§2.4) |
| [`refactor_result_pattern.md`](refactor_result_pattern.md) | Devplan refactoring | Application (§2.5) |
| [`MOB-13_devplan.md`](MOB-13_devplan.md) | Devplan | Attributs (§2.6) |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Devplan | Mutuelle (§2.7) |
| [`TRANSFER_devplan.md`](TRANSFER_devplan.md) | Devplan transverse | Transfert (§2.8) |

**Non consolidés ici** (autres genres) : contrats front (`note_web_alexandre_*.md`, `docs/ui-web/*`), contrat HTTP Orders (`endPoint.md`), déploiement (`docs/deploiement/*`), `BUG_DISPLAY.MD`, `README.md`.
