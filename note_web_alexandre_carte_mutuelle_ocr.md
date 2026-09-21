# 💳 Note UI Web Vector — Écran de validation de la carte mutuelle (pour Alexandre)

> **Date** : 2026-09-20 · **Pour** : Alexandre, dev web de l'UI Vector.
> **Objet** : la lecture automatique **propose** quatre champs ; il faut un écran pour les valider.
> **Additif** : rien ne change dans ce que l'app reçoit aujourd'hui — un bloc s'ajoute.

Salut Alexandre 👋

Côté API, la carte mutuelle peut désormais être **lue automatiquement** après la capture. Le résultat
n'est **jamais** écrit dans les champs de facturation : il arrive à part, et c'est un humain qui
décide.

## Ce qui s'ajoute dans la réponse

`GET api/missions/{missionId}/mutuelle-card` et `GET api/beneficiaries/{id}/mutuelle-card` rendent un
bloc de plus, `OcrProposal` — **absent** tant qu'aucune lecture n'a rien donné :

```jsonc
{
  "Id": "…", "ImageUrl": "api/mutuelle-card/…/image",
  "MutuelleName": null, "AmcCode": null,          // les champs OFFICIELS (saisie humaine)
  "Concentrateur": null, "Teletransmission": null,
  "OcrStatus": "extracted",                        // none | pending | extracted | validated | error
  "OcrProposal": {                                 // ce que le modèle PROPOSE
    "MutuelleName": "Harmonie Mutuelle",
    "AmcCode": "12345678",
    "Concentrateur": null,
    "Teletransmission": "0123456789",
    "Confidence": 0.92,
    "ExtractedAt": "2026-09-20T10:14:00Z"
  }
}
```

## Ce qu'il faut afficher

- **Les deux colonnes côte à côte** : à gauche ce qui est enregistré (les champs officiels), à droite
  ce que la lecture propose. Un bouton « reprendre cette valeur » par champ, ou « tout reprendre ».
- **La confiance**, telle quelle. On ne valide pas une lecture à 0,4 comme une à 0,95 : si l'écran ne
  la montre pas, l'opérateur validera les deux du même geste.
- **L'image à côté** (`ImageUrl`) : c'est elle qui tranche, pas le texte proposé.

## Ce qui enregistre

Rien de neuf : le `PATCH api/mutuelle-card/{cardId}` existant, avec les valeurs **retenues par
l'opérateur**. Il passe la carte en `validated`. ⚠️ Rappel de son contrat actuel : il **remplace les
quatre champs** — un champ absent du corps repart à vide. Envoyez les quatre.

## Les statuts, et ce qu'ils veulent dire

| `OcrStatus` | Sens | Ce que l'écran en fait |
|---|---|---|
| `pending` | en file, pas encore lue | rien à proposer, saisie manuelle comme avant |
| `extracted` | lue — `OcrProposal` peut être **absent** si rien n'a été lu | proposer, ou laisser saisir |
| `validated` | un humain a tranché | afficher les champs officiels |
| `error` | lecture impossible après plusieurs tentatives | saisie manuelle, sans message d'alarme |

## ✅ Récap

- [ ] Afficher `OcrProposal` à côté des champs officiels, avec la confiance et l'image
- [ ] Un geste pour reprendre une valeur proposée, un autre pour toutes les reprendre
- [ ] Enregistrer par le `PATCH` existant, **les quatre champs à chaque fois**
- [ ] Ne rien casser quand `OcrProposal` est absent : c'est le cas aujourd'hui en production
