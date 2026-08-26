# 🖥️ Note UI Web Vector — Fermer la bascule du contexte : ce qu'il me faut de toi

> **Date** : 2026-08-27 · **Pour** : Alexandre, dev web de l'UI Vector.
> **Objet** : la bascule du contexte de mission est **en service depuis le 25/08**, et le filet de
> secours vient d'être retiré (§3). Je te dois l'explication, et il me reste quatre points à te demander.
> **Aucune nouveauté de contrat dans cette note** : pas de nouveau champ, pas de nouvelle route, rien
> à intégrer. Le détail technique est dans [`note_web_alexandre_context_mission_dto.md`](note_web_alexandre_context_mission_dto.md)
> (25/08), celle-ci ne fait que te demander un **oui / pas encore**.

Salut Alexandre 👋

## 1. Rappel : ce que la bascule a changé chez toi

Quatre points, tous déjà décrits dans la note du 25/08 — je les remets en une ligne chacun pour que
celle-ci se lise seule.

| # | Ce qui a changé | Où c'est détaillé |
|---|---|---|
| 1 | **`POST api/Contract/{jobId}` peut échouer** — `409` (type imposé par la régulation), `400` (type non applicable), `404`. Il réussissait **toujours** avant. | note du 25/08, §3 |
| 2 | **`PATCH api/JobEdit/{jobId}` peut échouer** — `409` (champ scellé), `400` (valeur invalide), et c'est **tout ou rien** : sur un 400, *rien* n'est enregistré. | note du 25/08, §5 |
| 3 | **`IsReadOnly` / `ReadOnlyReason`** sur chaque champ du formulaire : afficher la valeur, désactiver la saisie, montrer le motif. | note du 25/08, §4 |
| 4 | **Le NIR n'est corrigeable dans aucun module une fois posé** : à faire relire, ou confirmer avant validation. | note du 25/08, « Trois comportements métier » |

## 2. Pourquoi je te demande une confirmation plutôt que de conclure que ça marche

**Parce que le silence ne prouve rien ici.** Les deux refus les plus visibles ne se déclenchent sur
**aucune mission aujourd'hui** :

- le **409** du `POST` suppose un type configuré non surchargeable — il n'y en a aucun en production
  depuis le correctif du 25/08 : 0 mission verrouillée sur les 15 relevées ce jour-là, contre 20 sur 25 la veille ;
- le **400** ne part que si tu envoies un `Id` absent de la liste reçue, ce que ton code ne fait
  probablement jamais.

Autrement dit : ton écran peut très bien être **cassé sur ces deux chemins sans que personne ne le
voie**, et se réveiller le jour où la régulation configurera son premier type imposé. C'est le genre
de panne qui arrive six semaines plus tard, sur une mission réelle, sans rapport apparent avec quoi
que ce soit.

## 3. Il y avait un filet. Je l'ai retiré, et je te dois l'explication

Jusqu'ici, deux drapeaux de configuration permettaient de **revenir à l'ancien comportement en deux
minutes**, sans redéploiement : catalogue local, formulaire d'avant, appels qui réussissent toujours.
Je comptais attendre ton feu vert pour les enlever. La mesure m'a fait changer d'avis.

**Ce que j'ai relevé le 26/08 sur 40 missions de production** : la date de naissance est renseignée
sur 34 d'entre elles, et **verrouillée dans les 34 cas** — avec son motif. Le NIR, lui, n'est
**jamais** pré-rempli : c'est toujours le terrain qui le pose.

**Et ce que faisait l'ancien chemin** : il ne pose *jamais* `isReadOnly` et ne valide *aucune* valeur.
Revenir en arrière aurait donc rouvert à la saisie ces 34 dates de naissance, et accepté un NIR à clé
fausse sans un mot — dans un magasin que la facturation ne lit même pas.

Autrement dit, le filet protégeait contre un message d'erreur mal géré en rouvrant une donnée de
santé non corrigeable. Ce n'était plus un retour arrière, c'était une régression. Il est parti.

⚠️ **Ce que ça change pour toi, concrètement** : si tu constates un problème, le correctif est un
redéploiement — donc une courte coupure d'API — et non plus une bascule de configuration
instantanée. **Signale vite plutôt que de contourner.**

