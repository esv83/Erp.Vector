# 💳 Carte mutuelle — capture terrain, restitution facturation, OCR

> **Objet** : capturer la photo de la carte mutuelle d'un patient depuis le mobile, la conserver,
> la restituer à la facturation, et à terme en extraire automatiquement les quatre champs utiles :
> **nom de la mutuelle, n° AMC, concentrateur, n° de télétransmission**.
>
> **Mise à jour 2026-09-13** — la cause de la table vide est trouvée (§1) : l'app ne pouvait pas
> appeler la capture. Capture **par mission** ajoutée (§3.1, §5), décision `M7` de la facturation
> reportée (§2), fausse mesure « route image fermée » corrigée (§4). Plan du module :
> [`devplan.md`](devplan.md) (dont le paquet `field-data` qui transporte la carte).

---

## 1. Ce qui est livré

**L'ambulancier photographie la carte mutuelle du patient.** La photo est rattachée au **patient**,
pas à la mission : elle le suit d'un transport à l'autre. Chaque capture est conservée — on garde
l'historique, la plus récente fait foi — avec la trace de qui l'a prise et à quelle occasion.

**Il peut saisir les informations à la main.** Les quatre champs de facturation se renseignent
depuis l'app, sans attendre la lecture automatique. Une saisie humaine vaut validation.

**La facturation lit l'ensemble.** Carte et champs partent avec le dossier terrain de la mission ;
BillingGateway déclare le bloc depuis `d048558` — mais n'en fait rien pour l'instant (§2, `M7`).

*Livré le 2026-06-15 (P1 capture/stockage + P2 restitution et saisie manuelle), 16 tests. Le stockage
est en base Vector ; le pivot vers la mutuelle du référentiel reste le **code AMC**.*

### ⚠️ Livré mais inatteignable depuis l'app — cause trouvée le 2026-09-13

**Aucune carte en production** : 1 344 paquets terrain sur 1 344 portent `Mutuelle: null` du 24 au
27/08 (mesure de la facturation, 739 bénéficiaires). **La cause est dans le contrat, pas dans
l'usage** : la capture était exposée par **bénéficiaire** (`POST api/beneficiaries/{id}/mutuelle-card`),
et **aucun DTO servi au terrain ne porte l'identifiant du patient** — `ClPatientDto` n'a que nom,
DDN, âge et téléphones ; `ClMission.ContactId` est calculé mais jamais projeté. Le dev web n'avait
d'ailleurs reçu aucune note sur la carte, et `AppMobile_specifications.md` ne la mentionne pas.

---

## 2. Décisions actées

| # | Décision |
|---|---|
| M1 | **Pivot = code AMC** — c'est lui qui résout la mutuelle côté facturation. C'est donc le champ extrait le plus utile. |
| M2 | **Stockage en base Vector** (binaire, pas de base64 : +33 % évités). Sortie vers un stockage fichier planifiée V2 (`Vd-6`). |
| M3 | **Restitution = la facturation tire en HTTP** (option 2b). Le mobile n'écrit rien dans l'ERP et ne pousse rien. |
| M4 | **Clé = le bénéficiaire**, pas la mission — la carte suit le patient. Historisation assumée. |
| M5 | **OCR = LLM vision (Claude) + validation humaine.** Jamais d'écriture aveugle en facturation. |
| M6 | **RGPD** : MVP simple d'abord, durcissement en phase suivante (dette assumée, P4). |
| M7 | *(BillingGateway, 27/08/2026)* **La carte est un document consultable** : elle n'alimente **jamais** la colonne `C54` (ID mutuelle), qui n'accepte qu'un numéro issu de l'attribut `AMC`/`MUTUELLE`. L'écran de consultation facturation est **suspendu** tant qu'aucune carte n'arrive. |
| M8 | *(2026-09-13)* **L'app capture par mission**, le serveur résout le patient (mission → commande → bénéficiaire). Garde `M4` : la carte reste rattachée au patient. La route par bénéficiaire est conservée, non recommandée. |
| M9 | *(2026-09-13)* **`GET api/mutuelle-card/{id}/image` reste ouverte sans jeton** jusqu'à P4, bien qu'aucun consommateur ne s'en serve aujourd'hui. Dette assumée, à refermer avec P4 / DEC-6. |

---

## 3. Ce qui reste

### 3.1 ⏳ Adoption terrain — préalable à tout le reste

La capture par mission est **codée et testée, pas déployée** (`MutuelleCardController` :
`POST`/`GET api/missions/{missionId}/mutuelle-card`, `ClUploadMissionMutuelleCardUseCase`,
`ClGetMissionMutuelleCardUseCase`, `MissionBeneficiaryQueryService`, 8 tests).

1. **Déployer** Vector.Api.
2. **Transmettre** [`note_web_alexandre_carte_mutuelle.md`](note_web_alexandre_carte_mutuelle.md) au
   dev web, avec la date de mise en service.
3. **Mesurer** le remplissage de `MOB_MUTUELLE_CARD` après la livraison de l'écran. ⚠️ Le compte
   `ErpAccount` de `appsettings.json` est refusé depuis le poste de dev (2026-09-13) : mesurer via le
   paquet `field-data`, ou fournir un compte de lecture.

