# Note UI Web — Sélection d'équipage (personnel multi-crew)

**But :** un personnel peut être membre de **plusieurs équipages le même jour** (ex. change de véhicule/binôme entre le matin et l'après-midi). L'app doit **imposer le choix de l'équipage actif** au login, et permettre d'en **changer en cours de journée**. Toutes les actions rattachées au véhicule (conducteur, km, fin de service) ciblent l'équipage épinglé.

> **Principe : le backend décide, l'UI affiche.** Tu ne calcules rien (ni « quel équipage est actif maintenant », ni les libellés, ni s'il faut forcer un choix). Le serveur te livre du prêt-à-afficher et refuse tout accès incohérent.

---

## ⚠️ Changements de contrat (breaking)

| Avant | Maintenant | Note |
|---|---|---|
| `GET /api/joblist` (union tous crews) | `GET /api/joblist/{crewId}` | scopée à l'équipage épinglé |
| `GET /api/driver` (équipage deviné) | `GET /api/driver/{crewId}` | équipage explicite obligatoire |
| `POST /api/driver` (équipage deviné) | `POST /api/driver/{crewId}` | idem |
| — | `GET /api/crew/mine` | **nouveau** : le sélecteur d'équipage |
| `GET/POST /api/kilometers/{crewId}` | inchangé | déjà crew-scoped |
| `GET/POST /api/endofservice/{crewId}` | inchangé | déjà crew-scoped |

Les anciens endpoints « devinés » (`/api/joblist`, `/api/driver` sans `crewId`) **n'existent plus** : ils choisissaient un équipage arbitraire quand le personnel en avait plusieurs — c'était le bug. Désormais l'équipage est toujours explicite.

---

## Flux à implémenter

### 1. Au login → choisir l'équipage actif

```
GET /api/crew/mine        (Authorization: Bearer <token Keycloak>)
```

Réponse (exemple à 2 équipages) :

```jsonc
{
  "RequiresSelection": true,                 // > 1 équipage → tu DOIS afficher le choix
  "RecommendedCrewId": "3f2a…",              // à pré-cocher (équipage qui couvre l'heure actuelle)
  "Crews": [
    { "CrewId": "3f2a…",
      "DisplayLabel": "AB-123-CD · DUPONT Jean / MARTIN Paul",  // déjà composé
      "VehicleImmat": "AB-123-CD",
      "Members": "DUPONT Jean / MARTIN Paul",
      "ServiceWindow": "06:00 – 14:00",       // déjà formaté
      "IsCurrent": true,                      // couvre l'instant présent
      "IsClosed": false },                    // false = modifiable ; true = vacation finie (lecture seule)
    { "CrewId": "9b7e…",
      "DisplayLabel": "CD-456-EF · DUPONT Jean / LEROY Anne",
      "VehicleImmat": "CD-456-EF",
      "Members": "DUPONT Jean / LEROY Anne",
      "ServiceWindow": "14:00 – 22:00",
      "IsCurrent": false,
      "IsClosed": false }
  ]
}
```

**Ce que fait l'UI — rien d'autre :**

- `RequiresSelection == true` → affiche la liste `Crews` (labels déjà prêts), pré-coche `RecommendedCrewId`, l'utilisateur choisit → **épingle le `CrewId` choisi** (state app).
- `RequiresSelection == false` → un seul équipage : épingle `Crews[0].CrewId` directement, pas d'écran.
- `404` → aucun équipage actif aujourd'hui (afficher le message renvoyé).

### 2. Ensuite → tout est scopé au `CrewId` épinglé

Passe le `CrewId` épinglé dans l'URL de chaque appel :

```
GET  /api/joblist/{crewId}
GET  /api/driver/{crewId}
POST /api/driver/{crewId}          body: "<guid-conducteur>"
GET  /api/kilometers/{crewId}
GET  /api/endofservice/{crewId}
```

### 3. Changement d'équipage en cours de journée

Un bouton « changer d'équipage » → **re-`GET /api/crew/mine`** → ré-affiche la liste (les deux équipages du jour y figurent, le matin ET l'après-midi) → l'utilisateur re-choisit → épingle le nouveau `CrewId`. Rien de plus : les appels suivants portent le nouveau `crewId`.

> Les actions déjà posées sur l'équipage du matin restent sur lui ; les nouvelles vont sur l'équipage de l'après-midi. L'attribution par véhicule est correcte automatiquement.

---

## Le conducteur (`/api/driver/{crewId}`)

`GET` renvoie déjà tout, prêt à afficher :

```jsonc
{
  "SelectedDriver":   { /* conducteur actif — jamais null : Guid vide si non désigné */ },
  "DriversCollection": [ /* membres sélectionnables */ ],
  "ChangeDate":        "2026-07-06T08:12:00Z",
  "VehicleModel":      { /* véhicule de l'équipage */ }
}
```

`POST /api/driver/{crewId}` avec le `Guid` du conducteur choisi (un membre de l'équipage). Le serveur refuse (400) un conducteur hors équipage.

---

## Codes d'erreur communs à tous les endpoints crew-scoped

Le serveur valide **systématiquement** que le `crewId` fait partie des équipages actifs du personnel (l'UI ne peut pas cibler un équipage étranger, même par erreur) :

| Code | Sens | Action UI |
|---|---|---|
| `401` | token absent / rejeté / `sub` invalide | ré-authentifier (Keycloak) |
| `403` | compte non rattaché à un personnel, **ou** `crewId` hors de tes équipages actifs | afficher le message ; si équipage périmé → relancer `GET /api/crew/mine` |
| `404` | aucun équipage actif aujourd'hui | afficher le message |

En cas de `403` sur un `crewId` épinglé (ex. vacation terminée entre-temps), **rappelle `GET /api/crew/mine`** pour re-choisir : c'est le point d'entrée unique de (re)sélection.
