# [UI] J-175 — Vector : le mode formation
Cible : Mobile (React Native, app `us-mobile` / Vector) — Date : 22/11/2026 — Priorité : normale

## Contexte métier

Pour former un ambulancier à Vector, il faut des missions sur lesquelles il peut tout essayer : pointer, signer, saisir le patient, photographier une carte. Rien ne doit partir à la régulation ni en facturation. C'est le **mode formation**.

Le principe :
- l'ambulancier se connecte **avec son vrai compte**, comme d'habitude ;
- il active lui-même le mode formation ;
- tant que le mode est actif, l'app envoie un en-tête particulier sur **chaque** appel. Le serveur lui sert alors un **jeu de 7 missions fictives**, au lieu de ses vraies missions ;
- l'app ne change rien d'autre : les routes, les écrans et les DTO sont exactement les mêmes. **Seules les données changent.**

Le danger est double. Un stagiaire ne doit **jamais** croire qu'il travaille sur une vraie mission. Un ambulancier en service ne doit **jamais** croire qu'il est en formation alors qu'il travaille sur ses vraies missions. Toute la fiche découle de ces deux règles.

Le même jeu sert aussi au développement de l'app. Sur le serveur de développement seulement, des **personas** sans compte Keycloak vous permettent de tester vos écrans sans demander de compte.

## Périmètre

- Inclus :
  - le bouton « Mode formation » (entrée et sortie), avec vérification préalable auprès du serveur ;
  - l'en-tête `X-Vector-Simulation: 1` sur chaque appel tant que le mode est actif, posé par le client API ;
  - la purge complète des données affichées et en cache à l'entrée et à la sortie du mode ;
  - le bandeau « MODE FORMATION », visible sur tous les écrans tant que le mode est actif ;
  - l'affichage des refus du serveur (409 « mode non ouvert », 403 de cantonnement) ;
  - le bouton « Recommencer la formation » ;
  - en développement seulement, un écran de connexion par persona ;
  - les envois mis en file hors réseau, qui gardent le mode dans lequel ils ont été faits.
- Exclus :
  - toute modification des écrans de mission. Ils affichent les missions fictives comme les vraies ;
  - toute modification de l'API ;
  - le design graphique du bandeau, au-delà des exigences de visibilité ci-dessous.

## Prérequis serveur (à lire avant de commencer)

- **Le mode formation n'est pas encore publié sur l'instance de développement** que vous utilisez pour la recette. Nous vous préviendrons dès qu'il le sera. D'ici là, travaillez avec les mocks de la rubrique « Données de test ».
- **La production n'ouvre pas encore le mode formation.** Sur la production actuelle, l'appel de vérification (`GET /api/simulation`) répond **404**. Votre app doit alors refuser d'entrer dans le mode (voir « Entrer dans le mode formation »). **N'envoyez jamais l'en-tête de formation vers la production pour essayer** : nous vous dirons quand elle sera ouverte.

## Contrat d'API

Toutes les routes sont relatives à l'URL de base de l'API Vector, déjà configurée dans `@us/api` (`createUsApi`).

**Casse JSON : PascalCase** (`IsEnabled`, `DeletedRows`…), comme partout dans Vector.

### Authentification (rappel, vaut pour tous les appels)

- Chaque appel envoie `Authorization: Bearer <token>`.
- Le token est injecté une seule fois, par l'intercepteur du client API (`createApiClient`), jamais dans un composant.
- **401** : lancer le flux de reconnexion. En session persona, on revient à l'écran des personas (voir l'écran D).
- **403** : afficher le message du serveur s'il y en a un, sinon « Accès refusé ». **Ne jamais déconnecter.**
- Le token reste en stockage sécurisé (Keychain / Keystore), jamais en clair. Le jeton d'un persona aussi.

### L'en-tête du mode formation

| En-tête | Valeur | Quand |
|---|---|---|
| `X-Vector-Simulation` | `1` | Sur **chaque** appel tant que le mode formation est actif. Jamais en mode normal. |

- Le serveur accepte `1` ou `true`. **Envoyez toujours `1`.**
- L'en-tête est posé par l'intercepteur du client API, au même endroit que le token. Les composants et les hooks ne le posent jamais eux-mêmes.
- Exception : un envoi rejoué depuis la file hors réseau garde le mode **de sa création** (voir « Envois hors réseau »).

Ce que fait le serveur quand il reçoit cet en-tête :

