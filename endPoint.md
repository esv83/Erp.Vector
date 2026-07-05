# Orders.Api — endpoints à implémenter pour la feature « Driver » (MOB-4 / MOB-11)

Ce document est le **contrat** que Vector.Api attend d'Orders.Api pour la fonctionnalité
« conducteur d'équipage ». Le code Vector est déjà écrit et appelle ces deux routes ; il suffit
de les implémenter côté Orders au contrat ci-dessous.

## Contexte

- Vector est **découplé** d'Orders : il ne lit plus la base ERP directement, il consomme Orders.Api en HTTP.
- Le flux Driver côté Vector : `token Keycloak → sub → PER_ID (by-keycloak) → équipage actif (GET /crews) → détail équipage (GET /crews/{id}) → conducteur`.
- Ces 2 endpoints sont **additifs** (même logique que l'endpoint `personnel/by-keycloak/{sub}` ajouté précédemment).

## Conventions (IMPÉRATIF)

| Règle | Valeur |
|------|--------|
| Casse JSON | **camelCase** (`personnelId`, `driverPersonnelId`, `immatriculation`, `serviceStart`…) |
| Dates | `DateTime` **sans fuseau**, format `yyyy-MM-ddTHH:mm:ss` |
| `crewId` | exactement l'`id` renvoyé par `GET /crews?personnelId=&date=` (Vector enchaîne les deux) |
| `members[].id` / `personnelId` | **PER_ID** — même identifiant que renvoie `by-keycloak` |
| Introuvable | **404** (Vector le gère proprement : `GetOrNullAsync` → `null`) |

> ⚠️ Les noms de champs doivent correspondre **exactement** : c'est ainsi que Vector désérialise
> (`JsonSerializerDefaults.Web`, case-insensitive mais camelCase attendu en sortie).

---

## 1) `GET /crews/{crewId}` — détail complet d'un équipage

Retourne les membres (**avec leur PER_ID**), le conducteur actif, le véhicule et la fenêtre de service.
Sans les IDs des membres, l'UI ne peut pas proposer la sélection du conducteur — d'où cet endpoint
(la liste `GET /crews` ne renvoie que des noms).

### Requête
```
GET /crews/{crewId:guid}
```

### Réponses
| Code | Corps | Cas |
|------|-------|-----|
| `200` | `CrewFullDto` | équipage trouvé |
| `404` | (ProblemDetails standard) | équipage inconnu |

### Corps `200`
```jsonc
{
  "id": "d1f0e2a3-…",                       // Guid — identité de l'équipage (= id de GET /crews)
  "serviceStart": "2026-07-04T20:00:00",    // début de vacation
  "serviceEnd": "2026-07-05T08:00:00",      // fin de vacation, ou null si ouverte
  "vehicle": {                              // null si aucun véhicule affecté
    "id": "d435e9d0-f371-420f-afae-5f88f14a9065",
    "immatriculation": "AB-123-CD"
  },
  "activeDriver": {                         // null si aucun conducteur encore désigné
    "personnelId": "81cc3fd1-c2e9-4a40-b798-68da7f29b907",
    "from": "2026-07-04T21:10:00"
  },
  "members": [                              // membres de l'équipage pour cette vacation
    { "id": "81cc3fd1-c2e9-4a40-b798-68da7f29b907", "firstName": "Océane", "lastName": "VAZZOTTI" },
    { "id": "a2b3c4d5-…",                            "firstName": "Myke",   "lastName": "TRAPANI"  }
  ]
}
```

### DTO C# (référence)
```csharp
public sealed class CrewFullDto
{
    public Guid Id { get; init; }
    public DateTime ServiceStart { get; init; }
    public DateTime? ServiceEnd { get; init; }
    public CrewVehicleDto? Vehicle { get; init; }
    public CrewDriverDto? ActiveDriver { get; init; }
    public List<CrewMemberDto> Members { get; init; } = new();
}

public sealed class CrewMemberDto  { public Guid Id { get; init; } public string? FirstName { get; init; } public string? LastName { get; init; } }
public sealed class CrewVehicleDto { public Guid Id { get; init; } public string? Immatriculation { get; init; } }
public sealed class CrewDriverDto  { public Guid PersonnelId { get; init; } public DateTime From { get; init; } }
```

### Sémantique / mapping ERP
- `members[].id` = **PER_ID** (indispensable : le `POST` de changement renvoie ce PER_ID).
- `activeDriver` = conducteur courant de la vacation. **Conducteur par défaut (décision 2026-07-05)** :
  quand **aucun conducteur n'est explicitement affecté**, renvoyer `activeDriver` = **1er membre actif**
  (ou règle métier ERP : chef d'équipage / rôle conducteur). Objectif : l'app affiche toujours un
  conducteur par défaut, cohérent pour tous les consommateurs. Ne jamais renvoyer `activeDriver: null`
  quand l'équipage a au moins un membre. (Vector conserve une garde : si `null`, il renvoie un
  conducteur « vide » pour ne pas casser le client — mais le défaut doit venir d'Orders.Api.)
- `vehicle.immatriculation` = requis par l'UI (l'ID seul ne suffit pas).
- `serviceStart` / `serviceEnd` = fenêtre de vacation (Vector s'en sert pour départager
  quand un personnel a plusieurs équipages actifs le même jour).

---

## 2) `PUT /crews/{crewId}/driver` — désigner le conducteur

Fixe le conducteur actif de l'équipage. Le personnel indiqué **doit être membre** de l'équipage.

### Requête
```
PUT /crews/{crewId:guid}/driver
Content-Type: application/json
```
```jsonc
{
  "driverPersonnelId": "81cc3fd1-c2e9-4a40-b798-68da7f29b907",  // PER_ID du conducteur choisi
  "from": "2026-07-04T21:10:00"                                 // horodatage de la désignation
}
```
```csharp
public sealed class SetCrewDriverRequest
{
    public Guid DriverPersonnelId { get; init; }
    public DateTime From { get; init; }
}
```

### Réponses
| Code | Cas |
|------|-----|
| `200` ou `204` | succès (Vector teste seulement `IsSuccessStatusCode`) |
| `404` | équipage inconnu |
| `400` / `409` | `driverPersonnelId` n'est pas membre de l'équipage |

### Effet
Le personnel devient le conducteur actif à la date `from` → doit ensuite ressortir en
`activeDriver` dans `GET /crews/{crewId}`.

> Vector valide déjà l'appartenance du conducteur à l'équipage avant d'appeler (use case
> `ClSetDriverUseCase`), mais une garde défensive côté Orders (renvoyer 400/409 si non-membre)
> est recommandée.

---

## Côté Vector (déjà en place — pour info)

| Élément Vector | Appelle |
|----------------|---------|
| `HttpErpReadApiClient.GetCrewFullAsync(crewId)` | `GET crews/{crewId}` |
| `HttpErpWriteApiClient.SetCrewDriverAsync(crewId, driverPersonnelId, from)` | `PUT crews/{crewId}/driver` |
| `GET /vector/api/Driver` (token-canonique) | résout l'équipage du token puis lit le détail |
| `POST /vector/api/Driver/{crewId}` (body = PER_ID) | change le conducteur |

Une fois ces 2 endpoints livrés, la feature Driver fonctionne de bout en bout, **sans
redéploiement Vector supplémentaire** au-delà de celui de la feature elle-même.
