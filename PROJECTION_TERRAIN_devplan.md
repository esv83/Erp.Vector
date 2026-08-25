# 🎯 devplan — Projection du terrain vers Order (`PRJ`)

> **Objet** : faire de `Orders.Api` le **seul amont métier** de la facturation. Vector projette vers
> Order ce qu'il produit ; `BillingGateway` cesse de composer deux sources et ne revient vers
> Vector que pour des **octets**.
>
> **Statut** : ⬜ **proposition, non arbitrée** — aucune étape n'est engagée. Demande du 2026-08-25.
>
> **Filiation** : prolonge `TRF-5` (projection de l'opérationnel, livrée) et la bascule `OC`
> (référentiel de contexte, livrée). Ce n'est pas une refonte : c'est la tranche suivante d'un
> mouvement déjà commencé.
>
> Ce plan se lit **avec** [`devplan.md`](devplan.md). Il ne redit pas les décisions D1→D15, il s'y
> appuie. Les étapes qui y existent déjà (`E1`, `E4`, `B5`) ne sont pas dupliquées — elles sont
> renvoyées.

---

# 1. La cible

`BillingGateway` lit **Order seul** pour tout ce qui est métier, et ne conserve vers Vector qu'un
canal de téléchargement de binaires — sans logique, sans disponibilité critique.

## 1.1 Ce que ça change

| | Aujourd'hui | Cible |
|---|---|---|
| Amonts métier de la facturation | deux : Order **et** Vector | un : Order |
| Coût d'une publication | **1 appel HTTP par mission** (`field-data` unitaire) | 1 lecture bulk (`for-export`) |
| Publier avec Vector arrêté | impossible | possible, sauf les images |
| Où lire « ce qu'est une mission facturable » | composé depuis deux modules + une tolérance au 404 | un seul read-model |

## 1.2 Ce que ça ne change pas

| Élément | Impact |
|---|---|
| **Contrat de l'app web terrain** | aucun. L'ambulancier écrit toujours dans Vector, mêmes routes, mêmes DTO. Seul le chemin **aval** de la donnée change. `D14` n'est pas sollicitée. |
| **Garde-fous d'accès** | inchangés — `CrewAccess`, `IsMissionAccessible`, `FreezeOnTransfer` restent côté Vector. |
| **Sens des dépendances** | inchangé — Order reste un serveur pur, il ne rappelle personne. On lui donne plus à **porter**, pas plus à **faire**. |
| **Découplage de build** | inchangé — zéro référence projet, HTTP seul (`D3`). |
| **Propriété de la donnée terrain** | inchangée — Vector reste la source de vérité ; Order en reçoit une **projection**, jamais une copie éditable (`D2`). |

---

# 2. Ce qui est déjà en place — à ne pas refaire

C'est le point important de ce plan : **les deux plus gros blocs sont déjà faits ou presque.**

| Bloc | État réel | Reste |
|---|---|---|
| **Attributs de facturation** | 🟢 **fait**. Vector écrit par `PATCH missions/{id}/contextOrder/values` (`OC-7`), et `ContextOrderId` / `ContextOrderCode` / `ContextOrderAttributes` sont **déjà servis par `for-export`**. | rien |
| **Jalons opérationnels** | 🟢 **projetés** par l'Outbox dans `ORD_MISSION_OPERATIONAL`, servis par `ClMissionOperationalDtoOut` — mais **absents du read-model d'export**. | 1 champ additif **côté Orders** (§5, `B11`) |
| **Binaires par URL** | 🟢 en service. `field-data` porte des URL relatives, l'aval tire les octets sur Vector.Api — c'est `D8`, pas un provisoire. | rien à déplacer |
| **Mécanique de projection** | 🟢 `MOB_OPERATIONAL_OUTBOX` + `OperationalOutboxDispatcher` : écriture dans la même transaction que le changement, debounce, poll 2 s, backoff, livraison garantie. | à généraliser (`PRJ-1`) |

> Autrement dit, la première tranche utile (**les horaires réalisés lus dans Order**) ne coûte
> **aucun code dans ce dépôt** et aucun schéma : la donnée est déjà chez Orders, elle n'est
> simplement pas remontée sur `for-export`.

---

# 3. Décisions en vigueur qui encadrent le chantier

| # | Ce qu'elle impose ici |
|---|---|
| **D2** | La donnée terrain est déclarative et **n'écrase jamais l'ERP**. Tout champ projeté atterrit dans une table clairement marquée « projection terrain », comme `ORD_MISSION_OPERATIONAL` — jamais dans les colonnes de la régulation. |
| **D5** | Grain = **la mission**. La projection suit ce grain ; ce qui n'est pas mission-scoped ne se projette pas tel quel (→ le kilométrage, `E1`). |
| **D8** | **L'aval tire les octets depuis Vector.Api.** Verser des scans dans Order irait contre une décision en vigueur : Order est un référentiel de commande, pas un magasin de fichiers ni un dépositaire de données de santé. Ce plan **ne propose pas** de la réviser. |
| **D9** | Anomalies non bloquantes, transférées comme donnée et arbitrées en facturation — donc projetables sans effet de bord. |
| **D14** | Neutre ou additif. Toutes les étapes ci-dessous sont des **ajouts** ; aucune ne retire quoi que ce soit avant confirmation de l'aval. |

## 3.1 Décision à ajouter — proposition `D16`

> **D16 (proposée)** — **Tout champ projeté a un propriétaire unique et déclaré.**
> `ORD_MISSION_OPERATIONAL` reçoit déjà des jalons déclarés par Vector **et** des jalons déduits du
> GPS écrits par Certification ; `LoadedAtSource` existe précisément pour arbitrer. Chaque nouvelle
> projection doit dire **qui gagne** en cas de concurrence, faute de quoi la dérive est silencieuse
> et ne se constate qu'à la facture.

---

# 4. Étapes codables dans ce dépôt

*Format habituel : **état · préalable · contenu · fin**.*

## PRJ-1 — ⏳ Généraliser l'Outbox à la projection du terrain *(préalable technique)*

**Préalable** : aucun — attaquable seul, sans contrat en jeu.

**État actuel** : `MOB_OPERATIONAL_OUTBOX` porte **une ligne par mission** (`OOB_MISSION_ID`), et le
dispatcher projette l'**état consolidé** relu dans `MOB_MISSION_STATE` au moment de l'envoi. Le
debounce (`OOB_DISPATCH_AFTER` repoussé à chaque changement) est posé côté écriture.

**Deux voies, à trancher :**

| Voie | Principe | Pour | Contre |
|---|---|---|---|
| **(a) discriminant de nature** | clé = (mission, nature) ; chaque nature relit son silo | granularité fine, propriété par bloc évidente (`D16`) | migration de clé primaire, N appels par mission, debounce à dupliquer |
| **(b) paquet consolidé** ⭐ | on garde 1 ligne / mission ; le dispatcher projette **tous** les silos en un `PUT` | reprend le design existant tel quel, 1 seul appel, debounce inchangé, `PUT` idempotent | renvoie des blocs inchangés ; l'endpoint Orders est plus gros |

**Recommandation : (b).** L'Outbox est déjà conçue pour projeter un état consolidé, le debounce est
déjà par mission, et un `PUT missions/{id}/field` est le miroir exact de
`PUT missions/{id}/operational` qui tourne en production. Le surcoût — republier des blocs inchangés
— est sans conséquence sur un `PUT` idempotent.

**Contenu** : élargir l'écriture de l'entrée d'Outbox aux autres silos (signature, anomalies,
documents, mutuelle) — aujourd'hui seule `JobTimeRepository` l'alimente ; factoriser le point
d'entrée pour que **tout** dépôt terrain arme la projection dans sa propre transaction.

