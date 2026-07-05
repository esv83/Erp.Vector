# Refactor — Result pattern sur la couche Application

Branche : `ImplementCaSoftFramework`. Objectif : migrer **tous les use cases** du pattern legacy
(`Execute(presenter)` + `ClUseCaseResponseBase` non typé) vers le **Result pattern** du framework
(`Handle() As ClResult(Of T)` + `IError`), de façon **itérative et non cassante** (code en prod).

## Décisions arrêtées (2026-07-05)
| Sujet | Décision |
|---|---|
| Use cases « commande » (sans valeur) | **`ClResult(Of Boolean)`** — tout en générique, uniforme |
| « NotFound » (aujourd'hui `Data=null`→404) | **`IError` dédié `NotFound`** → mappé 404 (distinct d'une erreur métier 400) |
| `IError.Layer` (Domain/App/Infra) | **Diagnostic seulement** — mapping HTTP simple (Fail→400, NotFound→404, Ok→200) |
| Périmètre | **Itératif complet, priorisé** — les 31 use cases migrés par lots, Phase 0 d'abord |

## État des lieux (inventaire)
- **31 use cases**, tous héritant de `ClUseCaseBase` :
  - **25** en `execute` (minuscule), **6** (dossier `MechanicLog`) en `Execute` (majuscule). VB ignore la
    casse → fonctionne, mais **incohérence à normaliser** au passage.
- **2 mécanismes de consommation** (les deux via presenter) :
  - **M1 — `ClUseCaseHandler(useCase).Execute()`** (contrôleur → ActionResult) : `AnomalyController`, `JobListController`.
  - **M2 — `useCase.execute(presenter)` via un service** (`ClJobService`, `ClCrewService`) : `TimeController`, etc.
- **Pièges** :
  - `ClUseCaseResponseBase` implémente **à la fois** `IUseCaseResponse` et `IResponseHandler` (responsabilités mêlées).
  - Méthodes `<Obsolete>` dans `ClJobService`/`ClCrewService` (ClDefaultPresenter) — à supprimer au passage.
  - **Pas de `ClError : IError`** concret aujourd'hui — à créer (Phase 0).

## Mapping HTTP cible (miroir de `ClWebApiPresenter`, pour parité stricte)
| Result | HTTP |
|---|---|
| `IsSucces` + `Value` non nul | **200** OkObject(Value) |
| `IsSucces` + `Value` nul | **404** NotFound |
| `Fail` avec `IError` de type `NotFound` | **404** |
| `Fail` (autre) | **400** BadRequest(InnerError.ErrorText) |
> Garantit que la réponse HTTP d'un use case migré est **identique** à l'actuelle → non-régression observable.

---

## Stratégie : Strangler Fig avec pont bidirectionnel
Un **pont** fait cohabiter les deux patterns → on migre **un use case à la fois**, la prod reste verte à chaque commit.

### Phase 0 — Fondations (100 % additif, 0 use case touché)
1. **`ClError : IError`** (message + `ErrorLayer` + exception optionnelle) + fabriques :
   `ClError.Domain(msg)`, `ClError.Application(msg)`, `ClError.Infrastructure(msg)`, **`ClError.NotFound(msg)`**.
2. **Marqueur NotFound** : un `IError` reconnaissable (sous-type ou drapeau) pour que le mapper renvoie 404.
3. **`IResultUseCase(Of T)`** : `Function Handle() As ClResult(Of T)` (verbe + `Handle`, cf. CLAUDE.md).
4. **Pont legacy ← Result** (`ClResultToLegacyAdapter`) : expose un use case Result via l'ancien
   `Execute(presenter)` → traduit `ClResult(Of T)` en `ClUseCaseResponseBase`
   (`Value`→`SetResult` ; `NotFound`→Data nul (404) ; `Fail`→`AddError(InnerError.ErrorText)`).
   ⇒ un use case migré **fonctionne dans M1 et M2 sans toucher ses consommateurs**.
5. **Pont Result → ActionResult** : extension `ClResult(Of T).ToActionResult()` (mapping ci-dessus)
   pour les contrôleurs qui consomment un Result en direct.
6. Legacy conservé **intégralement**. ✅ Rien ne change en prod.

### Phase 1 — Pilote (1 use case, bout-en-bout)
- Cible : **`ClMarkMissionSeenUseCase`** (commande simple, récente, bien comprise).
- Réécriture en `Handle() As ClResult(Of Boolean)`.
- Consommé via l'adaptateur (`ClJobService.MarkMissionSeen` inchangé) **ou** contrôleur en `.Handle().ToActionResult()`.
- Tests xUnit + FluentAssertions. Déploiement. Validation. **Fige la recette.**

### Phases 2…N — Migration par lots (feature par feature)
Chaque lot : convertir le(s) use case(s) → `Handle()` + mettre à jour le(s) consommateur(s) direct(s) ; **build + test**. Ordre proposé (leaf-first, du plus maîtrisé au plus large) :

| Lot | Use cases | Note |
|-----|-----------|------|
| A — Time | `ClUpdateTimeUseCase`, `ClClearJobTimeUseCase`, `ClGetTimeUseCase` | récents (Outbox), bien compris |
| B — Driver | `ClGetDriverUseCase`, `ClGetCrewUseCase`, `ClGetCrewIdListUseCase` | feature Driver |
| C — JobList | `ClGetJobListUseCase`, `ClAckInstructionUseCase` | M1 (ClUseCaseHandler) |
| D — Job / Signature / Kilometers | `ClGetJobUseCase`, `ClUpdateJobEditUseCase`, `ClUpdateJobValuesUseCase`, `ClGetJobEditFormStructureUseCase`, `ClGetSignatureUseCase`, `ClUpdateSignUseCase`, `ClGet/SetKilometersUseCase` | |
| E — Terrain P1/P2 | `ClReportAnomalyUseCase`, `ClUploadDocumentUseCase`, `ClUpload/SetMutuelle*`, `ClGet/SetEndOfServiceUseCase`, `ClList/SelectContractUseCase` | |
| F — MechanicLog (6) | `ClGet/Insert/Update/DeleteLogAnalyze`, `ClGet/InsertMechanicLog` | **+ normaliser la casse `Execute`→`Handle`** |

Au fil de l'eau : **supprimer les méthodes `<Obsolete>`** de `ClJobService`/`ClCrewService`.
Invariant : les non-migrés tournent en legacy, les migrés via l'adaptateur → **toujours livrable**.

### Phase finale — Nettoyage (seul retrait, quand plus rien ne dépend du legacy)
Supprimer : `ClUseCaseBase.Execute(presenter)`, `IResponseHandler`, `ClUseCaseResponseBase`,
`ClWebApiPresenter`/`ClDefaultPresenter`/`ClPresenterBase`, `ClUseCaseHandler`, et les adaptateurs de pont.
Le code est alors **uniformément conforme à CLAUDE.md** (`Handle` → `ClResult(Of T)`).

---

## Garantie de non-régression
- Phase 0 = additive pure (rien retiré).
- À chaque use case migré : mapping HTTP **identique** à `ClWebApiPresenter` (200/404/400) → réponse inchangée.
- Chaque lot : build vert + tests + déploiement dev + smoke test de l'endpoint concerné.
- Le legacy n'est **retiré qu'en toute fin**, quand l'inventaire des consommateurs est à zéro.

## Effort (ordre de grandeur)
Phase 0 : ~0,5–1 j. Pilote : ~0,5 j. Puis ~31 use cases en petites unités isolées (≈30 min + test chacune,
groupées par lot). Nettoyage : ~0,5 j. **Aucun big-bang, interruptible à tout lot.**
