# BUG_DISPLAY — Affichage lieux Pickup / Dropoff (JobDetail mobile)

> Note de reprise. État au 2026-07-15. Concerne l'affichage des lieux de prise en
> charge / dépose dans `GET /api/JobDetail/{id}` (mobile UsVector).
>
> ⚠️ **DET-1 est implémenté côté serveur mais NON DÉPLOYABLE en l'état** — voir §6.
> Le déploiement est **bloqué tant qu'Alex n'affiche pas la ligne `Service`**.

## 1. Contexte

Le détail mission expose deux lieux structurés `PickupLocation` / `DropoffLocation`
(`ClJobLocationDto`), affichés **une ligne par champ non vide** par le mobile, dans
l'ordre : `Nom → Service → Adresse → Residence → BatEtage → Commune → Complement`,
plus un sous-objet `Coordinates` (`Latitude`/`Longitude`) **hors du flux d'affichage**.

La donnée traverse 7 couches, depuis 3 tables ERP (selon le **type de lieu**) jusqu'au DTO mobile.

```
BD ERP (ORD_SITE | BEN_BENEFICIARY_ADDRESS | REF_UNREFERENCED_LOCATION)
  └─ Erp.Order : MissionDetailQueryService.ResolveStageDetailAsync → ClStageDetailDtoOut
      └─ + override service depuis ORD_ORDER.PICKUP/DROPOFF_SERVICE (apparié par id de lieu)
          └─ JSON Orders.Api : GET /missions/{id}/full → Pickup / Dropoff
              └─ Erp.Vector : ErpStageDto (miroir JSON)
                  └─ JobRepository.ToJobLocation → ClJobLocation
                      └─ ClJobDetailAdapter.ToLocationDto → ClJobLocationDto
                          └─ Mobile : 1 ligne par champ non vide
```

## 2. Mapping exact par champ affiché

| Ligne (`ClJobLocationDto`) | ← `ClJobLocation` (Vector) | ← `ClStageDetailDtoOut` (ERP) | Type 1 **Établissement** `ORD_SITE` | Type 2 **Adr. bénéf.** `BEN_..._ADDRESS` | Type 3 **Libre** `REF_UNREF_LOCATION` |
|---|---|---|---|---|---|
| **Nom** | `Nom` | `LocationName` | `SITE_LABEL` | `BEN_ADR_LABEL` | `URL_LOCATION_NAME` |
| **Service** | `Service` | `ServiceLabel` | (service ↓) | *(pas de service)* | (service ↓) |
| **Adresse** | `Adresse` | `AddressLine1` | `SITE_ADDR_LINE_1` | `BEN_ADR_LINE_1` | `URL_ADDR_LINE_1` |
| **Residence** | `Residence` | `AddressLine2` | `SITE_ADDR_LINE_2` | `BEN_ADR_LINE_2` | `URL_ADDR_LINE_2` |
| **BatEtage** | `BatEtage` | `AddressLine3` | `SITE_ADDR_LINE_3` | `BEN_ADR_LINE_3` | `URL_ADDR_LINE_3` |
| **Commune** | `PostalCode` + ` ` + `City` | `PostalCode` + `City` | `SITE_POSTAL_CODE` + `SITE_CITY` | `BEN_ADR_POSTAL_CODE` + `BEN_ADR_CITY` | `URL_POSTAL_CODE` + `URL_CITY` |
| **Complement** | `Complement` | `Complement` | `SITE_COMPLEMENT` | *(vide)* | `URL_COMPLEMENT` |
| *(hors lignes)* `Coordinates` | `Latitude` / `Longitude` | `Latitude` / `Longitude` | `SITE_LATITUDE/LONGITUDE` | `BEN_ADR_LATITUDE/LONGITUDE` | `URL_LATITUDE/LONGITUDE` |

## 3. Le cas du Service ⚠️ (source de confusion)

`ClStageDetailDtoOut.ServiceLabel` a **deux sources**, par priorité :

1. **Référentiel** (`SITE_SERVICE` / `URL_SERVICE_LABEL`) — souvent **vide** pour un établissement.
2. **Commande** (écrase si non vide) : `ORD_ORDER.PICKUP_SERVICE` / `DROPOFF_SERVICE`,
   **apparié par id de lieu** (retour-safe : une mission retour inverse pickup/dropoff).
   → *le service d'un transport est choisi par commande, PAS porté par le site.*

Côté Vector, `ServiceLabel` a désormais **sa ligne dédiée** (`Service`, après `Nom`) et
`BatEtage` ne porte plus que `AddressLine3`. ⚠️ **Tant qu'Alex n'affiche pas cette ligne,
le service est invisible** (il ne transite plus par `BatEtage`) — cf. §6.