**C'est le seul point qui débloque de la valeur immédiate** : l'OCR (§3.2) et l'écran de consultation
facturation (`M7`) n'ont aucun intérêt tant qu'aucune photo n'arrive.

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
homogène et élevé. Rappel `M7` : même validés, ces champs n'alimentent pas `C54`.

### 3.3 ⏳ Saisie manuelle : deux faiblesses connues

- Le `PATCH` **remplace** les quatre champs (un champ absent repasse à `null`) et accepte un corps
  vide, qu'il marque pourtant `validated`. Signalé au dev web ; une validation FluentValidation reste
  à décider.
- Une nouvelle photo crée une carte aux champs vides : la saisie précédente n'est pas reportée.

### 3.4 ⚪ P4 — Durcissement RGPD (différé)

Donnée de santé servie par une API exposée : rétention et purge (3 ans, aligné sur la spec DMZ),
chiffrement au repos, contrôle d'accès fin sur `GET /api/mutuelle-card/{id}/image` (ouverte sans jeton,
`M9`), audit des accès. À traiter avec le même lot que documents et anomalies (cf.
[`devplan.md`](devplan.md) §3.5, ligne RGPD).

### 3.5 ⚪ `Vd-6` — Sortir l'image du SQL (V2)

`MMC_IMAGE` (et `MOB_DOCUMENT.DOC_CONTENT`) vers un stockage fichier/objet ; la base ne garde que la
référence et les métadonnées ; migration des blobs existants et purge à 3 ans. Décision V1 assumée :
on reste en blob SQL, le firewall ayant retiré le motif DMZ d'origine.

---

## 4. Retiré du plan — obsolète, abandonné ou faux

| Ce qui a disparu | Motif |
|---|---|
| **« Reste à exécuter `MOB_003` avec un compte db_owner »** | Fait — `MOB_MUTUELLE_CARD` existe en base (vérifié le 2026-08-24 sur `BD_ERP_MOBILE_APP`, 192.168.1.109). |
| **Arbitrage 2a / 2b / 2c** (où atterrissent les champs structurés) | Tranché : **2b**. Les options « ajouter les champs mutuelle au bénéficiaire ERP » et « pousser vers une API dédiée » sont abandonnées. |
| **« La facturation consomme déjà le bloc mutuelle et son `imageUrl` »** (écrit ici jusqu'au 13/09) | Faux jusqu'au 27/08 : BillingGateway ne déclarait pas le bloc et le jetait en silence (`T1`/`T2` de [`TRACABILITE_SAISIES_VECTOR_EXPORT.md`](TRACABILITE_SAISIES_VECTOR_EXPORT.md)). Depuis `d048558` le bloc est lu, mais aucune colonne ni aucun écran ne l'exploite (`M7`). |
| **« Aucune photo n'arrive : vérifier que les ambulanciers s'en servent »** | La cause n'était pas l'usage : la route était inatteignable depuis l'app (§1). |
| **« Les routes image de Vector répondent 401 »** (constat BillingGateway du 27/08, `ClFieldMutuelleDto`) | Mesure faite sur deux routes **qui n'existent pas** (`api/beneficiaries/{id}/mutuelle-card/image`, `POST api/mutuelle-card/presence`) : la politique d'autorisation par défaut répond 401 à toute route inconnue. La vraie route, `api/mutuelle-card/{id}/image`, est **ouverte** (`M9`). À signaler à BillingGateway. |
| **« `ClBeneficiaryDetailDtoOut` ne porte pas de champ mutuelle → pas de home ERP »** | Sans objet depuis 2b : les champs vivent côté Vector et sont tirés par la facturation. |
| **« `DocumentController` mobile = stub »** (constat de départ) | Périmé : les documents et photos sont livrés (TRF-10). |

---

## 5. Contrat exposé (rappel, pour les consommateurs)

| Route | Usage |
|---|---|
| `POST /api/missions/{missionId}/mutuelle-card` | ⭐ **Capture depuis l'app**, **multipart** (champ `file`), `crewId` optionnel en query. Patient résolu côté serveur, mission tracée d'office → `{ Id }`. `404` si mission introuvable ou sans patient. *(non déployé)* |
| `GET /api/missions/{missionId}/mutuelle-card` | ⭐ Carte courante du patient de la mission. `404` si aucune. *(non déployé)* |
| `POST /api/beneficiaries/{beneficiaryId}/mutuelle-card` | Capture par identifiant patient, traçabilité optionnelle `crewId` / `missionId`. Validation : MIME `image/*`, 8 Mo max. Conservée, non recommandée (`M8`). |
| `GET /api/beneficiaries/{beneficiaryId}/mutuelle-card` | Carte courante : métadonnées + les 4 champs + `imageUrl`. |
| `GET /api/mutuelle-card/{id}/image` | Les octets, avec le `Content-Type` d'origine. **Anonyme** (`M9`). |
| `PATCH /api/mutuelle-card/{cardId}` | Saisie manuelle `{ mutuelleName, amcCode, concentrateur, teletransmission }` — remplace les quatre → statut `validated`. |
| `GET /api/missions/{id}/field-data` | Bloc `mutuelle` du dossier terrain (lu par la facturation, non exploité — `M7`). |

---

**Fin du document**
