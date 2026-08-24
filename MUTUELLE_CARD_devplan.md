# 💳 Carte mutuelle — capture terrain, restitution facturation, OCR

> **Objet** : capturer la photo de la carte mutuelle d'un patient depuis le mobile, la conserver,
> la restituer à la facturation, et à terme en extraire automatiquement les quatre champs utiles :
> **nom de la mutuelle, n° AMC, concentrateur, n° de télétransmission**.
>
> **Mise à jour 2026-08-24** — livré résumé en prose, reste détaillé techniquement, pistes
> abandonnées listées au §4. Plan du module : [`devplan.md`](devplan.md) (dont le paquet
> `field-data` qui transporte la carte).

---

## 1. Ce qui est livré

**L'ambulancier photographie la carte mutuelle du patient.** La photo est rattachée au **patient**,
pas à la mission : elle le suit d'un transport à l'autre. Chaque capture est conservée — on garde
l'historique, la plus récente fait foi — avec la trace de qui l'a prise et à quelle occasion.

**Il peut saisir les informations à la main.** Les quatre champs de facturation se renseignent
depuis l'app, sans attendre la lecture automatique. Une saisie humaine vaut validation.

**La facturation récupère l'ensemble.** Carte et champs partent avec le dossier terrain de la
mission ; l'image elle-même est servie à la demande. Le module de facturation les consomme déjà.

*Livré le 2026-06-15 (P1 capture/stockage + P2 restitution et saisie manuelle), 16 tests. Le stockage
est en base Vector ; le pivot vers la mutuelle du référentiel reste le **code AMC**.*

### ⚠️ Livré mais pas encore utilisé

**La table est vide en production (0 ligne au 23/08/2026)** — constat du module de facturation, pour
qui le bloc « mutuelle » du dossier terrain ne remonte donc rien. La chaîne serveur fonctionne ; ce
qui manque est en aval : **vérifier que l'écran mobile expose réellement la capture et la saisie**,
et que les ambulanciers s'en servent. Tant que la table reste vide, la colonne mutuelle du fichier
de facturation restera vide elle aussi.

---

## 2. Décisions actées (2026-06-15)

| # | Décision |
|---|---|
| M1 | **Pivot = code AMC** — c'est lui qui résout la mutuelle côté facturation. C'est donc le champ extrait le plus utile. |
| M2 | **Stockage en base Vector** (binaire, pas de base64 : +33 % évités). Sortie vers un stockage fichier planifiée V2 (`Vd-6`). |
| M3 | **Restitution = la facturation tire en HTTP** (option 2b). Le mobile n'écrit rien dans l'ERP et ne pousse rien. |
| M4 | **Clé = le bénéficiaire**, pas la mission — la carte suit le patient. Historisation assumée. |
| M5 | **OCR = LLM vision (Claude) + validation humaine.** Jamais d'écriture aveugle en facturation. |
| M6 | **RGPD** : MVP simple d'abord, durcissement en phase suivante (dette assumée, P4). |

---

## 3. Ce qui reste

### 3.1 ⏳ Adoption terrain — préalable à tout le reste

Rien à coder côté API. À faire : confirmer avec le dev web que l'écran ambulancier appelle bien
`POST /api/beneficiaries/{beneficiaryId}/mutuelle-card` (multipart) et le `PATCH` de saisie, puis
mesurer le remplissage de `MOB_MUTUELLE_CARD` en production. **C'est le seul point qui débloque de
la valeur immédiate** : l'OCR (§3.2) n'a aucun intérêt tant qu'aucune photo n'arrive.

### 3.2 ⏳ P3 — Extraction automatique (Claude vision)

Aujourd'hui seul le **statut** existe (`MMC_OCR_STATUS` : `none|pending|extracted|validated`,
`MMC_OCR_VALIDATED_AT`) ; **aucun service d'extraction n'est écrit**. Pipeline **asynchrone** — la
capture ne doit jamais attendre l'IA :

