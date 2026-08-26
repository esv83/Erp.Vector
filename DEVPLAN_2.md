# 🔌 DEVPLAN_2 — Découplage Vector ↔ Orders : authentification de service et résilience

> **Plan d'exécution** des deux derniers chantiers de
> [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) §2 : `DEC-6`
> (authentification de service, **dans les deux sens**) et `DEC-7` (résilience des appels sortants).
>
> **Ce document n'est pas une seconde source de vérité.** Le devplan principal garde l'état
> ([`devplan.md`](devplan.md) §3.C2 et §3.D) ; ici vit le détail d'exécution : quoi coder, dans quel
> ordre, et comment savoir que c'est fini. Les trois autres restes du §2 — `DET-4`, `DET-3`,
> option 4b — ne sont **pas** dans ce plan et restent suivis au devplan principal.
>
> **Statut** : ⏳ **aucune étape livrée** — le plan est écrit, le code ne l'est pas. Rien ici n'est
> commencé, et rien n'y bloque hors décisions humaines (§6).
> **Branche** : `feat/decouplage-dec6-dec7` — ⚠️ elle porte aujourd'hui **autre chose** que ce plan
> (correctifs du contexte de mission et carte mutuelle, §1.2). Le premier commit `DEC-` reste à venir.
> **Créé le** : 2026-08-25 · **Dernière mise à jour** : 2026-08-26.

| | Sens |
|---|---|
| 🟢 | livré, vérifié |
| ⏳ | à faire, rien ne bloque |
| ⛔ | bloqué (décision humaine, ou livrable d'un autre dépôt) |
| 🔴 | décision qui appartient à un humain |
| ⚠️ | piège avéré / garde-fou |

---

# 1. Pourquoi ce plan existe

Le découplage est **livré** : Vector ne compile plus Orders, il le consomme en HTTP. Ce qui restait
au §2 tenait de la précaution — jusqu'au 25/08.

`DEC-6` était écrit comme une **anticipation** : « le jour où Orders.Api sera protégée ». Or la
fermeture de l'API Vector par défaut, livrée et déployée le 2026-08-25, a laissé **quatre routes
ouvertes** que la facturation tire sans jeton — dont l'image de la carte mutuelle, donnée de santé.
`DEC-6` n'anticipe plus : **c'est lui qui tient ces portes ouvertes.**

`DEC-7`, lui, n'a pas changé de nature : sans timeout explicite, un appel sortant peut pendre **100
secondes** pendant qu'un ambulancier regarde son écran.

## 1.1 Trois faits établis avant d'écrire une ligne

1. **Orders.Api n'a aucune authentification** (`Erp.Order/Orders.Api/Program.cs` : ni `AddJwtBearer`,
   ni section `Keycloak`). Un en-tête `Authorization` posé vers Orders sera donc **ignoré, pas
   rejeté**. `DEC-6` sortant est intégralement livrable **sans coordination avec Orders**, et sans
   risque : inerte tant qu'il n'est pas configuré, en-tête ignoré une fois qu'il l'est.

2. **Deux des quatre routes ouvertes n'avaient aucun consommateur prouvé** *(constat du 25/08 —
   ⚠️ **partiellement périmé, cf. §1.2**)*. Le DTO tolérant de la facturation
   (`ClVectorFieldDataClient.vb`) déclare `MissionId, OrderId, UpdatedAt, Timeline, Signature,
   Attributes, Kilometers` — **ni `Documents`, ni `Mutuelle`**. Les `FileUrl` / `ImageUrl` que Vector
   produit sont donc jetés à la désérialisation. La certification, elle, n'appelle pas Vector du tout.

3. **Les tests existants n'exercent pas le pipeline d'injection.** `ContextOrderAttributeTests.Build()`
   construit les clients à la main. Conséquence double : `DEC-7` **ne peut pas casser** les tests
   existants, et il **ne peut pas être testé** si les politiques restent inlinées dans `Program.cs`.
   D'où la fabrique publique en D2.

## 1.2 ⚠️ Ce qui a bougé depuis la rédaction *(2026-08-26)*

**La carte mutuelle cesse d'être une route orpheline.** Quatre choses, le même jour :

- l'**écran web de capture est passé en production** — la photo est prise, et relue, depuis un
  navigateur ;
