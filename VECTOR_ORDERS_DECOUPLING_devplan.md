# Découplage Vector ↔ Orders — passage en HTTP

> Décision (2026-06-14) : **direction A — HTTP**. Vector cesse de référencer Orders en projet ;
> il consomme `Orders.Api` en REST, comme il consomme déjà `Address.Api` (`HttpAddressApiClient`).
> Objectif : isoler le build mobile (un WIP Orders ne doit plus casser Vector) et homogénéiser
> l'accès aux modules ERP.

## 1. Problème

`CaSoft.Erp.USVector.Infrastructure.csproj` référence en **projet** :
`..\..\Erp.Orders\Orders.Application` et `Orders.Infrastructure`. Vector **compile Orders depuis
les sources** → toute erreur Orders casse le build mobile (constaté : `IBreakInterruptReasonQueryService`
absent). Incohérence : **Address est déjà en HTTP**, Orders en in-process.

## 2. Surface consommée par Vector (audit)

Adaptateurs `CaSoft.Erp.USVector.Infrastructure/Repositories/Erp/` + `Mapping/ErpReferenceMappings` :

| Service Orders consommé | Méthode appelée | DTO retour | Adaptateur Vector |
|---|---|---|---|
| `IMissionDetailQueryService` | `GetFullAsync` | `ClMissionFullDtoOut`, `ClStageDetailDtoOut` | `JobRepository` |
| `IOrderQueryService` | `GetByIdAsync` | `ClEditOrderDtoOut` | `JobRepository` |
| `IBeneficiaryQueryService` | `GetByIdAsync` | `ClBeneficiaryDetailDtoOut` | `JobRepository` |
| `ICrewQueryService` | `ListAsync` | `ClCrewDtoOut`, `ClCrewMemberDtoOut` | `CrewRepository` |
| `IMissionQueryService` | (liste missions) | — | `CrewRepository` |
| `IPersonnelQueryService` | `GetPersonnelIdByKeyCloakIdAsync` | `ClPersonnelDtoOut` | `MobileIdentityResolver` |
| `IVehicleQueryService` | (get) | `ClVehicleDtoOut` | `CrewRepository`/mappings |

## 3. Couverture des endpoints `Orders.Api`

| Besoin Vector | Endpoint existant | État |
|---|---|---|
| Détail mission complet | `GET /missions/{id}/full` | ✅ |
| Commande par id | `GET /orders/{id}` | ✅ |
| Bénéficiaire par id | `GET /beneficiaries/{id}` | ✅ |
| Liste / détail équipages | `GET /crews`, `GET /crews/{id}`, `GET /crews/{id}/members` | ✅ |
| Véhicule par id | `GET /vehicles/{id}` | ✅ |
| Personnel par id | `GET /personnel/{id}` | ✅ |
| **Identité Keycloak → personnel** | — | ❌ **GAP** (à créer) |

➡️ Quasi tout est déjà exposé. Seul manque la résolution `sub` Keycloak → `PER_ID`.

## 3bis. Décision : direction **4a** retenue (2026-06-15)

4b (Orders.Contracts partagé) reporté : la fermeture transitive des DTO est large
(`ClMissionCancellationDtoOut`, `ClEditOrderBodyDtoOut`…) **et** Orders a 251 fichiers non
commités → refactor entremêlé. On fait **4a** : DTO miroir côté Vector + clients HTTP, **sans
toucher Orders** (sauf 1 endpoint additif, cf. DEC-4).

### Contrat HTTP vérifié (Orders.Api)
- JSON **camelCase** (`JsonSerializerDefaults.Web`), enums sérialisés en **entiers** → DTO miroir en `int`.
- `GET /missions/{id}/full` → mission détail | `GET /orders/{id}` → commande | `GET /beneficiaries/{id}` → bénéficiaire.
- `GET /missions?from=&to=&unassignedOnly=&includeCancelled=&take=` → liste missions.
- `GET /crews?personnelId=&date=&take=` → liste équipages (`ClCrewListItemDtoOut`, on ne lit que `Id`).
- ❌ **Gap** : `sub` Keycloak → `PER_ID` non exposé → DEC-4 (endpoint additif `GET /personnel/by-keycloak/{sub}`, le service `IPersonnelQueryService.GetPersonnelIdByKeyCloakIdAsync` existe déjà).

