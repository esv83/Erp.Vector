# Spécification Fonctionnelle et Technique — Utilisateurs connectés à Vector

## Module USVector (application mobile ambulancier) — supervision de la présence

> **Date** : 2026-07-15 · **Statut** : conception (aucun code). · **Portée** : instrumentation côté API Vector + restitution.

---

# 1. Objectif

Fournir, à tout instant, la **liste des utilisateurs actuellement connectés à Vector** (ambulanciers), pour la supervision / le support / la régulation.

Contrainte structurante : l'API Vector est **stateless (JWT bearer Keycloak)** — pas de session serveur, pas de WebSocket, et **aucun suivi de présence n'existe aujourd'hui**. « Connecté » n'est donc pas une donnée native : il faut **la définir** puis **l'instrumenter**.

---

# 2. Définition de « connecté » (décision structurante)

Trois sens possibles, mesurant des choses différentes :

| Sens | Mesure | Coût |
|---|---|---|
| **A — App active** | L'utilisateur a émis une requête authentifiée récemment (fenêtre glissante) | Instrumentation à créer |
| **B — Session SSO** | L'utilisateur a une session Keycloak ouverte pour le client `us-ambulance` | Appel Admin Keycloak à créer |
| **C — En service** | L'utilisateur est membre d'un équipage démarré et non clôturé (planning) | Déjà exposé (Orders.Api) |

**Décision retenue (à confirmer)** : **A comme cœur** (« qui utilise réellement l'app »), **enrichi de C** (croisement avec l'état de service). **B** est documenté en alternative (§8), non retenu en v1.

Justification : A est le seul signal fidèle à l'usage réel de l'app ; C, gratuit, apporte le sens métier (« en tournée ») ; B mesure une session SSO qui peut survivre app fermée (peu fiable pour « connecté »).

---

# 3. Périmètre fonctionnel

**Couvre :**
- l'enregistrement de la **dernière activité** par utilisateur à chaque appel authentifié ;
- la **restitution** de la liste des utilisateurs actifs dans une fenêtre paramétrable (défaut ~10 min) ;
- pour chaque utilisateur : identité (personnel + compte Keycloak), **horodatage de dernière activité**, **équipage(s)** courant(s), et **état de service** (croisement C) ;
- un **endpoint de supervision** (réservé régulateur/admin).

