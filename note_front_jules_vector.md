# 🧭 Note front — Ce que Vector attend de toi (pour Jules)

> **Date** : 2026-10-04 · **Pour** : Jules, qui reprend l'app terrain Vector (le travail d'Alexandre,
> indisponible pour le moment) en plus des écrans de régulation et de certification.
> **Objet** : tout ce qui est ouvert côté front, en **une** note. Les notes adressées à Alexandre
> (`note_web_alexandre_*.md`, `note_ui_alex.md`, `docs/ui-web/*`) restent la référence détaillée ;
> celle-ci dit **quoi faire**, et renvoie vers elles pour le **comment**.

Salut Jules 👋

**Trois blocs, dans cet ordre :**
1. **L'app terrain — ce qui a changé le 04/10** : à intégrer, l'API est déjà en production.
2. **L'app terrain — ce qui avait été demandé à Alexandre** : je ne sais pas ce qu'il a livré, je ne
   vois pas le code de l'app. **À vérifier**, point par point.
3. **Tes écrans de régulation et de certification** : la carte mutuelle dans les listes de missions.

**Conventions Vector**, valables partout : base `…/vector/api/…` ; **JSON en PascalCase** ;
jeton Keycloak en `Authorization: Bearer` sur toutes les routes ; une URL rendue par l'API
(`ImageUrl`…) est **relative** — préfixe-la avec la base de l'API ; un corps d'erreur `400`/`404`/`409`
est une **phrase à afficher**.

---

## 1. L'app terrain — ce qui a changé le 04/10

### 1.1 Quand la régulation ne répond pas : `503` et un message, plus un `500` vide

Sur **toutes** les routes, quand la régulation (Orders) ou la base est à terre — coupures, mises à jour :

```http
HTTP/1.1 503 Service Unavailable
Retry-After: 10
Content-Type: application/problem+json

{ "status": 503, "title": "Service momentanément indisponible",
  "detail": "La régulation ne répond pas pour le moment. Réessayez dans une minute." }
```

- **Affiche `detail`** — au moins sur l'écran d'entrée (`GET api/Crew/mine`) : le 22/09, l'ambulancier
  y a reçu ~1 600 erreurs sans savoir pourquoi. Un bouton « Réessayer » ou une relance après
  `Retry-After` secondes est un plus.
- Ce `503` remplace aussi des `400` qui accusaient à tort l'ambulancier (timeline, signature, liste,
  conducteur) : un `400` est désormais **toujours** un vrai refus, avec son motif.
- Si l'app traite déjà tout non-2xx comme une erreur générique, rien ne casse — le message se perd.

*Détail : [`note_web_alexandre_503_regulation_indisponible.md`](note_web_alexandre_503_regulation_indisponible.md).*

### 1.2 La carte mutuelle connue, annoncée dans le détail de la mission

L'app ne savait qu'une carte existait qu'en recevant un `404` sur `GET api/missions/{jobId}/mutuelle-card`
(5 390 fois en deux semaines). `GET api/JobDetail/{jobId}` porte désormais :

| Champ | Type | Sens |
|---|---|---|
| `MutuelleCardKnown` | bool | une carte est déjà connue pour ce patient |
| `MutuelleCardCapturedAt` | date ou `null` | date de la dernière photo → « carte connue, photo du 20/09 » |
| `MutuelleCardFieldsInheritedFrom` | date ou `null` | les champs (mutuelle, AMC…) ont été **repris** d'une photo plus ancienne, pas encore revérifiés → « repris de la photo du 02/06 » |
| `MutuelleCardId` | Guid ou `null` | la carte courante — pour la saisie `PATCH api/mutuelle-card/{Id}` |
| `MutuelleCardImageUrl` | texte ou `null` | la photo, à la demande |

- **Carte connue** : « Carte mutuelle connue — photo du 20/09 », un bouton **« Voir la carte »** ; le
  bouton photo reste là (la carte a pu changer). **Pas de carte** : le bouton photo, comme aujourd'hui.
- **« Voir la carte »** : `fetch` avec le jeton, puis `URL.createObjectURL` — une balise `<img src>`
  n'envoie pas le jeton et reçoit `401`.
- **Une nouvelle photo ne fait plus perdre la saisie** : elle reprend les champs validés de la
  précédente, marqués `FieldsInheritedFrom`, jusqu'à ce que l'équipage les confirme ou les corrige.

### 1.3 La saisie des quatre champs de la carte refuse l'enregistrement vide

