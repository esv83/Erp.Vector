# Note UI Web — nouveau champ `Service` (Pickup / Dropoff)

**Pour :** dev UI mobile · **Date :** 2026-07-14 · **Endpoint :** `GET /vector/api/JobDetail/{jobId}`
**Concerne :** les objets `PickupLocation` et `DropoffLocation` du détail mission.

---

## En un mot

Les lieux `PickupLocation` / `DropoffLocation` ont un **nouveau champ `Service`** (le service
médical, ex. « Cardiologie »). Avant, ce service était **collé dans `BatEtage`** ; il a maintenant
son **champ dédié**. → **Affiche une ligne `Service` juste sous `Nom`.**

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

**Maintenant** — champ dédié :
```json
"PickupLocation": {
  "Nom":        "CHITS CH SAINTE MUSSE",
  "Service":    "Cardiologie",            // ⟵ NOUVEAU champ
  "Adresse":    "54 R HENRI SAINTE CLAIRE DEVILLE",
  "Residence":  "",
  "BatEtage":   "CS 31412",               // ⟵ ne contient plus que la ligne 3
  "Commune":    "83000 Toulon",
  "Complement": ""
}
```

## À faire côté UI

1. **Rendre le champ `Service`** sur sa propre ligne, **juste sous `Nom`**.
2. **Ordre d'affichage** (une ligne par champ **non vide**) :
   `Nom → Service → Adresse → Residence → BatEtage → Commune → Complement`
3. Même règle que les autres champs : **si `Service` est vide/absent → ne pas afficher la ligne.**

## Points d'attention

- `Service` est **souvent vide** : il n'existe que pour un **établissement de santé** ou un
  **lieu libre (FreeText)**. Pour un domicile / une adresse bénéficiaire, il vaut `""` → pas de
  ligne. C'est normal.
- **Lieu non référencé** (EHPAD, domicile…) : comme avant, seul `Nom` est rempli, tout le reste
  (dont `Service`) est vide → une seule ligne à l'écran.
- ⚠️ **`BatEtage` ne contient plus le service.** Si tu n'affiches pas la nouvelle ligne `Service`,
  l'information **disparaît** de l'écran. → livrer cette MAJ UI **en même temps** que le
  déploiement serveur.

## Rendu attendu (exemple)

```
CHITS CH SAINTE MUSSE
Cardiologie
54 R HENRI SAINTE CLAIRE DEVILLE
CS 31412
83000 Toulon
```

*(Aucun autre champ du détail mission ne change. Voir aussi `note_ui_alex.md` §4 pour le contexte complet du JobDetail.)*