### Surface mobile à migrer (bornée)
`JobRepository`, `CrewRepository`, `MobileIdentityResolver` (+ `ErpReferenceMappings` = **mort, à supprimer**),
`Program.cs` (retirer `AddOrdersInfrastructure`), `USVector.Infrastructure.csproj` (retirer 2 refs projet).

### Stratégie par tranches (chaque tranche compile) — ÉTAT
1. ✅ **T1** : DTO miroir (`ErpApi/ErpReadDtos`) + client `IErpReadApiClient`/`HttpErpReadApiClient` + DI `AddHttpClient` + `OrdersApi:BaseUrl` (appsettings).
2. ✅ **T2** : `JobRepository` migré (mission/full + order + beneficiary) vers le client HTTP.
3. ✅ **T3** : `CrewRepository` (liste missions) + `MobileIdentityResolver` (crew list + mission detail) migrés.
4. ✅ **T4 / DEC-4** : endpoint `GET /personnel/by-keycloak/{sub}` ajouté à **Orders.Api** (`PersonnelEndpoints`, injecte `IPersonnelQueryService` ; 200 `Guid` ou 404). Build Orders.Api vert.
5. ✅ **T5** : `ErpReferenceMappings` supprimé (mort), `AddOrdersInfrastructure` retiré, **2 refs projet Orders retirées**.
6. ✅ **T6** : **isolation prouvée** — rebuild ne compile que `USVector.*` + connecteurs ; `USVector.Infrastructure` ne référence plus Orders.

### Reste / points de déploiement
- **T4** (endpoint personnel/keycloak) : seul reste, côté Orders.Api. Touche Orders mais **additif**.
- **Runtime** : `OrdersApi:BaseUrl` (appsettings, défaut `http://localhost:5200/`) doit pointer vers
  l'`Orders.Api` réel **avant déploiement** — sinon `GET FormStructure`/`JobEdit` échouent (mission lue en HTTP).