`PATCH api/mutuelle-card/{Id}` **remplace toujours les quatre champs** — rien ne change si tu les
envoies tous. Deux cas qui passaient répondent maintenant **`400` avec une phrase à afficher** :
- **les quatre champs vides** — avant, la carte était effacée et marquée validée ;
- **un champ trop long** — mutuelle 200, code AMC 50, concentrateur 100, télétransmission 50
  caractères au plus : **borne tes zones de saisie** à ces longueurs.

*Détail des deux points ci-dessus : [`note_web_alexandre_carte_mutuelle_detail.md`](note_web_alexandre_carte_mutuelle_detail.md).*

### 1.4 La route du kilométrage est retirée

`GET`/`POST api/Kilometers/{crewId}` n'ont jamais fonctionné (15 lectures en 7 semaines, toutes en
`404`, aucune saisie). Elles répondent `404` comme avant : **retire l'appel** s'il existe. Le
kilométrage reviendra sous une autre forme, quand la facturation aura tranché.
*Détail : [routes retirées](note_web_alexandre_routes_retirees.md), section du 04/10.*

---

## 2. L'app terrain — ce qui avait été demandé à Alexandre, à vérifier

**Je ne sais pas ce qui est déjà fait** : l'API ne voit pas l'écran. Pour chaque point, dis-moi
**fait / pas fait / pas compris** — même en une ligne. Les deux premiers sont ceux qui comptent le plus.

