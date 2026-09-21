# CLAUDE.md — Erp.Vector

Module de l'ERP DELESSE. **Les invariants du `CLAUDE.md` global s'appliquent
intégralement** ; ce fichier ne porte que ce qui est propre à ce dépôt.

## LES DOCUMENTS DU DÉPÔT — LEQUEL DIT QUOI

Trois fichiers à la racine, et ce qui les sépare n'est pas le sujet mais le **régime
d'écriture** : le premier se régénère, les autres s'ajoutent. *Un plan qui porte aussi son
passé cesse d'être relu.*

| Fichier | Contenu | Régime |
|---|---|---|
| `DEVPLAN.md` | ce qui **reste** — en attente, bloqué, envisagé, décidé et pas fait | **réécrit de zéro** à chaque *« compact devplan »*, sur un relevé de l'état réel |
| `decided.md` | **pourquoi** c'est ainsi : décisions tranchées, pièges déjà payés | **ajouté**, jamais réécrit |
| `delivered.md` | ce qui est **fait**, horodaté et **constaté** plutôt qu'annoncé — jamais au commit | **ajouté**, jamais réécrit |

🔴 **`decided.md` se lit AVANT d'écrire du code dans ce dépôt.** Il n'est pas là pour la
postérité : chacune de ses lignes a coûté quelque chose à quelqu'un, et plusieurs ont coûté
une livraison.

**Au prompt « compact devplan »**, dans cet ordre : verser le livré **constaté** dans
`delivered.md`, descendre dans `decided.md` ce que l'édition a tranché ou appris, **puis**
régénérer le plan sur les seuls sujets ouverts. Ce que l'édition a changé dans le plan
lui-même va dans le **message du commit**.

**Dans une référence croisée, nommer la rubrique, jamais la numéroter** — un numéro ne
survit pas à une réorganisation. Le 21/09/2026, celle qui a créé `decided.md` a fait
glisser tous ceux de ce dépôt.
