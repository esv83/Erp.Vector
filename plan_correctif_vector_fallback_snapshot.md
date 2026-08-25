# 🚑 Plan correctif 6‑B — Vector : fallback sur le snapshot `ORD_ORDER`

> **But** : réparer **immédiatement** les ~3883 étapes déjà cassées dans Vector
> (`GET /missions/{id}/full`), **sans migration de données**. Quand le référentiel vivant lu par Vector
> est **vide/orphelin** (cas des saisies libres type 3), retomber sur le **snapshot `ORD_ORDER`**, qui
> porte **toutes les lignes + complément + service** (`ORD_ORDER.cs:60‑69`).
>
> Complément du plan principal ([plan_correctif_domicile_auto_attache.md](plan_correctif_domicile_auto_attache.md)) :
> celui‑ci assainit les commandes **futures** (domicile → type 2) ; **celui‑ci répare l'existant**.

---

## 1. Rappel du diagnostic

- Vector lit l'adresse via `MissionDetailQueryService.ResolveStageDetailAsync` (`:127`) : jointure
  dynamique selon `PICKUP_SOURCE_TYPE` (1=site, 2=adresse bénéf., 3=saisie libre).
- Pour ~50 % des saisies libres (type 3), cette jointure est **orpheline/vide** (`URL` non rattachée ou
  sans lignes) **et** sans `ADDRESS_ID` → aucun fallback canonique → **affichage vide**.
- La **certification** ne souffre pas du problème : elle lit le **snapshot `ORD_ORDER`**, rempli à ~99 %.
  Vector, lui, **ne lit jamais** ce snapshot.

⇒ Correctif : **ajouter le snapshot `ORD_ORDER` comme source de repli** dans le chemin de lecture Vector.

---

## 2. Précédence d'adresse retenue (par champ)

Ordre de préséance, du plus prioritaire au dernier recours :

1. **Référentiel vivant** (`ORD_SITE` / `BEN_BENEFICIARY_ADDRESS` / `REF_UNREFERENCED_LOCATION`) — valeur
   actuelle et potentiellement plus fraîche (adresse éditée après la commande) ;
2. **Snapshot `ORD_ORDER`** (nouveau repli) — figé mais **complet** (lignes 1/2/3 + complément + service
   + nom de lieu + CP/ville) ;
3. **Canonique** (Address.Api) — dernier recours, **une seule ligne normalisée**.

Règle : on ne remplit depuis un niveau inférieur **que** les champs encore vides au niveau supérieur
(même logique que `ModCanonicalAddressMerge.Merge`).

---

## 3. Le piège d'orientation aller/retour (⚠️ à traiter en premier)

