# Plan de développement — Erp.Vector

> **Édition du** 2026-09-19 (soir) · **En production** `03f9d01` *(`main`, publié le 19/09 à 16:46,
> constaté par sourcelink et journal)* · **191 tests verts** · Dépôt `github.com/esv83/Erp.Vector`
> (`USVector.sln`) · Prod `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`)
>
> ✅ **Six itérations en production le même jour**, publiées à travers la nouvelle garde de
> déploiement : routes de la facturation fermées, lots adoptés par la facturation (sa part Vector passe
> de 12,8 s à 6,8 s par journée), jeton de service par le paquet partagé, motif du refus de conducteur.
> Détail : [`delivered.md`](delivered.md).
>
> ⏳ **Ce qui pèse le plus est désormais ailleurs** : la lecture groupée chez Orders (tout ce qui reste
> de la part Vector de la facturation), le rattachement des comptes (RH), la règle d'applicabilité des
> types (Orders) — *itération 14*.
>
> Ce document se régénère sur demande — prompt **« compact devplan »** — et il est **relevé sur l'état
> réel du dépôt, de la production et des modules voisins**. **Il ne porte que l'ouvert** : le livré,
> daté, vit dans [`delivered.md`](delivered.md).
>
> **Règle de travail** : on code neutre ou additif, jamais de rupture du contrat consommé par l'app
> web — elle n'est pas déployée en même temps que l'API (D14).

**Cinq rubriques, communes à tous les dépôts de l'ERP.** Le format est partagé ; les données ne le
sont jamais.

## 📍 Point de reprise — 19/09 au soir, la facturation n'attend plus Vector

**La journée a fermé les deux chantiers qui tenaient la facturation.** La sonde a prouvé qu'elle
passait avec son jeton : les quatre routes qu'elle tire exigent désormais un jeton, sans un seul refus
constaté. Et elle acquiert une journée en quelques appels au lieu de 559 — même résultat, à l'octet.

> ⚖️ **La garde de déploiement a servi dès sa première publication** : `main` propre et poussé, commit
> relu dans le `.pdb`, et le `nlog.config` périmé depuis juin enfin remplacé — la sonde retrouvera son
> propre fichier au premier appel anonyme.

**Ce qui reste ici est de taille modeste** — constats, robustesse, dettes — **ou attend une décision** :
le kilométrage, la fin de service, la présence. Le plus lourd est la protection des données du patient.

---
# 1. Fonctionnalités livrées

*Ce que le module sait faire **en production**, en une page. Le récit de chaque livraison vit dans
[`delivered.md`](delivered.md), **à jour au 19/09 au soir**.*

| Domaine | Ce que le module apporte aujourd'hui |
|---|---|
| **Se connecter** | L'ambulancier entre avec son **compte d'entreprise** · l'app retrouve seule son ou ses équipages du jour et lui fait choisir celui qu'il occupe · ses missions sont visibles **30 minutes avant sa prise de service** · quand l'accès est refusé, **le message dit pourquoi et quoi faire** |
| **Confirmer sa prise de service** | L'app lui dit **qu'une confirmation l'attend**, même la veille, et il confirme **sans passer par le courriel** |
| **Voir son travail du jour** | La liste des missions **engagées** par la régulation · le détail : patient, adresses, horaires, sens, service destinataire · l'affichage des lieux est composé par le serveur |
| **Faire avancer la mission** | Cinq étapes horodatées, **annulables** · « mission vue » · signature du patient · choix du conducteur — **un refus dit pourquoi** · **tout remonte à la régulation en quasi temps réel**, un envoi en échec est rejoué sans bloquer la saisie |
| **Compléter le dossier** | Type de mission et informations de facturation **servis par la régulation**, remplis et verrouillés quand la fiche patient les connaît · un refus arrive **avec son motif** · anomalies, documents et photos |
| **Photographier la carte mutuelle** | Depuis la mission, avec ses quatre champs · la régulation et la facturation peuvent **l'afficher** sur leurs écrans |
| **Passer à la facturation** | Une mission clôturée devient transférable d'elle-même · la facturation tire **un dossier par mission, désormais par lots** de 200, et les signatures par lots de 50 · une fois transmis, **le dossier est gelé** |
| **Tenir debout** | **Le terrain n'écrase jamais la donnée officielle** · API fermée par défaut : la facturation et l'app entrent **avec leur jeton** · Vector présente le sien à Orders · la production ne se publie **que depuis `main` propre et poussé** |

