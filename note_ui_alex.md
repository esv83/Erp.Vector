# Note d'intégration UI — Vector API (pour Alex)

**Date : 2026-07-05** · Contexte : évolutions du contrat de l'API terrain (`api.urgencesante.net/vector`) livrées cette semaine. Principe : **toute la logique est côté serveur, l'UI n'affiche que ce qu'on lui envoie.** JSON en **PascalCase** (sauf `schedule` en minuscule, historique).

Résumé des actions attendues côté UI en fin de note (§6).

---

## 1. JobList — « Bien reçu » = `IsSeen` (ex-`IsAck`)

L'acquittement « bien reçu » est désormais le marqueur **« Mission vue »** (aligné sur la spec régulation).

- **Icône** : afficher si `IsSeen == false`, **masquer** si `IsSeen == true`.
- **Action au clic** : `PATCH /vector/api/Joblist` avec le corps :
  ```json
  { "IsJob": true, "JobId": "<guid-mission>" }
  ```
  → pose l'horodatage, remonte à la régulation, **idempotent** (re-cliquer ne change rien).
- **Compat** : le champ **`IsAck`** est encore présent (alias de `IsSeen`, même valeur) le temps de ta bascule. **Migre vers `IsSeen`** ; `IsAck` sera retiré.

## 2. JobList — missions clôturées masquées (automatique)

Le joblist ne renvoie plus que les missions **jusqu'à « Terminé »**. Les missions **clôturées disparaissent** d'elles-mêmes. **Aucune action UI** — juste ne pas t'étonner qu'une mission terminée puis clôturée sorte de la liste.

---

## 3. Driver (conducteur) — endpoints token-canoniques

L'équipage est **dérivé du token** (plus besoin de passer un `crewId`).

- **Lire le conducteur** : `GET /vector/api/Driver`
  ```json
  {
    "DriversCollection": [ { "DriverId": "...", "DriverName": "VAZZOTTI OCEANE" }, ... ],
    "SelectedDriver":    { "DriverId": "...", "DriverName": "..." },
    "ChangeDate":        "2026-07-05T08:55:09" ,
    "VehicleModel":      { "VehicleID": "...", "Immatriculation": "GS-561-PV" }
  }
  ```
  - `DriversCollection` = membres sélectionnables. `SelectedDriver` = conducteur courant.
  - ⚠️ **`SelectedDriver` est toujours non-null** : si aucun conducteur désigné, `DriverId` = `00000000-...` et `DriverName` = `""` (→ rien de pré-sélectionné). *(C'était la cause de la page blanche : ne fais plus `SelectedDriver.DriverName` sans garde, même si le serveur le garantit désormais.)*
- **Changer le conducteur** : `POST /vector/api/Driver`, corps = **le Guid du conducteur** (chaîne JSON brute) :
  ```json
  "81cc3fd1-c2e9-4a40-b798-68da7f29b907"
  ```
  → 200. Après quoi `GET` renvoie ce conducteur en `SelectedDriver`.
- **Défaut** : la régulation renseignera un conducteur par défaut ; sinon `SelectedDriver` vide.

---

## 4. JobDetail — DtoOut enrichi (`GET /vector/api/JobDetail/{jobId}`)

**Nouveaux champs à utiliser** (les anciens restent temporairement, cf. §5) :

| Nouveau champ | Contenu | Remplace |
|---|---|---|
| `ScheduleLabel` | Prise en charge formatée : **« à 15:19 »** le jour même, sinon **« mardi 14/07/2026 à 15:00 »** | `Schedule` (brut) |
| `TransportModeLabel` | Mode : **sous-catégorie** (« Bariatrique », « TPMR ») si présente, sinon **principal** (« AMBULANCE », « VSL ») | `TransportMode` |
| `PickupLocation` / `DropoffLocation` | Lieu **structuré** (voir ci-dessous) | `Departure` / `Arrival` (bloc texte) |
| `Comments` | Commentaire mission, dans un champ à part | (était vide) |

**Structure d'un lieu** (`PickupLocation` / `DropoffLocation`) — **afficher une ligne par champ non vide**, dans cet ordre :
```json
{
  "Nom":        "CHITS CH SAINTE MUSSE",
  "Adresse":    "54 R HENRI SAINTE CLAIRE DEVILLE",
  "Residence":  "",
  "BatEtage":   "CS 31412",
  "Commune":    "83000 Toulon",
  "Complement": ""
}
```
> ⚠️ **Lieux non référencés** (EHPAD, domicile…) : l'ERP ne fournit pas de champs structurés — seul **`Nom`** est rempli (avec le libellé complet, ex. « EHPAD LES TAMARIS - CHAM 38 RDC - La Valette-du-Var »), les autres champs sont vides. Gère donc le cas « une seule ligne ».

> 🧩 **Dette connue — champ `Service` à venir** : pour un **établissement de santé** (et un lieu FreeText), l'ERP porte un **service** (ex. « Cardiologie ») distinct du bâtiment/étage. Aujourd'hui, faute de champ dédié, le service est **concaténé dans `BatEtage`** (service puis ligne 3). Un futur champ **`Service`** sera ajouté à `PickupLocation`/`DropoffLocation` ; `BatEtage` ne portera alors plus que la ligne 3. Prévois l'affichage d'une ligne `Service` (après `Nom`) le moment venu — je te préviens avant de basculer le contrat.

Champs inchangés utiles : `TransportSens`, `IsSerial`, `IsSign`, `Appointment`, `Beneficiary` (`CompleteName`, `DDN`, `Age`, `Phones`).

---

## 5. Champs dépréciés (à migrer, seront retirés)

| Écran | Ancien champ | Nouveau |
|---|---|---|
| JobList | `IsAck` | **`IsSeen`** |
| JobDetail | `Schedule` (brut) | **`ScheduleLabel`** |
| JobDetail | `TransportMode` | **`TransportModeLabel`** |
| JobDetail | `Departure` / `Arrival` (texte) | **`PickupLocation` / `DropoffLocation`** (structuré) |

Ils cohabitent aujourd'hui (double champ). Dis-moi quand tu as basculé, je les retire.

---

## 6. Authentification — pas de re-login avant 15 h (à implémenter côté client)

L'access token Keycloak est **court (5 min)** — c'est normal. Pour éviter une ré-authentification avant **15 h**, le client doit **rafraîchir silencieusement** l'access token avec le **refresh token** (endpoint `/token` de Keycloak) avant expiration (ou sur un 401), **sans redemander le login**. Re-login uniquement quand la **session (15 h)** expire ou que le refresh échoue.
*(La durée de session 15 h est réglée côté Keycloak ; le silent-refresh est côté client.)*

---

## Récap actions UI
1. **JobList** : icône « bien reçu » sur `IsSeen` (+ `PATCH` au clic). Migrer `IsAck` → `IsSeen`.
2. **Driver** : `GET`/`POST /api/Driver` (token, sans crewId) ; garder la garde null sur `SelectedDriver`.
3. **JobDetail** : basculer sur `ScheduleLabel`, `TransportModeLabel`, `PickupLocation`/`DropoffLocation`, `Comments`.
4. **Auth** : implémenter le **silent refresh** (refresh token) pour la session 15 h.

Questions / besoin d'exemples de payloads réels : reviens vers moi.
