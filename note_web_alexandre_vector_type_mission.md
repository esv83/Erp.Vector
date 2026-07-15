# 🖥️ Note UI Web Vector — Context de la mission : verrou régulateur + liste filtrée (pour Alexandre)

> **Date** : 2026-07-14
> **Pour** : Alexandre, **dev web de l'UI Vector** (écran ambulancier).
> **Objet** : le **context de la mission** (aujourd'hui « contrat » MOB-13, `GET/POST /api/Contract/{jobId}`) devient **piloté par l'Order**.
> Deux changements d'écran : (1) la **liste est filtrée serveur** (agence/mode), (2) un **verrou** peut la mettre en **lecture seule**.
> **Statut** : le **back Order est prêt** (endpoints OC-9). La **bascule côté API Vector est à venir (Phase 2)** — cette note
> te permet de **préparer l'UI** dès maintenant. Rien n'est cassé tant que la bascule n'est pas livrée.
> **Voir aussi** : `note_vector_orderContext_mission.md` (côté intégrateur Vector) et `Erp.Order/note_front_jules_order_context.md` (côté régulateur).

> 🔤 **Vocabulaire harmonisé (important)** — on parlait de « type de mission » / « contrat » ; on dit désormais **« context de la mission »**.
> Son identifiant s'appelle **`contextOrderId`** (côté code : `ContextOrder` / `ContextOrderId`). Dans cette note, « context » = ce que
> l'ambulancier choisit (Centre 15, CPAM…), et `contextOrderId` = son `id`.

Salut Alexandre 👋

Le **context** que l'ambulancier choisit sur une mission n'est plus un catalogue **autonome** Vector : il vient
**de la commande (Order)**. Concrètement, la **régulation peut fixer** le context → dans ce cas l'ambulancier **ne peut plus le
changer** (verrou). Sinon, il choisit dans une **liste déjà filtrée** selon l'agence et le mode de transport de la mission.

**De quoi on parle — le « context de la mission »** (catalogue **configurable en base**, ne le code pas en dur) :

| `contextOrderId` | code | libellé affiché (`display`) |
|---|---|---|
| 1 | `CPAM` | **CPAM** |
| 2 | `ART80` | **Article 80** |
| 3 | `ASSISTANCE` | **Assistance** |
| 4 | `CENTRE15` | **Centre 15** |
| 5 | `SECOURS_PISTE` | **Secours sur piste** |
| 6 | `TELE_ALARME` | **Télé-Alarme** |
| 7 | `NON_PRIS_EN_CHARGE` | **Non pris en charge** |

> C'est **ça** que l'ambulancier choisit dans le sélecteur. La **liste réellement proposée** pour une mission donnée est un
> **sous-ensemble** : ex. « Secours sur piste » n'apparaît que pour les agences/modes concernés (montagne, ambulance…),
> « Centre 15 » pour l'urgence, etc. Le serveur fait ce tri — tu affiches ce que tu reçois.

---

## 1. Ce qui change à l'écran (UX)

| # | Avant (aujourd'hui) | Après (cible) |
|---|---|---|
| 1 | La liste des contexts contient **tout** le catalogue | La liste est **filtrée serveur** (agence + mode de la mission) — tu affiches **ce que tu reçois**, sans re-filtrer |
| 2 | Aucune notion de verrou : l'ambulancier choisit toujours | Un **verrou** (`locked`) peut passer le sélecteur en **lecture seule** (context fixé par la régulation) |
| 3 | Si rien n'est choisi → **défaut = 1er context actif** | **Plus de défaut auto** : « non renseigné » est un état valide (le régulateur ou l'ambulancier posera le context) |

👉 Tes 3 chantiers : **désactiver** le sélecteur quand `locked`, **retirer** la logique « défaut = 1er », **gérer** un refus au save quand c'est verrouillé.

---

## 2. L'écran « context de la mission »

**Sélecteur** : `<select>` alimenté par la liste renvoyée (déjà triée/filtrée). Valeur = `contextOrderId`, libellé = `display`, option courante = celle avec `isSelected: true`.

**État verrouillé (`locked: true`)** — *ex. mission d'urgence : la régulation a fixé « Centre 15 »* :
- **Désactive** le `<select>` (grisé, non éditable), affiché sur « **Centre 15** ».
- Affiche un **indicateur cadenas** + tooltip « Context fixé par la régulation ».
- N'envoie **aucun** `POST` de changement (il serait refusé — cf. §3).

**État libre (`locked: false`)** — *ex. mission VSL programmée non fixée* :
- Sélecteur éditable, options = ce que renvoie le serveur (ex. **CPAM**, **Article 80**, **Assistance**) ; au choix, `POST /api/Contract/{jobId}` avec le `contextOrderId`.
- Si « non renseigné » (aucun `isSelected`), n'auto-sélectionne **pas** le premier (ex. ne force pas « CPAM ») — laisse l'utilisateur choisir.