**Fin** : une modification de n'importe quel silo terrain arme la projection, et rien ne peut être
perdu — même garantie qu'aujourd'hui pour les jalons.

---

## PRJ-2 — ⏳ Projeter la signature (présence + horodatage)

**Préalable** : `PRJ-1` · endpoint côté Orders (§5, `B12`).

**Contenu** : ajouter `SignatureExists` et `SignedAt` au paquet projeté. ⚠️ Corriger au passage
`SIG_DATETIME`, écrit en **heure locale** (`DateTime.Now`, deux occurrences) contrairement au reste —
c'est `E2` de [`devplan.md`](devplan.md), et **il devient bloquant ici** : projeter un horodatage de
fuseau indéterminé vers l'ERP fabriquerait une incohérence durable.

**Bénéfice au-delà de la facturation** : Orders porte déjà un badge `SIGNATURE_MISSING` dans
`ClBadgeCatalog`, annoté « alimenté par le module Mobile ». Cette projection sert donc **deux**
consommateurs — la facturation et le board de régulation.

**Fin** : la présence d'une signature se lit dans Order, en UTC déclaré.

---

## PRJ-3 — ⏳ Projeter les anomalies (nature + horodatage)

**Préalable** : `PRJ-1` · endpoint côté Orders (§5, `B12`).

