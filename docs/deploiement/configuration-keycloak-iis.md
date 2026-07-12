# Configuration USVector.Api — Keycloak & IIS

Guide de configuration de l'API mobile **CaSoft.Erp.USVector.Api** : authentification
**Keycloak** (chapitre 1) et déploiement **IIS** sur le serveur de dev (chapitre 2).

---

## 0. Modèle de configuration

La configuration se lit dans cet ordre (le dernier gagne) :

1. `appsettings.json` — **versionné**, valeurs par défaut, **secrets = `__SET_VIA_ENV__`** (placeholder).
2. `appsettings.{Environment}.json` — surcharge par environnement (`ASPNETCORE_ENVIRONMENT`).
3. **Variables d'environnement** — surcharge finale, **c'est là qu'on met les secrets** et les valeurs propres au serveur.

> **Nommage des variables d'environnement** : la hiérarchie JSON `A:B` s'écrit `A__B`
> (double underscore). Ex. `ConnectionStrings:MobileDb` → `ConnectionStrings__MobileDb`,
> `Keycloak:AdminClientSecret` → `Keycloak__AdminClientSecret`.

### Clés de configuration (récapitulatif)

| Clé | Source recommandée | Exemple | Rôle |
|---|---|---|---|
| `ConnectionStrings:MobileDb` | **env** (secret) | `Server=192.168.1.109,1440;Database=BD_ERP_MOBILE_APP;User Id=ErpAccount;Password=***;TrustServerCertificate=True` | BD Mobile (sessions, timeline, signatures) |
| `OrdersApi:BaseUrl` | env / appsettings | `https://.../order/` | ERP lu en HTTP. **⚠ doit finir par `/`** |
| `AddressApi:BaseUrl` | appsettings | `http://localhost:5100/api/v1/` | API adresses |
| `Keycloak:Enabled` | env / appsettings | `true` | Active la validation JWT |
| `Keycloak:RequireHttpsMetadata` | appsettings | `true` | Exige HTTPS pour les métadonnées OIDC |
| `Keycloak:DisableValidation` | **dev only** | `false` | ⚠ décode sans vérifier signature — **jamais en prod** |
| `Keycloak:Authority` | **env** | `https://auth.ade-dev.fr/realms/delesse` | Realm — **utilisé par la recherche par username** (voir §1.3) |
| `Keycloak:AdminClientId` | env / appsettings | `usvector-diag` | Service account (recherche username) |
| `Keycloak:AdminClientSecret` | **env** (secret) | `***` | Secret du service account |
| `MobileIdentityCache:PersonnelMinutes` | appsettings | `30` | TTL cache `sub→PER_ID` |
| `MobileIdentityCache:ActiveCrewsMinutes` | appsettings | `15` | TTL cache crews actifs |
| `Diagnostics:Enabled` | **env** (dev/staging) | `true` | Active l'outil de diag `/api/diag` |
| `GpsGate:*`, `Sirus:*` | env (secrets) | — | Connecteurs géoloc / régulation |

---

## Chapitre 1 — Keycloak

### 1.1 Vue d'ensemble

- **Serveur** : `https://auth.ade-dev.fr`
- **Realm** : `delesse`
- **Deux clients** à configurer :
  1. **Client de l'app mobile** — celui avec lequel l'ambulancier se connecte ; il émet l'*access token* que USVector.Api valide (§1.2).
  2. **`usvector-diag`** — *service account* utilisé uniquement par l'outil de diagnostic pour résoudre un `username` en `sub` (§1.3).

```
Ambulancier ──login──► [client app mobile] ──access token (aud: us-ambulance)──► USVector.Api (valide le JWT)
Développeur ──diag──►  [client usvector-diag / service account] ──Admin API──► liste users (id = sub)
```

### 1.2 Client de l'app mobile (validation JWT)

USVector.Api valide chaque token porté par `Authorization: Bearer …` :