| Situation | Réponse |
|---|---|
| Mode ouvert sur ce serveur | Toutes les routes servent le jeu fictif. **Le contrat de chaque route est inchangé.** |
| Mode non ouvert sur ce serveur (version récente) | **409** sur n'importe quelle route, corps texte : `Le mode formation n'est pas ouvert sur ce serveur.` |
| Serveur d'une version antérieure au mode formation | **L'en-tête est ignoré** et les vraies données sont servies. C'est pour cela que l'entrée dans le mode passe d'abord par la vérification `GET /api/simulation` (voir plus bas). |
| Token absent ou invalide | 401 **avant** tout contrôle du mode. |

### 1. État du mode — `GET /api/simulation`

- Rôle : utilisateur authentifié de l'app mobile (token Keycloak de l'app, ou jeton persona).
- Pas de paramètre, pas de corps.
- **Avec l'en-tête**, cet appel sert de **vérification d'entrée** dans le mode.
- **Sans l'en-tête**, il sert à savoir si le serveur propose le mode, par exemple pour afficher ou griser le bouton.

Réponse **200** (`SimulationStatus`) :
```json
{ "IsEnabled": true, "HasPersonas": true, "IsSimulatedRequest": true, "HeaderName": "X-Vector-Simulation" }
```

| Champ | Sens |
|---|---|
| `IsEnabled` | Le serveur ouvre le mode formation. |
| `HasPersonas` | Le serveur accepte les personas sans compte (développement uniquement). Toujours `false` en production. |
| `IsSimulatedRequest` | **Cette requête-ci** est traitée en formation. Elle vaut `true` si l'en-tête a été envoyé et que le mode est ouvert, ou si l'appel vient d'un persona. |
| `HeaderName` | Toujours `X-Vector-Simulation`. |

| Code | Corps | Cas | Comportement attendu dans l'UI |
|---|---|---|---|
| 200 | `SimulationStatus` | — | Voir « Entrer dans le mode formation » |
| 401 | — | token absent ou expiré | Reconnexion |
| 403 | — | token qui n'est pas celui de l'app mobile | « Accès refusé » |
| 404 | — | serveur d'une version qui ne connaît pas le mode | « Le mode formation n'est pas disponible sur ce serveur. » On **reste en mode normal**. |
| 409 | texte `Le mode formation n'est pas ouvert sur ce serveur.` | en-tête envoyé, mode fermé | Afficher le texte. On **reste en mode normal**. |
| (réseau) | — | pas de réseau | « Connexion nécessaire pour entrer en formation. » On reste en mode normal. |

### 2. Liste des personas — `GET /api/simulation/personas` (développement uniquement)

- **Anonyme** : n'envoyez ni token ni en-tête de formation. C'est cet appel qui donne le jeton.
- Réponse **200** (`SimulationPersona[]`), toujours 5 personas :
```json
[
  { "Number": 1, "Name": "Camille ROUX", "Token": "simulation-persona-1" },
  { "Number": 2, "Name": "Hugo FABRE", "Token": "simulation-persona-2" },
  { "Number": 3, "Name": "Léa GIRAUD", "Token": "simulation-persona-3" },
  { "Number": 4, "Name": "Nora VIDAL", "Token": "simulation-persona-4" },
  { "Number": 5, "Name": "Théo GARCIA", "Token": "simulation-persona-5" }
]
```
- **404 sans corps** : les personas sont fermés sur ce serveur. C'est **toujours** le cas en production. L'écran des personas ne doit alors pas apparaître.
- Le `Token` s'utilise tel quel : `Authorization: Bearer simulation-persona-1`. **Ce n'est pas un JWT.** Ne le décodez pas, ne cherchez ni date d'expiration ni rafraîchissement (refresh token). Votre logique de rafraîchissement silencieux doit l'ignorer.
- Chaque persona a son propre équipage et ses propres missions. Deux développeurs sur le même persona voient et modifient les mêmes données.

### 3. Recommencer la formation — `POST /api/simulation/reset`

- Rôle : utilisateur authentifié, **en mode formation** (en-tête posé) ou persona.
- Pas de corps.
- Effet : efface les saisies **du jour** de la session de formation (horaires, « vu », signatures, cartes, documents…) et remet les 7 missions dans leur état de départ.
- **Idempotent** : l'appeler deux fois ne pose aucun problème.

Réponse **200** (`SimulationResetResult`) :
```json
{ "DeletedRows": 12 }
```
`DeletedRows` sert au journal ou au débogage. **Ne l'affichez pas** : 0 est une réponse normale, par exemple quand rien n'avait encore été saisi.

| Code | Corps (texte) | Cas | Comportement attendu dans l'UI |
|---|---|---|---|
| 200 | `{ "DeletedRows": n }` | remise à zéro faite | Purger le cache, recharger l'équipage et la liste, message « Formation remise à zéro » |
| 401 | texte ou vide | token absent ou expiré | Reconnexion |
| 403 | `Remise à zéro réservée au mode formation (en-tête X-Vector-Simulation).` | appel sans l'en-tête | Bug d'intégration : le bouton ne doit exister qu'en mode formation. Afficher le texte. |
| 409 | `Le mode formation n'est pas ouvert sur ce serveur.` | mode fermé entre-temps | Comme tout 409 de mode (voir plus bas) |
| (réseau) / 500 | — | — | « La remise à zéro n'a pas pu être faite », bouton Réessayer. **Ne purgez rien.** |

### 4. Toutes les autres routes (existantes, contrat inchangé)

