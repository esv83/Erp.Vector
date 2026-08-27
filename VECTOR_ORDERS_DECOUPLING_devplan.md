# 🔌 Découplage Vector ↔ Orders — accès HTTP

> **Objet** : Vector ne compile plus Orders depuis les sources ; il consomme `Orders.Api` en REST,
> comme il consomme déjà `Address.Api`. Décision du 2026-06-14, **direction 4a** (DTO miroir côté
> Vector, sans toucher Orders sauf un endpoint additif).
>
> **Mise à jour 2026-08-24** — livré résumé en prose, reste détaillé techniquement, pistes
> abandonnées listées au §4.

---

## 1. Ce qui est livré

**Le build mobile est indépendant d'Orders.** Un chantier en cours côté Orders ne casse plus la
compilation de Vector, et les deux modules se déploient séparément. L'isolation a été prouvée : une
reconstruction complète ne compile plus que Vector et ses connecteurs.

**Tout l'ERP se consomme de la même façon.** Missions, commandes, patients, équipages, véhicules et
personnel sont lus par appels HTTP, au même titre que les adresses — plus aucune référence de projet
ni de chaîne de connexion vers la base d'Orders.

**Le seul manque côté Orders a été comblé** : la résolution d'un compte Keycloak vers un ambulancier,
ajoutée en tant qu'endpoint additif, sans rien casser d'existant.

**Un chemin d'écriture a suivi la même voie** : l'avancement terrain est poussé vers Orders en HTTP,
avec une file d'attente qui rejoue les envois en échec.

*Livré en six tranches (T1→T6) + l'endpoint additif, isolation de build vérifiée. Détail des tranches
dans `git log`.*

---

## 2. Ce qui reste

> 📌 **`DEC-6` et `DEC-7` ont leur plan d'exécution dédié** :
> [`devplan.md`](devplan.md) §3.C2 et §3.D — étapes codables, ordre de livraison et décisions
> à trancher. *(Le plan d'exécution séparé `DEVPLAN_2.md` y a été fusionné le 2026-08-27.)*
> Les trois autres restes ci-dessous (`DET-4`, `DET-3`, option 4b) n'y sont pas et restent suivis
> au devplan principal.

### 2.1 ⏳ DEC-6 — Authentification de service à service

`Orders.Api` est aujourd'hui appelée **sans jeton** : les deux `HttpClient` de
`Program.cs` (`IErpReadApiClient`, `IErpWriteApiClient`) ne posent aucun en-tête `Authorization`.
Cela tient tant qu'Orders.Api n'est pas protégée. Le jour où elle l'est, il faut un **client
credentials Keycloak** (compte de service dédié à Vector), avec cache et renouvellement du jeton, sur
le modèle de ce que le module Identity a mis en place (`Keycloak:AllowedAzp` côté serveur appelé).
**À anticiper** : le symptôme sera une série de 401 sur la joblist, en production, sans autre indice.

### 2.2 ⏳ DEC-7 — Résilience des appels sortants

Les deux clients sont enregistrés avec la seule `BaseAddress` : **pas de timeout explicite**
(donc 100 s par défaut, ce qui fait pendre une requête mobile), **pas de retry ni de disjoncteur** sur
le chemin de **lecture**. Le chemin d'écriture est couvert (file `MOB_OPERATIONAL_OUTBOX` + worker
avec retry), pas la lecture.

