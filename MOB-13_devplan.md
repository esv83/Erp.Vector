# MOB-13 — Édition des attributs de mission (overlay BD Mobile)

> Devplan. Décision d'archi actée : **persistance overlay en BD Mobile** (aucune écriture ERP).
> Périmètre : commentaires, coordonnées patient (tél/mail), type de contrat + attributs de
> facturation dynamiques, et statut/horaires (déjà fait, hors lot — voir §6).

## 1. État des lieux (constat)

| Élément | État actuel | Fichier |
|---|---|---|
| `PATCH api/JobEdit/{gJobId}` | ❌ `JobRepository.Save` → `throw NotImplementedException("MOB-13")` | `Infrastructure/Repositories/Erp/JobRepository.cs:92` |
| `GET api/FormStructure/{gJobId}` | ❌ renvoie 0 attribut (`ContractType` vide) | `JobRepository.cs:73` (`new ClContractType()`) |
| `IContractTypeRepository` / `IAttributsRepository` / `IInvoicingRepository` | esquissés, **non implémentés** | `Application/Abstractions/*` |
| BD Mobile `BD_ERP_MOBILE_APP` | ✅ existe (MOB_SESSION, MOB_MISSION_STATE, MOB_SIGNATURE) | `Infrastructure/Sql/MOB_001_Initial.sql` |

**Cause** : la feature attributs dynamiques est un stub des deux côtés (lecture + écriture).
Ce n'est **pas** un problème de base de données.

## 2. Modèle de domaine existant (à réutiliser tel quel)

- `ClJob.ContractType : ClContractType` → `{ Id, Display, Attributs : ClAttributCollection }`.
- `ClAttributCollection : Dictionary(Of String, ClContractAttribut)` (clé = `Name`).
- `ClContractAttribut` : `Name, Label, FieldType, Index, Required, PlaceHolder, InstantUpdate, IsList, Options, Value`.
- `ClJob.UpdateAttribute(name, value)` mute `ContractType.Attributs(name).Value` (no-op si clé absente).
- DTO API entrant (PATCH) : `List<ClAttributValueModel> { AttributName, AttributValue }`.
- DTO API sortant (FormStructure) : `List<ClMobileAppFieldModel>`.

➡️ Le contrat de données mobile est déjà bon. Il manque **la source du catalogue** et **le stockage des valeurs**.

## 3. Schéma overlay — nouvelles tables BD Mobile

Script idempotent `MOB_002_JobAttributes.sql`.

### 3.1 Catalogue (définit le formulaire dynamique — qu'on possède côté Mobile)
```
MOB_CONTRACT_TYPE
  CTT_ID            INT IDENTITY PK
  CTT_CODE          NVARCHAR(50)  UNIQUE
  CTT_DISPLAY       NVARCHAR(200)
  CTT_ACTIVE        BIT DEFAULT 1

MOB_CONTRACT_ATTRIBUTE                          -- attribut défini UNE fois
  CAT_ID            INT IDENTITY PK
  CAT_NAME          NVARCHAR(50) UNIQUE         -- COMMENTS, PHONES, MAILS, REFERENCE, ...
  CAT_LABEL         NVARCHAR(200)
  CAT_FIELD_TYPE    NVARCHAR(30)                -- text|textarea|checkbox|list|phone|email|number|date
  CAT_INDEX         INT
  CAT_REQUIRED      BIT
  CAT_PLACEHOLDER   NVARCHAR(200) NULL
  CAT_INSTANT_UPDATE BIT DEFAULT 0
  CAT_IS_MULTI      BIT DEFAULT 0               -- saisie répétable (tél/mail)
  CAT_IS_GLOBAL     BIT DEFAULT 0               -- 1 = appliqué à TOUS les contrats

MOB_CONTRACT_ATTRIBUTE_CONTRACT                 -- liaison N..N (attribut non global -> 1..N contrats)
  CAC_ATTRIBUTE_ID  INT -> CAT_ID
  CAC_CONTRACT_ID   INT -> CTT_ID
  PK (CAC_ATTRIBUTE_ID, CAC_CONTRACT_ID)

MOB_CONTRACT_ATTRIBUTE_OPTION                   -- pour CAT_FIELD_TYPE = 'list'
  CAO_ID            INT IDENTITY PK
  CAO_ATTRIBUTE_ID  INT -> CAT_ID
  CAO_KEY           INT
  CAO_LABEL         NVARCHAR(200)
```
> Applicabilité d'un attribut : `CAT_IS_GLOBAL=1` (tous les contrats) **ou** liaisons dans
> `MOB_CONTRACT_ATTRIBUTE_CONTRACT` (un ou plusieurs contrats). Couvre les 3 cas demandés.

