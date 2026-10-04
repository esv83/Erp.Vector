# 💳 Note front — La carte mutuelle dans les listes de missions (pour Jules)

> **Date** : 2026-10-04 · **Pour** : Jules, front de la régulation (Orders) et de la certification.
> **Objet** : une entrée de menu **« Carte mutuelle »** sur chaque ligne des deux listes de missions —
> active quand l'équipage a déjà photographié la carte du patient, avec la date de la photo, et
> l'image affichée à la demande.
> **API** : Vector (`…/vector/api/…`), appelée **directement depuis le navigateur**, avec le jeton de
> l'utilisateur. **JSON** : PascalCase, comme tout le contrat Vector.

Salut Jules 👋

Les ambulanciers photographient la carte mutuelle depuis la mission ; Vector la garde **par patient**
(elle le suit d'un transport à l'autre). Tes deux listes n'ont que l'identifiant de la **mission** :
c'est suffisant, Vector retrouve le patient tout seul.

---

## Le parcours en trois appels

```
Affichage de la page  ──► POST api/missions/mutuelle-card/presence   → une ligne par mission (pastille, date)
Clic « Carte mutuelle » ─► GET  api/missions/{missionId}/mutuelle-card → nom, AMC, concentrateur, télétransmission
                       └► GET  api/missions/{missionId}/mutuelle-card/image → la photo (fetch + blob)
```

### 1. Présence — une fois par page affichée

```http
POST api/missions/mutuelle-card/presence
Authorization: Bearer <jeton de l'utilisateur>
Content-Type: application/json

{ "MissionIds": [ "…", "…" ] }
```

**200 missions au plus par appel** (au-delà : `400`, découpe la page). Réponse : **une ligne par mission
demandée**, dans l'ordre de ta demande :

```json
[
  { "MissionId": "…", "BeneficiaryId": "…", "HasCard": true,
    "CapturedAt": "2026-09-20T08:30:00Z", "ImageUrl": "api/missions/…/mutuelle-card/image" },
  { "MissionId": "…", "BeneficiaryId": "…", "HasCard": false, "CapturedAt": null, "ImageUrl": null },
  { "MissionId": "…", "BeneficiaryId": null, "HasCard": false, "CapturedAt": null, "ImageUrl": null }
]
```

- `HasCard = false` → entrée de menu **grisée**. `BeneficiaryId = null` : mission sans patient (ou
  inconnue de la régulation).
- `CapturedAt` → « photo du 20/09 ». C'est **la plus récente** : une nouvelle photo remplace l'affichage.
- `ImageUrl` est **relative** : préfixe-la avec la base de l'API Vector que tu as en configuration.

### 2. Les données de la carte — au clic

```http
GET api/missions/{missionId}/mutuelle-card
Authorization: Bearer <jeton>
```

`200` → `MutuelleName`, `AmcCode`, `Concentrateur`, `Teletransmission`, `CapturedAt`, et :
- **`FieldsInheritedFrom`** : si renseigné, les champs ont été **repris d'une photo précédente** (celle
  de cette date) et pas encore revérifiés sur la nouvelle. Affiche-le : *« repris de la photo du
  02/06, à revérifier »*. Nul = validés sur la photo affichée.
- **`404`** = pas de carte (ou pas de patient). Ce n'est pas une erreur.

### 3. La photo — avec le jeton, donc pas par `<img src>`

Une balise `<img src>` **n'envoie pas** le jeton : la route répondrait `401`. Charge l'image puis
affiche-la depuis un blob :

```js
const r = await fetch(`${vectorApi}/${ligne.ImageUrl}`, { headers: { Authorization: `Bearer ${jeton}` } });
if (r.ok) img.src = URL.createObjectURL(await r.blob());   // pense à URL.revokeObjectURL à la fermeture
```

---

## Erreurs

| Code | Sens | Quoi faire |
|---|---|---|
| `401` | Pas de jeton, ou client Keycloak non déclaré chez Vector | voir « Avant de commencer » |
| `403` | Jeton valide, mais route hors de ce qui est ouvert aux écrans | ces quatre routes seulement — tout le reste de Vector est réservé au terrain |
| `400` | Plus de 200 missions | découper |
| `503` | La régulation ou la base ne répond pas | réessayer dans une minute ; `Detail` est affichable |

## ⚠️ Avant de commencer

**Vector doit connaître ton client Keycloak** : donne-nous l'`azp` des jetons émis pour l'écran de la
régulation et pour celui de la certification. Tant qu'il n'est pas déclaré (`Keycloak:ScreenAzp`), ces
routes te répondent `401`.

**Les deux routes sans jeton** (`POST api/mutuelle-card/presence` par bénéficiaire, `GET
api/beneficiaries/{id}/mutuelle-card/image`) **vont fermer** : elles ont été ouvertes pour un affichage
par `<img src>` que personne n'a jamais branché. N'en dépends pas — dis-nous quand tu as basculé sur
celles-ci.

## ✅ Récap

- [ ] Communiquer à Vector l'`azp` des deux écrans
- [ ] Liste de régulation et liste de certification : un appel `presence` par page, entrée « Carte mutuelle » grisée si `HasCard = false`, date de la photo
- [ ] Au clic : données de la carte, mention « repris de la photo du … » si `FieldsInheritedFrom`
- [ ] La photo par `fetch` + blob, jamais par `<img src>` nu
