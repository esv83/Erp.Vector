# Erp.Vector — ce qui est décidé

> **Ce document s'ajoute, il ne se régénère pas.** C'est sa différence avec `DEVPLAN.md`,
> réécrit de zéro à chaque *« compact devplan »* : une décision tranchée et un piège payé
> n'ont aucune raison d'être repassés sous une plume tous les deux jours.

| Document | Contenu | Régime |
|---|---|---|
| `DEVPLAN.md` | ce qui **reste** — en attente, bloqué, envisagé, décidé et pas fait | **régénéré** |
| `decided.md` *(ici)* | **pourquoi** c'est ainsi, et ce qu'on a déjà payé pour l'apprendre | **ajouté** |
| `delivered.md` | ce qui est **fait**, horodaté et **constaté** plutôt qu'annoncé | **ajouté** |

🔴 **À lire avant d'écrire du code dans ce dépôt.** Ce n'est pas de la documentation
d'ambiance : c'est ce qui évite de rejouer un arbitrage déjà rendu et de repayer une erreur
déjà payée.

**Mis à jour au prompt « compact devplan »** : ce qu'une édition a tranché ou appris
descend ici *avant* que le plan ne soit réécrit.

---

# 4. Décisions tranchées — ne pas les rejouer

*Les décisions appliquées vivent dans [`delivered.md`](delivered.md) §4 — **celles du 19/09 au §4.3** :
fermer sur une mesure, lots par liste d'identifiants, pas plus de 8 appels vers Orders, le paquet
partagé sans toucher la configuration, publier depuis `main` seulement. Ce qui suit est la façon dont
ce plan se tient.*

| Date | Décision, et pourquoi |
|---|---|
| 2026-08-24 | **D14 — on code neutre ou additif.** L'app web n'est pas déployée avec l'API |
| 2026-09-13 | **Le livré sort du plan et entre, daté, dans `delivered.md`** au prompt « compact devplan » |
| 2026-09-19 | **Une attente envers l'amont se vérifie chez l'amont avant d'être reconduite.** Ce plan a attendu d'Orders pendant **huit semaines** un repli livré le 23/07 |
| 2026-09-19 | **Une entrée se cite par son titre**, l'ancienne référence entre crochets |
| 2026-09-19 | **Une édition qui n'a pas tout revérifié le dit**, avec la date du dernier relevé |
| 2026-09-21 | **Le `.pdb` dit ce qui a été COPIÉ, `api/version` dit ce qui TOURNE.** Les deux se vérifient après une publication, et le second seul répond à « le serveur a-t-il redémarré dessus ? ». Le 13/09 puis le 15/09, la production a servi autre chose que ce que le partage portait — un contrôle sur les fichiers ne pouvait pas le voir |

---