### Les réserves à ne pas perdre de vue

- ⚠️ **Deux routes restent anonymes** : l'affichage de la carte mutuelle dans les écrans d'Orders et de
  la facturation (`M9`) — une balise `<img src>` ne porte pas de jeton *(rubrique 3)*.
- ⚠️ **Un ambulancier rattaché depuis l'écran d'Employee seul n'entre pas** : 8 membres d'équipage sur
  244 sans compte reconnu *(13/09 — non remesuré)* *(itération 14)*.
- ⚠️ **Ouvrir l'app avant que la régulation ait composé l'équipage échoue** — 23 min d'attente
  médiane *(juillet-août)*. Le message dit quoi faire.
- ⚠️ **Les 7 types de mission sont proposés partout**, faute de règle d'applicabilité *(25/08)*.
- ⚠️ **Un n° de sécurité sociale mal tapé part en facturation** : aucun module ne le corrige, et
  l'écran ne le fait pas relire *(demandé au dev web le 26/08)*.

---
# 2. Code bloqué, manquant ou en attente

*Classé par itération de session de codage, **du plus simple au plus complexe**. Une itération = une
session de travail réaliste. **L'ordre est un ordre de difficulté, pas de priorité.** Chaque itération
porte entre crochets son **ancienne référence** : d'autres dépôts et documents la citent encore. **Les
numéros changent d'une édition à l'autre — une entrée se cite par son titre.***

## Itération 1 — Constater et mesurer en production *[ex-F1, B2, B8 ; suites du 19/09]*

| | |
|---|---|
| **Nature** | Constats et mesures — aucun code |
| **Effort** | Une petite session, plus des constats au fil de l'eau |
| **Bloqué par** | ⚠️ Un **compte de lecture** pour les mesures : `ErpAccount` est refusé depuis le poste de dev *(13/09 — non revérifié)*. À défaut, les journaux et le paquet terrain |

**À constater quand l'événement arrivera** — le code est en production depuis le 19/09 :
- **Un refus de conducteur** apparaît en `WARN` avec la phrase d'Orders, et plus en `ERROR`. Aucun
  refus depuis la publication. Que l'**écran** affiche la phrase dépend du dev web.
- **Le fichier `usvector-surface-anonyme-*.log`** se crée au premier appel anonyme — seules les deux
  routes d'affichage de la carte en produisent encore.
- **La garde de déploiement refuse** une autre branche, ou un `main` non poussé : jamais éprouvé en
  réel.

**À mesurer** :
- **Taux de remplissage de la carte mutuelle** (cartes / missions) — il dira si la lecture automatique
  *(itération 10)* vaut la peine.
- **Les étapes de mission vides** : ~3 883 annoncées avant le repli d'Orders (livré le 23/07).
  Constater qu'elles ont disparu.
- **Les adresses « non structurées »** : chaque cas est journalisé, personne n'a compté.

## Itération 2 — La liste des documents charge leurs contenus *🆕 relevé le 19/09*

| | |
|---|---|
| **Nature** | Performance — une route de l'app |
| **Effort** | Une session courte |
| **Bloqué par** | Rien |

`GET` documents d'une mission (`DocumentController.cs:59`) passe par `DocumentRepository.ListByMission`,
qui sort **le contenu de chaque document** de la base pour n'en rendre que les métadonnées. Le même
défaut vient d'être retiré du paquet terrain (`FieldDataQueryService`) ; la liste de l'app l'a gardé.
⇒ Une projection sans `DOC_CONTENT`, comme pour la carte mutuelle le 26/08.

## Itération 3 — Un jeu de requêtes rejouables *[ex-G3, MOB-9]*

| | |
|---|---|
| **Nature** | Outil de vérification |
| **Effort** | Une session |
| **Bloqué par** | Rien |