| Paramètre attendu | Valeur | Où c'est vérifié |
|---|---|---|
| **Authority (issuer)** | `https://auth.ade-dev.fr/realms/delesse` | `Program.cs` (voir ⚠ ci-dessous) |
| **Audience (`aud`)** | `us-ambulance` | `Program.cs` |
| **Signature** | JWKS du realm (récupéré via l'Authority) | middleware JwtBearer |
| **Claim `sub`** | l'ID utilisateur Keycloak (= le sub) | lu par l'app (résolution PER_ID) |

**Côté Keycloak**, sur le client de l'app mobile :

1. Le token doit porter **`aud: us-ambulance`**. Si ce n'est pas le cas nativement, ajouter un
   *Audience mapper* (Client scopes → mapper « Audience » → *Included Client Audience* = `us-ambulance`,
   *Add to access token* = ON).
2. `RequireHttpsMetadata = true` en prod → l'Authority **doit** être joignable en HTTPS par le
   serveur (le middleware récupère la config OIDC + les clés JWKS au démarrage / à la volée).

> ⚠ **Limitation connue** : dans `Program.cs`, `options.Authority` et `options.Audience` sont
> **codés en dur** (`https://auth.ade-dev.fr/realms/delesse` et `us-ambulance`) — les clés
> `Keycloak:Authority` / `Keycloak:Audience` d'`appsettings.json` sont commentées pour ces deux
> réglages. Changer de realm/audience pour la **validation JWT** nécessite donc aujourd'hui une
> modif de code. (Recommandation : re-brancher ces deux options sur la configuration.)
> À l'inverse, la **recherche par username** (§1.3) lit bien `Keycloak:Authority` **depuis la config**.

#### Mode dev sans Keycloak joignable

Pour tester la lecture du token sans contacter Keycloak (Authority injoignable en local) :

```jsonc
"Keycloak": { "Enabled": true, "DisableValidation": true }
```

`DisableValidation=true` décode le token **sans vérifier** signature / issuer / audience / expiration.
**Uniquement en dev** — ne jamais activer en prod.

### 1.3 Service account pour la recherche par username (`usvector-diag`)

L'outil de diag peut résoudre un `username` → `sub` via l'**Admin API** de Keycloak. Cela nécessite
un client *service account* dédié.

**Étapes dans la console admin Keycloak (realm `delesse`) :**

1. **Clients → Create client**
   - Client ID : `usvector-diag`
   - Client authentication : **ON** (client confidentiel)
   - Authentication flow : décocher *Standard flow*, cocher **Service accounts roles**
2. **Save**, puis onglet **Credentials** → copier le **Client secret**.
3. Onglet **Service account roles** → **Assign role** → filtrer sur le client `realm-management` →
   assigner **`view-users`** et **`query-users`**.
4. Renseigner côté serveur (§2) :
   - `Keycloak__AdminClientId = usvector-diag`
   - `Keycloak__AdminClientSecret = <secret copié>`
   - `Keycloak__Authority = https://auth.ade-dev.fr/realms/delesse` *(indispensable : la valeur d'`appsettings.json` est un placeholder)*

> Tant que ce n'est pas configuré, l'endpoint `/api/diag/resolve-user` répond **501** avec un message
> explicite, et on peut toujours coller un `sub` à la main dans la page de diag.

**Sécurité** : ce service account peut **lister les utilisateurs** du realm. Il n'est appelé que par
l'outil de diag, lui-même réservé au dev (§2.7). Le secret se met **en variable d'environnement**,
jamais dans un fichier versionné.

### 1.4 Note — claim `per_id` (non retenu)

Le raccourci « porter le PER_ID dans un claim `per_id` » a été **évalué puis écarté** (cf.
`docs/auth/optimisation-chaine-authentification.md`) : on conserve la résolution `sub→PER_ID` via
Orders.Api (cachée, TTL 30 min). **Aucune configuration Keycloak n'est requise pour ça.**

---

## Chapitre 2 — Serveur IIS

### 2.1 Prérequis

- **IIS** avec le module **ASP.NET Core Module V2** → installer le **.NET 8 Hosting Bundle**
  (`dotnet-hosting-8.x-win.exe`) puis `iisreset`.
- Un **Application Pool** dédié en **« No Managed Code »** (l'app tourne en out-of-process/in-process
  via le module natif, pas via le CLR géré d'IIS).
- Accès réseau depuis le serveur vers : SQL Server `192.168.1.109,1440`, l'API Orders, et Keycloak
  (`auth.ade-dev.fr` en HTTPS).

### 2.2 Architecture de déploiement

USVector.Api est déployée en **sous-application IIS** sous un site, au chemin **`/vector`** :

```
https://<serveur>/vector            → USVector.Api (sous-application, App Pool dédié)
https://<serveur>/vector/swagger    → Swagger (endpoint relatif, compatible sous-chemin)
https://<serveur>/vector/api/...     → endpoints
```

> Le Swagger et la page de diag utilisent des chemins **relatifs / basés sur `PathBase`** : ils
> fonctionnent sous le sous-chemin `/vector` sans réglage supplémentaire.

### 2.3 Publication

Publier en **déploiement portable** (framework-dependent), **sans RID** :

```bash
dotnet publish CaSoft.Erp.USVector.Api/CaSoft.Erp.USVector.Api.csproj ^
  -c Release -p:PublishProfile=IIS-DevServer
```

> ⚠ **Ne pas publier en `win-x64` (RID-specific)**. Une publication RID charge la façade
> « PlatformNotSupported » de `Microsoft.Data.SqlClient` (l'implémentation Windows sous `runtimes/win`
> est ignorée) → **SQL Server injoignable**. Le profil `IIS-DevServer.pubxml` est **portable** (RID
> retiré) — c'est voulu.

Copier le résultat de publication vers le dossier physique de la sous-application (ex. partage UNC),
en **conservant le `web.config`** existant du serveur (voir §2.4).

### 2.4 `web.config` (géré manuellement, hors git)

Le `web.config` **n'est pas versionné** (il porte les secrets). Il est maintenu **manuellement** sur
le serveur. Exemple pour un déploiement **portable** (processPath `dotnet` + la DLL) :

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\CaSoft.Erp.USVector.Api.dll"
                  hostingModel="inprocess"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Staging" />

          <!-- Secrets & config serveur -->
          <environmentVariable name="ConnectionStrings__MobileDb"
            value="Server=192.168.1.109,1440;Database=BD_ERP_MOBILE_APP;User Id=ErpAccount;Password=***;TrustServerCertificate=True" />
          <environmentVariable name="OrdersApi__BaseUrl" value="https://<orders-host>/order/" />

          <!-- Auth Keycloak -->
          <environmentVariable name="Keycloak__Enabled" value="true" />
          <environmentVariable name="Keycloak__RequireHttpsMetadata" value="true" />
          <environmentVariable name="Keycloak__Authority" value="https://auth.ade-dev.fr/realms/delesse" />

          <!-- Recherche par username (outil diag) -->
          <environmentVariable name="Keycloak__AdminClientId" value="usvector-diag" />
          <environmentVariable name="Keycloak__AdminClientSecret" value="***" />

          <!-- Outil de diagnostic (dev/staging uniquement) -->
          <environmentVariable name="Diagnostics__Enabled" value="true" />

          <!-- Connecteurs (si utilisés) -->
          <environmentVariable name="GpsGate__User" value="***" />
          <environmentVariable name="GpsGate__Password" value="***" />
          <environmentVariable name="Sirus__Host" value="***" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

> `inheritInChildApplications="false"` + `<location path=".">` : indispensable pour une
> **sous-application**, afin de ne pas hériter (et casser) la config du site parent.

### 2.5 Points de configuration critiques (bugs runtime déjà rencontrés)

| Piège | Symptôme | Correctif |
|---|---|---|
| `OrdersApi:BaseUrl` **sans `/` final** | **500** sur `joblist` (le segment `/order` est perdu à la résolution d'URI relative) | Toujours terminer par `/` : `https://…/order/` |
| `OrdersApi:BaseUrl` = `http://localhost/order` | **404** (depuis `w3wp`, `localhost` tombe sur un autre site IIS) | Pointer une URL **réellement joignable** depuis le process du pool |
| Publication **RID `win-x64`** | SQL Server injoignable (`PlatformNotSupported`) | Publier **portable** (profil `IIS-DevServer`) |
| Dossier `logs/` non inscriptible | Pas de logs fichier / erreur au démarrage NLog | Donner le droit **écriture** à l'identité du pool sur `.\logs` |

### 2.6 Identité du pool & permissions

L'identité de l'App Pool (ex. `IIS AppPool\<nom-du-pool>` ou un compte de service) doit avoir :

- **Lecture/exécution** sur le dossier de l'application.
- **Écriture** sur le sous-dossier **`logs/`** (NLog écrit
  `${aspnet-appbasepath}/logs/usvector-api-<date>.log`) et sur `.\logs\stdout` si `stdoutLogEnabled`.
- **Accès réseau** vers SQL (`192.168.1.109,1440`), Orders.Api et Keycloak.
  *(L'accès SQL passe par le login SQL `ErpAccount` dans la chaîne de connexion — pas d'auth Windows
  requise pour la BD, mais la connectivité réseau doit être ouverte.)*

### 2.7 Activer l'outil de diagnostic sur le serveur

Le serveur ne tourne pas en `ASPNETCORE_ENVIRONMENT=Development` → activer explicitement :

```
Diagnostics__Enabled = true
Keycloak__Authority  = https://auth.ade-dev.fr/realms/delesse   (pour la recherche par username)
Keycloak__AdminClientId / Keycloak__AdminClientSecret            (service account §1.3)
```

Accès (attention au **sous-chemin `/vector`**) :

- Page visuelle : **`https://<serveur>/vector/api/diag`**
- Endpoint brut : `https://<serveur>/vector/api/diag/crew-chain?sub=<guid>`

> ⚠ **Dev / staging uniquement.** Le diag expose la résolution d'identité et permet de lister des
> utilisateurs Keycloak. **Mettre `Diagnostics__Enabled=false` (ou le retirer) en production** — sinon
> l'endpoint renvoie 404, ce qui est le comportement voulu hors dev.

### 2.8 Vérification post-déploiement (checklist)

1. `GET /vector/swagger` → l'UI Swagger s'affiche.
2. `GET /vector/api/auth/whoami` **sans token** → **401** `{"reason":"no_token"}` (l'auth répond).
3. `GET /vector/api/auth/whoami` **avec un vrai token** → **200** avec `sub` renseigné.
4. `GET /vector/api/diag` → la page de diag se charge (si `Diagnostics__Enabled=true`).
5. Dans la page : **Chercher par username** → un utilisateur remonte (service account OK) → le `sub`
   se remplit et la chaîne se résout jusqu'aux crews actifs.
6. Le fichier `logs/usvector-api-<date>.log` se crée et se remplit (droits d'écriture OK).

---

## Annexe — Limitations connues / à améliorer

- **JWT `Authority`/`Audience` codés en dur** dans `Program.cs` (cf. §1.2) — à re-brancher sur
  `Keycloak:Authority` / `Keycloak:Audience` pour rendre la config pleinement pilotable par
  environnement.
- **`Keycloak:Authority` dans `appsettings.json` est un placeholder** (`keycloak.placeholder`) : la
  recherche par username **ne marche que si on surcharge cette clé** par la vraie valeur (env).
- Le `web.config` étant hors git, **documenter/versionner un `web.config.template`** (sans secrets)
  faciliterait les redéploiements.
