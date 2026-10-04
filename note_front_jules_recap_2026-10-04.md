# 📋 Récap pour Jules — ce que le travail du 04/10 attend du front

> **Date** : 2026-10-04 · **Pour** : Jules (app terrain Vector, reprise d'Alexandre, et écrans de
> régulation et de certification).
> **Objet** : pour chaque livraison du jour, ce qu'elle demande au front. Le détail — payloads,
> exemples, demandes plus anciennes à vérifier — est dans [`note_front_jules_vector.md`](note_front_jules_vector.md).

**En production** : `9c15f94`. La validation de la saisie de la carte (point 4) n'est **pas encore
publiée** : elle partira à la prochaine publication.

Deux livraisons du jour ne demandent rien au front : la lecture par lot pour la facturation et le
passage des derniers cas d'usage en asynchrone.

---

## 1. Un `503` quand la régulation ne répond pas — *en production*

- **Afficher `detail`**, au moins sur l'écran d'entrée `GET api/Crew/mine` : le 22/09, l'ambulancier
  y a reçu ~1 600 erreurs sans message.
- En option : un bouton « Réessayer », ou une relance après `Retry-After` (10 s).

## 2. Une panne n'est plus un `400` — *en production*

- **Timeline, signature, liste, conducteur** répondent `503` au lieu de `400` pendant une panne :
  traiter le `503` sur ces routes (point 1), et lire un `400` comme un **vrai refus**, motif à afficher.
- **Retirer l'appel à `api/Kilometers`** s'il existe : la route est retirée et répond `404`, comme avant.
- Deux `400` de la signature sont reformulés, même code : « Aucune signature à modifier pour cette
  mission », « Identifiant de mission vide ».

## 3. La carte mutuelle — *en production*

**App terrain — détail de la mission (`GET api/JobDetail/{jobId}`)**
- « Carte connue, photo du JJ/MM » à partir de `MutuelleCardKnown` et `MutuelleCardCapturedAt`.
- Bouton **« Voir la carte »** : `MutuelleCardImageUrl` chargée par `fetch` avec le jeton, affichée
  depuis un blob — une `<img src>` nue reçoit `401`.
- « Repris de la photo du JJ/MM, à revérifier » quand `MutuelleCardFieldsInheritedFrom` est renseigné.

**Écrans de régulation et de certification**
- 🔴 **Bloquant : me donner le nom du client Keycloak (`azp`) de chacun des deux écrans.** Sans lui,
  ces routes répondent `401`.
- `POST api/missions/mutuelle-card/presence` une fois par page (200 missions au plus) : entrée
  « Carte mutuelle » grisée si `HasCard = false`, date de la photo affichée.
- Au clic : `GET api/missions/{id}/mutuelle-card` pour les données, la photo par `fetch` + blob.
- **Me dire quand tu ne dépends plus des deux routes sans jeton**, pour qu'on les ferme.

## 4. La saisie des champs de la carte — *pas encore publiée*

- `PATCH api/mutuelle-card/{Id}` : afficher le motif d'un `400` — un envoi entièrement vide est refusé.
- Borner les zones de saisie : mutuelle **200**, code AMC **50**, concentrateur **100**,
  télétransmission **50** caractères.

---

## Dans quel ordre

1. **Le client Keycloak des deux écrans** — c'est le seul point qui te bloque.
2. **Le `503` sur l'écran d'entrée.**
3. **La carte** — dans l'app, puis dans tes écrans.

## ✅ À cocher

- [ ] Me donner l'`azp` de l'écran de régulation et de l'écran de certification
- [ ] `503` : afficher `detail` (au moins sur `Crew/mine`) ; « Réessayer » en option
- [ ] Un `400` est un refus : afficher son motif (timeline, signature, liste, conducteur)
- [ ] Retirer l'appel à `api/Kilometers`
- [ ] Détail : carte connue, date, « Voir la carte » par `fetch` + blob, « repris de la photo du … »
- [ ] Écrans : `presence` par page, menu grisé sans carte, données et photo au clic
- [ ] Me dire quand les deux routes sans jeton ne te servent plus
- [ ] Saisie de la carte : motif du `400`, longueurs bornées
