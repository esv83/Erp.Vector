# 🖥️ Note UI Web Vector — Context de la mission : les DTO à traiter (pour Alexandre)

> **Date** : 2026-08-25 · **Pour** : Alexandre, dev web de l'UI Vector.
> **Objet** : la bascule annoncée dans `note_web_alexandre_vector_type_mission.md` est **en service
> depuis aujourd'hui**. Voici les cinq DTO concernés, tels qu'ils répondent réellement en production.
> **JSON** : PascalCase, comme le reste du contrat mobile.
>
> ⚠️ **On a retenu l'option « tableau + endpoint `state` séparé »**, pas le passage à un objet évoqué
> en juillet. `GET api/Contract/{jobId}` **garde donc sa forme de tableau** : rien à migrer de ce
> côté-là.

Salut Alexandre 👋

Un point avant les DTO, parce que c'est probablement ce qui casse ton écran en ce moment.

## ⚠️ Il n'y a plus de sélection par défaut

Avant, quand personne n'avait choisi, l'API cochait **le premier type de la liste**. Il y avait donc
toujours un `IsSelected: true` quelque part. C'était un faux choix — il partait en facturation comme
un renseignement validé — et il a été supprimé.

**Aujourd'hui, une mission non qualifiée renvoie 7 entrées dont aucune n'est sélectionnée.** Si ton
code fait `list.find(c => c.IsSelected).Id`, il reçoit `undefined` et le composant ne se rend pas.
C'est le correctif n°1 : traiter « aucune sélection » comme l'état normal de départ, et afficher une
ligne vide « — non renseigné — ».

⚠️ Deuxième piège : **les `Id` ont changé d'espace**. Ils viennent maintenant du catalogue de la
régulation. L'`Id` `4` valait « Article 80 », il vaut aujourd'hui « Centre 15 ». Si tu as des ids en
dur, ils affichent faux **sans erreur**. Affiche le `Display` reçu, renvoie l'`Id` reçu, ne fabrique
jamais l'un à partir de l'autre.

---

## 1. `GET api/Contract/{jobId}` — la liste

Réponse réelle, mission non qualifiée (les 7 entrées, aucune sélectionnée) :

```jsonc
[
  { "Id": 1, "Display": "CPAM",               "IsSelected": false, "Locked": false },
  { "Id": 2, "Display": "Article 80",         "IsSelected": false, "Locked": false },
  { "Id": 3, "Display": "Assistance",         "IsSelected": false, "Locked": false },
  { "Id": 4, "Display": "Centre 15",          "IsSelected": false, "Locked": false },
  { "Id": 5, "Display": "Secours sur piste",  "IsSelected": false, "Locked": false },
  { "Id": 6, "Display": "Télé-Alarme",        "IsSelected": false, "Locked": false },
  { "Id": 7, "Display": "Non pris en charge", "IsSelected": false, "Locked": false }
]
```

Et sur une mission que la régulation a qualifiée : `{ "Id": 4, "Display": "Centre 15", "IsSelected": true, "Locked": false }`.

| Champ | À en faire |
|---|---|
| `Id` | valeur de l'option, et **seule** chose à renvoyer au `POST` |
| `Display` | libellé affiché |
| `IsSelected` | l'entrée en vigueur. **Peut n'être vraie sur aucune entrée** |
| `Locked` | ⭐ **NOUVEAU** — `true` ⇒ sélecteur en lecture seule + cadenas. Même valeur sur toutes les entrées (le verrou porte sur la mission, pas sur le type) |

**Ne re-trie pas, ne re-filtre pas** : la liste arrive triée et déjà restreinte à l'agence et au mode
de transport de la commande.

## 2. `GET api/Contract/{jobId}/state` — l'état, avec la provenance *(facultatif)*

