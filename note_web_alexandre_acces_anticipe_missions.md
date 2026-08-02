# 🖥️ Note UI Web Vector — Missions visibles 30 min avant la prise de service (CREW-1, pour Alexandre)

> **Date** : 2026-08-02
> **Pour** : Alexandre, **dev web de l'UI Vector**.
> **Objet** : `GET /api/crew/mine` peut désormais renvoyer un équipage **dont la vacation n'a pas encore
> commencé** (jusqu'à **30 min avant**), pour que l'ambulancier consulte ses missions avant de démarrer.
> Un nouveau flag **`IsPending`** te dit lequel est dans ce cas.
> **Additif, non cassant** : aucun champ retiré ni renommé, rien à changer sur la joblist.
> **JSON** : PascalCase (comme le reste du contrat mobile).

Salut Alexandre 👋

Jusqu'ici, avant l'heure de prise de service, `GET /api/crew/mine` répondait **404** → l'app ne pouvait épingler
aucun équipage → aucune mission visible. Maintenant l'équipage remonte **dès 30 min avant**. C'est le seul
changement : la suite du parcours (joblist, détail mission…) est inchangée.

---

## 1. Ce qui change dans la réponse

```jsonc
// GET /api/crew/mine  —  appelé à 13:40, prise de service à 14:00
{
  "RequiresSelection": false,
  "RecommendedCrewId": "3f2a…",
  "Crews": [
    {
      "CrewId": "3f2a…",
      "DisplayLabel": "AB-123-CD · DUPONT Jean / MARTIN Paul",
      "VehicleImmat": "AB-123-CD",
      "Members": "DUPONT Jean / MARTIN Paul",
      "ServiceWindow": "14:00 – 22:00",
      "IsCurrent": false,        // ⬅️ la vacation ne couvre pas encore « maintenant »
      "IsPending": true,         // ⬅️ NOUVEAU : prise de service pas encore atteinte
      "IsClosed": false
    }
  ]
}
```

**`IsPending`** (nouveau, booléen) = la prise de service **n'a pas encore eu lieu**, l'équipage est proposé en avance.

## 2. Les trois états d'un équipage

| `IsPending` | `IsCurrent` | `IsClosed` | Sens | Rendu suggéré |
|---|---|---|---|---|
| `true` | `false` | `false` | **À venir** (dans ≤ 30 min) | badge « prise de service à HH:mm » |
| `false` | `true` | `false` | **En service** | rendu normal |
| `false` | `false` | `true` | **Terminé** | lecture seule (comme aujourd'hui) |

L'heure du badge est déjà dans **`ServiceWindow`** (`"14:00 – 22:00"` → tu prends la partie gauche). Rien à calculer,
et surtout **ne compare aucune heure côté front** : le serveur décide, comme d'habitude.

## 3. Ce que tu as à faire

1. **Le minimum** : rien. L'écran marche déjà, l'équipage à venir s'affiche comme un équipage normal.
2. **Le confort** (recommandé) : afficher le badge « prise de service à HH:mm » quand `IsPending = true`, pour que
   l'ambulancier comprenne qu'il consulte ses missions **en avance** et qu'il n'est pas encore en service.

## 4. Un effet de bord à connaître

En **double vacation** (A finit à 14:00, B démarre à 14:00), les deux équipages remontent dès **13:30** →
`RequiresSelection` passe à `true` **30 min plus tôt** qu'avant, donc ton écran de choix s'ouvre plus tôt.
C'est voulu. `RecommendedCrewId` reste juste : c'est l'équipage **en cours** (A) qui est pré-coché, pas celui à venir.

## 5. ⚡ TL;DR

- `GET /api/crew/mine` répond maintenant **30 min avant** la prise de service (avant : 404).
- Nouveau flag **`IsPending`** sur chaque crew ; `IsCurrent` est alors `false`, `IsClosed` aussi.
- Heure de prise de service = partie gauche de **`ServiceWindow`** (déjà formatée).
- Écran de choix possiblement ouvert 30 min plus tôt en double vacation ; la pré-sélection reste correcte.
- **Additif et non cassant** : rien à migrer, la joblist ne bouge pas.

Ping-moi si tu veux que je te renvoie un exemple de réponse complète avec deux équipages (un en cours + un à venir). 🚀
