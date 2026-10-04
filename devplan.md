# Plan de développement — Erp.Vector

> **Édition du** 2026-10-04 (nuit) · **En production** `9c15f94` — **constaté par
> `GET /vector/api/version`** à 16:04 : commit, `Tree: clean`, environnement Production ; `.pdb`
> concordant ; **schéma 9 sur 9** · **287 tests verts** · Dépôt `github.com/esv83/Erp.Vector`
> (`USVector.sln`) · Prod `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`)
>
> ✅ **La carte mutuelle sort du terrain** : l'équipage sait qu'elle est connue, les écrans de la
> régulation et de la certification peuvent la lire — dès qu'on leur aura ouvert la porte.
>
> ⏳ **Ce qui reste est surtout chez les autres** : les clients Keycloak des écrans, le front de Jules,
> l'écran d'Alexandre. Chez nous : voir passer la semaine, puis démonter deux échafaudages.
>
> Ce document **se régénère** au prompt *« compact devplan »* et ne porte **que l'ouvert**. Le
> **pourquoi** vit dans [`decided.md`](decided.md), à lire avant de coder ; le **fait**, horodaté et
> constaté, dans [`delivered.md`](delivered.md).
>
> **Règle de travail** : neutre ou additif, jamais de rupture du contrat que consomme l'app web — elle
> n'est pas déployée en même temps que l'API. Envers l'amont, une route neuve se consomme avec un
> repli sur son absence. **Une colonne lue par EF se crée en base avant la publication.**

**Cinq rubriques, communes à tous les dépôts de l'ERP.** Le format est partagé ; les données ne le
sont jamais.

## 📍 Point de reprise — 04/10 dans la nuit : quatre publications, une journée

**Quatre publications le 04/10** : le 503 quand Orders est à terre, la lecture par lot, l'itération des
dettes de forme, puis la carte mutuelle — la dernière à 16:04, **`MOB_011` joué avant**, comme il se
doit : le schéma s'est annoncé « 9 sur 9 ».

> ⚖️ **La carte mutuelle est restée où elle naît.** Elle aurait pu partir chez Orders ou dans un module
> de documents ; le premier n'a aucun champ mutuelle sur un patient souvent tenu par AidesNSoft, le
> second n'existe pas. Ce sont les écrans qui viennent la lire, avec leur jeton, par mission.

**Par quoi reprendre — lundi 05/10** : le relevé de « Constater ce qui vient d'être publié ». Puis, s'il
est propre, retirer le repli de la lecture par lot. Et relancer la demande des clients Keycloak.

---
# 1. Fonctionnalités livrées

*Ce que le module sait faire **en production**, en une page. Le récit daté vit dans
[`delivered.md`](delivered.md), à jour au 04/10 dans la nuit.*

| Domaine | Ce que le module apporte aujourd'hui |
|---|---|
| **Se connecter** | Compte d'entreprise · l'app retrouve seule l'équipage du jour et fait choisir quand il y en a plusieurs · missions visibles **30 min avant la prise de service** · un accès refusé **dit pourquoi et quoi faire** |
| **Confirmer sa prise de service** | L'app dit **qu'une confirmation attend**, même la veille, et l'ambulancier confirme **sans le courriel** |
| **Voir son travail du jour** | Missions **engagées** par la régulation · détail : patient, adresses, horaires, sens, service destinataire, **carte mutuelle connue et date de sa photo** · affichage des lieux composé par le serveur |
| **Faire avancer la mission** | Cinq étapes horodatées, **annulables** · « mission vue » · signature · conducteur — **un refus dit pourquoi** · tout remonte à la régulation en quasi temps réel, un envoi en échec est rejoué |
| **Compléter le dossier** | Type de mission et informations de facturation servis par la régulation, pré-remplis et verrouillés quand la fiche patient les connaît · un refus arrive **avec son motif** · anomalies, documents, photos |
| **Photographier la carte mutuelle** | Depuis la mission · **une nouvelle photo garde les données validées**, marquées « repris de la photo du … » · **lisible par les écrans de régulation et de certification, par mission, avec jeton** (dès leurs clients déclarés) · journal des accès · lecture automatique en place, inerte |
| **Passer à la facturation** | Transfert automatique à la clôture · dossier **par lots de 200**, signatures par lots de 50 · Orders lu par lot (repli mission par mission tant que non vu) · dossier **gelé** après transfert |
| **Tenir debout** | Le terrain n'écrase jamais la donnée officielle · API fermée par défaut, jetons exigés · délais et disjoncteur vers Orders, **vus sur de vraies pannes** · réessai SQL · **une panne rend 503 et un message ; un refus, son motif** · aucun appel bloquant · publication **depuis `main` propre et poussé**, commit servi et schéma vérifiés |
| **Se dire** | `api/version` : commit, **état de l'arbre au build**, environnement · `api/version/runtime` : base résolue, drapeaux, **état du schéma** |

