# Note UI Web — Ordre des statuts sur la timeline mission

**Endpoint concerné :** `GET /api/time/{gJobId}`
**DtoOut :** `ClJobTimeModel`
**Statut :** correctif rapide livré côté API (« Option B »). Action recommandée côté UI ci-dessous.

---

## Symptôme

Depuis le rebranchement de l'API, l'app affichait bien les 3 statuts horodatés
(**En route / Sur place / Disponible**) mais **dans le désordre** sur la timeline.

## Cause

Le DtoOut ne transporte **ni label ni champ d'ordre explicite** : ce sont 3 champs plats
d'heures brutes. L'UI reconstitue le libellé à partir du **nom de la propriété** et affiche
les jalons **dans l'ordre des clés du payload JSON**.

Or les propriétés étaient déclarées dans un ordre non chronologique
(`OnSiteTime`, `TerminatedTime`, `GoTime`), donc le JSON sortait :

```json
{ "OnSiteTime": "...",     // Sur place  (2)
  "TerminatedTime": "...", // Disponible (3)
  "GoTime": "..." }        // En route   (1)
```

→ timeline rendue **Sur place → Disponible → En route**.

## Correctif API livré (Option B)

Les propriétés du DtoOut sont désormais déclarées **et sérialisées dans l'ordre
chronologique réel**, verrouillé par `[JsonPropertyOrder]` :

```json
{ "GoTime": "...",           // 1 — En route
  "OnSiteTime": "...",       // 2 — Sur place
  "TerminatedTime": "..." }  // 3 — Disponible
```

Contrat inchangé par ailleurs : **PascalCase**, heures en **String UTC (suffixe `Z` / Zulu)**,
et **chaque champ peut être `null`/absent** tant que le jalon n'est pas franchi.

Mapping label ↔ champ :

| Ordre | Champ JSON        | Label UI     |
|-------|-------------------|--------------|
| 1     | `GoTime`          | En route     |
| 2     | `OnSiteTime`      | Sur place    |
| 3     | `TerminatedTime`  | Disponible   |

## Ce que l'UI Web doit faire

⚠️ **Ne dépendez pas de l'ordre des clés JSON pour ordonner l'affichage.** C'est fragile
(un sérialiseur, un proxy ou un `JSON.parse` réordonnant les clés casserait à nouveau la timeline).

Deux options, non exclusives :

1. **Mapping explicite (recommandé, court terme).** Câblez en dur l'ordre et les labels :
   `[GoTime → « En route », OnSiteTime → « Sur place », TerminatedTime → « Disponible »]`,
   puis n'affichez que les jalons dont l'heure est non nulle.

2. **Tri par valeur d'horaire.** Si vous préférez trier dynamiquement, triez les jalons
   présents par leur `DateTime` (après parsing UTC) plutôt que par position dans le JSON.

Le correctif Option B suffit à débloquer l'affichage **immédiatement** sans changement UI,
mais l'un des deux points ci-dessus rend l'UI robuste durablement.

## Option A — contrat riche (DISPONIBLE ✅, recommandé)

Un nouvel endpoint sert désormais une **liste ordonnée et labellisée** de jalons : l'UI n'a
plus rien à inférer (ni l'ordre, ni le libellé). C'est la cible pour la timeline Web.

**Endpoint :** `GET /api/time/{gJobId}/timeline`
**DtoOut :** `ClJobTimelineDtoOut`

```json
{
  "JobId": "…",
  "Statuses": [
    { "Order": 1, "Code": "EnRoute",    "Label": "En route",   "At": "2026-07-06T08:12:00Z" },
    { "Order": 2, "Code": "SurPlace",   "Label": "Sur place",  "At": "2026-07-06T08:31:00Z" },
    { "Order": 3, "Code": "Disponible", "Label": "Disponible", "At": null }
  ]
}
```

Garanties du contrat :

- **Les 3 jalons sont toujours présents et déjà triés** par `Order` (= ordre de la liste).
  Affichez dans l'ordre reçu, point.
- `At` = horodatage **UTC (suffixe `Z` / Zulu)** en String, ou **`null`** si le jalon n'est pas
  encore franchi → affichez l'étape « à venir » / en attente.
- `Code` est **stable et technique** (ne pas traduire, sert de clé) ; `Label` est le texte à afficher.
- **PascalCase** (comme le reste de l'API).

👉 Migrez la timeline Web vers cet endpoint : plus de mapping en dur ni de tri à faire côté UI.

## Compatibilité

L'ancien endpoint plat `GET /api/time/{gJobId}` (`ClJobTimeModel`) **reste servi** pour l'app
mobile et n'est pas modifié au-delà du correctif Option B ci-dessus. Aucune coordination de release
n'est nécessaire : chaque UI migre vers `/timeline` à son rythme.
