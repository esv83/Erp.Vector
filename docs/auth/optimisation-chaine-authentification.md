# Optimisation de la chaîne d'authentification mobile

Chaque endpoint crew-scoped valide l'accès via : **JWT (local)** → `sub → personnelId` → `personnel → crews actifs` → `crewId ∈ crews actifs`. Les deux derniers maillons sont des appels HTTP à Orders.Api. Le JWT, lui, est validé localement (JWKS en cache middleware) : pas de coût réseau par requête.

Ces deux maillons n'ont **pas la même volatilité** — d'où deux traitements distincts.

| Maillon | Volatilité | Traitement |
|---|---|---|
| `sub → personnelId` | quasi-immuable | cache long (défaut 8 h) |
| `personnel → crews actifs` | volatile intra-journée | cache court (défaut 15 min) + lecture fraîche à la sélection |

---

## 1. Cache (implémenté)

`CachingMobileIdentityResolver` décore `IMobileIdentityResolver` (transparent pour controllers et use cases). Il utilise `IMemoryCache`.

- **`ResolvePersonnelId`** : cache par `sub`, TTL long. **Seules les résolutions positives** sont mises en cache — un compte pas encore rattaché à un personnel peut l'être plus tard le jour même sans attendre l'expiration.
- **`ResolveActiveCrewIds`** (garde-fou, chemin chaud) : cache par `(personnelId, date)`, TTL court.
- **`ResolveActiveCrewIdsFresh`** (`GET /api/crew/mine`) : **contourne** le cache et le rafraîchit. C'est le seul point où un crew créé le jour même doit apparaître — et c'est de toute façon le seul chemin pour le choisir. Le garde-fou des requêtes suivantes réutilise l'entrée fraîche.

### Pourquoi pas un cache « demi-journée » unique

Un TTL long sur les **crews** masquerait un crew créé (ou clôturé) le jour même. La sécurité reste garantie : le garde-fou ne valide qu'un crew **déjà sélectionné** (donc légitime au moment du choix), et l'écriture ERP (conducteur, km) refuse déjà toute opération sur un crew clôturé (400). Au pire, quelques minutes de tolérance sur un crew qui vient de fermer — sans trou réel.

### Configuration (`appsettings.json`)

```jsonc
"MobileIdentityCache": {
  "PersonnelMinutes": 480,      // sub → PER_ID (défaut 8 h)
  "ActiveCrewsMinutes": 15      // crews actifs (défaut 15 min)
}
```

Absent → valeurs par défaut. Mettre `ActiveCrewsMinutes` très bas (ex. 1) rapproche du comportement « sans cache » sans le désactiver.

### Effet

Entre deux sélections, le chemin chaud passe de **2 appels Orders.Api → ~0**. La création d'un crew le jour même reste gérée (visible dès la prochaine ouverture du sélecteur).

---

## 2. Option — porter `personnelId` dans le token Keycloak (claim `per_id`)

Le cache **réduit** le coût du maillon 1 ; ce claim le **supprime** — sans état, définitivement.

### Principe

Ajouter à Keycloak un *protocol mapper* qui injecte un claim `per_id` (le PER_ID ERP) dans le token, alimenté par la même source que `PER_KEYCLOAK_MAP`. `ResolvePersonnelId` n'est alors plus un appel HTTP : on lit le claim du JWT déjà validé.

### Mise en œuvre

1. **Keycloak** — sur le client mobile, ajouter un mapper « User Attribute → Token Claim » :
   - User Attribute : `per_id` (attribut porté par l'utilisateur Keycloak, synchronisé depuis l'ERP).
   - Token Claim Name : `per_id`, Claim JSON Type : `String`, *Add to access token* : ON.
   - Alternative si le PER_ID n'est pas un attribut utilisateur : mapper « hardcoded » via script/SPI interrogeant le référentiel au login.
2. **API** — étendre `MobileCallerExtensions` d'un `GetPersonnelId(this ClaimsPrincipal)` lisant le claim `per_id` (Guid). Puis, dans `CrewAccess`, préférer le claim et ne retomber sur `ResolvePersonnelId` (HTTP) que si le claim est absent (compat pendant la transition).

### Ce que ça élimine / coûte

- ✅ Supprime **totalement** le maillon `sub → personnelId` (plus d'appel, plus de cache à gérer dessus).
- ✅ Sans état, aligné avec la trajectoire « identité société unifiée » (cf. `IMobileIdentityResolver`, Track B).
- ⚠️ Touche la **config Keycloak** (hors code applicatif) + la synchro de l'attribut `per_id` côté annuaire. Un rattachement modifié n'est reflété qu'au **prochain login** (renouvellement du token) — acceptable pour un lien quasi-immuable.
- ➖ N'aide **pas** le maillon crews (trop volatil pour un claim figé au login) : celui-ci reste un lookup caché.

### Reco

Garder le claim `per_id` en **option** : à activer si la charge le justifie ou lors de la bascule Track B. Le cache actuel couvre le besoin immédiat sans dépendance à l'infra Keycloak.
