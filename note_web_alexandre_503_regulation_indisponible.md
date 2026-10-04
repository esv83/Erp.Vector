# 🔌 Note UI Web Vector — Quand la régulation ne répond pas : 503 au lieu de 500 (pour Alexandre)

> **Date** : 2026-10-04 · **Pour** : Alexandre, dev web de l'UI Vector.
> **Objet** : quand Order (la régulation) ne répond pas, Vector rendait un **500 vide**. Après le
> prochain déploiement de Vector, il rendra un **503 avec un message affichable**.

Salut Alexandre 👋

## Ce qu'on a vu

Du 21/09 au 04/10, **~2 600 requêtes** ont reçu un 500 brut pendant que la régulation était à terre :
coupures des 22/09 (40 min) et 02/10 (15 min), et ses mises à jour. **72 % sur `GET api/Crew/mine`**,
l'écran d'entrée : l'ambulancier ne pouvait pas entrer, et rien ne lui disait pourquoi.

## Ce qui change — sur toutes les routes, seulement dans ce cas-là

```http
HTTP/1.1 503 Service Unavailable
Retry-After: 10
Content-Type: application/problem+json

{ "status": 503,
  "title": "Service momentanément indisponible",
  "detail": "La régulation ne répond pas pour le moment. Réessayez dans une minute." }
```

- **`detail` est affichable tel quel**, comme pour les autres refus de Vector.
- Les en-têtes CORS sont présents : le navigateur voit bien la réponse.
- Un vrai bogue reste un **500**. Les 401 / 403 / 404 / 400 ne changent pas.

## Ce qui ne change pas

Si l'app traite déjà tout code non-2xx comme une erreur, **rien ne casse**. C'est seulement le message
qui devient disponible.

## ✅ Récap

- [ ] Sur un **503**, afficher `detail` (au moins sur l'écran d'entrée, `api/Crew/mine`)
- [ ] Optionnel : proposer « Réessayer », ou relancer seul après `Retry-After` secondes