Une mission de **retour** inverse pickup/dropoff par rapport à la commande. Il faut donc apparier le
**stage pickup de la mission** au **bon côté du snapshot** (pickup **ou** dropoff de l'`ORD_ORDER`).

**L'appariement existant ne marche PAS pour le FreeText.** `GetFullAsync` apparie aujourd'hui le service
par id de lieu (`MissionDetailQueryService.cs:55,57`) :
`PickupLocId = o.PICKUP_BENEFICIARY_ADDRESS_ID ?? o.PICKUP_SOURCE_SITE_ID` — **NULL pour une saisie libre**.
Donc pour les étapes justement cassées (type 3), ce mécanisme ne peut pas choisir le côté.

**Clé d'orientation retenue : `ORD_MISSION.MIS_KIND`** (`ORD_MISSION.cs:42` — 1=Aller, 2=Retour) :
- `MIS_KIND = 1` (Aller) → mission.pickup ↔ order.pickup, mission.dropoff ↔ order.dropoff ;
- `MIS_KIND = 2` (Retour) → mission.pickup ↔ order.**dropoff**, mission.dropoff ↔ order.**pickup**.

> 🔴 **Checkpoint bloquant** : confirmer, dans le code de **génération de mission**, qu'un retour
> **stocke** effectivement pickup/dropoff inversés (et donc que `MIS_KIND` suffit à choisir le côté du
> snapshot). Rédiger 2 tests (aller + retour) qui le prouvent **avant** de brancher le fallback. Si la
> génération ne respecte pas cette convention, l'orientation devra s'appuyer sur un autre invariant
> (ex. coordonnées/label figés) — à réévaluer.

---

## 4. Changements — `MissionDetailQueryService.cs`

### 4.1 Charger le snapshot dans `GetFullAsync` (`:49‑60`)
Étendre le `Select` de `orderInfo` pour rapatrier aussi, **pour les deux côtés**, les champs snapshot :
`*_LOCATION_NAME`, `*_ADDR_LINE_1/2/3`, `*_POSTAL_CODE`, `*_CITY`, `*_CITY_ID`, `*_COMPLEMENT`,
`*_SERVICE`, `*_LATITUDE`, `*_LONGITUDE` (`ORD_ORDER.cs:60‑97`). Ajouter `MIS_KIND` (déjà sur `m`).

### 4.2 Nouveau repli snapshot (avant le canonique)
Nouvelle méthode `ApplySnapshotFallback(stage, snapSide)` appelée pour pickup et dropoff, **après**
`ResolveStageDetailAsync` et **avant** `ApplyCanonicalOverlayAsync` (`:43`) :
- `snapSide` = côté order choisi selon `MIS_KIND` (§3) ;
- pour chaque champ du DTO (`AddressLine1/2/3`, `City`, `PostalCode`, `Complement`, `ServiceLabel`,
  `LocationName`, `Latitude`, `Longitude`) : **remplir depuis le snapshot uniquement si vide** ;
- ne rien faire si le snapshot est lui aussi vide.

### 4.3 Le canonique reste en dernier
`ApplyCanonicalOverlayAsync` (`:219`) inchangé : il ne remplira `AddressLine1`/`City`/`PostalCode` que
s'ils sont **encore** vides après référentiel + snapshot. Pour un domicile réparé par snapshot, il
n'écrasera rien (précédence snapshot > canonique côté texte, cf. `ModCanonicalAddressMerge.vb:39`).

### 4.4 Certification : **ne rien changer**
`MissionQueryService.ListForCertificationAsync` lit déjà le snapshot et n'affiche que la voie — conforme
au besoin. Hors périmètre.

---

## 5. Impact attendu

- **~3883 blancs Vector → résolus** dès le déploiement, sans toucher aux données.
- Résiduel : les étapes dont **même le snapshot** est vide (~93 mesurées) — cas marginaux, non couverts
  (donnée absente à la source).
- Aucune dépendance à Address.Api ni au géocodage pour ce repli.
- **Code‑only**, pas de script SQL le jour J.

---

## 6. Risques & tests

**Risques**
- **Mauvais côté** (aller/retour) → afficherait l'adresse de départ à l'arrivée. Mitigation : checkpoint
  §3 + tests aller/retour obligatoires.
- **Snapshot périmé** si l'adresse a été éditée après la commande → acceptable (précédence référentiel >
  snapshot : le repli ne s'active que si le référentiel est vide).
- Perf : le `Select` `orderInfo` est déjà fait (1 requête) ; on ajoute seulement des colonnes → coût nul.

**Tests (Infrastructure / intégration)**
- Type 3 orphelin (URL vide, pas d'`ADDRESS_ID`), mission **aller** → pickup/dropoff remplis depuis le
  snapshot, bon côté.
- Type 3 orphelin, mission **retour** → côtés **inversés** correctement.
- Référentiel **présent** → snapshot **non** appliqué (précédence respectée).
- Référentiel + snapshot vides mais canonique dispo → canonique appliqué (chaîne complète).
- Complément (`PICKUP_COMPLEMENT`) présent au snapshot → **exposé** dans `ClStageDetailDtoOut.Complement`.

---

## 7. Ordre de déploiement recommandé

1. **6‑B (ce plan)** en premier : répare l'existant, faible risque, code‑only, bénéfice immédiat pour les
   ambulanciers.
2. **Plan principal** (domicile → type 2) ensuite : assainit le flux futur et nourrit le carnet bénéficiaire.

Les deux sont **indépendants** et cumulables ; 6‑B reste utile même après le plan principal (couvre toute
saisie libre non‑domicile qui resterait orpheline).

---

### Fichiers touchés (récap)
- `Orders.Infrastructure/QueryServices/MissionDetailQueryService.cs` *(chargement snapshot + repli + orientation MIS_KIND)*
- *(vérif préalable)* code de **génération de mission** *(convention retour = pickup/dropoff inversés)*
- *(tests)* `tests/Orders.UnitTests/…` et/ou tests d'intégration Infrastructure
