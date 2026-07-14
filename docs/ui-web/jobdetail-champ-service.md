# Note UI Web — lieux Pickup / Dropoff : `DisplayLines` + champ `Service`

**Pour :** dev UI mobile · **Date :** 2026-07-14 · **Endpoint :** `GET /vector/api/JobDetail/{jobId}`
**Concerne :** les objets `PickupLocation` et `DropoffLocation` du détail mission.

---

## En un mot

Deux évolutions sur les lieux `PickupLocation` / `DropoffLocation` :

1. **`DisplayLines`** — un tableau de lignes **prêtes à afficher**, **identique** pour un
   établissement et un lieu non référencé. **Rends-le tel quel** → plus de logique champ-à-champ,
   **plus de cas « une seule ligne »**.
2. **`Service`** — le service médical (ex. « Cardiologie ») a désormais son **champ dédié** (avant :
   collé dans `BatEtage`). Il est déjà **inclus, au bon endroit**, dans `DisplayLines`.

## Le changement JSON

Chaque lieu est un objet en **PascalCase**.

**Avant** — service concaténé dans `BatEtage` :
```json
"PickupLocation": {
  "Nom":        "CHITS CH SAINTE MUSSE",
  "Adresse":    "54 R HENRI SAINTE CLAIRE DEVILLE",
  "Residence":  "",
  "BatEtage":   "Cardiologie CS 31412",   // ⟵ service + ligne 3 mélangés
  "Commune":    "83000 Toulon",
  "Complement": ""
}
```

**Maintenant** — champ `Service` dédié **+** `DisplayLines` :
```json
"PickupLocation": {
  "Nom":        "CHITS CH SAINTE MUSSE",
  "Service":    "Cardiologie",            // ⟵ NOUVEAU champ
  "Adresse":    "54 R HENRI SAINTE CLAIRE DEVILLE",
  "Residence":  "",
  "BatEtage":   "CS 31412",               // ⟵ ne contient plus que la ligne 3
  "Commune":    "83000 Toulon",
  "Complement": "",
  "DisplayLines": [                        // ⟵ NOUVEAU : lignes prêtes à afficher (le plus simple)
    "CHITS CH SAINTE MUSSE",
    "Cardiologie",
    "54 R HENRI SAINTE CLAIRE DEVILLE",
    "CS 31412",
    "83000 Toulon"
  ]
}
```

Pour un **lieu non référencé**, `DisplayLines` est déjà éclaté en lignes (le libellé figé n'est plus
une seule chaîne) :
```json
"DropoffLocation": {
  "Nom": "EHPAD LES TAMARIS - CHAM 38 RDC - La Valette-du-Var",   // champ brut (compat)
  "DisplayLines": [ "EHPAD LES TAMARIS", "CHAM 38 RDC", "La Valette-du-Var" ]
}
```

## À faire côté UI — le plus simple

**Rends `DisplayLines` ligne par ligne. C'est tout.**
```js
location.DisplayLines.forEach(line => afficher(line));
```
- Tableau **déjà ordonné et filtré** (aucun champ vide). **Identique** pour un établissement (plusieurs
  lignes) et un lieu non référencé (libellé déjà éclaté). **Aucun cas spécial, plus de « une seule ligne ».**
- `Service` y figure déjà, à sa place (après `Nom`).

> *Rendu champ-à-champ (optionnel, si tu veux styliser différemment chaque ligne)* : les champs
> individuels restent disponibles — ordre `Nom → Service → Adresse → Residence → BatEtage → Commune →
> Complement`, une ligne par champ non vide. Mais alors **c'est à toi** de gérer le cas « une seule
> ligne » — d'où la reco `DisplayLines`.

## Points d'attention

- `Service` est **souvent vide** (établissement de santé / lieu FreeText uniquement) → dans
  `DisplayLines` il est simplement **absent** quand il n'y a pas de service. Rien à gérer.
- **Lieu non référencé** (EHPAD, domicile…) : l'ERP ne fournit qu'un libellé — `DisplayLines` le
  **découpe en lignes** pour toi (rendu multi-lignes homogène). Plus de cas « une seule ligne ».
- ⚠️ **`BatEtage` ne contient plus le service.** Avec `DisplayLines`, c'est déjà géré. Si tu restes
  en **champ-à-champ**, pense à afficher `Service` (sinon l'info disparaît). Dans tous les cas, livrer
  la MAJ UI **en même temps** que le déploiement serveur.

## Rendu attendu (exemple)

```
CHITS CH SAINTE MUSSE
Cardiologie
54 R HENRI SAINTE CLAIRE DEVILLE
CS 31412
83000 Toulon
```

*(Aucun autre champ du détail mission ne change. Voir aussi `note_ui_alex.md` §4 pour le contexte complet du JobDetail.)*