---

## 3. Contrat API Vector (ce que tu consommes)

### `GET /api/Contract/{jobId}` — liste + état

**Aujourd'hui** (liste plate, tout le catalogue) :
```jsonc
[
  { "id": 1, "display": "CPAM",       "isSelected": true },
  { "id": 2, "display": "Article 80", "isSelected": false },
  { "id": 4, "display": "Centre 15",  "isSelected": false }
]
```

**Cible (Phase 2)** — la liste est **filtrée** (agence/mode) et un **`locked`** apparaît. Comme un flag global ne rentre pas dans un tableau, la réponse devient un **objet**, et on harmonise les noms (`contextOrderId`, `contextOrders`). *Exemple : mission VSL programmée, non fixée par la régulation* :
```jsonc
{
  "locked": false,                 // ⭐ NOUVEAU — true ⇒ sélecteur en lecture seule
  "contextOrderId": null,          // context effectif (null = non renseigné)
  "contextOrders": [               // déjà filtré agence + mode ; à afficher tel quel
    { "id": 1, "display": "CPAM",       "isSelected": false },
    { "id": 2, "display": "Article 80", "isSelected": false },
    { "id": 3, "display": "Assistance", "isSelected": false }
  ]
}
```
*Variante **verrouillée** — mission d'urgence, la régulation a fixé « Centre 15 »* :
```jsonc
{
  "locked": true,                  // 🔒 sélecteur en lecture seule
  "contextOrderId": 4,             // Centre 15
  "contextOrders": [
    { "id": 4, "display": "Centre 15",         "isSelected": true },
    { "id": 5, "display": "Secours sur piste", "isSelected": false }
  ]
}
```
> ⚠️ **À coordonner** : (a) le **passage tableau → objet**, et (b) un éventuel renommage de la route `/api/Contract` → `/api/ContextOrder`
> pour coller au vocabulaire. Je te préviens avant de livrer la Phase 2 et on cale ça ensemble. Si tu préfères garder le tableau +
> un endpoint séparé `GET /api/Contract/{jobId}/state` pour `locked`, c'est jouable aussi — dis-moi ce qui t'arrange côté front.

### `POST /api/Contract/{jobId}` — choix ambulancier

Body inchangé (le `contextOrderId`). Nouveaux retours :

| HTTP | Quand | Réaction UI |
|---|---|---|
| **200/OK** | choix accepté (ex. l'ambulancier passe sur **Article 80**) | rafraîchis l'écran |
| **409** | context **verrouillé** par la régulation (ex. « **Centre 15** » a été fixé) | message « Context fixé par la régulation, non modifiable » + re-`GET` pour resynchroniser (`locked` devrait être `true`) |
| **400** | context **non applicable** (agence/mode) ou inactif (ex. « **Secours sur piste** » envoyé sur une mission VSL urbaine) | message « Ce context n'est pas disponible pour cette mission » (ne devrait pas arriver si tu n'envoies que des `contextOrderId` de la liste) |

> Idéalement, tu **n'atteins jamais** le 409 : lis `locked` au `GET` et désactive le sélecteur en amont. Le 409 reste un **garde-fou** serveur.

> ℹ️ **Côté Order (la source)**, le même vocabulaire est déjà en ligne : catalogue `GET /referentiels/context-orders`,
> endpoints mission `GET`/`PATCH /missions/{id}/contextOrder` (champs `contextOrderId`, `contextOrderCode`, `contextOrderDisplay`,
> `locked`, `availableContextOrders`). L'API Vector projettera ça en Phase 2.

---

## 4. Récupérer les attributs du context (FormStructure)