## 4. Règles spéciales

- **Repli label figé** : si les 7 champs structurés sont vides (jointure orpheline / lieu
  supprimé), Vector met le `Label` figé dans **`Nom`** (ex. « EHPAD LES TAMARIS - … »)
  → affichage 1 ligne. Les coordonnées **ne comptent pas** comme champ structuré : un lieu
  géocodé mais sans texte doit quand même afficher son label.
- **`Coordinates` absent (`null`) si non géocodé** — jamais `{ 0, 0 }` : l'UI teste la présence
  du sous-objet pour savoir si le lieu est cartographiable.
- **Champs ERP toujours non mappés** : `LocationType`, `LocationId`, `CityId`, `IsActive`,
  `BeneficiaryId`, `UnreferencedLocationStatus`.

## 5. État actuel (fait)

- ✅ **Service pickup/dropoff** rapatrié depuis la commande — `Erp.Order`,
  `MissionDetailQueryService` (branche `fix/missions-engaged-only-exclude-closed`).
- ✅ **DET-1 implémenté** (2026-07-15) — `Service` a son champ dédié, `BatEtage` ne porte
  plus que `AddressLine3`, et le sous-objet `Coordinates` (lat/lon) est exposé.
  Tests : `JobLocationMappingTests` (8). Build + 49/49 verts.
  ⚠️ **Non déployable seul** — cf. §6.

## 6. DET-1 — implémenté, en attente d'Alex 🚧

Le code est fait des deux côtés du mapping (`ClJobLocation`, `ClJobLocationDto`,
`ToJobLocation`, `ToLocationDto`, `ErpStageDto`). **Il reste le contrat UI.**

⚠️ **Le service ne transite plus par `BatEtage`.** Déployer Vector avant qu'Alex n'affiche
la ligne `Service` ferait **disparaître le service** de l'écran. Séquence obligatoire :

1. **Alex d'abord** : ajoute la ligne `Service` après `Nom`, affichée si non vide. Déployable
   immédiatement et **visuellement neutre** (le serveur n'envoie pas encore le champ → vide
   → rien ne s'affiche). Aucun risque, aucune fenêtre de régression.
2. **Vector ensuite** : déployer ce changement. Le service saute de « Bât./Étage » à sa
   propre ligne. Rien ne disparaît à aucun moment.

Message rédigé pour Alex (§ ordre legacy `ClHelper.GetSiteString` : Site → Service → lignes
→ CP/Commune) — en attente de sa réponse.

## 7. Fichiers clés

**Erp.Vector**
- `CaSoft.Erp.USVector.Infrastructure/Repositories/Erp/JobRepository.cs` → `ToJobLocation` (mapping)
- `CaSoft.Erp.USVector.Infrastructure/ErpApi/ErpReadDtos.cs` → `ErpStageDto` (miroir JSON)
- `CaSoft.Erp.USVector.Domain/Entities/ClJobLocation.vb`
- `CaSoft.Erp.USVector.Application/UseCases/Job/Query/ClJobDetailModel.vb` → `ClJobLocationDto`
- `CaSoft.Erp.USVector.Application/Presenters/ClJobDetailAdapter.vb` → `ToLocationDto`
- `note_ui_alex.md` (contrat d'affichage mobile)

**Erp.Order**
- `Orders.Infrastructure/QueryServices/MissionDetailQueryService.cs` → résolution stage + override service
- `Orders.Application/Missions/ClMissionFullDtoOut.vb` → `ClStageDetailDtoOut`
- `Orders.Infrastructure/Persistence/Entities/ORD_ORDER.cs` (PICKUP/DROPOFF_SERVICE)
- `Orders.Infrastructure/Persistence/Entities/ORD_MISSION.cs` (pointeurs légers MIS-1)

## 8. Checklist de reprise

- [x] Décider si on implémente DET-1 → **oui**, implémenté le 2026-07-15 (+ sous-objet `Coordinates`).
- [ ] 🚧 **Bloquant** — Alex : confirmer + déployer la ligne `Service` (étape 1 du §6) **avant**
      de déployer Vector.
- [ ] Vérifier l'affichage sur une mission **retour** (service non inversé).
- [ ] Vérifier un lieu **non référencé** (FreeText) : service référentiel conservé.
- [ ] Fraîcheur des coordonnées : le re-géocodage sur **édition d'adresse bénéficiaire** n'est
      pas implémenté (module Beneficiary — cf. `Erp.Order/docs/features/featDesc_AddressHandling_Order.md:106`).
      Un `Coordinates` peut donc pointer l'ancien bâtiment → à traiter **avant** tout usage navigation.
