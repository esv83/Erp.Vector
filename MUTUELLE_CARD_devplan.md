# 💳 Carte mutuelle — capture terrain, restitution facturation, OCR

> **Objet** : capturer la photo de la carte mutuelle d'un patient depuis le mobile, la conserver,
> la restituer à la facturation, et à terme en extraire automatiquement les quatre champs utiles :
> **nom de la mutuelle, n° AMC, concentrateur, n° de télétransmission**.
>
> **Mise à jour 2026-09-19** — compacté : ce document ne porte plus que l'ouvert, les décisions et le
> contrat. Le livré, daté, est dans [`delivered.md`](delivered.md) ; le suivi dans
> [`devplan.md`](devplan.md) (itérations « Constater et mesurer en production », « Lire la carte mutuelle
> automatiquement », « Protéger les données du patient »).

---

## 1. Ce qui est livré — en bref

**L'ambulancier photographie la carte depuis la mission** et saisit ses quatre champs ; la carte suit
le patient d'un transport à l'autre, historique conservé. **La facturation la reçoit** dans le dossier
terrain, et **Order comme la facturation peuvent l'afficher** sur leurs écrans. **Elle arrive** : 26
captures du 13/09 au soir au 15/09, 13 le 16/09 (journaux de production).

*Le récit — la table vide jusqu'au 13/09 parce que l'app ne pouvait pas appeler la capture, la
capture par mission, les routes des écrans amont, l'incident du 15/09 — est dans
[`delivered.md`](delivered.md) (journal et §8).*

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
| M7 | *(26-27/08/2026)* **La carte est un document consultable** : elle n'alimente **jamais** la colonne `C54` (ID mutuelle), qui n'accepte qu'un numéro issu de l'attribut `AMC`/`MUTUELLE`. Order et BillingGateway la **consultent**, un opérateur lit et décide — mapper un code AMC lu sur une photo contredirait `M5`. L'écran de consultation facturation est **suspendu** tant qu'aucune carte n'arrive. |
| M8 | *(2026-09-13)* **L'app capture par mission**, le serveur résout le patient (mission → commande → bénéficiaire). Garde `M4` : la carte reste rattachée au patient. La route par bénéficiaire est conservée, non recommandée. |
| M9 | *(2026-09-13, précisée le 19/09)* **Les routes image sont ouvertes sans jeton**, pour deux raisons distinctes. `GET api/mutuelle-card/{id}/image` l'était faute de jeton de la facturation : **fermée avec DEC-6** (code du 19/09, à publier) — elle admet la facturation ou l'app, avec leur jeton. `GET api/beneficiaries/{id}/mutuelle-card/image` et `POST api/mutuelle-card/presence` le sont pour un affichage par `<img src>` dans Order et la facturation, qui ne porte jamais de jeton : **elles se referment avec P4**, quand ces écrans passeront à un appel authentifié. |

---

## 3. Ce qui reste

### 3.1 ⏳ Mesurer le remplissage — préalable à P3

L'app capture. Reste à savoir **quelle part des missions** repart avec une carte : c'est ce chiffre qui
dira si l'extraction automatique (§3.2) et l'écran de consultation facturation (`M7`) valent la peine.
⚠️ Le compte `ErpAccount` de `appsettings.json` est refusé depuis le poste de dev (2026-09-13, non
revérifié) : compter en base demande un compte de lecture ; à défaut, les journaux.

### 3.1.b ⏳ Côté facturation — seulement si elle veut les quatre champs

Pour la photo, rien à changer. Pour les champs saisis (nom de mutuelle, AMC, concentrateur,
télétransmission), son `ClFieldEnrichmentDto` ne déclare **ni `Mutuelle` ni `Documents`** : Vector sert
le bloc, sa désérialisation le jette. À lui signaler aussi que ses « 401 » du 27/08 portaient sur deux
routes alors absentes de la production (§4).

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
chiffrement au repos, fermeture des deux routes d'affichage (`M9`), audit des accès. À traiter avec le
même lot que documents et anomalies ([`devplan.md`](devplan.md), itération « Protéger les données du
patient »).

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
| **« Les routes image de Vector répondent 401 »** (constat BillingGateway du 27/08, `ClFieldMutuelleDto`) | Mesure faite sur deux routes **alors absentes de la production** (`api/beneficiaries/{id}/mutuelle-card/image`, `POST api/mutuelle-card/presence`, codées sur `feat/decouplage-dec6-dec7` le 26/08 mais pas publiées) : la politique d'autorisation par défaut répond 401 à toute route inconnue. Elles sont en production, **anonymes**, depuis la fusion du 15/09 (`M9`). À signaler à BillingGateway. |
| **« `ClBeneficiaryDetailDtoOut` ne porte pas de champ mutuelle → pas de home ERP »** | Sans objet depuis 2b : les champs vivent côté Vector et sont tirés par la facturation. |
| **« `DocumentController` mobile = stub »** (constat de départ) | Périmé : les documents et photos sont livrés (TRF-10). |

---

## 5. Contrat exposé (rappel, pour les consommateurs)

| Route | Usage |
|---|---|
| `POST /api/missions/{missionId}/mutuelle-card` | ⭐ **Capture depuis l'app**, **multipart** (champ `file`), `crewId` optionnel en query. Patient résolu côté serveur, mission tracée d'office → `{ Id }`. `404` si mission introuvable ou sans patient. *(en service 2026-09-13)* |
| `GET /api/missions/{missionId}/mutuelle-card` | ⭐ Carte courante du patient de la mission. `404` si aucune. *(en service 2026-09-13)* |
| `POST /api/beneficiaries/{beneficiaryId}/mutuelle-card` | Capture par identifiant patient, traçabilité optionnelle `crewId` / `missionId`. Validation : MIME `image/*`, 8 Mo max. Conservée, non recommandée (`M8`). |
| `GET /api/beneficiaries/{beneficiaryId}/mutuelle-card` | Carte courante : métadonnées + les 4 champs + `imageUrl`. **Jeton requis.** Ne charge pas le binaire (26/08). |
| `GET /api/mutuelle-card/{id}/image` | Les octets d'une carte **désignée**, avec le `Content-Type` d'origine. **Anonyme** (`M9`). |
| `GET /api/beneficiaries/{id}/mutuelle-card/image` | *(codée 26/08, en production depuis le 15/09)* Les octets de la carte **courante** — l'URL stable, qui suit les nouvelles captures. **Anonyme** : Order et la facturation l'affichent par balise `<img src>` (`M9`). |
| `POST /api/mutuelle-card/presence` | *(codée 26/08, en production depuis le 15/09)* `{ beneficiaryIds: [...] }` → pour ceux qui portent une carte : `{ beneficiaryId, capturedAt, imageUrl }` ; les autres sont absents. **Anonyme**, **500** par appel au plus, ni nom de mutuelle ni code AMC. |
| *Pour le dev web* | Upload : `image/*` obligatoire, **8 Mo maximum**, sinon `400` avec le motif — prévoir une compression côté client. `imageUrl` est un **chemin relatif**, à composer avec la base de l'API. |
| `PATCH /api/mutuelle-card/{cardId}` | Saisie manuelle `{ mutuelleName, amcCode, concentrateur, teletransmission }` — remplace les quatre → statut `validated`. |
| `GET /api/missions/{id}/field-data` | Bloc `mutuelle` du dossier terrain (lu par la facturation, non exploité — `M7`). |

---

**Fin du document**