**Contenu** : projeter la **nature** (`EnAnomalyType`) et l'horodatage. Le **texte libre reste chez
Vector** : le dupliquer dans l'ERP n'apporte rien à l'éligibilité (`D9` — non bloquantes) et crée
une seconde copie d'une saisie libre à durée de vie non tranchée. L'aval qui veut le texte le lit sur
Vector, comme il lit les octets.

**Fin** : la facturation sait qu'une mission porte des signalements, et de quelle nature, sans
appeler Vector.

---

## PRJ-4 — ⏳ Projeter les métadonnées des binaires *(pas les binaires)*

**Préalable** : `PRJ-1` · endpoint côté Orders (§5, `B12`).

**Contenu** : projeter, pour chaque document et pour la carte mutuelle, `Category`, `ContentType`,
`ByteSize`, `CapturedAt` et l'**URL relative**. Les octets ne bougent pas — `D8` reste en vigueur.

**Fin** : Order sait qu'une pièce existe et où la chercher ; Vector reste le seul à la servir.
C'est ce qui permet à `BillingGateway` de couper l'appel `field-data` **sans** perdre l'accès aux
pièces.

---

## PRJ-5 — ⏳ Émettre le watermark avec la projection

**Préalable** : `PRJ-1`.

**Pourquoi** : `field-data` expose aujourd'hui `UpdatedAt` — le **max des horodatages des silos**,
calculé à la lecture par `FieldDataReader` — pour qu'un consommateur sache re-tirer si la donnée a
bougé et que la mission n'est pas encore transférée. En passant par une projection, cette garantie
doit **voyager avec elle**.

**C'est le seul vrai recul de la bascule**, et il se comble par un champ : sans lui,
`BillingGateway` lit une copie **sans savoir de quand elle date**, alors qu'aujourd'hui sa lecture
est autoritative à l'instant du clic.

**Contenu** : porter le watermark dans le paquet projeté, à charge pour Orders de le republier sur
`for-export` (§5, `B13`).

**Fin** : la fraîcheur de la donnée terrain est lisible dans Order.

---

## PRJ-6 — ⬜ `field-data` : décider ce qu'il devient — *décision d'abord*

**⚠️ Fait à vérifier avant d'arbitrer** : relevé le 2026-08-25 sur les quatre dépôts,
**`BillingGateway` est le seul appelant de `GET /api/missions/{id}/field-data`.** Certification ne
l'appelle pas. Une fois `PRJ-2` → `PRJ-5` livrés et l'aval basculé, l'endpoint **n'a plus de
consommateur**.

Trois sorties possibles :

| Option | Conséquence |
|---|---|
| **Le garder tel quel** | contrat stable pour un futur consommateur, coût nul, mais un endpoint non authentifié qui expose tout le dossier terrain reste ouvert (cf. `C2`) |
| **Le faire maigrir** | il ne sert plus que ce qui n'est pas projeté : texte des anomalies, et le pointeur vers les octets |
| **Le retirer** | plus rien à maintenir — mais irréversible sans redéploiement de l'aval |

**Recommandation** : le faire maigrir, pas le retirer. Il reste le contrat naturel de « tout le
dossier terrain d'une mission », et son coût est nul une fois qu'il n'est plus sur le chemin chaud.

> Note : le devplan d'Orders (`00-orders_devplan.md`) anticipait déjà que *« `field-data` ne
> disparaît pas : il reste nécessaire pour les horaires et la signature. Il maigrit. »* — ce plan-ci
> projette précisément **les horaires et la signature**, et le fait donc maigrir d'un cran de plus
> que prévu là-bas.

---

## PRJ-7 — ⏳ Retirer les blocs devenus redondants — *après confirmation de l'aval, jamais d'office*

**Préalable** : `PRJ-6` tranché **et** confirmation écrite que `BillingGateway` ne lit plus les blocs
concernés.

**Contenu** : retirer de `ClFieldEnrichmentDtoOut` ce qui est devenu doublon de la projection.
Même règle que `G2` de [`devplan.md`](devplan.md) : les alias et blocs de compatibilité **ne se
retirent que sur confirmation**, jamais parce que le serveur est prêt.

**Fin** : un seul chemin par donnée.

---

# 5. Ce que ce plan attend d'Orders — rien à coder ici

*Même logique que le chapitre B de [`devplan.md`](devplan.md) : à suivre et à réclamer, pas à
traiter ici.*