### 3.2 Overlay (saisies terrain par mission — la vraie donnée éditée)
```
MOB_JOB_CONTRACT                               -- contrat sélectionné pour la mission
  JCT_MISSION_ID    UNIQUEIDENTIFIER PK         -- réf ERP ORD_MISSION (pas de FK cross-DB)
  JCT_CONTRACT_ID   INT -> CTT_ID
  JCT_UPDATED_AT    DATETIME2(0)

MOB_JOB_ATTRIBUTE_VALUE                          -- 1 ligne par (mission, attribut)
  JAV_MISSION_ID    UNIQUEIDENTIFIER
  JAV_ATTRIBUTE_NAME NVARCHAR(50)
  JAV_VALUE         NVARCHAR(MAX) NULL           -- listes (tél/mail) sérialisées JSON
  JAV_UPDATED_AT    DATETIME2(0)
  PK (JAV_MISSION_ID, JAV_ATTRIBUTE_NAME)
```

> `COMMENTS`, `PHONES`, `MAILS` = attributs **core** (`CAT_CONTRACT_ID = NULL`), toujours
> présents quel que soit le contrat. Tout passe par le même mécanisme uniforme.
> Coordonnées patient : valeur **initiale** lue depuis l'ERP (lecture seule), valeur **éditée**
> stockée dans l'overlay ; l'overlay prime à l'affichage si présent.

## 4. Découpage en tickets — état de livraison

> Livré, compilé, déployé sur `\\192.168.1.112\dev_api\Vector.Api` et **validé à l'exécution**
> (GET FormStructure + PATCH JobEdit testés sur mission réelle, persistance overlay confirmée
> le 2026-06-14). Schéma `MOB_002` appliqué en BD.

| Ticket | Objet | Statut |
|---|---|---|
| **MOB-13.1** | `MOB_002_JobAttributes.sql` : tables catalogue + overlay, idempotent | ✅ livré + **appliqué en BD** |
| **MOB-13.2** | Seed du catalogue | ⚠️ **minimal provisoire** (1 contrat STANDARD + core + 1 exemple par type de contrôle). Vrai catalogue métier à fournir (§7.1) |
| **MOB-13.3** | Entités EF (6) + mappings `MobileDbContext` | ✅ livré |
| **MOB-13.4** | Accès overlay (`Repositories/Mobile/JobAttributeOverlayRepository`) | ✅ livré — **écart assumé** : port ciblé `IJobAttributeOverlay` au lieu des 3 interfaces legacy `IContractTypeRepository`/`IAttributsRepository`/`IInvoicingRepository` (laissées en l'état ; `Invoicing`/`UpdateCommande` lèvent encore) |
| **MOB-13.5** | `JobRepository.GetJob` : `ContractType` peuplé (catalogue + overlay) | ✅ livré |
| **MOB-13.6** | `JobRepository.Save` : upsert overlay ; `throw` retirés | ✅ livré |
| **MOB-13.7** | DI : `IJobAttributeOverlay` enregistré (`Program.cs`) | ✅ livré |
| **MOB-13.8** | Sélection du contrat : `GET/POST api/Contract/{jobId}` → `MOB_JOB_CONTRACT` ; use cases `ClListContractsUseCase`/`ClSelectContractUseCase` + DTO `ClContractChoiceDto` | ✅ livré (DTO propres, **pas** les `ClContractType*Model` legacy) |
| **MOB-13.9** | Nettoyage/validation | ✅ livré — `Save` ne crée plus de lignes vides (obs. 1) ; le PATCH rejette les attributs hors contrat au lieu de les ignorer. *(Required à l'enregistrement final = différé, dépend de l'étape « finalize/transfert facturation », cf. 13.12.)* |
| **MOB-13.10** | Tests xUnit + FluentAssertions (overlay) | ✅ livré — projet `CaSoft.Erp.USVector.Tests` (EF InMemory), **11 tests verts** : BuildContractType (global/lié, options, défaut, fusion multi+baseline dédoublonnée), Save (scalaire, skip-empty, multi hors-ERP, update), sélection de contrat (upsert/inconnu). |
| **MOB-13.11** | `InstantUpdate` | ✅ **couvert sans code** — flag exposé dans `FormStructure` ; le front déclenche le `PATCH JobEdit` existant sur le champ concerné. |

### Écarts vs plan initial (actés)
- **Modèle d'applicabilité N..N** : un attribut est défini une fois (`CAT_NAME` unique) puis soit
  `CAT_IS_GLOBAL=1` (tous contrats), soit lié à 1..N contrats via `MOB_CONTRACT_ATTRIBUTE_CONTRACT`.
  Remplace le `CAT_CONTRACT_ID` unique du plan initial (qui ne gérait pas « plusieurs contrats »).
- **Port ciblé** `IJobAttributeOverlay` (BuildContractType + Save + GetContracts/Select) au lieu des
  3 interfaces legacy. Plus simple, suffisant pour lecture/écriture overlay + sélection.
- **Type d'affichage** exposé au dev web : chaque champ porte `Type` (text/textarea/checkbox/list/
  phone/email…), `IsMulti` (saisie répétable) et `Options` (pour `Type='list'`).

