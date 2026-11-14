# Note pour Jules — saisie sur le patient et option « Autre » dans l'API Vector

*14/11/2026 — Vector. Tout est **additif** : rien ne change pour un client qui ignore ces champs.
Disponible à la prochaine publication de Vector.Api (je te préviens quand `/version` l'a montrée).*

## 1. Option « Autre » (J-154)

`GET api/FormStructure/{jobId}` : deux champs nouveaux par champ, `Options` (objet `{clé: libellé}`) ne bouge pas.

| Champ | Type | |
|---|---|---|
| `OptionItems` | `[{ Key, Label, TextRequired }]` ou `null` | Mêmes options qu'`Options`, dans l'ordre d'Order. `TextRequired = true` ⇒ « Autre » : afficher une zone de texte |
| `OtherText` | string ou `null` | Texte libre de l'« Autre » en place |

`PATCH api/JobEdit/{jobId}` : chaque élément accepte `OtherText` à côté d'`AttributName` / `AttributValue`
(toujours un **tableau**, comme avant). Relayé tel quel à Order.

🪤 **Renvoie l'`OtherText` reçu pour chaque champ, même inchangé.** Order enregistre le texte qu'on lui
envoie : un champ « Autre » renvoyé sans `OtherText` efface le texte terrain déjà saisi.

## 2. Contact du patient

```
PUT api/missions/{id}/beneficiary/contact
{ "Phone": "06 12 34 56 78", "Email": "paul.martin@exemple.fr" }
```

L'un, l'autre ou les deux. `setBy` est tiré du jeton, ne l'envoie pas.

## 3. Identité du patient anonyme (J-144)

```
PUT api/missions/{id}/beneficiary/identity
{ "LastName": "Martin", "FirstName": "Paul", "BirthDate": "1965-03-02", "Gender": 1 }
```

`LastName` obligatoire ; `BirthDate` au format `AAAA-MM-JJ`, facultative ; `Gender` : 0 inconnu, 1 homme, 2 femme.

## Réponses des deux routes

Le corps d'un refus est **le motif d'Order en texte simple**, à afficher tel quel (comme `JobEdit`).

| Statut | Quand | Que faire |
|---|---|---|
| `204` | Enregistré (contact : ou rien à enregistrer) | — |
| `400` | Valeur mal formée, rien n'est écrit | Afficher le motif, laisser corriger |
| `401` | Jeton absent, ou sans identifiant d'ambulancier | Reconnexion |
| `403` | Hors de l'équipage affecté, ambulancier inconnu d'Order, mission de plus de deux jours | **Définitif**, ne pas rejouer |
| `404` | Mission ou patient introuvable ; identité : commande qui n'est pas née anonyme | Définitif |
| `409` | Identité seulement : la régulation l'a déjà établie | Afficher, relire le détail, masquer le formulaire |
| `503` | Order injoignable (ProblemDetails, `detail` affichable) | Rejouer plus tard |

## 4. `BeneficiaryIdentification` dans `JobDetail` (J-127, J-129, J-143)

`null` pour une commande ordinaire. Sinon :

| Champ | Type | |
|---|---|---|
| `State` | int | 1 anonyme · 2 à vérifier (saisie de l'équipage en attente) · 3 identifié |
| `Description` | string ou `null` | Signalement de la régulation ; `null` ⇒ afficher seulement « Patient anonyme » |
| `IdentityEditableByField` | bool | Faux dès que la régulation a tranché ⇒ masquer le formulaire d'identité |

Relu chez Order à chaque appel de `JobDetail` : rappelle-le à chaque ouverture de la mission, ne garde pas
« Anonyme » en mémoire (la régulation peut corriger l'identité à tout moment).

Formulaire d'identité affiché ⇔ `BeneficiaryIdentification` non nul **et** `IdentityEditableByField`
**et** `CanEditBeneficiary`.

## 5. `CanEditBeneficiary` dans `JobDetail`

Booléen : faux quand la date de la mission a plus de deux jours. Même calcul qu'Order : sert à **masquer**
les formulaires d'identité et de contact. Le 403 d'Order reste la référence.