Une fois le context établi (fixé par la régul **ou** choisi par l'ambulancier), les **attributs** à afficher/saisir dépendent de ce context.
Bonne nouvelle : **le mécanisme existe déjà** et **ne change pas pour toi**.

**Le point clé** : `GET /api/FormStructure/{jobId}` est indexé par **`jobId`**, **pas** par `contextOrderId`. Il renvoie **toujours les
attributs du context *effectif*** de la mission. Tu ne passes **jamais** le context — le serveur fait le lien context → jeu d'attributs.

### Séquence (les 3 endpoints s'enchaînent)
```
1. GET  /api/Contract/{jobId}       → context effectif + locked + liste filtrée   (cf. §3)
        └─ si non verrouillé et l'ambulancier change :  POST /api/Contract/{jobId} { contextOrderId }
2. GET  /api/FormStructure/{jobId}  → LES ATTRIBUTS du context effectif
3. GET/PATCH /api/JobEdit/{jobId}   → lecture / saisie des VALEURS d'attributs
```
⚠️ **Après un changement de context (`POST /api/Contract`), re-`GET FormStructure`** : le jeu de champs change.

### 🔑 Verrou ≠ formulaire en lecture seule
`locked` verrouille **le choix du context**, **pas la saisie des attributs**. Sens métier : la régul dit « c'est une **Centre 15** »
(context figé) → l'ambulancier **renseigne quand même** les infos terrain (n° dossier SAMU…). Donc `locked=true` ⇒
**sélecteur grisé** *mais* **formulaire d'attributs éditable**.

### Ce que renvoie FormStructure
Un tableau de champs (`ClMobileAppFieldModel`). Exemple — context effectif = **Centre 15** :
```jsonc
[
  { "name": "SAMU_FILE", "label": "N° dossier SAMU",  "index": 10, "type": "text", "required": true,  "value": "" },
  { "name": "DDN",       "label": "Date de naissance", "index": 20, "type": "date", "required": false, "value": "1958-03-12" }
]
```
L'ambulancier bascule sur **CPAM** (`POST /api/Contract` + re-`GET FormStructure`) → **autre jeu** de champs :
```jsonc
[
  { "name": "NIR", "label": "N° sécurité sociale", "index": 10, "type": "text", "required": true, "value": "" },
  { "name": "AMC", "label": "Mutuelle", "index": 20, "type": "list", "isMulti": true,
    "options": ["MGEN", "Harmonie Mutuelle", "…"], "value": "" }
]
```

| Champ | Usage rendu |
|---|---|
| `name` | clé technique (à renvoyer dans `JobEdit`) |
| `label` | libellé du champ |
| `index` | ordre d'affichage |
| `type` | `text` / `date` / `list` … → widget à rendre |
| `required` | champ obligatoire |
| `options` | valeurs de la liste (si `type: "list"`) |
| `isMulti` | liste à choix multiple |
| `value` | valeur courante (pré-remplissage) |

*(champs illustratifs — le vrai jeu dépend des liaisons context↔attributs en base.)*

### Saisie des valeurs
`PATCH /api/JobEdit/{jobId}` (inchangé) avec les couples `name` / valeur. Le gel post-transfert (`[FreezeOnTransfer]`) reste tel quel.

### Aujourd'hui vs cible — ton code ne bouge pas
| | Context effectif | Attributs (FormStructure) |
|---|---|---|
| **Aujourd'hui (v1+)** | overlay Vector `MOB_JOB_CONTRACT` | catalogue Vector `MOB_CONTRACT_ATTRIBUTE` |
| **Cible (Phase 2/3, différé)** | **Order** (OC-9) | définition **déplacée dans Order** (form-structure définie une fois) |

👉 **L'endpoint et le JSON de `FormStructure` ne changent pas** : seule la **source interne** bascule. Ton front attributs est **stable** à travers la migration.

---

## 5. À retirer / adapter côté front

- ❌ **Logique « défaut = 1er context actif »** → supprime-la (l'état « non renseigné » est légitime).
- ❌ **Filtrage client** de la liste des contexts → inutile, le serveur filtre déjà.
- ✅ Ajoute la **gestion de `locked`** (désactivation + cadenas) et le **traitement du 409** au save.
- ✅ Prévois le **passage tableau → objet** du `GET /api/Contract` (ou l'endpoint `state` séparé, à décider ensemble).
- ✅ Aligne le **vocabulaire** côté front : `contextOrderId` (plus « contractId » / « typeId »).

---

## 6. Statut & calendrier

- ✅ **Back Order (OC-9)** : endpoints mission-scoped `GET/PATCH /missions/{id}/contextOrder` **faits, testés** (verrou → 409, filtrage agence/mode). C'est la **source**.
- 🔜 **API Vector (Phase 2)** : bascule de `/api/Contract` (+ `JobEdit`) vers cette source — **à venir**. Le contrat UI ci-dessus est le **cible** ; je te ping avant de livrer pour qu'on synchronise le front.

---

## 7. ⚡ TL;DR

- Le **context de la mission** est **piloté par la régulation** : un **`locked`** peut mettre le sélecteur en **lecture seule**.
- La **liste est filtrée serveur** (agence/mode) → affiche-la telle quelle, **plus de filtrage ni de défaut « 1er actif »** côté front.
- `GET /api/Contract/{jobId}` gagne un **`locked`** (la réponse passe en **objet** `{ locked, contextOrderId, contextOrders[] }` — à coordonner).
- `POST /api/Contract/{jobId}` : **409** si verrouillé, **400** si context non applicable → messages + re-`GET`.
- **Attributs** : `GET /api/FormStructure/{jobId}` renvoie les champs du **context effectif** (re-fetch après changement de context) ; `locked` ne bloque **que le context**, pas la saisie. Endpoint **stable** à travers la migration.
- **Vocabulaire** : « type de mission » → **« context de la mission »** ; l'id = **`contextOrderId`**.
- **Timing** : back prêt (OC-9) ; bascule API Vector en **Phase 2** — je te préviens avant le switch.

Ping-moi pour caler la **forme de réponse** (objet vs endpoint `state` séparé), le **renommage éventuel de la route**, et le **moment de la bascule**. 🚀