- `ConnectionStrings:OrdersDb` devient **inutilisé** côté mobile (plus d'OrdersDbContext).

## 4. Décision structurante : d'où viennent les DTO côté Vector ?

Les DTO (`ClMissionFullDtoOut`, …) vivent dans `Orders.Application`. Si Vector ne référence plus
Orders, il lui faut ces formes autrement. Deux options :

- **4a. DTO recopiés côté Vector** (dans `USVector.Contracts` ou `Infrastructure/ErpApi/Dtos`).
  Simple, zéro réf Orders, mais **duplication + risque de drift** quand Orders change le JSON.
- **4b. Assembly de contrats partagé** `Orders.Contracts` (DTO de lecture only, **sans logique**),
  référencé par `Orders.Api` ET Vector. Pas de duplication, et **pas de couplage au code métier**
  Orders (l'assembly contrats ne contient pas le handler cassé). À créer par extraction.

> Recommandation : **4b** si on veut éviter la dérive de contrat sur la durée ; **4a** si on veut
> un découplage strict et rapide sans toucher Orders. À trancher avant DEC-1.

## 5. Tickets

| Ticket | Objet | Dépend de |
|---|---|---|
| **DEC-1** | DTO de lecture côté Vector (option 4a) ou `Orders.Contracts` (4b) | §4 |
| **DEC-2** | Clients HTTP typés `IErpMissionClient`/`Order`/`Beneficiary`/`Crew`/`Vehicle`/`Personnel`, sur le modèle `HttpAddressApiClient` | DEC-1 |
| **DEC-3** | Réécrire les adaptateurs `Repositories/Erp/*` + `ErpReferenceMappings` pour consommer les clients HTTP au lieu des query services in-process | DEC-2 |
| **DEC-4** | Combler le gap : endpoint Orders.Api `GET /personnel/by-keycloak/{sub}` (ou query) + service | — |
| **DEC-5** | Retirer les `ProjectReference` Orders.Application/Infrastructure de `USVector.Infrastructure` ; supprimer `AddOrdersInfrastructure` ; `OrdersApi:BaseUrl` en config (comme `AddressApi`) ; enregistrer les `HttpClient` | DEC-3, DEC-4 |
| **DEC-6** | Auth service-to-service si Orders.Api est protégé (token client credentials Keycloak) | DEC-5 |
| **DEC-7** | Résilience : timeouts, `HttpClient` nommés ; (Polly optionnel) | DEC-5 |
| **DEC-8** | Validation : build mobile isolé (casser volontairement Orders → Vector compile toujours) ; tests fumée GET FormStructure/Crew | DEC-5 |

## 6. Risques / points à trancher

1. **Parité DTO** — vérifier que `GET /missions/{id}/full` renvoie bien la forme `ClMissionFullDtoOut`
   (et idem order/beneficiary). Sinon adapter le mapping JSON.
2. **Perf** — `JobRepository.GetJob` fera 3 appels HTTP (mission/full + order + beneficiary) au lieu
   de 3 requêtes in-process. Acceptable ; sinon prévoir un endpoint agrégé « job detail » côté Orders.
3. **PathBase IIS** — `Orders.Api` est hébergé sous un sous-chemin (cf. fix Swagger). `OrdersApi:BaseUrl`
   doit inclure le sous-chemin correct.
4. **Auth** — Keycloak est gated off en dev (`Keycloak:Enabled=false`). Si activé, prévoir DEC-6.
5. **Transaction / cohérence** — lecture seule, pas d'enjeu transactionnel cross-module.

## 7. Bénéfice attendu

- Build mobile **indépendant** d'Orders (débloque MOB-13.10 et la suite).
- Cohérence avec Address (tout l'ERP consommé en HTTP).
- Déploiement Vector/Orders découplé.

## 8. Ordre proposé

**4 (trancher 4a/4b) → DEC-4 (gap personnel) → DEC-1 → DEC-2 → DEC-3 → DEC-5 → DEC-7 → DEC-8**,
DEC-6 si auth activée. DEC-4 peut démarrer en parallèle (côté Orders.Api).

## 9. Dette technique

| # | Dette | Contexte / correctif propre |
|---|---|---|
| **DET-1** ✅ | **RÉSOLU (2026-07-14) — Champ `Service` dédié.** Le service médical (`ServiceLabel`, ex. « Cardiologie ») avait été concaténé dans `BatEtage` faute de champ dédié. | Livré : `Service` ajouté à `ClJobLocation` **et** `ClJobLocationDto` ; `ToJobLocation` → `Service = ServiceLabel`, `BatEtage = AddressLine3` seul ; `ClJobDetailAdapter` mappe le champ. Contrat UI basculé — `note_ui_alex.md` + `docs/ui-web/jobdetail-champ-service.md` (ligne `Service` après `Nom`). ⚠ déployer serveur + app ensemble. |
| **DET-2** | **Nommage des DTO du contrat mobile non aligné sur `DtoIn`/`DtoOut`** — DTO hérités du portage USVector nommés en legacy (`ClJobLocationDto`, `ClJobDetailModel`…) au lieu du suffixe directionnel de la convention (`DtoOut` = Application → UI, `DtoIn` = UI → Application). | Renommer progressivement en `…DtoOut` / `…DtoIn`. **Aucun impact JSON** (le nom de type n'apparaît pas sur le fil — seules les propriétés comptent) → simple refactor de types + références. Priorité basse, par lot. |
| **DET-3** | **Une adresse ne devrait jamais être « non structurée »** (une seule ligne / label figé). Le repli de `ToJobLocation` ne devrait pas se déclencher : il signale une donnée non normalisée ou une référence orpheline côté ERP. Vector loggue désormais un WARNING (`mission`, `pickup/dropoff`, `label`) quand ça arrive, au lieu de le masquer. | **Revue de code côté saisie/validation (Orders / Address.Api)** : garantir qu'une adresse enregistrée porte **a minima `AddressLine1` + Commune (CP/Ville)** — jamais un simple label mono-ligne. Contrôler aussi les **références orphelines** (site/adresse supprimé après commande). *Adresse totalement vide = problème distinct*, à traiter à part. Surveiller les WARNING « Lieu non structuré » en prod pour mesurer l'ampleur. |
