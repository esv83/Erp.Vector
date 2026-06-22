# TRANSFER — Workflow terrain → comptabilité (Orders ↔ Vector ↔ Certification)

> Feature : organiser le cycle complet **lecture mission (Orders) → enrichissement terrain
> (Vector/BD Mobile) → transfert en comptabilité (Certification)**, avec projection de
> l'avancement opérationnel vers Orders (régulateurs informés en temps réel) et gel de la
> donnée terrain après transfert.
>
> Chantier **transverse 3 modules** : `Orders` (schéma + endpoints), `CaSoft.Erp.USVector`
> (endpoint consolidé + gel + table anomalies), `CaSoft.Erp.SanitaryTrypCertification`
> (découverte + tirage + écriture statut). Prérequis livrés : MOB-5/6 (lecture),
> MOB-7 (timeline BD Mobile), MOB-8 (signature), MOB-13 (attributs overlay),
> MUTUELLE_CARD P1/P2, DEC-* (découplage HTTP Vector↔Orders).

---

## 0. Décisions arrêtées (2026-06-22)

| # | Décision |
|---|---|
| 1 | Transfert **automatique** des missions clôturées ; la facturation dispose d'une **file de suivi des non-transférées** (contrôle). |
| 2 | Grain de transfert = **mission**, avec **rattachement order** (commande) conservé dans le paquet. |
| 3 | La donnée terrain part **directement**, sans validation régulateur préalable ; **la facturation agrège / corrige** les silos. |
| 4 | **Tout** l'enrichissement part en compta. |
| 5 | Après transfert : **gel côté ambulancier** (édition Vector bloquée). |
| 6 | **Nouveau statut de transfert porté par Orders** ; la **timeline opérationnelle alimente Orders en temps réel** (régulateurs voient l'avancement). |
| 7 / 15 | Non-écrasement de l'officiel ; **`UpdatedAt`** comme watermark de re-synchro (le terrain n'écrase jamais l'ERP officiel). |
| 16 | La compta **tire les octets** (signature, image mutuelle, documents) depuis Vector.Api — pas de blob partagé pour l'instant. |
| A | Atterrissage timeline → **nouvelle table Orders `ORD_MISSION_OPERATIONAL`** (jalons + temps réels) poussée par le mobile ; `MIS_STATUS` admin **dérivé** (go→InProgress, terminate→Done). |
| B | Temps réel régulateur = **persistance Orders + polling UI** au MVP ; **push (SignalR/notifications, spec §15) en V2**. |
| C | **`MIS_TRANSFER_STATUS`** dans Orders : `Transferable` (auto à Closed) → `Transferred` → `Billed` ; **Certification écrit** Transferred/Billed via endpoint Orders.Api. |
| D | **Anomalies non bloquantes** : transférées comme donnée, arbitrées par la compta. Nouvelle table BD Mobile **`MOB_ANOMALY`**. |
| E | Référentiel contrats / attributs facturation = **possédé par le catalogue Mobile** (MOB-13), Certification le consomme. |
| F | **OCR carte mutuelle non bloquant** : saisie manuelle (MUTUELLE P2) suffit à facturer ; l'OCR (P3) enrichit après. |
| G | **Endpoint consolidé unique** `GET /missions/{id}/field-data` (DTO agrégé **versionné**). |

---

## 1. Flux cible

```
┌──────────────┐  enrichit (BD Mobile : timeline, signature, attributs, mutuelle, km, docs, anomalies)
│  Ambulancier │
│  (Vector)    │── (1) push avancement opérationnel ──────────────┐
└──────────────┘                                                  ▼
                                                       ┌────────────────────────┐
                                                       │       Orders            │
                                                       │  ORD_MISSION_OPERATIONAL│ ← régulateurs (polling UI)
                                                       │  MIS_STATUS (dérivé)    │
                                                       │  MIS_TRANSFER_STATUS    │
                                                       └──────────┬──────────────┘
   Closed ⇒ MIS_TRANSFER_STATUS = Transferable (auto)            │
                                                                  │ (3) set Transferred/Billed
┌───────────────────────────┐  (2) GET field-data (pull HTTP)    │
│      Certification         │◄───────────────────────────────── Vector.Api
│  - découvre Transferable   │     {timeline, signature, attributs, mutuelle,
│  - agrège / corrige        │      km, docs, anomalies, version/UpdatedAt}
│  - écrit Transferred ──────┼──────────────────────────────────►│
└───────────────────────────┘                                    
                                  Transferred ⇒ Vector bloque toute édition (gel #5)
```

**Trois nouveaux chemins d'écriture** (n'existaient pas — Vector ne faisait que *lire* Orders) :
1. **Vector → Orders.Api** : projection de l'avancement opérationnel (temps réel).
2. **Certification → Vector.Api** : tirage du paquet `field-data` consolidé (généralise MUTUELLE 2b).
3. **Certification → Orders.Api** : écriture du statut de transfert (`Transferred`/`Billed`).