```jsonc
{
  "MissionId": "f5c66d3a-48cb-45a2-a34d-254cc796073f",
  "Locked": false,
  "ContextOrderId": null,       // null = non renseigné
  "ContextOrderCode": null,     // code technique, ne pas afficher
  "ContextOrderDisplay": null,  // libellé, celui qu'on affiche
  "Origin": null                // "Regulator" | "Field" | null
}
```

`Locked` seul ne porte que deux situations sur quatre. `Origin` donne les deux autres :

| `Origin` | `Locked` | Ce que voit l'ambulancier |
|---|---|---|
| `null` | `false` | rien de proposé, il choisit librement |
| `"Regulator"` | `false` | **valeur poussée par la régulation, qu'il peut changer** — affiche « proposé par la régulation », laisse la main |
| `"Regulator"` | `true` | imposée, lecture seule + cadenas |
| `"Field"` | `false` | son propre choix, déjà enregistré |

La 2ᵉ ligne est le cas qui justifie cet appel : sans elle, une valeur pré-cochée passe pour un défaut
technique et se fait écraser sans y penser. Tu peux t'en passer au premier jet — `Locked` de la liste
suffit pour les cas 1, 3 et 4.

## 3. `POST api/Contract/{jobId}` — le choix de l'ambulancier

Corps **inchangé** : l'entier seul.

```json
4
```

⚠️ **Cet appel réussissait toujours. Ce n'est plus vrai.**

| Code | Quand | Réaction UI |
|---|---|---|
| `200` | accepté | rafraîchis, **puis recharge `FormStructure`** (§4) |
| `409` | verrouillé par la régulation | message + re-`GET` pour resynchroniser (`Locked` devrait être `true`) |
| `400` | type non applicable à cette commande | message + re-`GET`. Ne devrait pas arriver si tu n'envoies que des `Id` reçus |
| `404` | mission inconnue | message, retour à la liste |

Dans **tous** les cas de refus, rien n'a été enregistré : ne laisse pas l'écran afficher le nouveau choix.

## 4. `GET api/FormStructure/{jobId}` — le questionnaire

Réponse réelle (valeur masquée ici) :

```jsonc
[
  { "Name": "DDN", "Label": "Date de naissance", "Index": 1, "Type": "date",
    "Required": false, "InstantUpdate": false, "PlaceHolder": null,
    "IsMulti": false, "Options": null, "Value": "1954-03-02",
    "IsReadOnly": true,
    "ReadOnlyReason": "Date de naissance verrouillée : elle est déjà renseignée sur la fiche bénéficiaire, et c'est là qu'elle se corrige." },

  { "Name": "NIR", "Label": "N° de sécurité sociale", "Index": 2, "Type": "text",
    "Required": false, "InstantUpdate": false, "PlaceHolder": null,
    "IsMulti": false, "Options": null, "Value": null,
    "IsReadOnly": false, "ReadOnlyReason": null }
]
```

**Deux champs s'ajoutent, tout le reste est identique à ce que tu parses déjà :**