Le fichier `.http` ne couvre ni la liste, ni les étapes, ni la signature *(13/09)*, ni les deux routes
en lot. **Fin** : connexion → liste → détail → étapes → signature → lots, rejouables après chaque
publication.

## Itération 4 — Des appels sortants qui ne pendent pas *[ex-D, DEC-7]*

| | |
|---|---|
| **Nature** | Robustesse — **plus pressant depuis les lots** |
| **Effort** | Une session |
| **Bloqué par** | Rien |

Les trois clients vers Orders n'ont **ni délai court, ni nouvelle tentative, ni disjoncteur** en
lecture — 100 secondes par défaut *(13/09)*. Depuis le 19/09, **un seul appel à Orders qui pend retient
tout un lot de 200 paquets**. ⇒ Délai explicite, puis `AddStandardResilienceHandler`, **en gardant** la
tolérance au 404 — et sans relancer en rafale un Orders qui sert aussi la régulation.

## Itération 5 — Dire l'heure qu'il est *[ex-E2]*

| | |
|---|---|
| **Nature** | Données ambiguës transmises à la facturation |
| **Effort** | Une session, **à coordonner avec la facturation** |
| **Bloqué par** | Rien |

Les étapes sont en UTC **sans le déclarer** ; l'heure de signature est écrite **en heure locale**
(`SignatureRepository.cs:34`, `:43`). Même motif à examiner dans `ClMarkMissionSeenUseCase`,
`ClSetDriverUseCase`. **Fin** : tout en UTC, fuseau **déclaré** dans le contrat du paquet.

## Itération 6 — Savoir ce qui tourne *[ex-G8, partie 2]*

| | |
|---|---|
| **Nature** | Traçabilité d'exploitation |
| **Effort** | Une session |
| **Bloqué par** | Rien — la garde de publication est en service depuis le 19/09 |

Le `.pdb` donne le commit ; rien ne dit **la base** à laquelle l'API parle, ni les drapeaux en vigueur.
Un correctif a déjà été joué sur la mauvaise base Orders sans erreur *(25/08)*. ⇒ Une route réservée
qui expose commit, environnement, **base résolue** (sans identifiants) et drapeaux.

## Itération 7 — Suivre les migrations SQL *[ex-G4]*

| | |
|---|---|
| **Nature** | Divergence de schéma — **a déjà coûté une journée sans données terrain** *(06/08)* |
| **Effort** | Une session, plus une réconciliation |
| **Bloqué par** | Rien |

Aucune table de suivi : prod et dev ont divergé en sens inverse. ⇒ Table de suivi (modèle
`__BillingGatewaySchema`) et **contrôle au démarrage**. À réconcilier : la migration du transfert est
`027` dans l'historique, `034` dans le dépôt.

## Itération 8 — Les dettes de forme *[ex-G2, G5]*

| | |
|---|---|
| **Nature** | Hygiène — aucun changement de contrat |
| **Effort** | Plusieurs petites sessions, à piocher |
| **Bloqué par** | Rien, sauf les alias *(le front)* |

- `ListMissionsAsync` n'a plus aucun appelant : à supprimer.
- `AddressApi:BaseUrl` : configurée, lue par aucun code — la retirer des `appsettings` *(19/09)*.
- `ClAutorizationCommand.AutorizeJob` rend **toujours `true`** : reste de l'ancienne session mobile,
  appelé par `GetSignature` *(19/09)* — le retirer.
- `ClReliableValue` et `ClValueInfo` (socle, `0 - Outils`) n'ont plus d'usage depuis le retrait de la fin
  de service *(19/09)*.
- Nommage des DTO en `…DtoIn` / `…DtoOut` — aucun impact JSON.
- Pont synchrone/asynchrone (`.GetAwaiter().GetResult()`) sur liste, détail, identité, conducteur.
- `IResultUseCase` est synchrone : les cas d'usage asynchrones n'implémentent aucune interface.
- **Alias de compatibilité** (`IsAck`, champs historiques du détail, `SelectedDriver` jamais nul,
  champs typés des lieux) — retrait **sur confirmation du front uniquement**.

