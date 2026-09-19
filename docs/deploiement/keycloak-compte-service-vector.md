# Keycloak — comptes de service pour C2 (Vector → Orders, facturation → Vector)

> **But** : donner à Vector un jeton de service pour appeler Orders.Api, et créer celui de la
> facturation pour appeler Vector. Suite de [`devplan.md`](../../devplan.md) §C2, étapes 1 et 2.
> **Realm** : `delesse` sur `https://auth.ade-dev.fr` · **Point de jeton** (vérifié le 2026-09-13) :
> `https://auth.ade-dev.fr/realms/delesse/protocol/openid-connect/token` — `client_credentials` accepté.
>
> **Convention du realm** : les API des modules de l'ERP sont nommées **`erp-<module>-api`**
> (`erp-employee-api`, `erp-order-api`) ; les applications utilisées par des personnes, `us-…`.
>
> ⚠️ Libellés de la console d'administration récente (Keycloak ≥ 19). Sur une version plus ancienne,
> « Client authentication » s'appelle « Access Type : confidential » et « Service accounts roles »
> s'appelle « Service Accounts Enabled ».
>
> 🔒 **Aucun secret dans un fichier suivi par git**, ni dans un ticket, ni dans une conversation.

---

## Étape 1 — Le client de service de Vector : `erp-vector-api`

> **Déjà créé sous le nom `us-vector-api`** (13/09) : onglet **Settings** → champ **Client ID** →
> `erp-vector-api` → **Save**. Le secret ne change pas. Sinon, créer le client comme suit.

1. Console d'administration → realm **`delesse`** → **Clients** → **Create client**.
2. **General settings**
   - *Client type* : `OpenID Connect`
   - *Client ID* : **`erp-vector-api`**
   - *Name* : `erp-vector-api`
3. **Capability config**
   - *Client authentication* : **On** (client confidentiel)
   - *Authorization* : Off
   - *Authentication flow* : décocher tout **sauf** **Service accounts roles** (coché)
     — ni *Standard flow*, ni *Direct access grants* : personne ne se connecte avec ce client.
4. **Login settings** : laisser vide → **Save**.
5. Onglet **Credentials** → *Client Authenticator* `Client Id and Secret` → **copier le secret**.
6. Onglet **Service account roles** : **ne rien ajouter**. Orders n'exige aucun rôle ; le jeton dit
   seulement *qui* appelle (son `azp` vaudra `erp-vector-api`).

### Tester un client de service depuis un poste Windows

```powershell
$client = "erp-vector-api"   # ou erp-billinggateway-api pour l'étape 3
$secret = Read-Host "Secret de $client" -AsSecureString
$clair  = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret))
$r = Invoke-RestMethod -Method Post `
  -Uri "https://auth.ade-dev.fr/realms/delesse/protocol/openid-connect/token" `
  -Body @{ grant_type = "client_credentials"; client_id = $client; client_secret = $clair }
$charge = $r.access_token.Split('.')[1].Replace('-', '+').Replace('_', '/')
$charge = $charge.PadRight($charge.Length + (4 - $charge.Length % 4) % 4, '=')
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($charge)) | ConvertFrom-Json | Select-Object azp, exp
```

Attendu : `azp` = le nom du client. `401 unauthorized_client` : *Service accounts roles* n'est pas
coché. `401 invalid_client` : secret faux.

---

## Étape 2 — Poser le secret de Vector sur son serveur

Fichier `\\192.168.1.112\prod_api\Vector.Api\web.config`, bloc `<environmentVariables>` (géré à la
main, jamais régénéré par la publication) — ajouter **deux lignes** :

```xml
<environmentVariable name="OrdersApi__ServiceAccount__ClientId" value="erp-vector-api" />
<environmentVariable name="OrdersApi__ServiceAccount__ClientSecret" value="LE-SECRET-COPIÉ" />
```

- **Pas de `TokenEndpoint`** : Vector le déduit de `Keycloak:Authority`, et l'adresse déduite est
  exactement celle du realm.
- Faire une copie du fichier avant. L'enregistrer **recycle l'application** : courte coupure.

**Ce qui change** : Vector pose un jeton sur ses appels à Orders. **Orders ne le vérifie pas encore** :
rien ne change pour le terrain. Si le realm ne répond pas, l'appel part sans jeton et une erreur est
journalisée — la joblist ne tombe pas.

**Vérification** (je peux la faire après l'enregistrement), dans `logs/usvector-api-<date>.log` :
- ✅ `Jeton de service obtenu pour erp-vector-api, valable jusqu'à …` (niveau DEBUG)
- ❌ `Jeton de service de Vector indisponible — … envoyé sans jeton.` → relire l'étape 1

---

## Étape 3 — Le client de service de la facturation : `erp-billinggateway-api`

La facturation n'a **aucun client** aujourd'hui : dans BillingGateway, Keycloak est désactivé et le
nom `us-facturation` de sa configuration n'a jamais été créé dans le realm.

1. Créer **`erp-billinggateway-api`** avec **exactement les réglages de l'étape 1** (confidentiel,
   *Service accounts roles* seul, aucun rôle).
2. Copier son secret — il servira à **BillingGateway**, pas à Vector : il ira dans la configuration
   du serveur de BillingGateway, au moment où son dépôt posera le jeton.
3. Tester avec le script ci-dessus (`$client = "erp-billinggateway-api"`) : attendu
   `azp = erp-billinggateway-api`.

Vector attend déjà ce nom : `Keycloak:ServiceAzp` = `erp-billinggateway-api` **à partir du prochain
déploiement de Vector** (la version en production accepte encore l'ancien nom `us-facturation`, qui
n'existe pas — donc rien).

---

## Et ensuite

| Étape | Qui | Effet |
|---|---|---|
| ✅ Déployer Vector (nouveau `ServiceAzp`) | dépôt Vector | fait le 13/09 |
| ✅ BillingGateway pose son jeton sur ses appels à Vector | dépôt BillingGateway | fait le 19/09 |
| ✅ Vérifier en production `JWT validé … azp=erp-billinggateway-api` | journal de Vector | constaté le 19/09 à 11:48 |
| ✅ Fermer les quatre routes anonymes | dépôt Vector | en production le 19/09 à 16:46, aucun refus constaté |
| Orders exige un jeton du terrain | dépôt Orders | son plan attendait ce jeton de Vector |