### Les réserves à ne pas perdre de vue

- ⚠️ **Les écrans ne lisent encore rien** : `Keycloak:ScreenAzp` est vide en production, faute de
  connaître leurs clients Keycloak.
- ⚠️ **Deux routes de la carte restent anonymes** — elles ne servent à personne (zéro appel du 21/09 au
  04/10) — *voir « Fermer les routes anonymes de la carte »*.
- ⚠️ **Le message du 503 n'atteint l'ambulancier que si l'app l'affiche** — demandé au dev web le 04/10.
- ⚠️ **Un ambulancier rattaché depuis Employee seul n'entre pas** : 8 membres sur 244 *(13/09, non
  remesuré)*.
- ⚠️ **Ouvrir l'app avant que l'équipage soit composé échoue** — 23 min d'attente médiane
  *(juillet-août)*. Le message dit quoi faire.
- ⚠️ **Tous les types de mission sont proposés partout** : 11 au catalogue, tables de restriction vides
  *(relevé chez Orders le 04/10)*.
- ⚠️ **Un n° de sécurité sociale mal tapé part en facturation** *(demandé au dev web le 26/08)*.

---
# 2. Code bloqué, manquant ou en attente

*Classé par itération de session de codage, **du plus simple au plus complexe**. **L'ordre est un ordre
de difficulté, pas de priorité.** Chaque itération porte entre crochets son **ancienne référence**.
**Dans une référence croisée, on nomme la rubrique.***

## Itération 1 — Constater ce qui vient d'être publié *[ex-F1, B2, B8]*

| | |
|---|---|
| **Nature** | Constats et mesures — aucun code |
| **Effort** | Un relevé lundi, puis au fil de l'eau |
| **Bloqué par** | **Le trafic de la semaine** |

**À voir passer** — `9c15f94` en production depuis le 04/10 à 16:04 ; 35 s de trafic vues, sans erreur :
- **les routes rendues asynchrones, sous une vraie charge** : sélecteur, conducteur, liste, détail,
  timeline, signature en 200, **aucune exception non gérée** ;
- **un détail de mission dont le patient a une carte** : `MutuelleCardKnown` vrai, date de la photo ;
- **une deuxième photo d'un patient dont la carte est validée** : champs repris, `FieldsInheritedFrom`
  posé — vérifiable en base ;
- **la passe de la facturation par la route par lot** : aucun `/missions/{id}/full` pendant les lots,
  aucune ligne « Orders ne porte pas batch-refs » ;