| # | Ce qui est attendu de l'écran | Pourquoi ça compte | Référence |
|---|---|---|---|
| 1 | **Un refus a son motif, affiché** : `POST api/Contract` (`409`, `400`, `404`), `PATCH api/JobEdit` (`409` champ scellé, `400` valeur invalide — **tout ou rien**, rien n'est enregistré), `POST api/driver/{crewId}` (motif de la régulation : « la vacation s'est terminée à 18:00… »). Sur un refus, **ne pas afficher le choix comme enregistré**, re-`GET` | Ces refus sont rares : un écran cassé sur ce chemin peut le rester six semaines sans que personne ne le voie. **Aucun refus de conducteur n'a encore été vu en production** | [`note_web_alexandre_context_mission_confirmation.md`](note_web_alexandre_context_mission_confirmation.md), [`note_web_alexandre_context_mission_dto.md`](note_web_alexandre_context_mission_dto.md) |
| 2 | **Un champ grisé dit pourquoi** : `IsReadOnly` → saisie désactivée **et** `ReadOnlyReason` affiché | Grisé sans explication, l'ambulancier le prend pour un bug et appelle la régulation | idem, §4 de la note du 25/08 |
| 3 | **Le n° de sécurité sociale est relu avant validation** | Aucun module ne le corrige une fois posé : une clé fausse part en facturation | idem, « Trois comportements métier » |
| 4 | **Sélection d'équipage** : choix au login, `CrewId` épinglé sur toutes les routes, `404` sur `Crew/mine` → message reçu + bouton **« Réessayer »** | L'équipage peut ne pas être encore composé par la régulation : le message dit quoi faire | [`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md) |
| 5 | **Champs nouveaux plutôt qu'anciens** : `IsSeen` (pas `IsAck`), `ScheduleLabel`, `TransportModeLabel`, `PickupLocation` / `DropoffLocation`, la ligne **`Service`** du lieu | Les anciens champs restent servis **tant que tu t'en sers** : dis-moi quand tu as basculé, je les retire | [`note_ui_alex.md`](note_ui_alex.md) §1, §4, §5 |
| 6 | **Rafraîchissement silencieux du jeton** (refresh token) : pas de re-login avant la fin de la session de 15 h | Le jeton dure 5 min : sans cela, l'ambulancier se reconnecte en pleine mission | [`note_ui_alex.md`](note_ui_alex.md) §6 |
| 7 | *(plus tard)* **Écran de validation des cartes lues** : la proposition de la lecture automatique à côté des champs, avec sa confiance et l'image | **Pas urgent** : la lecture automatique est en place mais **inerte**, faute de décision d'exploitation | [`note_web_alexandre_carte_mutuelle_ocr.md`](note_web_alexandre_carte_mutuelle_ocr.md) |

⚠️ La note `note_ui_alex.md` §3 décrit l'ancien conducteur « sans `crewId` » : c'est **périmé**, la route
est `api/driver/{crewId}` depuis la sélection d'équipage (point 4).

---

## 3. Tes écrans de régulation et de certification — la carte mutuelle dans les listes

Une entrée de menu **« Carte mutuelle »** sur chaque ligne des deux listes de missions : active quand
l'équipage a déjà photographié la carte du patient, avec la date de la photo, et l'image à la demande.
Tes listes n'ont que l'identifiant de la **mission** : c'est suffisant, Vector retrouve le patient.

```
Affichage de la page    ──► POST api/missions/mutuelle-card/presence      → une ligne par mission
Clic « Carte mutuelle » ──► GET  api/missions/{missionId}/mutuelle-card    → nom, AMC, concentrateur, télétransmission
                        └►  GET  api/missions/{missionId}/mutuelle-card/image → la photo (fetch + blob)
```

### 3.1 Présence — une fois par page affichée

```http
POST api/missions/mutuelle-card/presence
Authorization: Bearer <jeton de l'utilisateur>
Content-Type: application/json

{ "MissionIds": [ "…", "…" ] }
```

**200 missions au plus par appel** (au-delà : `400`, découpe la page). Réponse : **une ligne par
mission demandée**, dans l'ordre de ta demande :

```json
[
  { "MissionId": "…", "BeneficiaryId": "…", "HasCard": true,
    "CapturedAt": "2026-09-20T08:30:00Z", "ImageUrl": "api/missions/…/mutuelle-card/image" },
  { "MissionId": "…", "BeneficiaryId": "…", "HasCard": false, "CapturedAt": null, "ImageUrl": null },
  { "MissionId": "…", "BeneficiaryId": null, "HasCard": false, "CapturedAt": null, "ImageUrl": null }
]
```

- `HasCard = false` → entrée **grisée**. `BeneficiaryId = null` : mission sans patient, ou inconnue.
- `CapturedAt` → « photo du 20/09 » : toujours **la plus récente**.

### 3.2 Les données de la carte — au clic

`GET api/missions/{missionId}/mutuelle-card` → `MutuelleName`, `AmcCode`, `Concentrateur`,
`Teletransmission`, `CapturedAt`, et **`FieldsInheritedFrom`** : si renseigné, affiche « repris de la
photo du 02/06, à revérifier ». **`404`** = pas de carte, ce n'est pas une erreur.

### 3.3 La photo — avec le jeton, donc pas par `<img src>`

```js
const r = await fetch(`${vectorApi}/${ligne.ImageUrl}`, { headers: { Authorization: `Bearer ${jeton}` } });
if (r.ok) img.src = URL.createObjectURL(await r.blob());   // URL.revokeObjectURL à la fermeture
```

### 3.4 Erreurs et préalables

| Code | Sens | Quoi faire |
|---|---|---|
| `401` | Pas de jeton, ou **client Keycloak non déclaré chez Vector** | voir ci-dessous |
| `403` | Jeton valide, mais hors de ce qui est ouvert aux écrans | ces quatre lectures seulement — le reste de Vector est réservé au terrain |
| `400` | Plus de 200 missions | découper |
| `503` | Régulation ou base à terre | réessayer ; `detail` est affichable |

⚠️ **Vector doit connaître le client Keycloak de tes deux écrans** : donne-moi l'`azp` des jetons
émis pour l'écran de la régulation et pour celui de la certification. Tant qu'il n'est pas déclaré,
ces routes te répondent `401`.

⚠️ **Les deux routes sans jeton** (`POST api/mutuelle-card/presence` par bénéficiaire, `GET
api/beneficiaries/{id}/mutuelle-card/image`) **vont fermer** — personne ne s'en est jamais servi.
N'en dépends pas, et dis-moi quand tu as basculé sur celles-ci.

---

## ✅ Récap

**App terrain — à intégrer (04/10)**
- [ ] `503` : afficher `detail`, au moins sur l'écran d'entrée ; « Réessayer » en option
- [ ] Détail : « carte connue, photo du … », « Voir la carte » par `fetch` + blob, mention « repris de la photo du … »
- [ ] Saisie de la carte : afficher le motif d'un `400`, borner les zones de saisie
- [ ] Retirer l'appel à `api/Kilometers` s'il existe

**App terrain — à vérifier (demandes à Alexandre), réponds fait / pas fait**
- [ ] 1. Refus avec motif : `Contract`, `JobEdit`, conducteur
- [ ] 2. Champ grisé avec son motif
- [ ] 3. Relecture du NIR
- [ ] 4. Sélection d'équipage et « Réessayer »
- [ ] 5. Champs nouveaux (`IsSeen`, `ScheduleLabel`…) — dis-moi quand je peux retirer les anciens
- [ ] 6. Rafraîchissement silencieux du jeton
- [ ] 7. *(plus tard)* Écran de validation des cartes lues

**Tes écrans de régulation et de certification**
- [ ] Me donner l'`azp` des deux écrans
- [ ] Un appel `presence` par page, entrée « Carte mutuelle » grisée si `HasCard = false`, date de la photo
- [ ] Au clic : les données, la mention « repris de la photo du … », la photo par `fetch` + blob
- [ ] Me dire quand tu ne dépends plus des deux routes sans jeton
