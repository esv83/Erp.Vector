# Carte mutuelle — capture, stockage, restitution facturation, OCR/IA

> Feature : capturer la photo de la carte mutuelle d'un bénéficiaire depuis le mobile,
> la stocker, la restituer à la facturation, puis (très prochainement) extraire par IA :
> **nom mutuelle, n° AMC, concentrateur, n° télétransmission**.

## Décisions actées (2026-06-15)
- **Facturation** = module **Certification** (`SanitaryTrypCertification`). Pivot = **code AMC**
  (`IMutuelleQueryService.GetMutuelleId(amcCode)` → mutuelle dans la base Urgence).
- **Stockage** : démarrer en **BD Mobile** (migration blob plus tard).
- **OCR/IA** : **LLM vision (Claude)** + validation humaine.
- **RGPD** : MVP simple d'abord, durcissement en phase suivante (dette assumée — voir §6).

## Constat code
- `DocumentController` mobile = stub. `MOB_SIGNATURE` = précédent image base64.
- `ClBeneficiaryDetailDtoOut` (ERP) **ne porte pas** de champ mutuelle/AMC → pas de « home »
  actuel pour les champs extraits côté ERP (à trancher en P2).
- Certification résout la mutuelle par **code AMC** → le champ extrait le plus utile = l'**AMC**.

## P1 — Capture & stockage (BD Mobile) — ✅ LIVRÉ (2026-06-15)

> Code : `MOB_003_MutuelleCard.sql` (table) ; Domain `ClMutuelleCard` ; Application
> `IMutuelleCardRepository` + use case `ClUploadMutuelleCard` + DTO + mapping ;
> Infra `MOB_MUTUELLE_CARD` + `MutuelleCardRepository` ; API `MutuelleCardController`
> (POST multipart / GET métadonnées / GET image) ; DI ; **3 tests xUnit verts**.
> ⚠️ Reste à **exécuter `MOB_003` avec un compte db_owner** (ErpAccount n'a pas CREATE TABLE).


### Table `MOB_MUTUELLE_CARD`
```
MMC_ID               UNIQUEIDENTIFIER PK (NEWSEQUENTIALID)
MMC_BENEFICIARY_ID   UNIQUEIDENTIFIER       -- réf ERP (pas de FK cross-DB)
MMC_IMAGE            VARBINARY(MAX)          -- binaire (multipart) — pas de base64 (+33%)
MMC_CONTENT_TYPE     NVARCHAR(100)
MMC_BYTE_SIZE        INT
MMC_CAPTURED_AT      DATETIME2(0) DEFAULT SYSUTCDATETIME()
MMC_CAPTURED_CREW_ID UNIQUEIDENTIFIER NULL   -- traçabilité (qui)
MMC_MISSION_ID       UNIQUEIDENTIFIER NULL   -- traçabilité (contexte)
-- Champs extraits (P3, nullable au départ)
MMC_MUTUELLE_NAME    NVARCHAR(200) NULL
MMC_AMC_CODE         NVARCHAR(50)  NULL
MMC_CONCENTRATEUR    NVARCHAR(100) NULL
MMC_TELETRANSMISSION NVARCHAR(50)  NULL
MMC_OCR_STATUS       NVARCHAR(20)  NULL      -- none|pending|extracted|validated
MMC_OCR_VALIDATED_AT DATETIME2(0)  NULL
```
> Clé = **bénéficiaire** (la carte le suit d'une mission à l'autre). Historisé (plusieurs lignes
> possibles) ; la plus récente fait foi.

### Endpoints (USVector.Api)
- `POST /api/beneficiaries/{beneficiaryId}/mutuelle-card` — **multipart** (`IFormFile`),
  options de traçabilité (crewId/missionId) → renvoie `MMC_ID`.
- `GET  /api/beneficiaries/{beneficiaryId}/mutuelle-card` — métadonnées de la carte courante.
- `GET  /api/mutuelle-card/{id}/image` — renvoie les octets (Content-Type d'origine).

### Archi (Clean) — réutilise les patterns existants
- Domain : `ClMutuelleCard` (peu de logique).
- Application : `IMutuelleCardRepository` (port) + use cases `Upload`/`GetCurrent`/`GetImage`.
- Infra : `MutuelleCardRepository` (EF, `MobileDbContext`) + entité `MOB_MUTUELLE_CARD`.
- Validation : type MIME image/*, taille max (ex. 8 Mo), bornes.

## P2 — Restitution à la facturation (Certification)
**Point à trancher** : où atterrissent les champs structurés pour que l'export Certification les voie ?
- **2a.** Ajouter les champs mutuelle au **bénéficiaire ERP** (Orders) → l'export les résout via AMC.
  Le plus « propre » mais touche le module Orders/Beneficiary.
- **2b.** Le mobile **expose** carte + champs ; Certification **tire** (HTTP) au moment de l'export.
  Autonome côté mobile, mais couple l'export au mobile.
- **2c.** Le mobile **pousse** vers une API Certification dédiée (réception mutuelle).

Quel que soit le choix : le mobile reste **client HTTP** (cohérent avec le découplage). La photo
elle-même est servie par le mobile (`GET image`) tant qu'on ne migre pas vers un blob partagé.

## P3 — OCR / IA (Claude vision)
Pipeline **asynchrone** (la capture n'attend jamais l'IA) :
1. Upload (P1) → `MMC_OCR_STATUS = pending`.
2. Service d'extraction : image → Claude (vision) avec **sortie structurée** :
   `{ nomMutuelle, numeroAMC, concentrateur, numeroTeletransmission, confiance }`.
3. `extracted` → **écran de validation humaine** (jamais d'écriture aveugle en facturation).
4. Validé → alimente P2 + `MMC_OCR_STATUS = validated`.

Notes : modèle Claude vision le plus récent ; prompt + schéma stricts ; journaliser la confiance.
Comparaison Azure Document Intelligence possible si volume homogène/élevé.

## P4 (différé) — Durcissement RGPD
Rétention/purge, chiffrement au repos, contrôle d'accès fin, audit des accès (donnée de santé).

## Ordre proposé
**P1** (capture+stockage, livrable et testable) → **P2** (décider 2a/2b/2c puis restituer) →
**P3** (OCR Claude + validation) → **P4** (RGPD).