À faire : timeout court et explicite sur les deux clients, puis `AddStandardResilienceHandler`
(Microsoft.Extensions.Http.Resilience) ou Polly — en gardant le comportement déjà en place côté
lecture, qui tolère un 404 (`ListCrewIdsAsync` → liste vide plutôt qu'erreur).

### 2.3 ⚠️ DET-4 — Nommage des DTO du contrat mobile

*(Renuméroté : ce point portait le numéro DET-2, déjà utilisé par l'affichage pickup/dropoff piloté
serveur, livré et communiqué au dev web.)*

Les DTO hérités du portage sont nommés en legacy (`ClJobLocationDto`, `ClJobDetailModel`…) au lieu du
suffixe directionnel de la convention (`…DtoOut` Application → UI, `…DtoIn` UI → Application).
Renommage progressif, **sans aucun impact JSON** (le nom du type n'apparaît pas sur le fil, seules
les propriétés comptent) : simple refactor de types et de références. **Priorité basse, par lots.**

### 2.4 ⚠️ DET-3 — Une adresse ne devrait jamais être « non structurée »

Le repli de `ToJobLocation` (label mono-ligne) ne devrait jamais se déclencher : quand il le fait, il
signale une donnée non normalisée ou une **référence orpheline** côté ERP (site ou adresse supprimé
après la commande). Vector journalise désormais un WARNING (`mission`, `pickup`/`dropoff`, `label`)
au lieu de masquer le cas.

Le correctif n'est pas dans Vector : **revue de la saisie et de la validation côté Orders /
Address.Api** pour garantir qu'une adresse enregistrée porte au minimum `AddressLine1` + commune
(CP/Ville), et contrôle des références orphelines. *Une adresse totalement vide est un problème
distinct, à traiter à part.* Surveiller les WARNING « Lieu non structuré » en production pour mesurer
l'ampleur avant d'engager quoi que ce soit.

### 2.5 ⚪ Différé — Assembly de contrats partagé (option 4b)

`Orders.Contracts` (DTO de lecture seule, sans logique, référencé par `Orders.Api` **et** Vector)
supprimerait la duplication des DTO miroir et le **risque de dérive** quand Orders change son JSON.
Reporté à l'époque parce que la fermeture transitive des DTO était large et qu'Orders avait un gros
WIP non commité. **Le risque de drift est assumé** : il se manifestera par un champ silencieusement
null côté mobile, pas par une erreur.

---

## 3. Contrat consommé (rappel opérationnel)

JSON **camelCase** (`JsonSerializerDefaults.Web`), **enums sérialisés en entiers** → les DTO miroir
côté Vector sont en `int`.

| Besoin | Endpoint Orders.Api |
|---|---|
| Détail mission complet | `GET /missions/{id}/full` |
| Commande / bénéficiaire | `GET /orders/{id}` · `GET /beneficiaries/{id}` |
| Missions du jour | `GET /missions?from=&to=&unassignedOnly=&includeCancelled=&take=` |
| Équipages | `GET /crews?personnelId=&date=&take=` · `GET /crews/{id}` · `PUT /crews/{id}/driver` |
| Véhicule / personnel | `GET /vehicles/{id}` · `GET /personnel/{id}` |
| **Keycloak → ambulancier** | `GET /personnel/by-keycloak/{sub}` *(endpoint additif livré pour Vector)* |
| Projection de l'avancement | `PUT /missions/{id}/operational` |

⚠️ **`OrdersApi:BaseUrl` doit se terminer par un `/` et inclure le PathBase IIS** — sans le slash
final, le dernier segment est perdu à la résolution d'URI relative et tout part en 500. Cause d'une
panne réelle en juillet 2026.

Attente côté Orders, encore non honorée : le filtre `assignedCrewId` sur `GET /missions` (Vector
l'envoie, Orders l'ignore → toute la journée est rapatriée puis filtrée en mémoire). Contrat détaillé
dans [`endPoint.md`](endPoint.md).

---

## 4. Retiré du plan — obsolète ou abandonné

| Ce qui a disparu | Motif |
|---|---|
| **Audit de la surface consommée** (tableau des query services in-process) et **table de couverture des endpoints** | Servaient à préparer la migration ; elle est faite. Le contrat réellement consommé est au §3. |
| **Tranches T1→T6 et tickets DEC-1, DEC-2, DEC-3, DEC-5, DEC-8** | Livrés. |
| **Arbitrage 4a / 4b** et sa recommandation | Tranché : **4a**. 4b subsiste seulement comme option différée (§2.5). |
| **`ErpReferenceMappings`** | Supprimé (code mort après la bascule). |
| **`AddOrdersInfrastructure` + les 2 références projet Orders + `ConnectionStrings:OrdersDb`** | Retirés ; la clé de connexion est devenue inutilisée côté mobile. |
| **Risque « parité des DTO »** | Levé : formes vérifiées à la migration, puis en service réel. |
| **Risque « perf : 3 appels HTTP au lieu de 3 requêtes in-process »** | Accepté et mesuré en service ; pas d'endpoint agrégé « job detail » à demander à Orders. |
| **DET-1 — champ `Service` concaténé dans `BatEtage`** | Résolu le 2026-07-14 : champ `Service` dédié, contrat UI basculé. |

---

**Fin du document**