---

## 2. Évolutions de schéma

### 2.1 Orders — `ORD_MISSION_OPERATIONAL` (nouvelle table, 1:1 mission)

> Migration Database First `027_AddMissionOperationalAndTransfer.sql` sur `BD_ERP_SANITAIRE_DEV`.
> Reflète les 5 jalons de `MOB_MISSION_STATE` (BD Mobile) qui **reste la source détaillée** ;
> Orders en est la **projection** consommable par la régulation. Pas de FK cross-DB.

```
ORD_MISSION_OPERATIONAL
  MIS_ID            UNIQUEIDENTIFIER PK -> ORD_MISSION.MIS_ID
  MOP_ACK_AT        DATETIME2(0) NULL    -- accusé réception
  MOP_READ_AT       DATETIME2(0) NULL    -- mission lue
  MOP_GO_AT         DATETIME2(0) NULL    -- en route   (⇒ MIS_STATUS = InProgress)
  MOP_ONSITE_AT     DATETIME2(0) NULL    -- sur place
  MOP_TERMINATE_AT  DATETIME2(0) NULL    -- terminée   (⇒ MIS_STATUS = Done)
  MOP_SOURCE_CREW_ID UNIQUEIDENTIFIER NULL
  MOP_UPDATED_AT    DATETIME2(0) NOT NULL
```

- **Dérivation `MIS_STATUS`** (règle domaine Orders, pas mobile) : `MOP_GO_AT` non null ⇒ au moins
  `InProgress` ; `MOP_TERMINATE_AT` non null ⇒ `Done`. Transition `Done → Closed` reste une règle
  Orders existante (**DEP-1**, cf. §6) ; le mobile n'écrit jamais `Closed`.
- L'enum `ClMissionStatus` (ToDo/InProgress/Done/Closed) **n'est pas dénaturé** : les jalons fins
  vivent dans la nouvelle table, l'enum reste admin.

### 2.2 Orders — `MIS_TRANSFER_STATUS` (colonne sur `ORD_MISSION`)

```
ALTER TABLE ORD_MISSION ADD
  MIS_TRANSFER_STATUS  INT NOT NULL DEFAULT 0,  -- En MissionTransferStatus
  MIS_TRANSFERRED_AT   DATETIME2(0) NULL,
  MIS_BILLED_AT        DATETIME2(0) NULL
```

```
EnMissionTransferStatus : NotTransferable = 0, Transferable = 1, Transferred = 2, Billed = 3
```

- `NotTransferable` par défaut. **`Transferable` posé automatiquement à l'entrée en `Closed`**
  (règle domaine Orders, sur le même évènement que DEP-1).