| Réf | Ce qui manque côté Orders | Effet | Priorité |
|---|---|---|---|
| **B11** | **Exposer l'opérationnel sur `for-export`.** La donnée est déjà dans `ORD_MISSION_OPERATIONAL` et déjà servie par `ClMissionOperationalDtoOut` — elle n'est pas sur le read-model d'export. Additif, le contrat de `for-export` l'autorise explicitement. | supprime à lui seul la raison principale pour laquelle l'aval appelle Vector | 🥇 **la tranche la moins chère du chantier** |
| **B12** | **Endpoint de réception du paquet terrain projeté** — `PUT /missions/{id}/field`, miroir de `/operational`, table de projection distincte (`D2`) | sans lui, `PRJ-2` → `PRJ-4` n'ont nulle part où écrire | 🥈 |
| **B13** | **Republier le watermark** sur `for-export` | sans lui, `PRJ-5` s'arrête à mi-chemin | 🥈 |
| **B14** | **Propriété des champs concurrents** (`D16`) : arbitrage déclaré entre déclaration terrain et déduction GPS, sur le modèle de `LoadedAtSource` | dérive silencieuse sinon | ⛔ décision |
| **B15** | ⚠️ **`for-export` reste sans identifiant d'adresse** — hors périmètre de ce plan, mais c'est la seconde raison pour laquelle l'aval fait des appels supplémentaires. Voir aussi `B8`. | contrôle d'adresse neutralisé côté facturation | ⏳ |

---

# 6. Décisions à trancher avant de coder

| # | Question | Renvoi |
|---|---|---|
| 1 | **Qui possède `TransferStatus` ?** Si l'aval ne lit plus que Order, sa vision de « ce qui est prêt » repose entièrement sur un statut écrit par **Certification** — seul appelant de `PUT /missions/{id}/transfer-status` dans les quatre dépôts. Le même statut ferme l'édition terrain ici (`D7`, `FreezeOnTransfer`). Trois responsabilités sur un champ, dont aucune n'appartient au module qui facture. | `E4` de [`devplan.md`](devplan.md) |
| 2 | **Le kilométrage.** Crew/véhicule-scoped, pas mission-scoped (`D5`) : aucun transport ne dit comment un relevé d'odomètre devient une valeur de mission. Ce plan ne le résout pas et ne prétend pas le faire. | `E1` de [`devplan.md`](devplan.md) |
| 3 | **Voie (a) ou (b) pour l'Outbox** — granularité par nature, ou paquet consolidé. | `PRJ-1` |
| 4 | **Sort de `field-data`** — garder / maigrir / retirer. | `PRJ-6` |
| 5 | **Adopter `D16`** — propriétaire unique et déclaré par champ projeté. | §3.1 |

---

# 7. Ce que ce plan rend caduc

| Réf | Sort |
|---|---|
| **`B5` / `E3` — `field-data` par période** | ⬜ **à clore sans le faire.** L'endpoint bulk répondait au coût de l'appel unitaire (14,7 s pour 284 missions). Si l'aval lit les horaires et la signature dans `for-export`, l'appel unitaire **disparaît du chemin chaud** et le bulk perd son objet. Livrer les deux serait payer deux fois la même solution. ⚠️ À confirmer avec la facturation avant de clore : c'est elle le demandeur. |

---

# 8. Migrations SQL

| Base | Script | Contenu | État |
|---|---|---|---|
| Vector | `MOB_008` | élargissement de l'Outbox de projection (`PRJ-1`) — forme selon la voie retenue | ⬜ à écrire |
| Orders | *n° à déterminer* | table de projection du paquet terrain (`B12`) | ⬜ autre dépôt |

> Prochain numéro Vector relevé au dépôt le **2026-08-25** (dernier livré : `MOB_007`).
> **Le recontrôler avant d'écrire** — et se rappeler qu'aucune table de suivi de schéma n'existe
> encore (`G4`/`G8`) : rien ne garantit l'alignement des environnements.

---

# 9. Documents voisins

- [`devplan.md`](devplan.md) — plan de référence du module ; décisions D1→D15, chapitres A→H
- [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) — le découplage HTTP dont ce plan est la suite
- [`endPoint.md`](endPoint.md) — contrat détaillé de ce que Vector attend d'Orders
- `Erp.Order/00-orders_devplan.md` — anticipait déjà que `field-data` maigrisse
- `Erp.BillingGateway/DEVPLAN.md` — le demandeur : y figure la demande d'endpoint bulk que §7 propose de clore
