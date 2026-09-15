# 🧹 Note UI Web Vector — Routes retirées du contrat (pour Alexandre)

> **Date** : 2026-09-13 · **Pour** : Alexandre, dev web de l'UI Vector.
> **Objet** : trois contrôleurs sortent du contrat mobile. **Aucune de ces routes n'a jamais
> fonctionné** : elles répondaient toutes `500`. Après le prochain déploiement de Vector, elles
> répondront `404`.

Salut Alexandre 👋

Ces routes reposaient sur des implémentations vides, héritées du portage. Plutôt que de les
implémenter, on les retire. Si un écran les appelle encore, il était déjà cassé : il suffit de
retirer l'appel (ou l'écran).

## Ce qui disparaît

| Route | Usage prévu |
|---|---|
| `GET api/Contact?FullSearchName=…` | recherche de bénéficiaire par nom |
| `PATCH api/Contact` | modification d'un bénéficiaire |
| `GET api/MecanicLog` · `GET api/MecanicLog/{crewId}` · `POST api/MecanicLog` | main courante mécanicien |
| `GET analyze/{logId}` · `POST analyze` · `PUT analyze` · `DELETE analyze/{logId}` · `DELETE analyze/{logId}/actions/{actionId}` | analyses de la main courante |
| `GET api/ReferenceData/actors` · `…/actions` · `…/constraints` · `…/nature` · `…/concerning` | listes de la main courante (valeurs codées en dur) |

⚠️ Les listes `api/ReferenceData/*` répondaient `200`, avec des valeurs fictives : ce sont les seules
qui changent réellement de comportement. Elles n'alimentaient que l'écran de main courante.

## Ce qui ne change pas

Tout le reste du contrat : joblist, détail, jalons, signature, contexte de mission, documents,
anomalies, carte mutuelle.

## ✅ Récap

- [ ] Retirer tout appel à `api/Contact`, `api/MecanicLog`, `analyze`, `api/ReferenceData`
- [ ] Retirer l'écran de main courante mécanicien s'il existe
