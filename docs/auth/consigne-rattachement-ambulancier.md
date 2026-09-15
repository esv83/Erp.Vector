# Consigne transitoire — rattacher un ambulancier à son compte pour qu'il reçoive ses missions

> **Pour** : la régulation et la RH, et quiconque traite un « l'appli ne me montre rien ».
> **Depuis** : 2026-09-13. **Jusqu'à** : la bascule d'Orders sur le carnet d'Identity — cette consigne
> tombe alors d'elle-même (devplan C1).

---

## Pourquoi il faut rattacher deux fois

Le rattachement « ce compte = cette personne » vit aujourd'hui à **deux endroits qui ne se parlent
pas** :

| Où | Alimenté par | Lu par |
|---|---|---|
| Carnet d'**Identity** | l'écran de rattachement d'**Employee** | Employee, et à terme tout l'ERP |
| `PER_KEYCLOAK_MAP` chez **Orders** | **aucun écran** — l'API d'Orders seulement | **l'application terrain Vector** |

**Un ambulancier rattaché uniquement depuis l'écran d'Employee n'existe pas pour Vector.** À la
connexion, il reçoit : *« Compte Keycloak … non rattaché à un personnel. Contactez la régulation. »*
(HTTP 403). Écart mesuré le 05/09 : 155 rattachements chez Orders, 116 chez Identity.

La vraie correction — qu'Orders lise le carnet d'Identity — attend que la RH complète Employee :
273 des 422 personnels actifs d'Orders n'y ont pas de fiche. **Ne pas fabriquer ces fiches depuis
Orders** (consigne d'Identity : doublons au prochain import).

---

## Le geste, en trois appels

Base : `https://api.urgencesante.net/order/`

### 1. Trouver le compte de l'ambulancier

```
GET keycloak/users?search=<nom ou identifiant>&onlyUnlinked=true
```

Relever le `keycloakId` (un Guid) du bon compte. `onlyUnlinked=true` écarte les comptes déjà pris.

### 2. Trouver sa fiche de personnel chez Orders

```
GET personnel?search=<nom>&activeOnly=true
```

Relever son `id` (un Guid). ⚠️ C'est l'identifiant **Orders** du personnel, pas son numéro Employee.

### 3. Rattacher

```
PUT personnel/{id}/keycloak
Content-Type: application/json

{ "keycloakId": "<keycloakId relevé à l'étape 1>" }
```

| Réponse | Sens |
|---|---|
| `204` | Rattaché |
| `404` | Fiche de personnel introuvable |
| erreur de refus | **Ce compte est déjà rattaché à un autre personnel** — vérifier avant d'aller plus loin : c'est souvent un homonyme ou une ancienne erreur |

⚠️ **Un personnel déjà rattaché voit son compte remplacé sans avertissement.** Vérifier d'abord
avec `GET personnel/{id}/keycloak` (404 = aucun compte).

---

## Vérifier que Vector le voit

```
GET personnel/by-keycloak/{keycloakId}
```

`200` avec l'identifiant du personnel : l'ambulancier accède à l'application. S'il voit ensuite
« Votre équipage n'est pas encore composé par la régulation », le rattachement est bon — c'est son
équipage qui manque.

---

## Et côté Employee

Faire **aussi** le rattachement dans l'écran d'Employee quand la personne y a une fiche : c'est le
référentiel qui restera. Si elle n'y a pas de fiche, le signaler à la RH — c'est exactement ce qui
bloque la bascule.