`GET /api/crew/mine`, `GET /api/JobList/{crewId}`, `PATCH /api/JobList`, `GET /api/JobDetail/{id}`, `GET /api/FormStructure/{id}`, `GET/PATCH /api/Time/{id}`, `/api/Signature/…`, bénéficiaire, carte mutuelle, documents, anomalies, contexte, conducteur… **Aucune ne change de forme.** En formation, elles renvoient des données fictives, avec les mêmes types TypeScript que d'habitude.

Ce qui change en formation, et que vous verrez passer :

**a) Le cantonnement (403).** En formation, le serveur refuse tout identifiant qui n'appartient pas au jeu fictif : un équipage, une mission ou un patient réels, reçus en route, en paramètre ou dans le corps. Pourquoi : sans ce verrou, un stagiaire pourrait signer une vraie mission par erreur, à partir d'un identifiant resté en cache. Corps texte :
```
Mode formation : gJobId 0b6f6c1e-1111-4a2b-9c3d-000000000001 n'appartient pas au jeu de formation.
```
(Le mot après « Mode formation : » est le nom technique du paramètre : `gJobId`, `crewId`, `missionId`…)

Ce 403 révèle presque toujours un **bug de purge** : un identifiant du mode normal est resté en mémoire, par exemple l'équipage épinglé ou une mission ouverte. Affichez le texte tel quel. Ne déconnectez pas. Proposez « Retour à la liste », qui recharge l'équipage et la liste.

**b) La plateforme d'urgence fermée (403).** Les routes `/api/EmergencyPlatform` répondent en formation, corps texte :
```
Cette fonction n'est pas disponible en mode formation.
```
Si votre app appelle ces routes (statut ou position du véhicule), **ne les appelez pas en mode formation** : un statut fictif n'a rien à y faire. Si un appel passe quand même, affichez le texte (voir Q3).

**c) Le gel des missions transférées (409, déjà existant).** Une mission transférée en facturation refuse les écritures avec un **409 au corps JSON**, à ne pas confondre avec le 409 du mode :
```json
{ "Message": "Mission transférée en facturation : édition close." }
```
Le jeu fictif en contient une (mission 6 de la check-list), pour que vous puissiez tester ce cas.

**d) Le 409 du mode non ouvert.** Il peut arriver sur n'importe quelle route si le serveur ferme le mode pendant une session (voir l'écran C).

**Comment distinguer les deux 409** : le corps du mode est une **chaîne**, celui du gel est un **objet** avec `Message`. Faites cette distinction **une seule fois**, dans le client API (voir « Lire un message d'erreur »).

**e) Les identifiants fictifs** commencent tous par `5e5e5e5e-` et changent **chaque jour**. C'est utile pour reconnaître une donnée fictive dans un journal ou un mock. **N'en tirez aucune logique dans l'app** : la seule source de vérité sur le mode est la vérification `GET /api/simulation`. Ne mettez jamais un de ces identifiants en dur.

**f) Les horaires.** L'équipage fictif prend son service à 07:00, heure du serveur, et s'ouvre 30 minutes avant. **Entre minuit et 06:30**, `GET /api/crew/mine` en formation répond donc le 404 habituel, avec son message :
```
Votre service commence à 07:00 : vos missions seront accessibles à partir de 06:30.
```
Affichez-le comme aujourd'hui, avec Réessayer. Le jeu est recréé chaque jour : les saisies de la veille ne sont plus visibles.

**g) Équipiers.** Chaque ambulancier en formation a **son propre** équipage fictif, avec un équipier fictif (« BLANC (FICTIF) Alex »). Deux ambulanciers en formation ne voient pas les saisies l'un de l'autre, contrairement au mode normal. C'est normal.

**h) Redémarrage du serveur.** Après un redémarrage, le statut et le contexte des missions fictives peuvent revenir à leur état de départ, alors que les horaires et signatures saisis restent. Ce n'est pas un bug de l'app.

### Lire un message d'erreur (dans le client API, une seule fois)

Les refus de cette fiche arrivent avec un **corps texte**, en principe `text/plain`. Écrivez un utilitaire unique, par exemple `lireMessageErreur(response): string | null` :
1. corps vide → `null` ;
2. corps JSON objet avec `Message` → `Message` (gel) ;
3. corps ProblemDetails → `detail`, sinon `title` (cas 503 existant) ;
4. corps JSON chaîne, entourée de guillemets → la chaîne ;
5. sinon → le texte brut.

Pourquoi le point 4 : selon l'en-tête `Accept` envoyé, le serveur peut renvoyer le même texte sous forme de chaîne JSON. L'utilitaire doit accepter les deux formes.

### Types TypeScript

À placer dans `@us/types`. Les trois premiers correspondent exactement aux réponses de `api/simulation` (le serveur ne leur donne pas de nom de DTO : nous les nommons ici).