- le blocage qui tenait la table vide depuis juin est levé : le détail mission sert désormais
  `beneficiaryId`, sans lequel le front ne pouvait pas construire l'URL de capture
  ([`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) §3.1) ;
- la décision **M7** pose que la carte est un **document consultable par les autres modules** —
  **Order** et **BillingGateway** —, et non une source de colonne de facturation ;
- **H1 est tranchée** : ces écrans affichent par **balise `<img src>`**, Order une carte à la fois et
  la facturation une liste de bénéficiaires. Deux routes ouvertes ont été ajoutées pour eux le jour
  même (§6.0).

⇒ **Trois publics, et aucun ne peut présenter de jeton pour une image** : un navigateur qui rend une
balise n'en porte pas. Les routes de carte mutuelle **sortent d'E3** : elles ne sont pas « pas encore
fermables », elles sont **ouvertes par décision**, et elles ne se refermeront pas avec `DEC-6` mais le
jour où ces écrans passeront à un `fetch` authentifié.

> 🪤 **La leçon, et elle vaut au-delà de ce plan** : « aucun consommateur prouvé » est un constat
> **daté**, pas une propriété. Ici il a tenu **un jour**. Toute étape qui ferme une porte sur ce motif
> doit revérifier le motif au moment de la fermer — c'est ce que fait E1, et c'est pourquoi il reste
> le premier de la séquence.

⚠️ **La branche porte autre chose que ce plan.** `feat/decouplage-dec6-dec7` contient à ce jour le
correctif du verrou de contexte, la remontée du motif de refus au mobile, la mise à jour des plans et
le `beneficiaryId`. Aucun commit `DEC-`. Qui reprend la branche ne doit pas en déduire que le chantier
est entamé.

---

# 2. Décisions actées — ne pas rejouer

| # | Décision |
|---|---|
| **T1** | **`DelegatingHandler`, et ce sera le premier du parc.** Trois raisons indépendantes : *(a)* les deux clients HTTP restent **purs**, donc testables en trois lignes — y injecter un fournisseur de jeton forcerait quatre harnais existants à stuber un jeton pour tester du parsing JSON ; *(b)* le **rejeu sur 401** n'est faisable que là, il faut voir la réponse — depuis les sites d'appel il faudrait le dupliquer dans 14 méthodes ; *(c)* l'**ordre avec Polly devient exprimable** : enregistré *après* les politiques, le handler d'auth est le plus **interne**, donc chaque tentative relit le jeton en cache. Le contre-argument (« aucun `DelegatingHandler` dans le parc ») est réel mais ne pèse pas : 40 lignes, locales à un enregistrement. |
| **T2** | **Polly, pas `Microsoft.Extensions.Http.Resilience`.** Ce dernier est introuvable dans tout le parc ; l'introduire ferait de Vector le seul module à porter **deux** modèles de résilience. |
| **T3** | **Fabriques de politiques publiques**, là où celles de la facturation sont privées. C'est le seul moyen de les tester (D3). Écart assumé au modèle maison, pour la raison qui manquait au modèle maison. |
| **T4** | **Recul 200 ms / 600 ms, pas 2/4/8 s.** Sous un budget de 10 s, un recul de 2+4+8 = 14 s ne produirait **jamais** la troisième tentative. Écrire un réessai qui ne peut pas s'exécuter, c'est fabriquer une illusion de robustesse. Gigue **±50 %** pour ne pas synchroniser une flotte de mobiles sur le même redémarrage d'Orders. |
| **T5** | **Le fournisseur de jeton a son propre `HttpClient`**, jamais celui d'Orders — sinon le jeton passerait par le disjoncteur d'Orders, et **une panne d'Orders empêcherait d'obtenir un jeton**. |
| **T6** | **Le compte de service ne mutualise rien avec `usvector-diag`.** Deux clients Keycloak distincts : le diag porte `realm-management/view-users`, le compte d'API n'en a pas besoin et **ne doit pas les avoir**. Deux durées de vie (quelques fois par mois à la main / toutes les 5 min sous concurrence) et deux rayons d'explosion (une page de diagnostic / la joblist de toute la flotte). On recopie 15 lignes plutôt que de coupler un outil de dev au chemin chaud de production. |
| **T7** | **Échec d'obtention de jeton = Warning + `null`, jamais une exception.** L'appel part sans en-tête et prendra un 401 le jour où Orders sera protégée — symptôme lisible, plutôt qu'un 500 fabriqué par Vector. |
| **T8** | **Le handler d'auth est enregistré inconditionnellement, dans tous les environnements.** Le conditionner à `IsConfigured` créerait un câblage qui n'existe qu'en production, donc **jamais exercé avant d'être indispensable**. Non configuré ⇒ trafic **octet pour octet** celui d'aujourd'hui. |
| **T9** | **Trois notions, pas deux** : jetons *acceptés* à la porte, client *ambulancier* (politique de repli → toute l'API mobile), clients *de service* (politiques nommées → quatre routes et rien d'autre). Copier telle quelle la liste d'`azp` d'`Erp.Identity` ouvrirait **toute l'API mobile** au compte de la facturation — remplacer une porte ouverte à tous par une porte ouverte à un service qui n'a rien à y faire. |
| **T10** | **Deux politiques nommées, pas une.** `field-data` est un paquet de transfert qu'aucun écran ne consomme → service-only. Les routes d'octets ont un usage humain plausible — l'ambulancier relit la photo qu'il vient de prendre → `ServiceOuAmbulancier`. Les mettre toutes en service-only casserait une fonctionnalité mobile pour rien. |
| **T11** | **Le point de jeton est dérivé de `Keycloak:Authority`, pas découvert par OIDC.** Vector valide déjà le format de l'Authority au démarrage, la découverte ajouterait un aller-retour sur le chemin chaud, et `/protocol/openid-connect/token` est déjà écrit en dur à `KeycloakAdminClient.cs:49` — on aligne au lieu de créer une incohérence. |

---

# 3. Ordre de livraison

```
D1 → D2 → D3                                   aucune dépendance externe
S0 → S1 → S2 → S3 → S4 → [Keycloak] → S5       aucune dépendance externe
E1 → [mesure 7 j] + [réponse dev web] → E2 → E3
                    E2 → [Keycloak] → [BillingGateway] → E4 → E5
```

**D** et **S** sont livrables **en parallèle** de **E**. Le seul couplage réel : **E2 avant E3 et E4**.

> 💡 **Séquence conseillée** : livrer **E1 en premier** — risque nul, et il démarre le compteur de
> 7 jours. Dérouler D et S pendant que la mesure court. Le chemin critique du chantier E est du
> **temps calendaire**, pas du temps de développement.
>
> ⚠️ Cette mesure est d'autant plus nécessaire depuis §1.2 : la surface des appelants a changé en un
> jour, et E1 est le seul moyen de savoir qui appelle **au moment où l'on ferme**.

---

# 4. Chantier D — `DEC-7`, résilience des appels sortants

## D1 — ⏳ Timeouts explicites

**Contenu.** Une classe `OrdersApiOptions` (`BaseUrl`, `ReadTimeoutSeconds`, `WriteTimeoutSeconds`,
`RetryCount`, `CircuitBreakerThreshold`, `CircuitBreakerSeconds`), sur le modèle exact de
`ContextOrderOptions` — POCO peuplé à la main, `AddSingleton`, pas d'`IOptions`. On y déplace
`OrdersBaseUri` (`Program.cs:170-175`) **en gardant son commentaire** sur le `/` final : ce piège a
déjà coûté une panne. Puis `c.Timeout` sur les deux `AddHttpClient` (`Program.cs:179-184`).

**Valeurs, et pourquoi elles ne sont pas celles de la facturation.** BillingGateway travaille par
lot, la nuit : 30 s de budget y sont raisonnables. **Ici un ambulancier attend devant son écran.**

| Client | Budget | Pourquoi |
|---|---|---|
| `IErpReadApiClient` | **10 s** | chemin chaud mobile (joblist, détail mission) — 100 s aujourd'hui |
| `IErpWriteApiClient` | **15 s** | mixte : la sélection du type et la saisie sont sur le chemin humain ; la projection est derrière l'outbox |

⚠️ **Ne pas allonger le budget d'écriture pour l'outbox** : il a déjà son propre recul 2→60 s
(`OperationalOutboxDispatcher.cs:87-89`), et une tentative avortée est remise en file. Le long
horizon est déjà à sa place.

⚠️ **Changement de type d'exception** : un dépassement de `HttpClient.Timeout` lève
`TaskCanceledException`, **pas** `HttpRequestException`. Les trois sites concernés attrapent
`Exception` (`ContextOrderCatalogService.cs:43`, `OrderEffectiveContractTypeResolver.cs:50`,
`OperationalOutboxDispatcher.cs:83`) — ils absorbent le nouveau type sans modification. **Aucun
`catch (HttpRequestException)` n'existe dans le dépôt** : vérifié. C'est la vérification à refaire si
quelqu'un en ajoute un.

**Fin.** Orders rendue injoignable : `GET api/JobList/…` échoue en ~10 s au lieu de ~100 s.
Au chronomètre, sans instrumentation.

**Risque.** Un appel Orders légitimement lent (> 10 s) deviendrait une erreur. Aucun appel unitaire
connu ne s'en approche — les 14,7 s mesurées côté facturation sont un **cumul de 284 appels**, pas un
appel. À valider sur le serveur de dev avant la prod. *(Arbitrage produit : 🔴 H5.)*

## D2 — ⏳ Réessai et disjoncteur (Polly), en **fabrique publique**

**Paquet** : `Microsoft.Extensions.Http.Polly` **8.0.10**, dans
`CaSoft.Erp.USVector.Infrastructure.csproj` (tout y est déjà pinné en 8.0.10) — cf. **T2**.

**Contenu.** `Infrastructure/ErpApi/OrdersApiResilience.cs`, deux fabriques **publiques** (**T3**) —
`RetryPolicy(options)` et `CircuitBreakerPolicy(options)`, au-dessus de
`HttpPolicyExtensions.HandleTransientHttpError()`. Recul selon **T4**.

**Ordre** : `.AddPolicyHandler(retry).AddPolicyHandler(breaker)` — réessai à l'extérieur, disjoncteur
à l'intérieur, comme la facturation. Cet ordre fait compter au disjoncteur **chaque tentative**, donc
il s'ouvre plus vite quand Orders est réellement tombée.

**Le vrai gain est pour l'outbox** : le dispatcher traite jusqu'à 50 entrées **séquentiellement**.
Orders tombée sans disjoncteur : 50 × (timeout + réessais). Avec : les 5 premières échouent, les 45
suivantes échouent instantanément, le cycle se termine et l'outbox repousse.

**Ce qui ne doit pas casser :**

- **La tolérance au 404** (`ListCrewIdsAsync:69`, `ResolvePersonnelIdByKeycloakAsync:83`,
  `GetOrNullAsync:106`). `HandleTransientHttpError()` couvre 5xx, 408 et `HttpRequestException` —
  **404 n'en fait pas partie**, la réponse traverse intacte.
  🚫 **La faute à ne pas commettre** : ajouter `.OrResult(r => !r.IsSuccessStatusCode)` « pour être
  complet ». Chaque 404 deviendrait 3 appels puis un échec, et la joblist d'un ambulancier **sans
  équipage** tomberait en erreur. **À écrire en commentaire dans le code.**
- **Les refus métier de l'écriture** (409/400/404 traduits en issues) sont des 4xx : non transitoires,
  non réessayés, non comptés par le disjoncteur. Un champ verrouillé reste un champ verrouillé.
- **Les dégradations existantes** attrapent `Exception` : `BrokenCircuitException` y est absorbée.

**Réessai sur PUT/PATCH : sûr, et à écrire.** Les quatre écritures sont **idempotentes par
construction** — la projection envoie l'état complet (pas un delta), les deux `PATCH` posent des
valeurs. Rejouer après une coupure survenue *après* traitement serveur ne double rien.
⚠️ **À vérifier par un test, pas par raisonnement** : Polly renvoie **la même instance** de
`HttpRequestMessage`. Le renvoi d'un corps `JsonContent` déjà consommé fonctionne depuis .NET Core
3.0, mais c'est exactement le genre d'affirmation qui se révèle fausse en production (test 4 en D3).

**Fin.** Orders injoignable : les logs montrent 3 tentatives par appel, puis — après 5 échecs — des
rejets immédiats pendant 30 s. Et la joblist d'un ambulancier sans équipage rend toujours une **liste
vide** tant qu'Orders est debout.

## D3 — ⏳ Les tests que la fabrique publique rend possibles

**Contenu.** `OrdersApiResilienceTests.cs` : monter le pipeline à la main
(`new HttpClient(new PolicyHttpMessageHandler(OrdersApiResilience.RetryPolicy(opts)) { InnerHandler = … })`),
avec un `StubHandler` dérivé de celui de `ContextOrderAttributeTests` qui **compte les tentatives** et
rend une séquence (503, 503, 200). Recul de 1 ms en test, sinon la suite s'allonge.

Quatre tests, quatre affirmations que le code seul ne prouve pas :

| Test | Ce qu'il protège |
|---|---|
| `Un_503_est_reessaye_et_finit_par_passer` | 3 appels, 1 succès |
| `Un_404_n_est_pas_reessaye` | **1 seul** appel ; la liste vide survit. *Protège la tolérance au 404 contre le refactor de demain.* |
| `Un_409_de_refus_metier_n_est_pas_reessaye` | 1 appel, `FieldLocked` rendu. *Protège la distinction refus / panne.* |
| `Un_PATCH_reessaye_renvoie_bien_son_corps` | le corps de la 2ᵉ tentative est identique et non vide |

**Fin.** Suite verte, aucun test existant modifié. **Risque** : nul.

---

# 5. Chantier S — `DEC-6` sortant : Vector présente un jeton à Orders

*Forme tranchée en **T1** (DelegatingHandler) et **T5**/**T6**/**T7**/**T11**.*

## S0 — ⏳ Extraire `ParseAuthority`, et **ne pas** refactorer le reste

**Contenu.** `Infrastructure/Security/KeycloakAuthority.cs` : le corps exact de
`KeycloakAdminClient.cs:67-74`, commentaire compris. Le privé disparaît.

On réutilise la **forme** du POST (`l.47-64`) et la **forme** de la garde `IsConfigured` (`l.25-29`),
sentinelle `__SET_VIA_ENV__` comprise. **Recopiées, pas partagées** — motif en **T6**.

**Fin.** Suite verte, aucun changement de comportement. **Risque** : nul.

## S1 — ⏳ `KeycloakServiceTokenProvider` : cache, marge, sérialisation

**Contenu.** Singleton (le cache doit survivre à la requête), avec **son propre `HttpClient`** (T5).
Surface : `GetTokenAsync(ct)` (rend `null` si non configuré ou en échec), `Invalidate()`,
`IsConfigured`.

Transposition de `ClMagasinJetonsUtilisateur` (facturation) de `refresh_token` vers
`client_credentials`, **en plus simple** : un seul jeton, donc pas de dictionnaire, pas de purge, pas
de rotation. Ce qu'on garde tel quel :

- **marge de 30 s** avant expiration — un jeton qui expire en vol donne un 401 ;
- **`SemaphoreSlim(1,1)` + double vérification** après acquisition — sans lui, 40 requêtes mobiles au
  démarrage déclenchent 40 POST vers Keycloak ;
- **`expires_in` → expiration absolue**, repli à 60 s si absente ou ≤ 0 ;
- **`[JsonPropertyName("access_token")]`** explicite — `JsonSerializerDefaults.Web` ne ramène pas
  `access_token` en camelCase.

```jsonc
"Keycloak": {
  "ServiceAccount": { "Enabled": false, "ClientId": "usvector-api", "ClientSecret": "__SET_VIA_ENV__" }
}
```

**Fin.** Tests avec horloge injectable (`Func<DateTimeOffset>`, sinon c'est un `Task.Delay`) :
non configuré → `null` et **zéro** appel HTTP · 10 appels concurrents → **un seul** POST ·
`expires_in: 60` + 35 s → **second** POST · `Invalidate()` → POST suivant.

**Risque.** Nul : rien ne le consomme encore.

## S2 — ⏳ `ErpApiAuthHandler`, branché et **inerte**

**Contenu.** Si `request.Headers.Authorization` est nul, demander un jeton ; s'il n'est pas nul,
poser le `Bearer`. Enregistré sur les deux clients **après** les `AddPolicyHandler` (donc le plus
interne — raison *(c)* de **T1**). Inertie assumée : **T8**.

⚠️ Le test `if (Authorization is null)` n'est pas décoratif : il évite d'écraser un en-tête qu'un
appelant aurait posé. Aucun ne le fait aujourd'hui — l'écrasement silencieux est un piège classique.

**Une ligne au démarrage, et c'est la moitié de la valeur de l'étape :**

> `Orders.Api : appels SANS jeton de service (Keycloak:ServiceAccount désactivé ou non configuré).`
> `Orders.Api : appels AVEC jeton de service (client 'usvector-api').`

Le symptôme que le devplan redoutait — « une série de 401 sur la joblist, en production, sans autre
indice » — devient **une ligne au démarrage**.

**Fin.** Non configuré : aucun en-tête. Configuré : en-tête présent, **et présent aussi sur la 2ᵉ
tentative** d'un réessai — c'est le test qui vérifie l'ordre d'enregistrement.

## S3 — ⏳ Rejeu unique sur 401

Si la réponse est 401 **et** qu'un jeton avait été posé : `Invalidate()`, reprendre un jeton,
renvoyer **une seule fois** (drapeau local, pas de boucle). Séparé de S2 pour pouvoir être abandonné
sans rien perdre.

**Fin.** Stub 401→200 : deux appels, deux jetons demandés, réponse 200. Stub 401 systématique :
**exactement deux** appels. *(Le second test existe pour la boucle infinie.)*

## S4 — ⏳ Garde-fou de démarrage, sur le modèle KC-1

`Enabled = true` **et** non configuré ⇒ **refus de démarrer**, message nommant
`Keycloak__ServiceAccount__ClientSecret`.

**Pourquoi.** `appsettings.Production.json` documente déjà l'incident historique : `Keycloak:Enabled`
retombé à `false` par perte des variables du `web.config`, et « claim sub absent » pendant des
heures. Le secret du compte de service vit **au même endroit et se perd de la même façon**. Sans
garde, l'API devient **silencieusement anonyme** vers Orders.

## S5 — 🔴 Activation *(action humaine — voir H3)*

Créer `usvector-api` dans le realm, client confidentiel, *Service accounts enabled*, **sans rôle
`realm-management`** (T6). Poser le secret dans le `web.config`, et basculer `Enabled` **par variable
d'environnement** — ⚠️ **pas** dans `appsettings.Production.json`, shippé par `dotnet publish` et
donc effacé au déploiement suivant (piège documenté au devplan §5.2, et déjà rencontré ce mois-ci).

**Fin, mesurable et seulement en production** : le log de démarrage dit « AVEC jeton », et le nombre
de POST vers le point de jeton Keycloak sur une heure de trafic est de l'ordre de **12** (un toutes
les 5 min) — **pas** plusieurs milliers. C'est le test réel du cache.

---

# 6. Chantier E — `DEC-6` entrant : refermer les quatre routes

## 6.0 La distinction qui structure tout

| Route | Consommateurs | Fermable |
|---|---|---|
| `GET api/missions/{id}/field-data` | facturation (`ClVectorFieldDataClient`) | ❌ après compte de service côté facturation |
| `GET api/Signature/{id}` | facturation (`ClVectorSignatureClient`) | ❌ idem |
| `GET api/documents/{id}/content` | **aucun prouvé** — absent du DTO tolérant | ✅ sous réserve de 🔴 H1 |
| `GET api/mutuelle-card/{id}/image` | facturation (D8) | ❌ avec les deux premières |
| `GET api/beneficiaries/{id}/mutuelle-card/image` | ⚠️ **écrans** Order + facturation, par balise `<img src>` *(26/08)* | ❌ **pas par DEC-6** — cf. ci-dessous |
| `POST api/mutuelle-card/presence` | ⚠️ **écran** facturation, liste de bénéficiaires *(26/08)* | ❌ idem |

> ⚠️ **Les deux dernières ne relèvent pas de ce chantier**, et c'est le point à ne pas confondre.
> Elles sont ouvertes parce qu'une balise `<img src>` **ne portera jamais de jeton** — donner un
> compte de service à Order n'y changerait rien, puisque c'est un navigateur qui affiche. Elles se
> referment le jour où ces écrans passent à un `fetch` authentifié, pas le jour où `DEC-6` est fait.
> `AnonymousSurfaceTests` les compte **à part**, dans un tableau distinct, pour que personne ne les
> croie couvertes par la séquence E.

## E1 — ⏳ Mesurer, avant de conclure

**Deux sources, la seconde seulement se code.**

**(a) Journaux IIS du serveur — gratuits, à demander.** Les journaux W3C portent `c-ip`,
`cs-uri-stem`, `cs(User-Agent)` : filtrer les quatre chemins sur 30 jours, grouper par IP + agent.
C'est la vérité sans écrire une ligne.

**(b) Sonde applicative.** `Api/Infrastructure/AnonymousSurfaceProbe.cs` : sur **ces quatre chemins
uniquement**, journaliser sous un logger dédié `Vector.SurfaceAnonyme` — méthode, chemin, IP,
`User-Agent`, `Referer`, présence d'un `Authorization`, `azp` s'il y en a un. Enregistré **après**
`UseAuthentication`/`UseAuthorization` pour que l'identité soit renseignée. Une règle NLog dédiée
route ce logger vers son fichier, pour compter à la ligne.

**Fin.** Après **7 jours** : pour chaque route, la liste des IP et des agents. Attendu — `field-data`
et `Signature` vus depuis l'IP de la facturation ; `documents/content` jamais vu ; **`mutuelle-card`
vu depuis les postes web** dès que l'écran de capture est en ligne. Si l'attendu n'est pas vérifié,
**E3 s'arrête** et on repart de la mesure.

**Risque.** Nul (lecture seule). Volume négligeable.

## E2 — ⏳ Deux publics sur la même API — *l'étape la plus risquée*

⚠️ **Le vrai piège de `DEC-6` entrant est ici** — motif complet en **T9**.

**Il faut distinguer trois choses, pas deux :**

| Notion | Config | Portée |
|---|---|---|
| Jetons **acceptés** par le pipeline | `Keycloak:Audience` ∪ `Keycloak:ServiceAzp[]` | le jeton n'est pas rejeté à la porte |
| Client **ambulancier** | `Keycloak:Audience` (`us-ambulance`) | politique de repli → **toute l'API mobile** |
| Clients **de service** | `Keycloak:ServiceAzp[]` (nouveau) | politiques nommées → **les quatre routes, et rien d'autre** |

**Contenu.**
1. L'`azp` attendu (`Program.cs:105-124`) devient un `HashSet<string>(Ordinal)`, comme
   `Identity.Api/Program.cs:68-75`. **Le dépôt du motif dans `HttpContext.Items` est conservé** — il
   alimente les messages de `CrewAccess` et vaut mieux qu'un `ctx.Fail` nu.
2. **Politique de repli resserrée** : `RequireAuthenticatedUser()` **+** `azp == Audience`.
   ⚠️ **À conditionner à `!disableValidation`**, exactement comme la vérification d'`azp` l'est déjà.
   Sinon le poste de développement — où la validation est volontairement désactivée — **ne répond
   plus rien**, et personne ne comprend pourquoi.
3. **Deux politiques nommées** : `"ServiceFacturation"` et `"ServiceOuAmbulancier"` (**T10**).
   ⚠️ Depuis §1.2, `ServiceOuAmbulancier` devra accueillir **deux** comptes de service en plus de
   l'ambulancier (Order, BillingGateway) pour l'image de carte : la liste d'`azp` est donc à prévoir
   **multi-clients dès l'écriture**, pas à rallonger après coup.

> 💡 **Un second garde-fou existe déjà, gratuitement** : un compte de service a un `sub` absent de
> `PER_KEYCLOAK_MAP`, donc `CrewAccess.ResolvePersonnel` rendrait **403**. Les cinq routes
> crew-scopées sont **doublement** fermées — par construction, pas par vigilance.

**Fin.** Étape **purement additive** : aucune route ne référence encore les nouvelles politiques. Seul
changement observable — un jeton de service n'est plus rejeté à la porte, mais reste refusé partout
par la politique de repli. En dev : jeton ambulancier → tout marche ; jeton de service → 403 partout.

**Risque.** **Le plus élevé des trois chantiers** : elle touche la politique globale. À déployer
**seule**, sur le serveur de dev d'abord, avec `api/Auth/WhoAmI` et `api/JobList/…` comme témoins.

## E3 — ⛔ Fermer `documents/content` — *réduit à une seule route*

*Anciennement « fermer les deux routes sans consommateur ». Réduit le 26/08 : H1 a été tranchée en
faveur de la **balise `<img src>`**, donc les routes de carte mutuelle restent ouvertes et sortent de
cette étape (§6.0).*

**Préalable** : E1 à zéro sur 7 jours pour `documents/content`.

**Contenu.** `[AllowAnonymous]` → `[Authorize(Policy = "ServiceOuAmbulancier")]` sur
`DocumentController.GetContent`. Mettre à jour `AnonymousSurfaceTests`. Remplacer le commentaire
« ⛔ … se referme avec DEC-6 » par ce qui reste ouvert, à qui.

⚠️ **Ne pas y rajouter la carte mutuelle par symétrie.** Ses deux routes sont ouvertes **par
décision**, pas par défaut d'authentification : un navigateur affiche par balise, et une balise ne
porte pas de jeton. Les fermer ici casserait les écrans d'Order et de la facturation le jour du
déploiement, sans erreur console et sans qu'aucun test ne le voie. Elles se referment ailleurs — le
jour où ces écrans passent au `fetch` authentifié.

**Fin.** Les octets d'un document ne sont plus accessibles sans jeton.

## E4 — ⛔ Fermer `field-data` et `Signature` — *cross-repo, ordre non permutable*

**E4.a — Keycloak (🔴 H2).** `us-facturation` est déclaré **public** (pas de secret, PKCE) : il ne
peut **pas** faire de `client_credentials`.

**E4.b — Côté facturation (autre dépôt).** Transposer S1+S2 en VB, branchés sur les **deux seules**
inscriptions Vector (`ModBillingGatewayInfraExtension.vb:185-204`) — **pas** sur les clients Orders,
qui appellent une API non protégée. Même politique d'inertie. *Hors périmètre de ce plan, mais
conditionne E4.c : mérite sa propre entrée au devplan de la facturation.*

**E4.c — Côté Vector.** `FieldDataController.Get` → `"ServiceFacturation"` ;
`SignatureController.GetSignature` → `"ServiceOuAmbulancier"`. Et **supprimer l'appel à
`ClAutorizationCommand.AutorizeJob`** (`SignatureController.cs:23`) : cette méthode **retourne `true`
en toutes circonstances**, son corps est commenté — c'est une garde décorative que la vraie politique
remplace. Supprimer le fichier s'il n'a plus d'appelant.

**Fin.** Sur le dev : une publication complète de la facturation réussit **avec** le compte de
service ; la même **sans** secret rend 401 sur `field-data`, et son journal le nomme. Puis la sonde
E1 montre **zéro** appel sans `Authorization`.

**Risque.** **Élevé et concentré** — c'est la chaîne vers la facturation. Vector fermé avant que la
facturation sache présenter un jeton, et **la publication du jour tombe**. Sur les deux serveurs,
**la facturation se déploie et se vérifie avant Vector.**

## E5 — ⛔ Retourner le garde-fou

`SurfaceAttendue` retombe à **deux** entrées (`AuthController.WhoAmI`, `DiagController.*`), et le
commentaire est réécrit : les quatre ouvertures ne sont plus « temporaires », elles sont **fermées**,
avec la date.

Et `Les_ouvertures_pour_la_facturation_sont_au_nombre_de_quatre` **n'est pas supprimé — il est
retourné** : un test qui vérifie par réflexion que ces quatre actions portent bien une politique
**nommée**. Supprimer le test laisserait la politique de repli seule gardienne : elle protège contre
l'oubli d'attribut, **pas** contre le remplacement d'une politique nommée par un `[Authorize]` nu —
qui rouvrirait ces quatre routes à l'ambulancier, dont le dossier terrain complet.

---

# 7. 🔴 Décisions qui appartiennent à un humain

| # | Décision | Bloque | Enjeu |
|---|---|---|---|
| ~~**H1**~~ | ~~L'app web consomme-t-elle `ImageUrl` par `fetch` ou par balise directe ?~~ | ~~E3~~ | ✅ **Tranchée le 26/08 : balise `<img src>`**, pour l'écran ambulancier comme pour ceux d'Order et de la facturation. Conséquence assumée : les deux routes de carte mutuelle **restent ouvertes** et sortent du périmètre d'E3 (§6.0). ⚠️ Reste ouverte pour `documents/content`, dont aucun écran n'est identifié — E3 ne concerne plus que celle-là. |
| **H2** | Rendre `us-facturation` confidentiel, ou créer `us-facturation-svc` ? | **E4.a** | Le rendre confidentiel **casserait son flux OIDC navigateur**. Recommandation : **client distinct** — écran et compte de service ont des cycles de vie et des rayons d'explosion différents. |
| **H3** | Créer `usvector-api` (sans rôle `realm-management`) et poser son secret. | **S5** | Purement Keycloak + `web.config`. Le code est prêt et inerte sans. |
| **H4** | Drapeau de réouverture par variable d'environnement sur `documents/content` ? | **E3** | Assurance d'un cycle de déploiement contre une machinerie permanente. **Recommandation : s'en passer** — H1 est tranchée, et la route n'a aucun consommateur identifié. |
| **H5** | Accepter **10 s** en lecture et **15 s** en écriture. | **D1** | Arbitrage **produit** — « au bout de combien de temps l'ambulancier préfère-t-il une erreur à une attente ? » — pas une constante technique. |
| **H6** | Accepter que la politique de repli interdise à tout compte de service d'atteindre l'API mobile. | **E2** | Interdit d'emblée un usage service-à-service futur non prévu, qui devra passer par une politique nommée explicite. **C'est voulu.** |
| ~~**H7**~~ | ~~Par quel compte Order et BillingGateway consultent-ils l'image ?~~ | ~~E3~~ | ✅ **Sans objet depuis H1** : leurs écrans affichent par `<img src>`, donc **aucun compte**. Les deux routes qu'ils utilisent sont ouvertes et le restent ; aucun `azp` à ajouter, aucun dépôt tiers à modifier. La question renaîtra le jour où l'on voudra les fermer. |

---

# 8. Retiré du plan — hors périmètre

*Conservé pour ne pas ré-instruire ces pistes.*

| Ce qui n'est pas ici | Motif |
|---|---|
| **CORS** — `Program.cs` fait `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` | Sujet réel, adjacent à H1, déjà noté en TODO dans le code — mais le mêler à `DEC-6` rendrait toute régression indémêlable. |
| **`Directory.Packages.props`** pour Vector | Hygiène ; chantier G du devplan principal. |
| **`Microsoft.Extensions.Http.Resilience`** | Introuvable dans le parc — **T2**. |
| **L'autorisation fine** | Comme le dit `Identity.Api`, une liste d'`azp` « n'est pas une autorisation fine : c'est une liste d'appelants reconnus, pas une liste de droits ». Vector va un cran plus loin — deux publics, deux politiques — mais reste **en deçà d'un modèle de droits**. À dire, pour que personne ne croie le problème résolu. |
| **Le mapping de la carte mutuelle vers une colonne de facturation** | Écarté par **M7** *(26/08)* : la carte est un document **consultable**, pas une source de colonne. Ce plan n'en traite que l'**accès** — et depuis H1, il n'a plus rien à y faire : les deux routes de consultation sont ouvertes et le restent. |

---

# 9. Suivi

| Étape | Objet | État |
|---|---|---|
| D1 | Timeouts explicites (10 s / 15 s) | ⏳ · 🔴 H5 |
| D2 | Réessai + disjoncteur Polly, fabrique publique | ⏳ |
| D3 | Tests de résilience (4) | ⏳ |
| S0 | Extraire `ParseAuthority` | ⏳ |
| S1 | Fournisseur de jeton (cache, marge, sémaphore) | ⏳ |
| S2 | `DelegatingHandler`, inerte | ⏳ |
| S3 | Rejeu unique sur 401 | ⏳ |
| S4 | Garde-fou de démarrage | ⏳ |
| S5 | Activation | 🔴 H3 |
| E1 | Sonde de surface anonyme + 7 jours de mesure | ⏳ |
| E2 | Deux publics, deux politiques nommées | ⏳ |
| E3 | Fermer `documents/content` — la carte mutuelle en est sortie (H1) | ⛔ E1 |
| E4 | Fermer `field-data` et `Signature` | ⛔ E2 + H2 + facturation |
| E5 | Retourner `AnonymousSurfaceTests` | ⛔ E4 |

---

# 10. Renvois

| Où | Quoi |
|---|---|
| [`devplan.md`](devplan.md) §3.C2 | `DEC-6`, les deux sens — **l'état fait foi là-bas** |
| [`devplan.md`](devplan.md) §3.D | `DEC-7` — idem |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) §2 | le chantier d'origine, et les trois restes hors périmètre |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | M7 et le contrat exposé — les deux routes de consultation ouvertes le 26/08 (§1.2, §6.0) |
| `CaSoft.Erp.USVector.Tests/AnonymousSurfaceTests.cs` | la définition exécutable d'« avoir fini » pour `DEC-6` entrant |

---

**Fin du document**