## Itération 9 — Le kilométrage dans le dossier transmis *[ex-E1, MOB-10]*

| | |
|---|---|
| **Nature** | Champ attendu par la facturation, toujours vide |
| **Effort** | Une session si le km véhicule suffit ; **plusieurs** s'il faut un relevé par mission |
| **Bloqué par** | 🔴 **Un arbitrage avec la facturation** |

Le kilométrage appartient à l'équipage et au véhicule, pas à la mission. Km du véhicule, ou relevé
début/fin par mission (table, saisie mobile, paquet) ?

## Itération 10 — Lire la carte mutuelle automatiquement *[ex-F2, P3]*

| | |
|---|---|
| **Nature** | Fonctionnalité neuve |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | **La mesure du remplissage** *(itération « Constater et mesurer en production »)* |

Extraction **asynchrone** par un modèle de vision, quatre champs proposés avec leur confiance, puis
**validation humaine** — jamais d'écriture aveugle. À cadrer : où tourne l'appel (DMZ ou LAN), le coût
par carte. Rappel : même validés, ces champs **n'alimentent pas** la colonne mutuelle de facturation.

## Itération 11 — La fin de service *[ex-F3, MOB-12]* ✅ *routes retirées le 19/09 — reste à publier*

| | |
|---|---|
| **Nature** | Code hérité, cassé, et contraire à une règle d'Orders |
| **Effort** | Fait — option « retirer » retenue le 19/09 |
| **Bloqué par** | **La publication** |

**Recadrage fait avant de coder** :
- **Contraire à la règle d'Orders** du 13/09 : « Qui connaît l'heure de fin ? Le régulateur, jamais
  l'ambulancier » — le laisser déclarer sa propre fin ouvre une fraude que rien ne peut contredire.
- **Cassé** : le contrôleur n'avait pas de préfixe de route (il répondait à `/{CrewId}`), le paramètre
  n'était pas lu (`intCrewId` toujours vide) ; le `POST` marquait la date « FromRegulation » et
  appelait `ICrewRepository.Update` — qui désigne le **conducteur** chez Orders.
- **Inutilisé** : aucun appel en production du 15 au 19/09.

**Retiré** : `EndOfServiceController`, les quatre fichiers `UseCases/EndOfService`, et la « fin de
service fiabilisée » du domaine (`ClCrew.ServiceEndDateR`, `ClReliableEndOfService`,
`ClReliableEndOfServiceValue`), qui ne servait qu'à eux. `ICrewCache` reste (kilométrage). Note au dev
web complétée ([`note_web_alexandre_routes_retirees.md`](note_web_alexandre_routes_retirees.md)).
191 tests verts.

**Écarté, à rouvrir côté Orders s'il le faut** : laisser l'ambulancier **signaler** une heure fausse,
sans la déclarer — piste qu'Orders range dans ses fonctionnalités envisagées.

## Itération 12 — Positions et statuts des véhicules *[ex-F3, MOB-16]*

| | |
|---|---|
| **Nature** | Connecteurs portés, jamais recâblés |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | Rien de connu — non réexaminé depuis juillet |

GpsGate (positions, REST) et Sirus (statuts, UDP) sont injectés mais ne servent à rien.

## Itération 13 — Protéger les données du patient *[ex-G7, P4]*

| | |
|---|---|
| **Nature** | RGPD — dette assumée |
| **Effort** | Plusieurs sessions, un seul lot |
| **Bloqué par** | Rien |

Documents, carte mutuelle et anomalies servis par une API exposée : **rétention et purge** (3 ans),
chiffrement au repos, fermeture des deux routes d'affichage de la carte (`M9`), **audit des accès**.

## Itération 14 — Ce qui attend ailleurs *[ex-A3, B, C1, C3, E4, F4]*

*Rien à coder ici tant que l'autre partie n'a pas bougé. Non revérifié à cette édition sauf mention,
et **c'est dit**.*

