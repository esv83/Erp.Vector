# 📱 devplan — Vector (module terrain ambulanciers)

> **Ne porte que les sujets ouverts** : en attente, bloqué, envisagé, décidé mais pas fait. Ce que le
> module fait, le journal daté, les décisions appliquées, la configuration et les pistes retirées sont
> dans [`delivered.md`](delivered.md).
>
> **Prod** : `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`) · **Dépôt** :
> `github.com/esv83/Erp.Vector` (`USVector.sln`) · **117 tests verts** (2026-09-13).
> **Régénéré le** 2026-09-13 (compact devplan).
>
> **Règle de travail** : on code neutre ou additif, jamais de rupture du contrat consommé par l'app
> web (D14, [`delivered.md`](delivered.md) §4).

| | Sens |
|---|---|
| ⏳ | à faire, rien ne bloque |
| 🟡 | engagé, reste une partie |
| ⛔ | bloqué (décision, contenu métier, ou livrable d'un autre module) |
| ⚪ | différé V2 |
| ⚠️ | dette / garde-fou |

---

# 0. À faire en premier

| # | Action | Pourquoi maintenant |
|---|---|---|
| 1 | **Déployer `main`** (messages du sélecteur §C3, abandon des missions inconnues d'Orders §G9) | La publication du 13/09 après `8a0da0b` n'est pas arrivée : la production tourne toujours `0122b7a` (vérifié à 16:19). |
| 2 | **Transmettre au dev web** : [carte mutuelle](note_web_alexandre_carte_mutuelle.md) (§F1), [routes retirées](note_web_alexandre_routes_retirees.md), [sélecteur d'équipage](docs/ui-web/UI_selection-equipage-multi-crew.md) (bouton *Réessayer*, §C3) | Les routes mutuelle sont en service ; tant que l'écran ne les appelle pas, aucune carte n'arrive. |

---

# 1. Chapitres

| Chapitre | Nature | Attaquable maintenant ? |
|---|---|---|
| **A** — Contexte de mission | suites de la bascule vers Order | A3 en attente du dev web |
| **B** — Dépendances amont Orders | rien à coder ici : suivre, réclamer | — |
| **C** — Identité & authentification | sécurité, chaîne de connexion | oui (C2) ; C1 et C3 sur décision |
| **D** — Robustesse des appels sortants | plomberie HTTP | **oui, isolé** |
| **E** — Chaîne facturation | contrat du paquet terrain | oui (E2) ; E1 après arbitrage |
| **F** — Fonctions terrain | features indépendantes | oui |
| **G** — Dette & hygiène | refactor, zéro changement de contrat | **oui, en continu** |
| **H** — Différé V2 | — | non |

---

## A. Contexte de mission — suites de la bascule vers Order

> ⚠️ **Deux numérotations `OC-` coexistent.** Un `OC-x` nu désigne la tâche **Vector** ; la tâche
> Order s'écrit `Order OC-x` ([plan Order](../Erp.Order/feature_order_context_devplan.md) §7).

> **En deux phrases.** Depuis le 25/08, le type de mission et le questionnaire d'attributs viennent
> d'Order, qui peut **refuser** une saisie ou **verrouiller** un champ. Vector transmet déjà tout
> cela ; l'écran affiche les refus (confirmé le 2026-09-13), A3 attend encore deux améliorations
> d'affichage — rien à coder ici.

### A3 — ⏳ Deux améliorations d'écran demandées — *attente dev web*

L'API envoie déjà l'information ; c'est l'affichage qui manque.

1. **Dire pourquoi un champ est grisé.** Un champ verrouillé arrive avec son motif (par exemple
   « déjà renseignée sur la fiche »). Sans ce texte, l'ambulancier voit un champ bloqué sans savoir
   pourquoi ni à qui s'adresser.
2. **Faire relire le n° de sécurité sociale avant d'enregistrer.** La fiche patient ne le fournit
   presque jamais (0 fois sur 40 missions mesurées le 26/08) : c'est l'ambulancier qui le tape. Une
   fois enregistré, **aucun module ne permet de le corriger** — une faute de frappe part en
   facturation. Un écran de confirmation suffit.

Demandé au dev web le 26/08. **Fin** : les deux constatés sur l'app.


---

## B. Dépendances amont — rien à coder dans ce dépôt

| Réf | Ce qui manque | Effet visible côté terrain | État |
|---|---|---|---|
| **B2** | **Repli sur le snapshot `ORD_ORDER`** dans le chemin de lecture d'Orders — plan code-only : [`plan_correctif_vector_fallback_snapshot.md`](plan_correctif_vector_fallback_snapshot.md) | **~3 883 étapes de mission s'affichent vides** ; résiduel attendu ~93 | ⏳ à coder dans `Erp.Order` |
| **B4** | `Billed` n'a **aucun écrivain** | palier théorique | ⛔ décision (E4) |
| **B5** | **`field-data` par période** (à l'image de `for-export`) | 14,7 s pour 284 missions, sur un clic | ⏳ non engagé côté demandeur |
| **B6** | **Tests xUnit du transfert côté Orders** : dérivation `MIS_STATUS`, pose de `Transferable`, garde-fous de `MarkTransferred` / `MarkBilled` | aucun filet aujourd'hui | ⏳ |
| **B7** | **Relance de clôture** des missions terminées non clôturées | dossiers qui n'arrivent jamais en facturation | ⏳ piste : tableau de bord `?status=Done` |
| **B8** | **Adresses « non structurées »** (`DET-3`) côté Orders / Address.Api | repli mono-ligne, WARNING journalisé | ⏳ mesurer l'ampleur d'abord |
| **B9** | **Applicabilité agence/mode non configurée** : `ORD_ORDER_CONTEXT_AGENCE` et `_MODE` vides → les 7 types proposés partout (mesuré le 2026-08-25) | « Secours sur piste » proposé sur des missions sans rapport | ⛔ décision métier : la **matrice**. ⚠️ la première liaison posée sur un type le restreint aux seules valeurs liées. Aucun écran ne gère ces liaisons |
| **B10** | **Attributs au catalogue Order rattachés à rien** : `COMMENTS`, `PHONES`, `MAILS`, `PMT`, `SMUR_DE`, `COMMUNE`, `NOM_CENTRALE` n'atteignent aucune mission (30 formulaires, 2026-08-25). `NOM_ASSISTANCE` déclaré `list`, servi `text` | commentaire libre et ajout téléphone/e-mail perdus sur **toutes** les missions ; `PMT` disparu | ⛔ paramétrage, famille de B9. `REFERENCE` et `URGENT` sans équivalent, à arbitrer |

> Contrat détaillé de ce que Vector attend d'Orders : [`endPoint.md`](endPoint.md).

---

## C. Identité & authentification

### C1 — ⛔ Écran de rattachement compte Keycloak ↔ ambulancier — *choisir l'hôte*

Aujourd'hui **INSERT SQL manuel** dans `PER_KEYCLOAK_MAP` : seul maillon manuel de la chaîne.
**Mesuré le 2026-09-13 : 8 membres d'équipage sur 244** (équipages à plusieurs membres des 7 derniers
jours) n'ont aucun compte rattaché — ils reçoivent un 403 au sélecteur. Les
endpoints Orders sont livrés ; le **module Identity possède désormais la correspondance** (reprise du
23/08/2026 : 146 pivots, 105 correspondances).
→ **Identity** (cible) **ou** endpoints Orders le temps de la bascule. Dans les deux cas : lister les
comptes, rechercher un `PER_PERSONNEL`, persister **via API**, afficher les garde-fous (409).
⚠️ Tant que trois emplacements coexistent, ils divergeront.

### C2 — ⏳ Authentification de service à service (`DEC-6`) — *les deux sens*

**Sortant** : `Orders.Api` est appelée **sans jeton**. Le jour où elle est protégée, il faut un
**client credentials Keycloak** avec cache et renouvellement ; symptôme sinon : des 401 sur la joblist
en production, sans autre indice.

**Entrant** : quatre routes répondent sans jeton, uniquement parce que la facturation les tire sans
jeton :

| Route ouverte | Ce qu'elle expose |
|---|---|
| `GET api/missions/{id}/field-data` | le dossier terrain complet |
| `GET api/Signature/{id}` | l'image de la signature du patient |
| `GET api/documents/{id}/content` | les octets d'un document |
| `GET api/mutuelle-card/{id}/image` | ⚠️ **carte mutuelle — donnée de santé** (maintenue ouverte, `M9`) |

Elles se referment **ensemble**, avec `DEC-6`. `AnonymousSurfaceTests` fige la liste.

### C3 — ⛔ 404 du sélecteur avant la composition de l'équipage — *décision*

**Diagnostic tranché le 2026-09-13** ([`delivered.md`](delivered.md)) : **ni défaut de code, ni donnée
fausse chez Orders.** Le 404 « Aucun équipage actif » survient quand l'ambulancier ouvre l'app **avant
que la régulation ait composé son équipage** ; il passe dès que c'est fait. Mesuré sur les journaux
du 04/07 au 24/08 : 24 cas, 21 équipages, délai médian de **23 min** entre la tentative et la
composition, 9 cas au-delà d'une heure. Quand l'équipage entier est composé en retard, **les deux
membres échouent ensemble** — d'où l'impression d'un « second membre » bloqué.

1. 🟡 **Le message terrain — décidé et codé le 2026-09-13, à déployer.** Le 404 de
   `CrewController.Mine` dit désormais « Votre équipage n'est pas encore composé par la régulation.
   Réessayez dans quelques minutes ; si rien ne change, appelez la régulation. » Même code, texte
   seul (D14). **Reste** : déployer ; faire ajouter le bouton *Réessayer* côté écran (contrat mis à
   jour : [`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md)).
   Le 404 **hors fenêtre** — équipage composé mais pas encore ouvert, clôturé ou expiré — porte lui
   aussi un message qui dit ce qui bloque (« Votre service commence à 14:00 : vos missions seront
   accessibles à partir de 13:30 », etc.) : motif calculé par `ClCrew.UnselectableReasonAt`, renvoyé
   par `CrewController.Mine` sans toucher `ToActionResult`, 8 tests.
2. ⛔ **L'organisation de la régulation — à trancher.** Composer les équipages avant la prise de
   service — sans quoi l'accès anticipé de 30 min (CREW-1) ne sert à rien pour ces équipages.

⚠️ Le filtre d'appartenance (`MobileIdentityResolver.cs:35`) est **volontaire** : ne pas le retirer.

---

## D. Robustesse des appels sortants (`DEC-7`) — ⏳ isolé

Les deux clients HTTP n'ont que leur `BaseAddress` : **pas de timeout explicite** (100 s par défaut),
**pas de retry ni de disjoncteur** en lecture (vérifié le 2026-09-13). L'écriture est couverte par la
file de projection.
**Contenu** : timeout court et explicite, puis `AddStandardResilienceHandler` (ou Polly), **en
gardant** la tolérance au 404 en lecture. **Fin** : aucun appel sortant ne pend au-delà du timeout.

---

## E. Chaîne vers la facturation

### E1 — ⏳ Le kilométrage dans le paquet (`MOB-10`) — *arbitrage puis code*

`Kilometers = null` (vérifié le 2026-09-13) ; la facturation attend ce champ pour activer son contrôle.
Le km est **équipage/véhicule-scoped**. → **Arbitrer avec la facturation** : km véhicule suffisant
(alimenter le champ), ou relevé début/fin par mission (table + saisie mobile + paquet).

### E2 — ⏳ Horodatages : dire l'heure qu'il est

- Les jalons sont en **UTC sans le déclarer**.
- **`SIG_DATETIME` est écrit en heure locale** (`DateTime.Now`, `SignatureRepository.cs:34` et `:43`).
  *Même motif relevé ailleurs, à examiner avec : `ClMarkMissionSeenUseCase` (heure de lecture),
  `ClSetDriverUseCase`, `ClInsertMechanicLogUseCase`.*

**Fin** : signature en UTC comme le reste, fuseau **déclaré** dans le contrat du paquet.

### E3 — ⏳ `field-data` en lot — *voir B5*, non engagé côté demandeur.

### E4 — ⛔ `Billed` : le faire écrire, ou retirer le palier — *décision*

La facturation est en lecture seule par décision de son module. **À trancher** : le faire poser à la
publication d'une journée, ou retirer le palier de l'énumération.

### E5 — ⚪ Repère de fraîcheur du paquet

`updatedAt` est servi, aucun consommateur ne s'en sert (la facturation re-tire à chaque construction).
**À garder, sans y investir** tant qu'un besoin de re-synchronisation n'apparaît pas.

---

## F. Fonctions terrain

### F1 — 🟡 Carte mutuelle : obtenir les premières cartes *(avant tout le reste)*

La capture par mission est **en service depuis le 2026-09-13** (cause de la table vide : la route
demandait un identifiant patient qu'aucun DTO terrain ne porte). Reste :

1. **Transmettre** [`note_web_alexandre_carte_mutuelle.md`](note_web_alexandre_carte_mutuelle.md) au dev web.
2. **Mesurer** le remplissage après livraison de l'écran. ⚠️ Le compte `ErpAccount` de
   `appsettings.json` est refusé depuis le poste de dev : mesurer via `field-data`, ou obtenir un
   compte de lecture.
3. **Signaler à BillingGateway** que ses « 401 » du 27/08 portaient sur deux routes inexistantes : la
   vraie route image est ouverte.

Faiblesses connues de la saisie (le `PATCH` remplace les quatre champs et accepte un corps vide ; une
nouvelle photo repart à vide) : [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) §3.3.

### F2 — ⏳ Carte mutuelle : extraction automatique (P3)

Seul le **statut** existe. Pipeline **asynchrone** : `pending` à l'upload → worker → **Claude vision**
à sortie structurée (quatre champs + confiance journalisée) → `extracted` → **validation humaine** →
`validated`. À cadrer : **où tourne l'appel au modèle** (DMZ, ou service LAN qui tire l'image), coût
par carte. Rappel `M7` : même validés, ces champs n'alimentent pas `C54`.
**Préalable** : F1. **Fin** : quatre champs proposés, jamais écrits en aveugle.

### F3 — ⏳ Deux chantiers hérités, autonomes

*(`MOB-14`, logs mécaniques, abandonné le 2026-09-13 : ses routes sont retirées du contrat, cf. [`delivered.md`](delivered.md).)*

| Réf | Objet | Ce qui reste |
|---|---|---|
| `MOB-12` | **Fin de service** | Le contrôleur vise `MOB_SESSION`, qui n'est plus la source d'authentification : la clôture doit viser la **vacation d'équipage côté Orders**. `TODO` ouvert sur les permissions. *Re-cadrage avant code.* |
| `MOB-16` | **Connecteurs Sirus / GpsGate** | Portés et injectés, **non recâblés** : positions (GpsGate REST), statuts véhicule (Sirus UDP). |

### F4 — ⛔ Présence : qui est connecté à Vector — *décision d'abord*

Spec sans code : [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md).
**Deux décisions** : la définition de « connecté » (retenu à confirmer : activité applicative croisée
avec l'état de service), et la **topologie** (cache par process inadapté en multi-instance).
**Découpage** : store + estampillage au point de passage d'identité → endpoint restreint →
enrichissement → rétention. ⚠️ Suivi d'activité d'un salarié : **cadrage RH/RGPD requis**.

---

## G. Dette & hygiène — aucun changement de contrat

### G2 — ⚠️ Alias de compatibilité — retrait **sur confirmation du front** uniquement

| Réf | Alias conservé | Condition de suppression |
|---|---|---|
| C1 | `IsAck`, alias de `IsSeen` | UI web sur `IsSeen` |
| C2 | Champs JobDetail legacy (`Schedule`, `TransportMode`, `Departure`/`Arrival`) | UI web sur `ScheduleLabel`, `TransportModeLabel`, `PickupLocation`/`DropoffLocation` |
| C3 | `SelectedDriver` jamais null | UI web garde-fou le `null` |
| — | Champs typés des lieux, en parallèle de `PickupDisplay`/`DropoffDisplay` | UI web sur l'affichage piloté serveur |

`ListMissionsAsync` n'a plus aucun appelant : à supprimer.

### G3 — ⏳ Suite de smoke `.http` (`MOB-9` résiduel)

`CaSoft.Erp.Mobile.Api.http` ne couvre toujours ni joblist, ni time, ni signature (vérifié le
2026-09-13). **Fin** : login → joblist → jobdetail → time → signature rejouables.

### G4 — ⏳ Suivi des migrations SQL

Aucune table de suivi de schéma : prod et dev ont déjà divergé en sens inverse (06/08). → table de
suivi (modèle `__BillingGatewaySchema`) + **contrôle au démarrage**. À réconcilier : la migration du
transfert est `027` dans l'historique et `034` dans le dépôt.

### G5 — ⚠️ Dettes de forme, sans urgence

- **Nommage des DTO (`DET-4`)** : suffixes `…DtoIn` / `…DtoOut`. Aucun impact JSON.
- **Pont sync/async** : `.GetAwaiter().GetResult()` sur joblist / jobdetail / identité.
- **`IResultUseCase` est synchrone** : les deux cas d'usage asynchrones de la carte mutuelle
  (`HandleAsync`) n'implémentent aucune interface ; le commentaire de `IResultUseCase.vb` cite encore
  `ClResultUseCaseAdapter`, supprimé.

### G6 — ⏳ Documentation à réconcilier

- **`README.md`** : accès ERP **in-process**, sous-app `/mobile`, « MOB-4 reporté » — faux (vérifié le 2026-09-13).
- **`docs/deploiement/configuration-keycloak-iis.md`** : « `Authority`/`Audience` codés en dur » — résolu par KC-1.
- **`BUG_DISPLAY.MD` §6** : DET-1 présenté comme bloquant — livré. Restent trois vérifications
  d'exploitation : mission **retour**, lieu **non référencé**, **fraîcheur des coordonnées** (pas de
  re-géocodage sur édition d'adresse : **à traiter avant tout usage navigation**).
- **`endPoint.md` §5** : `engagedOnly` encore décrit comme une demande — honoré par Orders.
- **`refactor_result_pattern.md`** : plan terminé, à marquer comme tel.
- **Deux liens vers `Erp.Order` sont cassés** (constaté le 2026-09-13) : `feature_order_context_devplan.md`
  (chapitre A) et `note_vector_orderContext_mission.md` (§2) n'existent plus dans ce dépôt.
- **`MUTUELLE_CARD_devplan.md`** et les autres devplans de fonctionnalité portent encore une section
  « livré » : à compacter sur le même principe.

### G7 — ⚠️ RGPD (P4)

Documents, carte mutuelle, anomalies servis par une API exposée : rétention et purge (3 ans),
chiffrement au repos, contrôle d'accès fin sur l'image de carte, audit des accès. Un seul lot.

### G8 — ⏳ Savoir ce qui est réellement en service

**Aucun module ne sait dire quel code il exécute ni à quelle base il parle.** Quatre occurrences :
trois le 2026-08-25, **une le 2026-09-13** (publication avant commit) — cf. [`delivered.md`](delivered.md) §8.
Le sourcelink du `.pdb` ne suffit pas : il donne le `HEAD`, pas l'état de l'arbre.

**Contenu** : une route réservée (gated comme `/api/diag`) qui expose le **commit** (SHA + date, et
**drapeau « arbre modifié »** injecté au build), l'**environnement**, la **base résolue** (serveur +
nom, jamais d'identifiants) et les **drapeaux** en vigueur. Idéalement, `deploy.ps1 prod` **refuse de
publier un arbre non commité**.
**Fin** : une requête dit quel commit tourne et sur quelle base, et une publication non commitée est
visible — ou impossible.

---

### G9 — 🟡 La file de projection relance sans fin une mission inconnue d'Orders — *codé, à déployer*

**Constaté le 2026-09-13** dans les journaux rétablis : `PUT missions/745c9f76-…/operational` répond
**404**, et `OperationalOutboxDispatcher` en est à la **tentative n° 55 414** — une relance par minute
depuis début août environ. Orders confirme : la mission **n'existe pas** (404 aussi en lecture).

**Cause** : le client levait la même exception pour un 404 que pour une panne, et le worker relançait
tout échec jusqu'au succès.

**Codé le 2026-09-13** : `ProjectOperationalAsync` renvoie `EnOperationalProjectionOutcome`
(`Applied` / `MissionNotFound`), sur le modèle de l'écriture du context ; sur `MissionNotFound`, le
worker **abandonne l'entrée** avec un `WARN` explicite. Les pannes (5xx, réseau) gardent le backoff :
**pas de plafond de tentatives**, pour ne perdre aucun jalon pendant une longue panne d'Orders.
3 tests (livrée, abandonnée, relancée).

**Reste** : déployer, puis constater dans les journaux l'abandon de `745c9f76` et la fin des relances.

## H. ⚪ Différé (V2 / hors MVP)

- **`Vd-1` — base `DB_VECTOR` dédiée** : **pertinent dès maintenant**, seul jalon DMZ non conditionné à la V2.
- **`CREW-2` — accès anticipé à cheval sur minuit** : correctif connu (J+1 puis dédoublonner), non
  prioritaire (vacations de nuit non concernées, décision du 2026-08-02).
- **Durcissement DMZ événementiel** (`Vd-2` à `Vd-4`, `Vd-7`, `Vd-8`) et **push temps réel** :
  [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md).
- **`Vd-6` — photos hors SQL** et **`Vd-5` — masquage** (NIR partiel, équipage retour).
- **Assembly de contrats partagé `Orders.Contracts`** (4b) — drift JSON assumé.
- **Éviction ciblée du cache d'identité**, **mode offline**, **géolocalisation avancée**, **renommage**
  `CaSoft.Erp.USVector.*` → `CaSoft.Erp.Vector.*`.

---

# 2. Documents voisins

| Doc | Genre | Ce qu'il apporte |
|---|---|---|
| [`delivered.md`](delivered.md) | Livré | Ce que le module fait, journal daté, décisions, configuration, pistes retirées, incidents |
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Spec fonctionnelle | Le besoin et le vocabulaire |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Devplan | Carte mutuelle (F1-F2) |
| [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md) | Devplan | Projection du terrain vers Order |
| [`TRACABILITE_SAISIES_VECTOR_EXPORT.md`](TRACABILITE_SAISIES_VECTOR_EXPORT.md) | Relevé | Des saisies Vector aux 91 colonnes de facturation |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Devplan | Auth de service, résilience (C2, D) |
| [`plan_correctif_vector_fallback_snapshot.md`](plan_correctif_vector_fallback_snapshot.md) | Plan correctif | B2, **à coder dans `Erp.Order`** |
| [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md) | Spec | Présence (F4) |
| [`endPoint.md`](endPoint.md) | Contrat HTTP | Ce que Vector attend d'Orders.Api |
| [`docs/auth/diag-404-second-membre-equipage.md`](docs/auth/diag-404-second-membre-equipage.md) | Procédure | C3 |
| [`../Erp.Order/note_vector_orderContext_mission.md`](../Erp.Order/note_vector_orderContext_mission.md) | Note d'intégration | ContextOrder : endpoints, attributs, règles |
| `note_web_alexandre_*.md`, `note_ui_alex.md`, `docs/ui-web/*` | Contrats front | Ce qui est promis au dev web |

---

**Fin du document**
