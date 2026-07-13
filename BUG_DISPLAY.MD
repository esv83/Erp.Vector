# BUG_DISPLAY — Affichage lieux Pickup / Dropoff (JobDetail mobile)

> Note de reprise. État au 2026-07-13. Concerne l'affichage des lieux de prise en
> charge / dépose dans `GET /api/JobDetail/{id}` (mobile UsVector).

## 1. Contexte

Le détail mission expose deux lieux structurés `PickupLocation` / `DropoffLocation`
(`ClJobLocationDto`), affichés **une ligne par champ non vide** par le mobile, dans
l'ordre : `Nom → Adresse → Residence → BatEtage → Commune → Complement`.

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
| **Adresse** | `Adresse` | `AddressLine1` | `SITE_ADDR_LINE_1` | `BEN_ADR_LINE_1` | `URL_ADDR_LINE_1` |
| **Residence** | `Residence` | `AddressLine2` | `SITE_ADDR_LINE_2` | `BEN_ADR_LINE_2` | `URL_ADDR_LINE_2` |
| **BatEtage** | `Service` + ` ` + `AddressLine3` | `ServiceLabel` **+** `AddressLine3` | (service ↓) **+** `SITE_ADDR_LINE_3` | *(pas de service)* `BEN_ADR_LINE_3` | (service ↓) **+** `URL_ADDR_LINE_3` |
| **Commune** | `PostalCode` + ` ` + `City` | `PostalCode` + `City` | `SITE_POSTAL_CODE` + `SITE_CITY` | `BEN_ADR_POSTAL_CODE` + `BEN_ADR_CITY` | `URL_POSTAL_CODE` + `URL_CITY` |
| **Complement** | `Complement` | `Complement` | `SITE_COMPLEMENT` | *(vide)* | `URL_COMPLEMENT` |

## 3. Le cas du Service ⚠️ (source de confusion)

`ClStageDetailDtoOut.ServiceLabel` a **deux sources**, par priorité :

1. **Référentiel** (`SITE_SERVICE` / `URL_SERVICE_LABEL`) — souvent **vide** pour un établissement.
2. **Commande** (écrase si non vide) : `ORD_ORDER.PICKUP_SERVICE` / `DROPOFF_SERVICE`,
   **apparié par id de lieu** (retour-safe : une mission retour inverse pickup/dropoff).
   → *le service d'un transport est choisi par commande, PAS porté par le site.*

Côté Vector, `ServiceLabel` **n'a pas de ligne dédiée** : il est **concaténé dans `BatEtage`**
(service d'abord, puis `AddressLine3`). Donc aujourd'hui le service s'affiche sur la ligne
« Bât./Étage ».

## 4. Règles spéciales

- **Repli label figé** : si les 6 champs structurés sont vides (jointure orpheline / lieu
  supprimé), Vector met le `Label` figé dans **`Nom`** (ex. « EHPAD LES TAMARIS - … »)
  → affichage 1 ligne.
- **Champs ERP non mappés par Vector** (donc non affichés) : `LocationType`, `LocationId`,
  `CityId`, `Latitude/Longitude`, `IsActive`, `BeneficiaryId`, `UnreferencedLocationStatus`.

## 5. État actuel (fait)

- ✅ **Service pickup/dropoff** rapatrié depuis la commande — `Erp.Order`,
  `MissionDetailQueryService` (branche `fix/missions-engaged-only-exclude-closed`).
- ✅ **Concaténation `Service` + `AddressLine3` dans `BatEtage`** — `Erp.Vector`,
  `JobRepository.ToJobLocation` (stopgap, ne perd plus le service).

## 6. Dette ouverte — DET-1 : champ `Service` dédié

Le service est logé dans `BatEtage` faute de champ dédié. À terme :

1. Ajouter `Service As String` à `ClJobLocation` **et** `ClJobLocationDto`.
2. `ToJobLocation` : `Service = ServiceLabel` ; `BatEtage = AddressLine3` seul.
3. `ClJobDetailAdapter.ToLocationDto` : mapper le nouveau champ.
4. **Contrat UI mobile** → coord. Alex + `note_ui_alex.md` : afficher une ligne `Service`
   **après `Nom`** (ordre legacy `ClHelper.GetSiteString` : Site → Service → lignes → CP/Commune).

⚠️ Changer le contrat : tant qu'Alex n'affiche pas la ligne `Service`, la remettre hors de
`BatEtage` la ferait **disparaître** de l'UI actuelle → basculer les deux côtés ensemble.

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

- [ ] Décider si on implémente DET-1 (champ `Service` dédié) ou on garde `BatEtage`.
- [ ] Si DET-1 : coordonner avec Alex la ligne `Service` (ordre après `Nom`).
- [ ] Vérifier l'affichage sur une mission **retour** (service non inversé).
- [ ] Vérifier un lieu **non référencé** (FreeText) : service référentiel conservé.