```ts
/** Réponse de GET /api/simulation. */
export interface SimulationStatus {
  /** Le serveur ouvre le mode formation. */
  IsEnabled: boolean
  /** Personas sans compte acceptés (développement uniquement ; toujours false en production). */
  HasPersonas: boolean
  /** Cette requête est traitée en formation (en-tête envoyé et mode ouvert, ou persona). */
  IsSimulatedRequest: boolean
  /** Toujours "X-Vector-Simulation". */
  HeaderName: string
}

/** Élément de GET /api/simulation/personas. */
export interface SimulationPersona {
  /** 1 à 5. */
  Number: number
  /** Nom fictif à afficher. */
  Name: string
  /** Jeton à poser tel quel en "Authorization: Bearer <Token>". Pas un JWT : ne pas décoder. */
  Token: string
}

/** Réponse de POST /api/simulation/reset. */
export interface SimulationResetResult {
  /** Nombre de saisies effacées. Information de débogage, ne pas afficher. */
  DeletedRows: number
}

/** Corps du 409 « mission transférée en facturation » (existant). */
export interface MissionFreezeError {
  Message: string
}

/** Mode courant de l'app. "normal" = vraies missions. */
export type VectorMode = 'normal' | 'formation'

export const SIMULATION_HEADER = 'X-Vector-Simulation' as const
export const SIMULATION_HEADER_VALUE = '1' as const
```

Les types des autres routes (équipage, liste, détail…) ne changent pas.

## Écrans et comportements

### L'état du mode (un hook partagé, pas dans les composants)

Créez un module unique, par exemple `useModeFormation()` adossé à un petit store, qui expose :
- `mode: VectorMode` ;
- `entrer()` ;
- `sortir()` ;
- `recommencer()` ;
- `enCours: boolean` (transition en cours).

Le client API lit `mode` pour poser l'en-tête. **Un seul endroit décide du mode.** Pourquoi : si le bandeau, l'intercepteur et le cache lisaient chacun leur propre état, ils finiraient par diverger, et on obtiendrait un bandeau « formation » sur de vraies missions.

### Entrer dans le mode formation (séquence obligatoire, dans cet ordre)

1. L'ambulancier touche « Mode formation ». Une confirmation s'ouvre : « Passer en mode formation ? Vous travaillerez sur des missions fictives. Rien ne sera envoyé à la régulation. » Boutons « Commencer » et « Annuler ».
2. L'app appelle `GET /api/simulation` **avec** l'en-tête `X-Vector-Simulation: 1`, posé explicitement pour cet appel. Le mode n'est pas encore actif.
3. **Seule une réponse 200 avec `IsSimulatedRequest: true` permet d'entrer.** Tout le reste (404, 409, 200 avec `false`, erreur réseau) laisse l'app en mode normal et affiche le message prévu au tableau de la route 1.

   Pourquoi : un serveur d'une version antérieure **ignore** l'en-tête et servirait les vraies missions sous un bandeau « formation ». C'est exactement le danger à éviter.