1. À l'upload, poser `MMC_OCR_STATUS = pending`.
2. Service d'extraction (worker ou file d'attente, sur le modèle de `OperationalOutboxDispatcher`
   déjà en place) : image → **Claude vision**, avec **sortie structurée imposée** :
   `{ nomMutuelle, numeroAMC, concentrateur, numeroTeletransmission, confiance }`.
   Prompt et schéma stricts ; **journaliser la confiance** retournée.
3. Statut `extracted` → **écran de validation humaine** (à créer, côté web) : l'opérateur confirme ou
   corrige les quatre champs.
4. À la validation → mêmes champs que la saisie manuelle (`ClSetMutuelleFieldsUseCase`, déjà livré) +
   `MMC_OCR_STATUS = validated`, `MMC_OCR_VALIDATED_AT`.

Points à cadrer avant de coder : **où tourne l'appel au modèle** (worker Vector en DMZ, ou service
LAN qui tire l'image — la seconde option évite de donner une clé API à un composant exposé) ;
**quotas et coût** par carte ; comparaison avec Azure Document Intelligence si le volume devient
homogène et élevé.

### 3.3 ⚪ P4 — Durcissement RGPD (différé)

Donnée de santé servie par une API exposée : rétention et purge (3 ans, aligné sur la spec DMZ),
chiffrement au repos, contrôle d'accès fin sur `GET /api/mutuelle-card/{id}/image`, audit des accès.
À traiter avec le même lot que documents et anomalies (cf. [`devplan.md`](devplan.md) §3.5, ligne RGPD).

### 3.4 ⚪ `Vd-6` — Sortir l'image du SQL (V2)

`MMC_IMAGE` (et `MOB_DOCUMENT.DOC_CONTENT`) vers un stockage fichier/objet ; la base ne garde que la
référence et les métadonnées ; migration des blobs existants et purge à 3 ans. Décision V1 assumée :
on reste en blob SQL, le firewall ayant retiré le motif DMZ d'origine.

---

## 4. Retiré du plan — obsolète ou abandonné

| Ce qui a disparu | Motif |
|---|---|
| **« Reste à exécuter `MOB_003` avec un compte db_owner »** | Fait — `MOB_MUTUELLE_CARD` existe en base (vérifié le 2026-08-24 sur `BD_ERP_MOBILE_APP`, 192.168.1.109). |
| **Arbitrage 2a / 2b / 2c** (où atterrissent les champs structurés) | Tranché : **2b**. Les options « ajouter les champs mutuelle au bénéficiaire ERP » et « pousser vers une API dédiée » sont abandonnées. |
| **« Reste côté Certification : client HTTP à l'export »** | Le consommateur n'est pas Certification mais **BillingGateway**, et c'est **fait** : `ClVectorFieldDataClient` appelle `GET /api/missions/{id}/field-data`, qui porte le bloc mutuelle et l'`imageUrl`. |
| **« `ClBeneficiaryDetailDtoOut` ne porte pas de champ mutuelle → pas de home ERP »** | Sans objet depuis 2b : les champs vivent côté Vector et sont tirés par la facturation. |
| **« `DocumentController` mobile = stub »** (constat de départ) | Périmé : les documents et photos sont livrés (TRF-10). |

---

## 5. Contrat exposé (rappel, pour les consommateurs)

| Route | Usage |
|---|---|
| `POST /api/beneficiaries/{beneficiaryId}/mutuelle-card` | Capture, **multipart** (`IFormFile`), traçabilité optionnelle `crewId` / `missionId` → renvoie l'id de la carte. Validation : MIME `image/*`, taille max. |
| `GET /api/beneficiaries/{beneficiaryId}/mutuelle-card` | Carte courante : métadonnées + les 4 champs + `imageUrl`. |
| `GET /api/mutuelle-card/{id}/image` | Les octets, avec le `Content-Type` d'origine. |
| `PATCH /api/mutuelle-card/{cardId}` | Saisie manuelle `{ mutuelleName, amcCode, concentrateur, teletransmission }` → statut `validated`. |
| `GET /api/missions/{id}/field-data` | Bloc `mutuelle` du dossier terrain (chemin réellement emprunté par la facturation). |

---

**Fin du document**