- `Transferred` / `Billed` écrits **par Certification** via Orders.Api (§3.3).
- **Gel (#5)** : `Transferred`/`Billed` ⇒ Vector bloque l'édition (§4).

### 2.3 BD Mobile — `MOB_ANOMALY` (nouvelle table, anomalies terrain — spec §17)

> Migration idempotente `MOB_004_Anomaly.sql` (`BD_ERP_MOBILE_APP`, **compte db_owner requis**).

```
MOB_ANOMALY
  ANO_ID           UNIQUEIDENTIFIER PK (NEWSEQUENTIALID)
  ANO_MISSION_ID   UNIQUEIDENTIFIER     -- réf ERP ORD_MISSION (pas de FK cross-DB)
  ANO_TYPE         INT                  -- tel|adresse|patient|admin|impossibilite (En)
  ANO_TEXT         NVARCHAR(MAX) NULL
  ANO_REPORTED_AT  DATETIME2(0) NOT NULL
  ANO_REPORTED_CREW_ID UNIQUEIDENTIFIER NULL
```

- **Non bloquantes** (décision D) : transférées dans le paquet `field-data`, arbitrées par la compta.

---

## 3. Endpoints

### 3.1 Vector → Orders.Api — projection avancement (chemin d'écriture #1)

| Route | Verb | Body | Effet |
|---|---|---|---|
| `PUT /missions/{id}/operational` | PUT | `{ ack?, read?, go?, onsite?, terminate? }` (horodatages) | Upsert `ORD_MISSION_OPERATIONAL` + dérive `MIS_STATUS`. Idempotent. |

- Appelé par Vector **à chaque geste** (en plus de l'écriture BD Mobile `MOB_MISSION_STATE`, qui
  reste la trace détaillée). Côté Vector : étendre `IErpReadApiClient` (devient lecture **+ écriture**,
  ou nouveau `IErpWriteApiClient`) appelé depuis `JobTimeRepository.SaveJobTime`.

### 3.2 Vector.Api — endpoint consolidé `field-data` (chemin de lecture #2)

| Route | Verb | Réponse |
|---|---|---|
| `GET /missions/{id}/field-data` | GET | `ClFieldEnrichmentDtoOut` **versionné** |

`ClFieldEnrichmentDtoOut` (DTO agrégé, **stable et versionné** — `SchemaVersion` + `UpdatedAt` global) :
```
{
  missionId, orderId,                 // rattachement commande (#2)
  schemaVersion, updatedAt,           // watermark de re-synchro (#7/#15)
  timeline:   { ack, read, go, onsite, terminate },
  signature:  { exists, signedAt, imageUrl },
  attributes: { contractCode, values:[{name,value}] },   // overlay MOB-13
  mutuelle:   { mutuelleName, amcCode, concentrateur, teletransmission, imageUrl, ocrStatus },
  kilometers: { value },              // MOB-10
  documents:  [ { id, category, contentType, imageUrl } ],   // MOB-15
  anomalies:  [ { type, text, reportedAt } ]                 // MOB_ANOMALY
}
```
- Les **binaires** (signature, image mutuelle, documents) sont **servis par Vector.Api** via leurs
  `imageUrl` (décision 16) — la compta tire les octets à la demande.
- `updatedAt` = max des `UpdatedAt` de tous les silos → Certification détecte un changement et **re-tire**
  tant que la mission n'est pas `Transferred`.

### 3.3 Certification → Orders.Api — statut de transfert (chemin d'écriture #3)

| Route | Verb | Effet |
|---|---|---|
| `GET /missions?transferStatus=Transferable&from=&to=` | GET | **File de découverte** des missions à transférer (contrôle #1). |
| `PUT /missions/{id}/transfer-status` | PUT `{ status, at }` | Pose `Transferred` puis `Billed`. Garde-fous : transitions monotones, refuse retour arrière. |

---

## 4. Gel terrain (#5) — application

- Avant toute écriture, Vector.Api lit `MIS_TRANSFER_STATUS` de la mission (via Orders.Api).
- Si `Transferred` ou `Billed` ⇒ **409 Conflict** (« mission transférée en facturation, édition close »)
  sur : `PATCH JobEdit`, `POST/PATCH Signature`, `PUT Time`, `PUT operational`, `POST/PATCH MutuelleCard`,
  `POST kilometers`, `POST documents`, `POST anomalies`, `POST Contract`.
- **Fenêtre `Transferable` (Closed, pas encore consommé)** : édition **encore autorisée** ; le watermark
  `updatedAt` permet à Certification de re-tirer avant de geler. Le gel est **au transfert**, pas à la clôture.

---

## 5. Découpage en tickets (TRF-*)

### Lot Orders (module Orders)
| Ticket | Objet | Taille | Statut |
|---|---|---|---|
| **TRF-1** | `034_AddMissionOperationalAndTransfer.sql` : `ORD_MISSION_OPERATIONAL` + `MIS_TRANSFER_STATUS`/`*_AT` + index. Entités EF + Fluent. | S | ✅ livré (SQL **appliqué en BD** 2026-06-22) |
| **TRF-2** | Domaine : `EnMissionTransferStatus` ; `ApplyOperationalProgress` (dérivation `MIS_STATUS` go→InProgress/terminate→Done, avance seule) ; pose auto `Transferable` à l'entrée `Closed` (+ reset au recul `Closed→Done` avant transfert) ; `MarkTransferred`/`MarkBilled` (garde-fous monotones) ; round-trip mapping EF. | S | ✅ livré |
| **TRF-3** | `PUT /missions/{id}/operational` : `ClProjectMissionOperationalCommand/Handler` + `IMissionRepository.UpsertOperationalAsync` (upsert 1:1, jalons coalescés). | M | ✅ livré |
| **TRF-4** | `GET /missions?transferStatus=` (filtre liste + champ `TransferStatus` dans le DTO) + `PUT /missions/{id}/transfer-status` (`ClSetMissionTransferStatusCommand/Handler`, Transferred/Billed seulement). | M | ✅ livré |

> Lot Orders **livré, build solution vert (0/0)**. Reste : tests xUnit Orders (dérivation, auto-Transferable,
> garde-fous transfert) à ajouter au lot test Orders existant. Côté contrat mobile : TRF-5..11.

### Lot Vector (CaSoft.Erp.USVector)
| Ticket | Objet | Taille | Statut |
|---|---|---|---|
| **TRF-5** | Chemin d'écriture Vector→Orders : `IErpWriteApiClient`/`HttpErpWriteApiClient` (`PUT operational`) ; `JobTimeRepository.Save` projette en **best-effort** (échec journalisé, n'altère pas l'écriture locale) ; DI 2ᵉ HttpClient. | M | ✅ livré |
| **TRF-7** | **Gel** : attribut `[FreezeOnTransfer("gJobId")]` + filtre MVC lisant `MIS_TRANSFER_STATUS` (via `GET /missions/{id}`, `TransferStatus` ajouté à `ClMissionDtoOut`) → **409** sur Time/Signature(×3)/JobEdit/Contract/Anomaly. | M | ✅ livré |
| **TRF-8** | `MOB_004_Anomaly.sql` + entité + domaine `ClAnomaly`/`EnAnomalyType` + `IAnomalyRepository` + use case + `ModAnomalyMapping` + repo + endpoints `POST/GET api/missions/{gJobId}/anomalies`. | M | ✅ livré |
| **TRF-10** | Documents/photos : `MOB_005_Document.sql` + entité + domaine `ClDocument`/`EnDocumentCategory` + port + use case + repo + `DocumentController` réécrit (POST multipart / GET liste / GET binaire). | M | ✅ livré |
| **TRF-9** | Kilométrage : **constat** — km est **crew/véhicule-scoped** (`crew.Vehicle.SetKilometers`, persisté via `ICrewRepository`, **pas** de table `MOB_KM`). Surfacé séparément (`KilometersController`, crew-keyed). `field-data.Kilometers = null` au MVP (documenté). | S | ✅ vérifié |
| **TRF-6** | `GET api/missions/{gJobId}/field-data` : `ClFieldEnrichmentDtoOut` versionné + port `IFieldDataReader` + assembleur Infra `FieldDataReader` (timeline+signature+attributs overlay+mutuelle+docs+anomalies, `updatedAt`=max). | L | ✅ livré |
| **TRF-11** | Tests xUnit Vector : `AnomalyRepositoryTests`, `DocumentRepositoryTests`, `FieldDataReaderTests` (assemblage + watermark + null). **24 verts**. | M | ✅ livré |

> Lot Vector **livré, solution USVector build vert (0 err), 24 tests verts**.
> **⚠️ SQL à exécuter (compte db_owner) sur `BD_ERP_MOBILE_APP`** : `MOB_004_Anomaly.sql`, `MOB_005_Document.sql`.
> **MOB-13.12 (purge orphelines)** : laissée différée — les valeurs orphelines sont gelées (post-Transfert) et non affichées (`BuildContractType` ne renvoie que le contrat courant), donc inoffensives ; purge explicite à câbler ultérieurement si besoin.

### Lot Certification (CaSoft.Erp.SanitaryTrypCertification)
| Ticket | Objet | Taille |
|---|---|---|
| **TRF-12** | Worker/service de **découverte** : interroge `GET /missions?transferStatus=Transferable`. | M |
| **TRF-13** | Client HTTP `field-data` (tire le paquet + binaires) ; mapping vers le modèle Certification (pivot **AMC** pour la mutuelle, cf. MUTUELLE_CARD). | L |
| **TRF-14** | Agrégation / correction côté facturation (relecture #3) + écriture `PUT transfer-status` (`Transferred`→`Billed`). | L |
| **TRF-15** | Re-synchro : mémoriser le watermark `updatedAt` ; re-tirer si modifié et non encore `Transferred`. | S |

---

## 6. Dépendances & points à confirmer en cours de route

- **DEP-1 — trigger `Done → Closed` : ✅ tranché (2026-06-22)**. La mission est clôturée par le
  **régulateur** (passage au statut `Closed` via l'endpoint existant `PUT /missions/{id}/status`),
  ce qui arme automatiquement `Transferable` (TRF-2). Aucun code supplémentaire. La facturation
  visualise les non-transférées via `GET /missions?transferStatus=1`.
  - **Différé** : alerter les régulateurs des missions **terminées (`Done`) mais non clôturées**
    (relance de clôture). Piste : requête/dashboard `?status=3` (Done) côté régulation, ou push V2 (spec §15).
- **Couplage assumé** : Certification dépend de Vector.Api (field-data) **et** d'Orders.Api (transfer-status).
  Cohérent avec MUTUELLE 2b déjà acté. Pas de médiateur neutre au MVP (dette assumée).
- **V2 (push, décision B)** : remplacer le polling régulateur par SignalR/notifications (spec §15),
  côté Orders. Non engagé ici.
- **RGPD** : documents de santé + anomalies + carte mutuelle servis par Vector.Api → durcissement
  (rétention, chiffrement, audit) suit MUTUELLE_CARD **P4**.

---

## 7. Clôture MOB-13.12 (purge des valeurs orphelines)

La purge des valeurs d'attributs hors-contrat est désormais **rattachée au transfert** : à l'écriture
`Transferred` (TRF-4/TRF-14), Vector peut purger les `MOB_JOB_ATTRIBUTE_VALUE` orphelines de la mission
(action explicite, déclenchée par l'étape de workflow « transfert facturation » — ce que MOB-13.12
laissait à spécifier). **Déclencheur** : passage `Transferred`. **Portée** : valeurs hors périmètre du
contrat courant de la mission. À implémenter en **TRF-7** (même garde que le gel).

---

**Fin du document**