4. Annuler les requêtes en cours, vider **tout** le cache : React Query en mémoire, **cache persistant** (`missions` compris) et **équipage épinglé**. Fermer les écrans de mission ouverts et revenir à l'accueil.
5. Passer `mode` à `formation`. Le bandeau apparaît.
6. Recharger comme au démarrage : `GET /api/crew/mine` (avec l'en-tête), puis le choix d'équipage habituel, puis `GET /api/JobList/{crewId}`.

Pour éviter qu'une réponse en retard de l'ancien mode ne se glisse dans le nouveau, **ajoutez le mode en tête de toutes les clés de requête**, par exemple `['formation', 'missions', crewId]` au lieu de `['missions', crewId]`. Gardez quand même la purge : les deux protections se complètent.

### Sortir du mode formation

1. Bouton « Quitter la formation ». Confirmation : « Revenir à vos vraies missions ? »
2. Annuler les requêtes en cours et vider **tout** le cache (mémoire, persistant, équipage épinglé), comme à l'entrée.
3. Passer `mode` à `normal`. Le bandeau disparaît.
4. Recharger : `GET /api/crew/mine` **sans** l'en-tête, puis le choix d'équipage, puis la liste.
5. Message bref : « Vous êtes revenu à vos vraies missions. »

Pas de vérification serveur à la sortie : le mode normal est toujours disponible.

**La déconnexion** en mode formation fait d'abord la sortie (purge comprise), puis la déconnexion habituelle.

### Écran A — Le bandeau « MODE FORMATION »

- Visible sur **tous** les écrans tant que `mode === 'formation'`, y compris les fiches mission, la signature, l'appareil photo de la carte mutuelle, les modales et les feuilles.
- Impossible à fermer ou à réduire.
- Contenu : « MODE FORMATION — missions fictives », et à droite le bouton « Quitter ».
- Couleur franche, qui ne sert nulle part ailleurs dans l'app (orange vif ou bandes hachurées, à valider avec nous). Elle doit se voir d'un coup d'œil, en plein soleil, téléphone posé sur le tableau de bord.
- Le bandeau ne doit **jamais** masquer un bouton d'action, par exemple le gros bouton d'étape : il **pousse** le contenu, il ne le recouvre pas.
- `accessibilityLabel` : « Mode formation actif, missions fictives ».
- En mode normal, il n'y a **aucun** bandeau. Aucune trace du mode ne reste.

Le menu ou l'écran du mode formation contient aussi « Recommencer la formation » (écran B).

### Écran B — « Recommencer la formation »

- Visible **uniquement** en mode formation.
- Confirmation : « Effacer tout ce que vous avez saisi aujourd'hui en formation ? Les 7 missions repartiront de zéro. »
- Pendant l'appel, le bouton affiche « Remise à zéro… » et un second appui est ignoré.
- 200 → purge du cache (le mode ne change pas), rechargement de l'équipage et de la liste, message « Formation remise à zéro ».
- Erreur → message et Réessayer, sans rien purger.

### Écran C — Le mode se ferme pendant une session (409 sur une route quelconque)

Si une route répond le 409 texte `Le mode formation n'est pas ouvert sur ce serveur.` pendant que l'app est en mode formation :
- écran bloquant : le texte du serveur, et un seul bouton « Revenir à mes missions », qui fait la sortie complète ;
- **pas de sortie automatique silencieuse.** Pourquoi : l'ambulancier se retrouverait sur ses vraies missions sans l'avoir vu passer, ce qui est l'inverse de ce qu'on cherche.

### Écran D — Connexion par persona (développement uniquement)

- **N'existe que dans les builds de développement**, derrière une variable d'environnement de build. Il **n'est jamais inclus** dans un build de production.
- Au lancement de l'écran de connexion, appeler `GET /api/simulation/personas` (anonyme, sans en-tête) :
  - **200** → afficher sous le bouton Keycloak une section « Développement : se connecter comme… » avec les 5 noms ;
  - **404**, erreur réseau ou autre → la section **n'apparaît pas**, sans message d'erreur.
- Toucher un persona :
  1. enregistrer son `Token` comme token de session (stockage sécurisé) ;
  2. marquer la session comme « persona » pour que la logique de rafraîchissement l'ignore ;
  3. passer `mode` à `formation` (bandeau compris) ;
  4. charger l'équipage et la liste.

  Envoyez **aussi** l'en-tête de formation : le serveur traite déjà un persona en formation, et l'en-tête garde l'app cohérente.
- 401 en session persona → retour à cet écran. Ne lancez pas Keycloak.
- « Quitter » sur le bandeau en session persona → déconnexion et retour à l'écran de connexion. Un persona n'a pas de vraies missions.

États de l'écran D :
- **chargement** : section masquée pendant l'appel ;
- **vide** : sans objet, le serveur rend toujours 5 personas ou 404 ;
- **erreur** : section masquée ;
- **succès** : la liste.

### États des écrans existants (accueil, liste, fiche) en mode formation

Ils ne changent pas :
- **chargement** : squelette habituel ;
- **vide** : « Aucune mission pour le moment » (ne devrait pas arriver en formation) ;
- **erreur** : message du serveur, lu par `lireMessageErreur`, et Réessayer ;
- **succès** : les missions fictives, sous le bandeau.

Toutes les règles de la fiche J-172 (verrou « Vu ») s'appliquent aussi aux missions fictives. Le jeu sert d'ailleurs à les recetter.

### Envois hors réseau

Cette règle vaut pour toute écriture que votre app met en file d'attente pour la rejouer au retour du réseau, aujourd'hui ou plus tard (voir Q4).
- **Chaque élément de la file enregistre le mode au moment de sa création** (`mode: VectorMode`).
- Au rejeu, la requête porte l'en-tête **selon ce mode enregistré**, pas selon le mode courant. L'intercepteur doit donc accepter un mode imposé par la requête (par exemple une option `vectorMode` dans la config de l'appel) et ne poser le mode courant que si aucun n'est imposé.
- Pourquoi : un pointage de formation rejoué sans l'en-tête partirait comme un vrai pointage. Un vrai pointage rejoué avec l'en-tête serait refusé (403 de cantonnement) et perdu.
- Si un envoi de formation rejoué reçoit le 409 « mode non ouvert », retirez-le de la file sans réessayer et journalisez-le. Un envoi de formation n'a aucune valeur hors formation.
- La **sortie** du mode ne vide pas la file : les envois de formation en attente partiront avec leur en-tête. La **remise à zéro**, elle, peut les laisser partir après coup. C'est sans gravité.

## Règles de validation côté client

- Valeur de l'en-tête : toujours `1`.
- « Recommencer la formation » n'est proposé qu'en mode formation.
- L'écran des personas n'existe que si le build de développement l'inclut **et** que la route répond 200.
- Pas de double appui sur « Commencer », « Quitter » ou « Recommencer » : bouton désactivé pendant la transition.
- Aucune autre saisie dans cette fiche.

## Contraintes techniques

- TypeScript strict, pas de `any`.
- Tous les appels passent par `@us/api`. L'en-tête de formation est posé par l'intercepteur de `createApiClient`, au même endroit que le token, jamais dans un composant.
- L'état du mode vit dans un seul module (hook et store). Le bandeau, l'intercepteur, les clés de cache et la file hors réseau le lisent tous là.
- Logique dans des hooks (`useModeFormation`, mutation de remise à zéro, requête des personas). Les composants ne font qu'afficher.
- `lireMessageErreur` est unique et partagé.
- URL de l'API par variable d'environnement, jamais en dur. L'inclusion de l'écran des personas est commandée par une variable de build.
- Aucune règle métier côté client. L'app ne décide jamais seule qu'une donnée est fictive : seul `GET /api/simulation` le dit.
- Le jeton persona n'est ni décodé ni rafraîchi.

## Critères d'acceptation

- **CA-1** — Étant donné un serveur qui ouvre le mode, quand je touche « Mode formation » puis « Commencer », alors le bandeau apparaît, l'équipage affiché est l'équipage fictif (véhicule « FORMATION ») et la liste montre les 7 missions fictives.
- **CA-2** — Étant donné le mode actif, alors **chaque** requête vers l'API porte `X-Vector-Simulation: 1` (à vérifier au proxy ou dans les journaux réseau, sur au moins la liste, le détail, un pointage, une signature et une photo).
- **CA-3** — Étant donné un serveur dont `GET /api/simulation` répond 404, quand je touche « Commencer », alors l'app reste en mode normal, affiche « Le mode formation n'est pas disponible sur ce serveur. » et aucun bandeau n'apparaît.
- **CA-4** — Étant donné un serveur dont `GET /api/simulation` avec l'en-tête répond 409, alors l'app affiche « Le mode formation n'est pas ouvert sur ce serveur. » et reste en mode normal.
- **CA-5** — Étant donné le mode actif, quand je touche « Quitter » puis confirme, alors le bandeau disparaît, mes vraies missions se rechargent sans l'en-tête, et **aucune** mission fictive n'apparaît nulle part, y compris hors réseau, après un redémarrage de l'app ou dans le cache persistant.
- **CA-6** — Étant donné mes vraies missions en cache, quand j'entre en formation (y compris hors réseau juste après), alors **aucune** vraie mission n'apparaît sous le bandeau.
- **CA-7** — Étant donné le mode actif, alors aucune requête ne part avec un identifiant d'équipage ou de mission du mode normal : aucun 403 « n'appartient pas au jeu de formation » pendant une utilisation normale.
- **CA-8** — Étant donné un 403 de cantonnement (simulé par mock), alors son texte s'affiche tel quel, je reste connecté, et « Retour à la liste » recharge l'équipage et la liste.
- **CA-9** — Étant donné le mode actif, quand le serveur répond 409 « mode non ouvert » sur une route quelconque, alors l'écran bloquant s'affiche avec ce texte et « Revenir à mes missions ». L'app ne sort pas seule du mode.
- **CA-10** — Étant donné la mission 6 du jeu (gelée), quand je tente une écriture, alors le message `Mission transférée en facturation : édition close.` s'affiche et l'app reste en formation : ce 409 n'est pas confondu avec celui du mode.
- **CA-11** — Étant donné des saisies sur plusieurs missions fictives, quand je touche « Recommencer la formation » et confirme, alors les 7 missions reviennent à leur état de départ (non vues, sans horaires, sans signature) et je reste en formation.
- **CA-12** — Le bandeau est visible sur chaque écran du mode, modales, signature et appareil photo compris. Il ne masque aucun bouton et ne peut pas être fermé.
- **CA-13** — Étant donné un build de développement et un serveur qui sert les personas, alors l'écran de connexion propose les 5 personas. En touchant « Camille ROUX », j'entre en formation sans Keycloak, bandeau compris.
- **CA-14** — Étant donné un serveur où `GET /api/simulation/personas` répond 404, ou un build de production, alors aucune trace des personas n'apparaît.
- **CA-15** — Étant donné une session persona, alors l'app ne tente jamais de décoder ni de rafraîchir le jeton. Un 401 ramène à l'écran des personas.
- **CA-16** — Étant donné un pointage fait hors réseau en formation, quand je quitte la formation puis que le réseau revient, alors ce pointage part **avec** l'en-tête de formation. Inversement, un pointage réel mis en file part **sans** l'en-tête, même si je suis entré en formation entre-temps.
- **CA-17** — Étant donné le mode actif, alors aucun appel à `/api/EmergencyPlatform` ne part.
- **CA-18** — Étant donné une déconnexion en mode formation, quand je me reconnecte, alors je suis en mode normal, sans bandeau ni donnée fictive en cache.

## Check-list de recette — les 7 missions du jeu

Chaque mission du jeu montre un cas d'écran précis. Une fois le serveur de développement publié, passez-les toutes, de préférence avec un persona, puis une fois avec un vrai compte de test. Les noms des patients portent « (FICTIF) ». Toutes les missions sont datées du jour.

La liste arrive triée par horaire. Elle commence donc par la mission 6 (07:00).

| # | Horaire | Ce que la mission montre | À vérifier dans l'app |
|---|---|---|---|
| 1 | 08:00 (RDV 08:45) | Ambulance, aller, domicile → centre hospitalier (fictif), service Cardiologie. Complément d'adresse (digicode, étage). Commentaire. **Contexte à choisir** parmi trois choix. | Heure de RDV affichée ; complément d'adresse ; commentaire ; choix du contexte possible et enregistré |
| 2 | 09:00, au plus tôt | VSL, aller. **Départ non structuré et non géocodé** (libellé seul : « Parking du stade, entrée nord, près du kiosque »). **Commentaire très long.** **Patient anonyme**, identité à saisir. | Pas de carte ni de GPS pour le départ, mais le libellé s'affiche ; texte long sans débordement ; saisie d'identité possible |
| 3 | 10:30 | **Déjà en cours**, VSL, retour hôpital → domicile. Sous-catégorie **TPMR**. **Contexte imposé** par la régulation. | Statut « en cours » ; sous-catégorie affichée ; contexte non modifiable |
| 4 | 12:15 (RDV 13:00) | Dialyse, VSL, aller. **Patient à vérifier.** **Arrivée non géocodée** (adresse sans coordonnées). | Signalement « à vérifier » ; arrivée affichée sans carte ni position |
| 5 | 14:00 (RDV 14:30) | Ambulance, aller, sous-catégorie Bariatrique. **Date de naissance figée** par la régulation (« Vérifiée par la régulation »). Motif « Autre » à préciser. | Date de naissance en lecture seule avec son motif ; saisie du texte « Autre » |
| 6 | 07:00 | **Terminée et transférée en facturation : gelée.** | Mission terminée, sans blocage des suivantes (J-172) ; toute écriture → 409 `Mission transférée en facturation : édition close.` affiché proprement |
| 7 | 16:00 | **Produits sanguins : aucun patient.** | Aucun bloc patient, aucune signature patient exigée ; pas de plantage sur l'absence de patient |

À vérifier en plus :
- [ ] Le verrou « Vu » (J-172) sur les missions fictives : elles commencent non vues, à voir dans l'ordre.
- [ ] La signature, la carte mutuelle (**photographiez une image fictive, jamais une vraie carte**), un document et une anomalie sur une mission fictive.
- [ ] « Recommencer la formation » après tout cela → tout repart de zéro.
- [ ] Les entrées et sorties du mode répétées, avec et sans réseau → aucun mélange (CA-5, CA-6).
- [ ] Deux personas différents sur deux téléphones → chacun ses missions.

## Données de test

Toutes les données sont fictives. Les identifiants `5e5e5e5e-…` ci-dessous sont des exemples **du 22/11/2026** : le serveur les régénère chaque jour.

**`GET /api/simulation` avec l'en-tête — mode ouvert (entrée permise)**
```json
{ "IsEnabled": true, "HasPersonas": true, "IsSimulatedRequest": true, "HeaderName": "X-Vector-Simulation" }
```

**`GET /api/simulation` sans l'en-tête — serveur qui propose le mode, sans personas (cas de la production une fois ouverte)**
```json
{ "IsEnabled": true, "HasPersonas": false, "IsSimulatedRequest": false, "HeaderName": "X-Vector-Simulation" }
```

**`GET /api/simulation` sans l'en-tête — mode fermé**
```json
{ "IsEnabled": false, "HasPersonas": false, "IsSimulatedRequest": false, "HeaderName": "X-Vector-Simulation" }
```

**`GET /api/simulation` avec l'en-tête — mode fermé** : `409`, `Content-Type: text/plain`
```
Le mode formation n'est pas ouvert sur ce serveur.
```

**`GET /api/simulation` — serveur d'une version antérieure** : `404`, sans corps.

**`GET /api/simulation/personas`** : voir l'exemple de la route 2. Variante fermée : `404`, sans corps.

**`GET /api/crew/mine` en formation (persona 1)** — libellés indicatifs, la forme est celle d'aujourd'hui :
```json
{
  "RequiresSelection": false,
  "RecommendedCrewId": "5e5e5e5e-0003-0145-00a0-010000000000",
  "Crews": [
    { "CrewId": "5e5e5e5e-0003-0145-00a0-010000000000",
      "DisplayLabel": "FORMATION · ROUX CAMILLE / BLANC (FICTIF) ALEX",
      "VehicleImmat": "FORMATION",
      "Members": "ROUX CAMILLE / BLANC (FICTIF) ALEX",
      "ServiceWindow": "07:00 – 17:00",
      "IsCurrent": true, "IsPending": false, "IsClosed": false }
  ]
}
```

**`GET /api/crew/mine` en formation avant 06:30** : `404`, texte
```
Votre service commence à 07:00 : vos missions seront accessibles à partir de 06:30.
```

**`GET /api/JobList/{crewId}` en formation (extrait, même forme que dans J-172)**
```json
{ "JobList": [
  { "Index": 1, "JobId": "5e5e5e5e-0004-0145-05a0-010000000000", "Patient": "FOURNIER (FICTIF) Louise", "TransportMode": 1, "TransportType": 0, "TransportSens": 2, "IsSerial": false, "IsSeen": false, "IsAck": false, "IsTerminated": true, "SignatureExists": false, "schedule": "07:00", "Appointment": null, "Departure": "Clinique du Littoral (fictive) — 20 rue du Port, 83500 La Seyne-sur-Mer", "Arrival": "Domicile — 12 rue des Lauriers, 83000 Toulon" },
  { "Index": 2, "JobId": "5e5e5e5e-0004-0145-00a0-010000000000", "Patient": "DUPONT (FICTIF) Jeanne", "TransportMode": 1, "TransportType": 0, "TransportSens": 1, "IsSerial": false, "IsSeen": false, "IsAck": false, "IsTerminated": false, "SignatureExists": false, "schedule": "08:00", "Appointment": "2026-11-22T08:45:00", "Departure": "Domicile — 12 rue des Lauriers, 83000 Toulon", "Arrival": "Centre hospitalier (fictif) — 1 boulevard de la Santé, 83100 Toulon" },
  { "Index": 7, "JobId": "5e5e5e5e-0004-0145-06a0-010000000000", "Patient": "", "TransportMode": 4, "TransportType": 0, "TransportSens": 1, "IsSerial": false, "IsSeen": false, "IsAck": false, "IsTerminated": false, "SignatureExists": false, "schedule": "16:00", "Appointment": null, "Departure": "Site de transfusion (fictif) — 3 rue de l'Hôpital, 83000 Toulon", "Arrival": "Centre hospitalier (fictif) — 1 boulevard de la Santé, 83100 Toulon" }
], "InstructionList": [] }
```
(La forme et l'ordre sont exacts. Les libellés précis, `TransportType` et `IsSerial` sont indicatifs.)

**`POST /api/simulation/reset`**
- succès : `200`, `{ "DeletedRows": 12 }` ; variante sans saisie : `{ "DeletedRows": 0 }` ;
- sans l'en-tête : `403`, texte `Remise à zéro réservée au mode formation (en-tête X-Vector-Simulation).`

**403 de cantonnement** (texte)
```
Mode formation : crewId 0b6f6c1e-2222-4a2b-9c3d-000000000009 n'appartient pas au jeu de formation.
```
Variante avec un texte long, pour tester le retour à la ligne :
```
Mode formation : gJobId 0b6f6c1e-1111-4a2b-9c3d-000000000001 n'appartient pas au jeu de formation.
```

**403 plateforme d'urgence** (texte) : `Cette fonction n'est pas disponible en mode formation.`

**409 gel** (JSON) : `{ "Message": "Mission transférée en facturation : édition close." }`

**Variante « chaîne JSON »** (à faire avaler par `lireMessageErreur`) : `409`, `Content-Type: application/json`, corps `"Le mode formation n'est pas ouvert sur ce serveur."` (avec les guillemets).

**401** sans corps exploitable ; **403** sans corps → « Accès refusé ».

## Questions ouvertes

- **Q1 — Où placer le bouton « Mode formation » ?** Nous pensons au menu du profil ou aux réglages, **pas** à l'accueil, pour qu'on ne l'active pas par erreur en service. Proposez-nous un emplacement avec une maquette rapide.
- **Q2 — Le mode doit-il survivre à un redémarrage de l'app ?** Rien n'est décidé. D'ici là, appliquez cette règle provisoire : **au lancement, l'app démarre toujours en mode normal.** Si elle a été fermée en formation (enregistrez ce fait dans le stockage), elle purge d'abord tout le cache de formation. Dites-nous si un stagiaire qui relance l'app au milieu d'une séance vous semble devoir retrouver le mode directement, après une nouvelle vérification serveur.
- **Q3 — L'app appelle-t-elle aujourd'hui `/api/EmergencyPlatform` ?** Si oui, depuis quels écrans ou à quel moment (pointage, position en tâche de fond) ? Il faut couper ces appels en formation.
- **Q4 — L'app met-elle déjà des écritures en file hors réseau** (photos, signatures, autre) ? La fiche J-172 partait du principe que non. Si c'est le cas, la rubrique « Envois hors réseau » s'applique tout de suite. Sinon, elle s'appliquera le jour où une file existera.
- **Q5 — Notifications.** Si l'app reçoit des notifications de vraies missions, que doit-il se passer quand l'une arrive en mode formation ? Notre proposition : l'afficher avec la mention « Vraie mission », et au toucher, proposer « Quitter la formation pour l'ouvrir ? ». Votre avis ?
- **Q6 — Charte du bandeau** : couleur et motif à valider avec nous avant livraison.