**Ne couvre pas (v1) :**
- l'historique/analytics de connexion dans le temps (rapports, durées de session) ;
- la déconnexion « propre » (l'app ne signale pas sa fermeture — présence = fenêtre glissante) ;
- la lecture des sessions Keycloak (sens B) ;
- une UI temps réel poussée (WebSocket/SignalR) — la restitution est en **pull** (polling) ;
- la géolocalisation des utilisateurs.

---

# 4. Approche technique retenue (A — suivi d'activité)

## 4.1 Point d'instrumentation
La chaîne d'identité est **`sub` (JWT Keycloak) → `PER_ID` (via Orders.Api `/personnel/by-keycloak/{sub}`, table `PER_KEYCLOAK_MAP`) → équipages actifs**.

Le **point de passage idéal existe déjà** : `CrewAccess.ResolvePersonnel` (`CaSoft.Erp.USVector.Api/Infrastructure/CrewAccess.cs`) est traversé sur quasiment tous les appels crew-scoped et dispose déjà de `sub` **et** `PER_ID` résolus. On y **estampille** la présence (ou via un middleware léger si on veut couvrir 100 % des routes authentifiées). Écriture **non bloquante** (best-effort, hors chemin critique de la requête).

## 4.2 Donnée enregistrée
Un enregistrement « dernière activité » par utilisateur (upsert) :
- `PER_ID` (clé), `sub` (Keycloak) ;
- `last_seen_at` (UTC) ;
- `crew_id` courant (si connu au moment de l'appel) ;
- (optionnel) `device` / `app_version` / `ip`, si présents dans la requête.

## 4.3 Fenêtre d'activité
« Connecté » = `last_seen_at` dans les **N dernières minutes** (paramétrable, défaut **10**). C'est une **heuristique** : une API stateless ne détecte pas la fermeture de l'app.

## 4.4 Restitution
Endpoint **`GET /api/presence`** (réservé régulateur/admin, gated comme le diag existant) → liste des utilisateurs `last_seen_at ≥ now − N`, enrichie de l'identité (nom via annuaire) et de l'état de service (§5).

## 4.5 Point dur — multi-instance
Si l'API Vector tourne en **plusieurs instances / réplicas**, un `IMemoryCache` par process **ne convient pas** (vue partielle, expiration silencieuse). Le store de présence doit être **partagé** :
- option **table `MOB_PRESENCE`** (BD Mobile) — simple, durable, cohérent avec l'archi existante ; ou
- option **Redis** — si volume/fréquence élevés et TTL natif souhaité.

→ **À trancher** selon la topologie de déploiement réelle (mono ou multi-instance).

---

# 5. Enrichissement métier (croisement C — en service)

Sans stockage neuf, on croise la présence (A) avec l'**état de service** lu depuis Orders.Api :
`GET /crews?date=aujourd'hui` → filtrer `ClCrew.IsSelectableAt(now)` (démarré, non clôturé, < 18 h) → membres (`PER_ID`).

Restitution consolidée, trois cas lisibles :
- **En service + app active** → nominal ;
- **En service + app silencieuse** → présence douteuse (à surveiller) ;
- **App active hors service** → connecté sans équipage démarré.

---

# 6. Identité & clés

- Clé stable par utilisateur : **`sub`** (Guid Keycloak) → **`PER_ID`** (1:1 via `PER_KEYCLOAK_MAP`).
- Libellé humain : nom/prénom via l'annuaire (`GET /keycloak/users` côté Orders, ou `KeycloakAdminClient` côté Vector).
- `crew_id` : contexte opérationnel (un utilisateur peut appartenir à plusieurs équipages dans la journée).

---

# 7. Confidentialité / rétention

- Donnée de présence = **donnée de suivi d'activité d'un salarié** → cadrage RH/RGPD requis (finalité « supervision opérationnelle », information des utilisateurs).
- **Rétention courte** : la présence est un état courant, pas un journal → purge/TTL (ex. enregistrement écrasé en continu ; pas d'historique en v1).
- Endpoint de restitution **restreint** (régulateur/admin), jamais exposé au terrain.

---

# 8. Alternative documentée (B — sessions Keycloak)

Le compte de service `KeycloakAdminClient` (déjà câblé pour la résolution username→sub) pourrait appeler l'API Admin `GET /admin/realms/delesse/clients/{us-ambulance}/user-sessions` → utilisateurs avec session SSO active.
- ✅ **Zéro stockage** côté nous, source d'autorité SSO.
- ⚠️ Session SSO **≠** app active (token valide/rafraîchissable app fermée ; dépend des réglages de session/offline tokens du realm). Nécessite d'ajouter le rôle `view` sessions au compte de service.

→ Non retenu en v1 (mesure la mauvaise chose pour « connecté »), mais **complément possible** pour un usage sécurité/gouvernance.

---

# 9. Risques & points ouverts

- **Notion de « connecté » à confirmer** (A / A+C / B) — pilote tout le design.
- **Topologie mono vs multi-instance** — pilote le choix du store (mémoire vs `MOB_PRESENCE`/Redis).
- **Fenêtre N** — compromis fraîcheur vs faux négatifs (app en veille).
- **Surcoût par requête** — l'estampillage doit rester best-effort/non bloquant.
- **RGPD/RH** — validation de la finalité et de l'information des salariés.

---

# 10. Découpage indicatif (lots)

| Lot | Contenu |
|---|---|
| PR-1 | Store de présence (choix mémoire vs `MOB_PRESENCE`/Redis) + upsert best-effort dans `CrewAccess.ResolvePersonnel` |
| PR-2 | `GET /api/presence` (fenêtre N) + restriction d'accès (régulateur/admin) |
| PR-3 | Enrichissement identité (annuaire) + croisement état de service (C) |
| PR-4 | Cadrage rétention/RGPD + paramétrage (N, TTL) |
| PR-5 *(option)* | Complément B (sessions Keycloak) pour vue sécurité |
