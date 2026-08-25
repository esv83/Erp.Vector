# Test de diagnostic — 404 sur le second membre d'un équipage

## Symptôme

Sur un équipage à **2 membres**, un seul des deux accède à la liste des missions. Le second reçoit un **404**.

## Ce que l'analyse du code établit déjà

Le 404 **ne vient pas de la joblist**. Sur `GET /api/joblist/{crewId}` :

- une liste vide renvoie **200** avec un tableau vide (`ClJobListModel` n'est jamais null, cf. `ClResultActionResultExtensions.cs:18-27`) ;
- le garde-fou d'accès renvoie **403**, pas 404 (`CrewAccess.cs:74`).

Le seul 404 de la chaîne est **en amont**, dans le sélecteur d'équipage que l'app appelle avant la joblist :

| Emplacement | Condition du 404 |
|---|---|
| `CrewController.cs:38-42` | `ResolveActiveCrewIdsFresh` renvoie 0 crew |
| `ClGetMyActiveCrewsUseCase.vb:31-36` | aucun crew candidat ne passe `IsSelectableAt` (démarré, non clôturé, ≤ 18 h) |

**Donc : le second membre n'arrive pas à sélectionner son équipage — il n'atteint jamais la liste des missions.**

Rien dans ce repo ne suppose « un seul membre par crew » : le modèle est 1-n (`ErpCrewFullDto.Members`), aucun `First`/`Single` sur les membres n'existe sur ce chemin, et le multi-membres est couvert par `MobileIdentityResolverTests`. Les deux ruptures possibles sont alimentées par les **données d'Orders.Api** :

1. `GET /crews?personnelId={B}&date=` ne remonte pas l'équipage pour le second membre → 0 candidat.
   *(dette déjà identifiée au commit 18b04b4 : jointure amont sur le **véhicule** plutôt que sur l'appartenance réelle)*
2. L'équipage remonte, mais son `Members` ne contient pas B → notre filtre d'appartenance l'écarte :
   `MobileIdentityResolver.cs:35` — `.Where(crew => crew.Members.Any(m => m.Id == personnelId))`

Ce test sert à **trancher entre 1 et 2**.

---

## Prérequis

Le diagnostic est dev-only. Il est actif si :

- l'environnement est `Development`, **ou**
- `Diagnostics:Enabled = true` — sur IIS, variable d'environnement `Diagnostics__Enabled=true`.

Sinon les endpoints `/api/diag*` renvoient 404 (`DiagController.cs:32,37`) — un 404 à ne pas confondre avec celui qu'on traque.

La recherche par username (facultative, permet d'éviter de récupérer les `sub` à la main) exige en plus `Keycloak:AdminClientId` / `Keycloak:AdminClientSecret`, sur un service account disposant des rôles `realm-management` **view-users** / **query-users**. Non configuré → 501, on colle alors les `sub` directement.

## Ce qu'il faut sous la main

- Les **deux comptes Keycloak** (usernames ou `sub`) des deux membres du même équipage.
- Idéalement, la **date/heure du constat** : les deux membres doivent être testés **au même instant** (paramètre `at`), sinon la fenêtre d'activité fausse la comparaison.

---

## Procédure

### 1. Confirmer que le 404 est bien celui du sélecteur

Côté serveur, le sélecteur trace explicitement le cas :

```
GET api/crew/mine — PER_ID={PerId} sans équipage actif aujourd'hui.
```

Si cette ligne apparaît au moment du 404 du second membre, l'hypothèse est confirmée. Sinon, relever l'URL exacte appelée par l'app au moment du 404 avant d'aller plus loin.

### 2. Lancer le diagnostic pour les **deux** membres

Page visuelle :

```
GET /api/diag        (ou /diag)
```
> Sur déploiement IIS en sous-application (ex. `/vector`), la page injecte elle-même la bonne base — utiliser `https://<hôte>/vector/api/diag`.

Saisir le username → « Résoudre le sub » → « Diagnostiquer ». Renseigner **le même instant testé** pour les deux membres.

JSON brut équivalent :

```
GET /api/diag/crew-chain?sub={sub}&at=2026-07-21T14:30
```

Le diagnostic appelle Orders.Api **directement, sans passer par le cache** (`CrewChainDiagnostic`), et rejoue la vraie règle domaine `ClCrew.IsSelectableAt` : ce qu'il affiche est l'état réel, pas une vue mise en cache.

### 3. Lire le résultat

La chaîne est déroulée maillon par maillon : `sub → PER_ID → crews candidats → membre ? → actif ?`

| Observation chez le membre en échec | Interprétation | Cause |
|---|---|---|
| Maillon 1 en rouge, « Compte non rattaché à un personnel » | le `sub` n'est pas dans `PER_KEYCLOAK_MAP` | rattachement Keycloak manquant (produirait un **403**, pas un 404 — piste différente) |
| `CandidateCount: 0` / « Aucun crew candidat » | Orders.Api ne rattache pas ce personnel à l'équipage | **cause 1** (requête `crews?personnelId=`) |
| L'équipage apparaît, verdict « Exclu : le personnel n'est pas membre de cet équipage », et la liste `Members` ne contient pas ce personnel | `GET /crews/{id}` ne renvoie pas tous les membres | **cause 2** (`Members` incomplet) |
| Verdict « Exclu : service pas encore démarré / clôturé / obsolète (> 18 h) » | fenêtre d'activité, pas un problème d'appartenance | règle métier `IsSelectableAt` — vérifier les horaires de service saisis |
| Verdict « Sélectionnable » alors que l'app renvoie 404 | l'échec est ailleurs (cache, autre endpoint, token) | reprendre à l'étape 1 |

Le comparatif avec le membre qui **fonctionne** est la partie décisive : sur le même équipage et au même instant, la différence entre les deux sorties isole le maillon fautif.

### 4. Collecter

Conserver les **deux** réponses JSON de `/api/diag/crew-chain` (membre OK et membre KO), en notant pour chacun le username et l'instant testé. C'est le matériau du ticket.

---

## Suite selon le verdict

Les causes 1 et 2 sont toutes deux **côté Orders.Api** (autre dépôt) — rien à corriger dans Erp.Vector si le diagnostic les confirme :

- **cause 1** → corriger la requête `GET /crews?personnelId=&date=` pour qu'elle joigne sur l'appartenance réelle du personnel et non sur le véhicule / le conducteur actif ;
- **cause 2** → `GET /crews/{crewId}` doit renvoyer **tous** les membres dans `Members`, avec `Id = PER_ID`.

À noter : le filtre d'appartenance de `MobileIdentityResolver.cs:35` est volontaire — il bouche un trou d'autorisation réel (commit 18b04b4). Il ne doit **pas** être retiré pour contourner le symptôme ; il ne fait que transformer une donnée amont incomplète en 404.

## Références

- `CaSoft.Erp.USVector.Infrastructure/Diagnostics/CrewChainDiagnostic.cs` — la chaîne rejouée et les verdicts
- `CaSoft.Erp.USVector.Api/Controllers/DiagController.cs` — endpoints et conditions d'activation
- `CaSoft.Erp.USVector.Infrastructure/Repositories/Erp/MobileIdentityResolver.cs` — filtre d'appartenance
- `docs/ui-web/selection-equipage-multi-crew.md` — contrat du sélecteur (dont `404 = aucun équipage actif`)
- `docs/auth/optimisation-chaine-authentification.md` — caches de la chaîne d'identité