### Observations à traiter (non bloquantes)
1. `JobRepository.Save` persiste **tous** les attributs du catalogue, même non soumis (lignes vides).
   → à affiner (n'upserter que renseigné/changé) — candidat 13.9.
2. Règle « tél/mail ERP verrouillés + dédoublonnage » codée mais **non encore exercée** (bénéficiaire
   de test sans tél ERP) — à valider sur une mission avec coordonnées ERP.

## 5. Chaîne cible (après MOB-13)

```
GET  api/FormStructure/{jobId}
  → ClGetJobEditFormStructureUseCase
  → JobRepository.GetJob → ContractType = catalogue(contrat sélectionné) ⊕ overlay(valeurs)
  → List<ClMobileAppFieldModel>   ✅ formulaire rempli

PATCH api/JobEdit/{jobId}  [ {AttributName, AttributValue}, ... ]
  → ClUpdateJobEditUseCase → job.UpdateAttribute(...) → JobRepository.Save(job)
  → upsert MOB_JOB_ATTRIBUTE_VALUE              ✅ persistance overlay

GET/POST api/Contract/{jobId}
  → liste des contrats + enregistrement du contrat choisi (MOB_JOB_CONTRACT)  ✅
```

## 6. Hors lot (déjà fonctionnel — ne pas re-coder)

- `PATCH api/Time/{id}` (statut/horaires) → `MOB_MISSION_STATE` ✅
- `POST api/Signature` → `MOB_SIGNATURE` ✅
- À garder tel quel ; juste vérifier la cohérence d'affichage avec les nouveaux attributs.

## 7. Questions ouvertes (à trancher avant 13.2)

1. **Contenu du catalogue** : quels types de contrat et quels attributs de facturation
   seeder ? (récupérer la définition legacy `T_CONTRACT*` de l'ancienne app, ou liste fournie ?)
   → bloquant uniquement pour 13.2/13.8, pas pour l'ossature 13.1/13.3-13.7.
2. **Coordonnées patient éditées** : overlay pur (ERP jamais mis à jour) confirmé ? Risque de
   divergence ERP↔terrain assumé ?
3. **`InstantUpdate`** : nécessaire au MVP (13.11) ou plus tard ?

## 8. Reste à faire

Ossature (13.1→13.10) **livrée et déployée**. Reste :
- **13.2 (vrai catalogue)** — donnée métier, alimentée par l'utilisateur (déjà : STANDARD + ART80).
- **13.12 (purge des valeurs orphelines)** — au changement de contrat, les valeurs overlay hors
  du nouveau périmètre sont **conservées** (PAS de purge auto). La purge est une **action explicite** :
  déclenchée par un utilisateur disposant du rôle adéquat, OU à une étape précise du workflow mission
  (ex. transfert en facturation). À spécifier : déclencheurs, rôle requis, portée de la purge.

### Comportement acté — valeurs orphelines
Changer le contrat d'une mission **ne supprime pas** les valeurs des attributs qui ne font plus
partie du contrat : `BuildContractType` ne renvoie que les attributs du contrat courant (les
orphelines ne s'affichent pas) mais elles restent en base jusqu'à une purge explicite (13.12).
