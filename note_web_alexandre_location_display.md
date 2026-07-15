# 🖥️ Note UI Web Vector — Affichage pickup/dropoff piloté serveur (DET-2, pour Alexandre)

> **Date** : 2026-07-15
> **Pour** : Alexandre, **dev web de l'UI Vector**.
> **Objet** : sur le **détail mission**, deux nouveaux champs **`PickupDisplay` / `DropoffDisplay`** décrivent
> **quoi afficher et comment** pour les lieux. L'UI devient un **moteur de rendu générique** : tu boucles sur
> la structure et tu affiches — plus de logique de mise en forme des lieux côté front.
> **Additif** : les anciens champs `PickupLocation` / `DropoffLocation` (typés) **restent** ; tu bascules quand tu veux.
> **JSON** : PascalCase (comme le reste du contrat mobile).

Salut Alexandre 👋

Le back compose désormais l'affichage des lieux sous forme de **sections de lignes**. Tu rends la structure telle quelle ;
**changer l'affichage (ordre, gras, couleur, quels champs) se fait côté serveur, sans toucher ton code.**

---

## 1. La structure

```jsonc
// détail mission → PickupDisplay (idem DropoffDisplay)
"PickupDisplay": {
  "Blocks": [                                   // tableau de SECTIONS
    [                                           // section 1 : identité
      { "Index": 1, "Label": null, "Value": "CHITS CH SAINTE MUSSE", "IsBold": true,  "Color": null },
      { "Index": 2, "Label": "Service", "Value": "CARDIOLOGIE",       "IsBold": false, "Color": null }
    ],
    [                                           // section 2 : adresse
      { "Index": 1, "Label": null, "Value": "54 R HENRI SAINTE CLAIRE DEVILLE", "IsBold": false, "Color": null },
      { "Index": 2, "Label": null, "Value": "CS 31412",                          "IsBold": false, "Color": null },
      { "Index": 3, "Label": null, "Value": "83000 Toulon",                      "IsBold": false, "Color": null }
    ]
  ],
  "Coordinates": { "Latitude": 43.1242, "Longitude": 5.9280 }   // ou null si non géocodé
}
```

- **`Blocks`** = un tableau de **sections** ; chaque section = un tableau de **lignes**.
- **`Coordinates`** = sous-objet **à part** (pour la carto), `null` si le lieu n'est pas géocodé → n'affiche pas de carte / pas d'itinéraire.

## 2. Une ligne (`LocationLine`)

| Champ | Rendu |
|---|---|
| `Value` | le texte à afficher (toujours présent) |
| `Label` | libellé optionnel devant la valeur (ex. « Service ») ; `null` → valeur seule |
| `IsBold` | mettre la ligne en gras |
| `Color` | couleur du texte en hexa `#RRGGBB` ; **`null` = ta couleur par défaut** (ne force rien) |
| `Index` | ordre de la ligne dans sa section (1-based) |

## 3. Comment rendre (pseudo-code)

```
pour chaque section de PickupDisplay.Blocks :
    ouvrir un bloc (paragraphe / groupe visuel)
    pour chaque ligne de la section (triée par Index) :
        texte = (Label != null ? Label + " : " : "") + Value
        style = { fontWeight: IsBold ? bold : normal, color: Color ?? défautThème }
        afficher(texte, style)
si PickupDisplay.Coordinates != null :
    activer la carte / le bouton itinéraire avec Latitude/Longitude
```

- **N'ajoute aucune règle métier** (pas de « si service alors… », pas de tri de champs) : tout est déjà décidé côté serveur.
- **Sections vides / lignes vides** : le serveur ne les envoie **pas** → tu n'as pas à filtrer.
- **`Color` par défaut `null`** : en v1 le serveur ne colore rien ; garde ta couleur de thème. (Le back pourra colorer ponctuellement plus tard sans que tu changes ton code.)

## 4. Migration
- **Aujourd'hui** : `PickupLocation` / `DropoffLocation` (champs typés Nom/Service/Adresse/…) restent servis en parallèle.
- **Cible** : tu passes tes vues lieux sur `PickupDisplay` / `DropoffDisplay` (rendu générique) ; on retirera les anciens champs une fois la bascule faite.

## 5. ⚡ TL;DR
- Détail mission : **`PickupDisplay` / `DropoffDisplay`** = `{ Blocks: LocationLine[][], Coordinates: {Latitude,Longitude}|null }`.
- Boucle sections → lignes ; applique `IsBold` / `Color` (`null` = défaut) ; `Label` optionnel.
- `Coordinates` séparé (carto), `null` = pas géocodé.
- **Aucune logique de mise en forme côté UI** — le serveur décide. Additif, non cassant.

Ping-moi si tu veux un exemple de rendu HTML/CSS de référence ou un cas dropoff complet. 🚀
