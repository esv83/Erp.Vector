# 💳 Note UI Web Vector — Carte mutuelle : photo et saisie depuis la mission (pour Alexandre)

> **Date** : 2026-09-13 · **Pour** : Alexandre, dev web de l'UI Vector.
> **Objet** : brancher la photo de la carte mutuelle du patient et la saisie de ses quatre champs
> dans l'écran mission. **Routes nouvelles et additives** : rien de ce qui existe ne change.
> **JSON** : PascalCase, comme le reste du contrat mobile.
>
> ✅ Les routes par mission sont **en service en production depuis le 2026-09-13**.

Salut Alexandre 👋

**Pourquoi cette note** : depuis la mise en service, **aucune carte mutuelle n'est arrivée en
production**. Ce n'était pas de ton fait : la seule route de capture demandait l'identifiant du
patient, et l'API ne te l'a jamais envoyé — le bloc `Beneficiary` du détail mission n'a pas d'`Id`.
Les nouvelles routes prennent **l'id de la mission**, que tu as déjà partout ; le serveur retrouve
le patient tout seul.

La carte suit le **patient**, pas la mission : photographiée une fois, elle ressort sur ses
transports suivants.

---

## Le parcours en trois appels

```
Ouverture de la mission ──► GET   api/missions/{jobId}/mutuelle-card    200 = carte connue · 404 = aucune
Photo                   ──► POST  api/missions/{jobId}/mutuelle-card    → { "Id" }
Saisie des 4 champs     ──► PATCH api/mutuelle-card/{Id}                → carte à jour
```

## 1. `GET api/missions/{jobId}/mutuelle-card` — y a-t-il déjà une carte ?

```jsonc
{
  "Id": "9b1c…",
  "BeneficiaryId": "2f4a…",
  "ContentType": "image/jpeg",
  "ByteSize": 412345,
  "CapturedAt": "2026-09-13T08:12:00",       // heure UTC
  "ImageUrl": "api/mutuelle-card/9b1c…/image",
  "MutuelleName": null,
  "AmcCode": null,
  "Concentrateur": null,
  "Teletransmission": null,
  "OcrStatus": "none"                         // "none" = photo sans saisie · "validated" = saisie faite
}
```

| Réponse | À en faire |
|---|---|
| `200` | Afficher la vignette (`ImageUrl`) et les quatre champs ; proposer « reprendre la photo » |
| `404` | **Cas normal** au premier transport du patient : proposer « photographier la carte ». Aussi renvoyé si la mission n'a pas de patient — même traitement |

## 2. `POST api/missions/{jobId}/mutuelle-card?crewId={crewId}` — la photo

- Corps **`multipart/form-data`**, champ **`file`** (même forme que les documents).
- `crewId` en query, facultatif : il trace qui a pris la photo. Envoie-le si tu l'as.
- **Image uniquement** (`image/*`), **8 Mo maximum**. Un JPEG redimensionné autour de 1600 px de
  large reste lisible et très en dessous.

| Réponse | Sens |
|---|---|
| `200` `{ "Id": "…" }` | Carte enregistrée — garde l'`Id` pour la saisie |
| `400` + message texte | Fichier manquant, pas une image, ou trop lourd |
| `404` | Mission introuvable ou sans patient |

⚠️ **Chaque photo crée une nouvelle carte**, la plus récente fait foi. Les champs saisis sur
l'ancienne **ne sont pas recopiés** : après une nouvelle photo, la carte courante a ses quatre champs
vides. Re-proposer la saisie, pré-remplie avec l'ancienne si tu veux épargner l'ambulancier.

## 3. `PATCH api/mutuelle-card/{Id}` — les quatre champs

```jsonc
{
  "MutuelleName": "Harmonie Mutuelle",
  "AmcCode": "98532001",
  "Concentrateur": "…",
  "Teletransmission": "…"
}
```

→ `200` avec la carte à jour (même forme qu'au §1, `OcrStatus: "validated"`) · `404` si l'`Id` est inconnu.

⚠️ **Le PATCH remplace les quatre champs** : un champ absent du corps repasse à `null`. Envoie
toujours les quatre, y compris ceux que l'ambulancier n'a pas touchés.

Tous les champs sont facultatifs : l'ambulancier saisit ce qu'il lit. Le **n° AMC** est le plus
utile à la facturation.

## Afficher l'image

`ImageUrl` est **relatif** : à composer avec la base de l'API. Envoie ton jeton `Bearer` comme sur
les autres appels, même si la route répond aujourd'hui sans : elle sera refermée.

## À ne pas utiliser

`api/beneficiaries/{beneficiaryId}/mutuelle-card` existe toujours, mais suppose un identifiant patient
que tu n'as pas. Reste sur les routes par mission.

---

## ✅ Récap

- [ ] À l'ouverture de la mission : `GET` → vignette + champs, ou bouton « photographier » sur 404
- [ ] Photo : `POST` multipart, champ `file`, `crewId` en query, image ≤ 8 Mo
- [ ] Saisie : `PATCH` sur l'`Id` renvoyé, **toujours les quatre champs**
- [ ] Après une nouvelle photo : re-proposer la saisie (la nouvelle carte part vide)
- [ ] Image : base de l'API + `ImageUrl`, avec le jeton