| Entrée | Qui doit bouger | Dernier relevé | En deux mots |
|---|---|---|---|
| **Lecture groupée « mission → commande → bénéficiaire »** *(B5, suite)* | Orders — **demande à lui porter** | 19/09 | Chaque mission d'un lot coûte encore un appel unitaire à Orders (~20 ms, 8 simultanés) : c'est tout ce qui reste de la part Vector de la facturation (6,8 s par journée). Une lecture par liste d'identifiants ramènerait chaque lot à **un** appel |
| **Exiger un jeton du terrain** | Orders | 19/09 | Plus rien ne l'en empêche côté Vector : les trois clients Orders portent le jeton de service `erp-vector-api` |
| **Règle d'applicabilité des types** *[B9]* | Orders *(itération « Restreindre un type »)* + décision métier | 19/09 | Les 7 types proposés partout. Orders l'a placée en tête par priorité **parce que Vector s'y déclare bloqué** |
| **Rattachement des comptes** *[C1]* | Orders, Identity, **RH** | 13/09 | Vector lit `PER_KEYCLOAK_MAP`, que l'écran d'Employee n'alimente pas. Débloqué par la bascule d'Orders sur le carnet d'Identity, bloquée par **273 personnels sans fiche Employee**. En attendant : [consigne](docs/auth/consigne-rattachement-ambulancier.md) **à transmettre à la régulation et à la RH** |
| **Écran : dire pourquoi un champ est grisé, faire relire le n° de sécurité sociale** *[A3]* | dev web | 26/08 | L'API envoie déjà le motif du verrou ; le NIR n'est **jamais** corrigeable après coup |
| **Écran : bouton *Réessayer* au sélecteur, et le motif du refus de conducteur** *[C3]* | dev web | 19/09 | Contrat : [`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md). Note des [routes retirées](note_web_alexandre_routes_retirees.md) à transmettre aussi |
| **Composer les équipages avant la prise de service** *[C3]* | 🔴 régulation — décision | 13/09 | Sans quoi l'accès anticipé de 30 min ne sert à rien. ⚠️ Le filtre d'appartenance (`MobileIdentityResolver.cs:35`) est **volontaire** |
| **`REFERENCE` et `URGENT`** *[B10]* | décision métier | 19/09 | Absents du catalogue ; tout le reste est servi |
| **`Billed` : l'écrire, ou retirer le palier** *[B4, E4]* | 🔴 décision | 13/09 | La facturation est en lecture seule par décision de son module |
| **Tests du transfert côté Orders** *[B6]* | Orders | 13/09 | Aucun filet sur la dérivation du statut et les gardes du transfert |
| **Relance des missions terminées non clôturées** *[B7]* | Orders | 13/09 | Des dossiers n'arrivent jamais en facturation |
| **Présence : qui est connecté** *[F4]* | 🔴 décision + cadrage **RH/RGPD** | 13/09 | Spec sans code : [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md) |

---
# 3. Fonctionnalités envisagées en Vn

*Non engagé, non planifié — avec la condition qui les remettrait sur la table.*

| Sujet | En deux mots | Ce qui la rouvrirait |
|---|---|---|
| **Fermer les routes d'affichage de la carte** *[M9]* | L'image courante et la présence restent anonymes : une balise `<img src>` ne porte pas de jeton. **Aucun appel constaté** du 15 au 19/09 | Les écrans d'Orders et de la facturation passent à un appel authentifié |
| **Lots en parallèle pour la facturation** | ~3 s de gain par journée, 16 appels simultanés vers Orders — **refusé le 19/09** | La lecture groupée chez Orders, qui rend la question sans objet |
| **Base Vector dédiée** *[Vd-1]* | Seul jalon DMZ non conditionné à la V2 | Pertinent dès maintenant ; personne ne l'a porté |
| **Accès anticipé à cheval sur minuit** *[CREW-2]* | Correctif connu | Les vacations de nuit ne sont pas concernées *(décision du 02/08)* |
| **Durcissement DMZ événementiel, push temps réel** *[Vd-2 à Vd-4, Vd-7, Vd-8]* | [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md) | Une exigence d'exposition, ou le polling qui ne suffit plus |
| **Photos hors SQL, masquage** *[Vd-6, Vd-5]* | NIR partiel, équipage retour | Le volume, ou la protection des données du patient |
| **Contrats partagés avec Orders** *[4b]* | Écart JSON assumé | Une rupture de contrat constatée |
| **Repère de fraîcheur du dossier** *[E5]* | `updatedAt` est servi, personne ne s'en sert | Un besoin de resynchronisation |
| **Éviction ciblée du cache d'identité, mode hors ligne, géolocalisation avancée, renommage `USVector` → `Vector`** | — | Une demande |

---
# 4. Décisions tranchées — ne pas les rejouer

*Les décisions appliquées vivent dans [`delivered.md`](delivered.md) §4 — **celles du 19/09 au §4.3** :
fermer sur une mesure, lots par liste d'identifiants, pas plus de 8 appels vers Orders, le paquet
partagé sans toucher la configuration, publier depuis `main` seulement. Ce qui suit est la façon dont
ce plan se tient.*

| Date | Décision, et pourquoi |
|---|---|
| 2026-08-24 | **D14 — on code neutre ou additif.** L'app web n'est pas déployée avec l'API |
| 2026-09-13 | **Le livré sort du plan et entre, daté, dans `delivered.md`** au prompt « compact devplan » |
| 2026-09-19 | **Une attente envers l'amont se vérifie chez l'amont avant d'être reconduite.** Ce plan a attendu d'Orders pendant **huit semaines** un repli livré le 23/07 |
| 2026-09-19 | **Une entrée se cite par son titre**, l'ancienne référence entre crochets |
| 2026-09-19 | **Une édition qui n'a pas tout revérifié le dit**, avec la date du dernier relevé |

---
# 5. Journal des livraisons

*Le journal daté vit dans [`delivered.md`](delivered.md) §2, les incidents au §8.*

## Ce qui a quitté la rubrique 2 à cette édition

| Entrée sortie | Pourquoi |
|---|---|
| **Fermer les quatre routes de la facturation** *[C2, DEC-6]* | ✅ En production le 19/09 : 559 appels de la facturation en 200 avec jeton, aucun refus |
| **Rendre impossible une publication hors de `main`** *[G8, partie 1]* | ✅ En production le 19/09, et première publication passée par la garde. Refus jamais éprouvés en réel *(itération 1)* |
| **Remettre la documentation d'équerre** *[G6]* | ✅ Close le 19/09 |
| **Dire à l'ambulancier pourquoi le conducteur est refusé** *[G9 du 13/09]* | ✅ En production le 19/09. Reste à le voir sur un premier refus *(itération 1)* |
| **Prendre le gestionnaire de jeton du paquet partagé** *[C2, reliquat]* | ✅ En production le 19/09, jeton obtenu au démarrage |
| **Le dossier terrain en lot**, **les images de signature en lot** *[B5]* | ✅ En production le 19/09, adoptés par la facturation : 12,8 s → 6,8 s par journée |

**Entrées neuves** : la liste des documents qui charge leurs contenus *(itération 2)* ; la lecture
groupée à demander à Orders, et Orders qui peut exiger un jeton du terrain *(itération 14)*.

---

## Annexe — documents voisins

| Doc | Ce qu'il apporte |
|---|---|
| [`delivered.md`](delivered.md) | Ce que le module fait, journal daté, décisions, configuration, pistes retirées, incidents |
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Le besoin et le vocabulaire |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Carte mutuelle |
| [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md) | Projection du terrain vers Orders |
| [`TRACABILITE_SAISIES_VECTOR_EXPORT.md`](TRACABILITE_SAISIES_VECTOR_EXPORT.md) | Des saisies Vector aux 91 colonnes de facturation |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Authentification de service, résilience |
| [`endPoint.md`](endPoint.md) | Ce que Vector attend d'Orders.Api |
| [`docs/auth/diag-404-second-membre-equipage.md`](docs/auth/diag-404-second-membre-equipage.md) | Diagnostic du sélecteur |
| `note_web_alexandre_*.md`, `note_ui_alex.md`, `docs/ui-web/*` | Ce qui est promis au dev web |

**Fin du document**