## 4. Ce que je te demande — quatre cases à cocher

Réponds point par point, même par « oui / oui / oui / pas encore » :

- [ ] **1. `POST api/Contract`** — les `409`, `400` et `404` sont traités : message à l'ambulancier,
      re-`GET` pour resynchroniser, et **l'écran n'affiche pas le choix comme enregistré**.
- [ ] **2. `PATCH api/JobEdit`** — sur `400`, tu **gardes la saisie à l'écran** et tu ne considères
      aucun champ comme enregistré ; sur `409`, tu re-`GET` le formulaire.
- [ ] **3. `IsReadOnly` / `ReadOnlyReason`** — champ désactivé **et** motif affiché. Un champ grisé
      sans explication, l'ambulancier le prend pour un bug et rappelle la régulation.
- [ ] **4. NIR** — relecture ou confirmation avant validation. ⚠️ **Celui-là est du vrai travail, pas
      une case à cocher** : si c'est encore à faire, dis-le, ça ne bloque pas le reste.

Un test qui vaut pour les points 1 et 2, sans rien casser : envoie un `POST api/Contract/{jobId}`
avec un `Id` bidon (`999999`) — tu récupères un **400** propre, aucune écriture, et tu vois comment
ton écran se comporte.

## 5. Ce que je fais de ta réponse

Ces quatre points ne bloquent plus rien de mon côté — c'est ton écran qu'ils protègent, pas mon
chantier.

- **Tout est traité** → parfait, je n'ai plus qu'à supprimer les vieilles tables et le chapitre se
  ferme.
- **Un ou deux points restent à faire** → dis-le simplement, avec un ordre d'idée de délai. Le `409`
  ne se déclenche sur aucune mission aujourd'hui, donc rien ne brûle ; le `400` de la saisie, lui,
  peut arriver dès qu'un ambulancier tape une valeur invalide.
- **Ça casse déjà** → préviens-moi tout de suite. Le correctif passe maintenant par un
  redéploiement, autant ne pas le découvrir dans deux semaines.

## 6. Question sans rapport, tant que je t'écris

Trois routes de l'API répondent **500** et l'ont toujours fait — elles n'ont jamais été implémentées :

| Route | |
|---|---|
| `GET api/Contact?FullSearchName=…` · `PATCH api/Contact` | recherche et mise à jour de bénéficiaire |
| `GET api/MecanicLog` · `GET api/MecanicLog/{crewId}` · `POST api/MecanicLog` | main courante mécanicien |
| `GET analyze/{logId}` | analyse d'une main courante |

**Est-ce que ton front les appelle ?** Si non, je les retire du contrat et le nettoyage se termine ;
si oui, dis-moi lesquelles et on décide quoi en faire. Ce n'est pas une régression du jour — c'est de
la dette ancienne qu'un nettoyage vient de mettre au jour.

## 7. Au passage — l'API est fermée par défaut depuis le 25/08

Toutes les routes exigent désormais un utilisateur authentifié ; avant, plusieurs répondaient `200`
à qui connaissait un identifiant de mission (dont le formulaire, **valeurs comprises**). Si ton front
envoie déjà son jeton partout, tu n'as rien à faire. **Si un appel te rend un `401` inattendu**,
c'est de là que ça vient — signale-le, c'est un oubli de notre côté, pas une décision.

---

## ⚡ TL;DR

1. **Rien à intégrer** dans cette note : je demande une réponse, pas du travail (sauf le NIR).
2. Les **409 / 400** ne se déclenchent sur aucune mission aujourd'hui — ton écran peut être cassé
   dessus sans que ça se voie. C'est pour ça que je demande une confirmation explicite.
3. **Le retour arrière par configuration n'existe plus** : il rouvrait 34 dates de naissance
   verrouillées sur 40 et acceptait un NIR faux en silence (§3). Un correctif passe désormais par un
   redéploiement — signale vite.
4. **Quatre cases** au §4 → réponds point par point.
5. **Une question bonus** au §6 : est-ce que tu appelles `api/Contact`, `api/MecanicLog` ou
   `analyze/…` ? Elles répondent 500 depuis toujours.

Ping-moi si tu veux qu'on déroule les quatre points ensemble sur une mission de test, c'est
l'affaire de vingt minutes. 🚀