- **une panne rendue en 503** : `WARN … 503 rendu` — à la prochaine coupure ou publication d'Orders ;
- **un refus de conducteur** — jamais vu (344 changements en 200 du 21/09 au 04/10) ;
- **la suite [`Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http)** rejouée une fois en entier,
  identifiants en main — y compris `api/Kilometers` en 404 et la présence par mission.

**À comprendre**, relevés le 04/10 et non examinés :
- **988 `403` sur `JobDetail`** et 345 sur `Joblist` en 14 jours — refus normal ou écran qui insiste ?
- **51 démarrages du pool en 14 jours**, clés de protection éphémères à chacun.
- **Pas de passe de la facturation le jeudi 24/09.**

**À mesurer chez Orders** : les **étapes de mission vides** (~3 883 annoncées avant leur repli du
23/07) — un comptage dans leur base.

## Itération 2 — Retirer le repli de la lecture par lot

| | |
|---|---|
| **Nature** | Échafaudage à démonter |
| **Effort** | Une courte session |
| **Bloqué par** | **Une passe de la facturation vue sur la route par lot** — voir « Constater ce qui vient d'être publié » |

Orders sert la 1.8.5 depuis le 04/10. Retirer `ReadRefsOneByOneAsync`, le **corps par défaut** de
`IErpReadApiClient.GetMissionBatchRefsAsync` et le traitement 404/405 du client — une route absente
redevient une panne. Les doubles de test implémentent alors la méthode.

## Itération 3 — Fermer les routes anonymes de la carte 🆕 *[ex-M9]*

| | |
|---|---|
| **Nature** | Échafaudage à démonter — donnée de santé servie sans jeton |
| **Effort** | Une courte session |
| **Bloqué par** | **Jules** : qu'il confirme ne pas s'en servir et être passé aux routes par mission |

`POST api/mutuelle-card/presence` (par bénéficiaire) et `GET api/beneficiaries/{id}/mutuelle-card/image`
ont été ouvertes pour un affichage par `<img src>` que personne n'a branché : **zéro appel** du 21/09 au
04/10. Relancer la mesure (sonde anonyme recoupée au journal principal), puis retirer
`[AllowAnonymous]`, poser `MutuelleCardRead`, et passer `OuverturesPourLesEcransAmont` à zéro dans
`AnonymousSurfaceTests`.

## Itération 4 — L'adresse publique de la recette *[relevé le 21/09]*

| | |
|---|---|
| **Nature** | Un contrôle à moitié armé |
| **Effort** | Une minute, dès que l'URL est connue |
| **Bloqué par** | **L'URL de la recette** *(21/09, non revérifié)* |

`deploy.ps1` lit `api/version` après la copie ; l'adresse est **vide pour la recette**. Renseigner
`VectorVersionUrl` dans `IIS-DevServer.pubxml`.

## Itération 5 — Dire l'heure qu'il est *[ex-E2]*

| | |
|---|---|
| **Nature** | Données ambiguës transmises à la facturation |
| **Effort** | Une session, **à coordonner avec la facturation** |
| **Bloqué par** | Rien |

Les étapes sont en UTC **sans le déclarer** ; l'heure de signature est écrite **en heure locale**
(`SignatureRepository.cs`, `Insert` et `Update`). Même motif à examiner dans `ClMarkMissionSeenUseCase`
et `ClSetDriverUseCase`. **Fin visée** : tout en UTC, fuseau **déclaré** dans le contrat du paquet.

## Itération 6 — Les dettes de forme qui restent *[ex-G2, G5]*

| | |
|---|---|
| **Nature** | Hygiène — aucun changement de contrat |
| **Effort** | Une session par point |
| **Bloqué par** | Rien |

- **10 cas d'usage sur le contrat synchrone `IResultUseCase`** — ceux qui ne lisent que la base Vector,
  avec leurs repositories EF synchrones. **Gain faible** : ils ne bloquent pas sur le réseau.
- **`ClApplicationResult` au lieu de `ClResult`** — **différé le 04/10** (`decided.md`).
- **Le `PATCH` de la carte remplace les quatre champs et accepte un corps vide**, qu'il marque pourtant
  validé (`MUTUELLE_CARD_devplan.md`, « Saisie manuelle : deux faiblesses connues ») — une validation
  à décider.

⚖️ **Les alias de compatibilité restent** : leur sort est en rubrique « Fonctionnalités envisagées
en Vn ».

## Itération 7 — Le kilométrage dans le dossier transmis *[ex-E1, MOB-10]*

| | |
|---|---|
| **Nature** | Champ attendu par la facturation, toujours vide |
| **Effort** | Une session si le km véhicule suffit ; **plusieurs** s'il faut un relevé par mission |
| **Bloqué par** | 🔴 **Un arbitrage avec la facturation** |

Km du véhicule, ou relevé début/fin par mission ? On repart de zéro : la route `api/Kilometers` est
retirée depuis le 04/10.

## Itération 8 — Activer la lecture automatique des cartes *[ex-F2, P3]*

| | |
|---|---|
| **Nature** | Fonctionnalité en place, **inerte** |
| **Effort** | Poser une clé ; puis l'écran de validation, **côté web** |
| **Bloqué par** | 🔴 **Une décision d'exploitation** : activer, c'est envoyer une **donnée de santé** au fournisseur du modèle |

~12 cartes par jour, de l'ordre du centime par carte, et l'image qui sort du réseau. Le code AMC n'est
saisi que **41 fois sur 85** *(20/09)*. Depuis le 04/10, une nouvelle photo garde les champs validés :
la lecture automatique servirait d'abord aux premières photos.

## Itération 9 — Positions et statuts des véhicules *[ex-F3, MOB-16]*

| | |
|---|---|
| **Nature** | Connecteurs portés, jamais recâblés |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | Rien de connu — non réexaminé depuis juillet |

GpsGate (positions, REST) et Sirus (statuts, UDP) sont injectés mais ne servent à rien.

## Itération 10 — Protéger les données du patient *[ex-G7, P4]*

| | |
|---|---|
| **Nature** | RGPD — dette assumée |
| **Effort** | Plusieurs sessions, un seul lot |
| **Bloqué par** | Rien |

Documents, carte mutuelle et anomalies servis par une API exposée : **rétention et purge** (3 ans —
l'historique des cartes s'accumule sans limite), chiffrement au repos, **audit des accès** (un journal
des lectures de carte existe depuis le 04/10, sans identifiant de patient : c'est un début, pas
l'audit). La fermeture des routes anonymes a son itération propre. ⚠️ Si la lecture automatique est
activée, ce lot porte aussi le sort de l'image envoyée.

## Itération 11 — Ce qui attend ailleurs *[ex-A3, B, C1, C3, E4, F4]*

*Rien à coder ici tant que l'autre partie n'a pas bougé. Non revérifié à cette édition sauf mention,
et **c'est dit**.*

| Entrée | Qui doit bouger | Dernier relevé | En deux mots |
|---|---|---|---|
| **Les clients Keycloak des écrans** 🆕 | exploitation Keycloak | **04/10** | L'`azp` des jetons de l'écran de régulation et de celui de la certification → `Keycloak:ScreenAzp`. Sans eux, les écrans reçoivent 401 |
| **La carte mutuelle dans les deux listes de missions** 🆕 | Jules | **04/10** | [`note_front_jules_carte_mutuelle.md`](note_front_jules_carte_mutuelle.md) : présence par page, données au clic, image par `fetch` + blob. Sa bascule débloque « Fermer les routes anonymes de la carte » |
| **Afficher la carte connue, le message du 503, prendre acte des kilomètres** | dev web (Alexandre) | **04/10** | [`note_web_alexandre_carte_mutuelle_detail.md`](note_web_alexandre_carte_mutuelle_detail.md), [`note_web_alexandre_503_regulation_indisponible.md`](note_web_alexandre_503_regulation_indisponible.md), [routes retirées](note_web_alexandre_routes_retirees.md). Neutre s'il ne bouge pas |
| **Règle d'applicabilité des types** *[B9]* | Orders + décision métier | 04/10 | **11 types proposés partout**, tables de restriction vides. Première de leur plan, **par priorité** |
| **Exiger un jeton du terrain** | Orders | 04/10 | Rien ne l'en empêche de notre côté. ⚠️ **`POST /missions/batch-refs` répond 200 sans jeton** |
| **La cause des coupures** | exploitation / réseau | 04/10 | 22/09 et 02/10 : SQL, Orders et DNS **ensemble** ; 04/10, 15:00 : redémarrage du serveur SQL de la base Vector. Hors de ce dépôt |
| **Rattachement des comptes** *[C1]* | Orders, Identity, **RH** | 13/09 | Vector lit `PER_KEYCLOAK_MAP`, que l'écran d'Employee n'alimente pas ; **273 personnels sans fiche Employee**. [Consigne](docs/auth/consigne-rattachement-ambulancier.md) **à transmettre à la régulation et à la RH** |
| **Écrans : motif d'un champ grisé, relecture du NIR, bouton *Réessayer*, motif du refus de conducteur, validation des cartes lues** | dev web | 21/09 | [`note_ui_alex.md`](note_ui_alex.md), [`docs/ui-web/UI_selection-equipage-multi-crew.md`](docs/ui-web/UI_selection-equipage-multi-crew.md), [`note_web_alexandre_carte_mutuelle_ocr.md`](note_web_alexandre_carte_mutuelle_ocr.md) |
| **Composer les équipages avant la prise de service** *[C3]* | 🔴 régulation — décision | 13/09 | Sans quoi l'accès anticipé de 30 min ne sert à rien. Le filtre d'appartenance est **volontaire** |
| **`REFERENCE` et `URGENT`** *[B10]* | décision métier | 19/09 | Absents du catalogue |
| **`Billed` : l'écrire, ou retirer le palier** *[B4, E4]* | 🔴 décision | 13/09 | La facturation est en lecture seule par décision de son module |
| **Tests du transfert côté Orders** *[B6]* · **Relance des missions terminées non clôturées** *[B7]* | Orders | 13/09 | Des dossiers n'arrivent jamais en facturation |
| **Présence : qui est connecté** *[F4]* | 🔴 décision + cadrage **RH/RGPD** | 13/09 | [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md) |

---
# 3. Fonctionnalités envisagées en Vn

*Non engagé, non planifié — avec la condition qui les remettrait sur la table.*

| Sujet | En deux mots | Ce qui la rouvrirait |
|---|---|---|
| **Historique des photos de la carte** | Toutes les photos sont gardées ; les écrans ne montrent que la plus récente (choix du 04/10) | Un transport contesté où la carte d'alors compte |
| **Retirer les alias de compatibilité** *[G2]* | `IsAck`, champs historiques du détail, `SelectedDriver` jamais nul, champs typés des lieux. **Conservés délibérément** | **La confirmation du front, champ par champ** — [`note_ui_alex.md`](note_ui_alex.md) |
| **Alerter sur les 500 et les coupures** | La coupure du 22/09 n'a été vue que douze jours après ; le redémarrage SQL du 04/10, par hasard | Une coupure de plus découverte en retard |
| **Sortir les images du SQL** *[Vd-6]* | ~2 Go/an : la base le porte ; gardé en SQL le 04/10 | Le volume, ou une contrainte de sauvegarde |
| **Base Vector dédiée** *[Vd-1]* | Seul jalon DMZ non conditionné à la V2 | Pertinent dès maintenant ; personne ne l'a porté |
| **Accès anticipé à cheval sur minuit** *[CREW-2]* | Correctif connu | Les vacations de nuit ne sont pas concernées *(02/08)* |
| **Durcissement DMZ événementiel, push temps réel** *[Vd-2 à Vd-8]* | [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md) | Une exigence d'exposition, ou le polling qui ne suffit plus |
| **Masquage** *[Vd-5]* | NIR partiel, équipage retour | Le lot RGPD |
| **Contrats partagés avec Orders** *[4b]* | Écart JSON assumé | Une rupture de contrat constatée |
| **Repère de fraîcheur du dossier** *[E5]* | `updatedAt` est servi, personne ne s'en sert | Un besoin de resynchronisation |
| **Éviction ciblée du cache d'identité, mode hors ligne, géolocalisation avancée, renommage `USVector` → `Vector`** | — | Une demande |

---
# 4. Décisions tranchées

**Dans [`decided.md`](decided.md)**, qui s'ajoute et ne se régénère pas. 🔴 *À lire avant d'écrire du
code.* Cette édition n'y ajoute rien : les quatre décisions de la carte mutuelle — elle reste chez
Vector, les écrans entrent par un azp dédié, une nouvelle photo hérite des champs validés, une colonne
lue par EF se crée avant la publication — y sont entrées avec leur code.

---
# 5. Journal des livraisons

**Dans [`delivered.md`](delivered.md)**, horodaté et constaté. Dernière entrée : la publication de
`9c15f94`, schéma 9 sur 9.

---

## Annexe — documents voisins

| Doc | Ce qu'il apporte |
|---|---|
| [`decided.md`](decided.md) | **Le pourquoi** : décisions tranchées, pièges déjà payés — à lire avant de coder |
| [`delivered.md`](delivered.md) | **Le fait** : journal daté et constaté, configuration, incidents |
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Le besoin et le vocabulaire |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Carte mutuelle — décisions M1 à M11, contrat des routes |
| [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md) | Projection du terrain vers Orders |
| [`TRACABILITE_SAISIES_VECTOR_EXPORT.md`](TRACABILITE_SAISIES_VECTOR_EXPORT.md) | Des saisies aux 91 colonnes de facturation |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Authentification de service, résilience |
| [`endPoint.md`](endPoint.md) | Ce que Vector attend d'Orders.Api |
| [`CaSoft.Erp.USVector.Api/Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http) | La suite de vérification, à rejouer après chaque publication |
| `note_front_jules_*.md`, `note_web_alexandre_*.md`, `note_ui_alex.md`, `docs/ui-web/*` | Ce qui est promis aux fronts |

**Fin du document**