| Champ | À en faire |
|---|---|
| `IsReadOnly` | ⭐ **NOUVEAU** — affiche la valeur, **désactive la saisie**. Verrou **par champ** |
| `ReadOnlyReason` | ⭐ **NOUVEAU** — motif affichable (info-bulle ou texte d'aide). `null` quand le champ est ouvert |

✅ **`Type` et `Options` ne changent pas de forme.** Le vocabulaire des contrôles est le même
(`text`, `textarea`, `checkbox`, `list`, `phone`, `email`, `number`, `date`), et `Options` reste un
objet `{ "0": "Non", "1": "Oui" }` sur les champs `list`. Ton rendu par type n'a rien à migrer.

⚠️ En revanche **le catalogue d'attributs n'est plus le même** : il vient désormais de la régulation.
Relevé sur 25 missions en production, il ne contient aujourd'hui que `BT` (checkbox), `DDN` (date),
`NIR`, `NOM_ASSISTANCE`, `NUM_CENTAURE`, `NUM_DOSSIER` (text). Les champs multi-valués (téléphones,
e-mails), les zones de texte long et les listes de l'ancien catalogue Vector **n'y figurent pas**.
Si des contrôles ont disparu de ton écran, c'est d'abord parce que ces champs n'existent plus — pas
parce qu'ils sont mal rendus.

🔑 **`IsReadOnly` ≠ `Locked`.** `Locked` gèle **le choix du type**, `IsReadOnly` gèle **un champ**.
Une mission au type imposé garde son questionnaire éditable, et une mission au type libre peut porter
une date de naissance verrouillée.

⚠️ **Le jeu de champs dépend du type.** Après chaque `POST` réussi au §3, **re-`GET` ce formulaire**.
Et `[]` est une réponse valide : mission sans type ⇒ aucun champ. Ce n'est pas une erreur.

## 5. `PATCH api/JobEdit/{jobId}` — la saisie

Corps **inchangé** :

```json
[ { "AttributName": "NIR", "AttributValue": "1540375116001" } ]
```

**Écriture tout ou rien** : une seule valeur invalide fait échouer le lot, **rien** n'est enregistré.

| Code | Quand | Réaction UI |
|---|---|---|
| `200` | enregistré — renvoie l'écho de ce que tu as posté | rafraîchis |
| `409` | tu **modifies** un champ verrouillé (DDN/NIR connus, PMT/BT scellés) | message + re-`GET` du formulaire |
| `400` | valeur invalide (clé du NIR fausse, date future…) | message « rien n'a été enregistré », garde la saisie à l'écran |
| `404` | mission inconnue | message |

✅ **Tu peux renvoyer le formulaire entier sans trier les champs verrouillés** : reposer une valeur
inchangée est sans effet, même verrouillée. Seule une vraie *modification* est refusée. Aucune
logique de tri à écrire.

---

## Trois comportements métier à connaître

- **DDN / NIR** — pré-remplis et verrouillés dès que la fiche bénéficiaire les connaît. Fiche vide ⇒
  ouverts, et la saisie de l'ambulancier alimente la fiche.
- ⚠️ **Le NIR ne se corrige dans aucun module une fois posé.** Fais-le relire, ou demande une
  confirmation avant validation. Une faute de frappe est définitive et part en facturation.
- **PMT / BT** — ces cases valent pour la commande, donc pour l'aller **et** le retour : ce que
  l'aller coche, le retour le voit. Cochée = scellée (409 si on décoche). Décochée = encore ouverte,
  c'est normal, le document est souvent remis au retour.

## Fraîcheur

Aucune notification : la régulation peut requalifier une mission à tout moment, tu ne l'apprends
qu'en relisant. Relis au minimum à l'ouverture du détail mission, au retour sur l'écran, et après
tout refus.

---

## ⚡ TL;DR

1. **`IsSelected` peut n'être vrai nulle part** → ligne vide « non renseigné », ne présélectionne pas
   le premier. *C'est ce qui bloque ton écran aujourd'hui.*
2. **Les `Id` ont changé de signification** → aucun id en dur, affiche le `Display` reçu.
3. **`Locked`** sur la liste → sélecteur grisé + cadenas. **`Origin`** via `/state` → « proposé par la
   régulation » (facultatif).
4. **`POST api/Contract` peut rendre 409 / 400 / 404** là où il réussissait toujours. Après un 200,
   **recharge `FormStructure`**.
5. **`IsReadOnly` / `ReadOnlyReason`** sur chaque champ → affiche, désactive, explique. À ne pas
   confondre avec `Locked`.
6. **`PATCH api/JobEdit` est tout ou rien** et peut rendre 409 / 400. Renvoie le formulaire entier
   sans trier.

Ping-moi si tu veux une capture complète d'une mission qualifiée et d'une non qualifiée, ou qu'on
cale ensemble le rendu des quatre situations du §2. 🚀
