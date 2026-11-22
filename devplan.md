# Plan de développement — Erp.Vector

> **Édition du** 2026-11-15 · **En production** `653e6d8` — **constaté par `GET /vector/api/version`**
> à 10:27 : commit, `Tree: clean`, environnement Production ; `.pdb` concordant ; **schéma 10 sur 10** ;
> socle `CaSoft.Framework` **2.12.0** · **345 tests verts** · Dépôt `github.com/esv83/Erp.Vector`
> (`USVector.sln`) · Prod `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`)
>
> ✅ **Le jeton est validé par le socle commun, et l'équipage peut saisir l'identité et le contact du
> patient** — dès que le front de Jules sera publié.
>
> ⏳ **Ce qui reste est surtout chez les autres** : le front de Jules, la route d'Order qui allumera la
> projection de la signature, le client Keycloak de la certification. Chez nous : constater, démonter
> un échafaudage, solder la dette de la revue du 11/11.
>
> Ce document **se régénère** au prompt *« compact devplan »* et ne porte **que l'ouvert**. Le
> **pourquoi** vit dans [`decided.md`](decided.md), à lire avant de coder ; le **fait**, horodaté et
> constaté, dans [`delivered.md`](delivered.md).
>
> **Règle de travail** : neutre ou additif, jamais de rupture du contrat que consomme l'app web — elle
> n'est pas déployée en même temps que l'API. Envers l'amont, une route neuve se consomme avec un
> repli sur son absence. **Une colonne lue par EF se crée en base avant la publication.** Un lot
> découpé en commits **se vérifie commit par commit** avant d'être poussé.

**Cinq rubriques, communes à tous les dépôts de l'ERP.** Le format est partagé ; les données ne le
sont jamais.

## 📍 Point de reprise — 15/11 : deux publications, le socle adopté

**14/11 à 17:28** : `14b554b` — saisie de l'identité (J-144) et du contact du patient par l'équipage,
relayée à la bordure d'Order ; option « Autre » (J-154) ; identification du patient anonyme dans le
détail ; et, publié avec, le motif « ne peut pas signer » du 10/11 (projection vers Order **coupée**).

**15/11 à 10:24** : `653e6d8` — validation du jeton par le socle (Itération 11), `IUnitOfWork` (DR-002),
abandon des refus d'Order sur l'état terrain (DR-001), CQ-001. **Cinq minutes de journal** : 14 jetons
mobiles validés, aucun rejet, aucun avertissement, aucune erreur.

**Par quoi reprendre** : constater la facturation et l'écran de régulation sous le socle, et une
écriture passée par `CommitAsync` (rubrique « Constater ce qui vient d'être publié »). Puis retirer le
repli de la lecture par lot.

---
# 1. Fonctionnalités livrées

*Ce que le module sait faire **en production**, en une page. Le récit daté vit dans
[`delivered.md`](delivered.md), à jour au 15/11.*

| Domaine | Ce que le module apporte aujourd'hui |
|---|---|
| **Se connecter** | Compte d'entreprise · l'app retrouve seule l'équipage du jour et fait choisir quand il y en a plusieurs · missions visibles **30 min avant la prise de service** · un accès refusé **dit pourquoi et quoi faire** |
| **Confirmer sa prise de service** | L'app dit **qu'une confirmation attend**, même la veille, et l'ambulancier confirme **sans le courriel** |
| **Voir son travail du jour** | Missions **engagées** par la régulation · détail : patient, adresses, horaires, sens, service destinataire, carte mutuelle connue · **patient anonyme signalé, et si l'équipage peut encore saisir** (deux jours) · affichage des lieux composé par le serveur |
| **Faire avancer la mission** | Cinq étapes horodatées, **annulables** · « mission vue » · signature, ou **motif « ne peut pas signer »** · conducteur — **un refus dit pourquoi** · tout remonte à la régulation en quasi temps réel, un envoi en échec est rejoué, **un refus d'Order n'est pas relancé** |
| **Compléter le dossier** | Type de mission et informations de facturation servis par la régulation, pré-remplis et verrouillés quand la fiche patient les connaît · **option « Autre » avec son texte** · **identité et contact du patient saisis par l'équipage**, règles et refus d'Order · anomalies, documents, photos |
| **Photographier la carte mutuelle** | Depuis la mission · une nouvelle photo garde les données validées, marquées « repris de la photo du … » · **lue par l'écran de régulation, par mission, avec jeton** · journal des accès · lecture automatique en place, inerte |
| **Passer à la facturation** | Transfert automatique à la clôture · dossier **par lots de 200**, signatures par lots de 50 · Orders lu par lot (repli mission par mission encore en place) · dossier **gelé** après transfert |
| **Tenir debout** | Le terrain n'écrase jamais la donnée officielle · API fermée par défaut, **jeton validé par le socle commun** · **une écriture, une validation, au cas d'usage** · délais et disjoncteur vers Orders · réessai SQL · une panne rend 503 et un message ; un refus, son motif · publication **depuis `main` propre et poussé**, commit servi et schéma vérifiés |
| **Se dire** | `api/version` : commit, état de l'arbre au build, environnement, **azp admis** · `api/version/runtime` : base résolue, drapeaux, état du schéma |

### Les réserves à ne pas perdre de vue

- ⚠️ **La saisie du patient et l'option « Autre » n'existent que côté API** tant que le front de Jules
  n'est pas publié *(note du 14/11, statut non revérifié)*.
- ⚠️ **La signature ne remonte pas encore à Order** : projection coupée (`FieldProjection:Enabled`)
  — *voir « Allumer la projection de la signature »*.
- ⚠️ **L'écran de la certification ne lit pas la carte** : son `azp` n'est jamais apparu *(05/10)*.
- 🔒 **Les deux routes anonymes de la carte sont fermées** (publié et constaté le 22/11) — *voir
  « Fermer les routes anonymes de la carte »*.
- ⚠️ **Le message du 503 n'atteint l'ambulancier que si l'app l'affiche** *(demandé le 04/10)*.
- ⚠️ **Un ambulancier rattaché depuis Employee seul n'entre pas** : 8 membres sur 244 *(13/09)*.
- ⚠️ **Ouvrir l'app avant que l'équipage soit composé échoue** — 23 min d'attente médiane
  *(juillet-août)*. Le message dit quoi faire.
- ⚠️ **Tous les types de mission sont proposés partout** : 11 au catalogue, tables de restriction vides
  *(04/10)*.
- ⚠️ **Un n° de sécurité sociale mal tapé part en facturation** *(demandé au dev web le 26/08)*.

---
# 2. Code bloqué, manquant ou en attente

*Classé par itération de session de codage, **du plus simple au plus complexe**. **L'ordre est un ordre
de difficulté, pas de priorité.** Chaque itération porte entre crochets son **ancienne référence**.
**Dans une référence croisée, on nomme la rubrique.***

## Itération 1 — Constater ce qui vient d'être publié *[ex-F1, B2, B8]*

| | |
|---|---|
| **Nature** | Constats et mesures — aucun code. Journal lu **en agrégats**, rien copié |
| **Effort** | Un relevé, puis au fil de l'eau |
| **Bloqué par** | **Le trafic** et **le front de Jules** |

✅ **Relevé du 22/11** (journal du 15/11 au 22/11, ~193 000 requêtes) — **sous `653e6d8`, validé** :
- **jetons validés par le socle pour les trois appelants** : `us-ambulance` (~30 000/jour),
  `us-regulation` (~4 400/jour), `erp-billinggateway-api` (chaque jour depuis le 16/11, 2 180 le 19/11).
  **9 rejets en 8 jours, tous des jetons expirés** — aucun d'émetteur ou d'azp ;
- **écritures par `CommitAsync` en 200** : 6 886 étapes, 131 annulations, 1 953 « mission vue », 13
  « ne peut pas signer », 143 photos de carte, 96 saisies de carte. Le 20/11 à 08:28, une collision
  rejetée **en bloc** (état et file de projection ensemble) montre la transaction unique à l'œuvre.
  *La ligne en base n'a pas été relue* (pas d'accès à la base de production depuis la session) ;
- **refus du conducteur vu** : 4 `PUT crews/…/driver` refusés par Order, rendus en 400 avec motif.

**Reste à voir passer** :
- **anomalie et document** : **aucun appel** en 8 jours (`POST …/anomalies`, `…/documents`) ;
- **un refus d'Order abandonné** sur l'état terrain — seulement une fois la projection allumée ;
- **sous `14b554b`**, dès que le front de Jules sera publié : un `PUT …/beneficiary/identity` et un
  `…/contact` en 204, un 403 relayé, un `OtherText` arrivé chez Order — **aucun appel au 22/11** ;
- *(non revérifiés depuis le 05/10)* une deuxième photo d'une carte validée (`FieldsInheritedFrom`) ;
  un détail avec `MutuelleCardKnown` vrai ; la suite [`Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http)
  rejouée en entier.

**À comprendre** *(relevé du 22/11)* :
- 🔧 **deux `PATCH Time` simultanés sur une mission sans état** rendaient **500 brut** (1 fois en 8 jours,
  20/11 08:28) : **corrigé et publié le 22/11 à 17:47 (`bba6eca`, republié à 19:38)** — la création perdante devient une
  mise à jour (`ClMobileUnitOfWork`). Reste à constater : plus de violation de `PK_MOB_MISSION_STATE` au
  journal sur une journée pleine (aucune de 17:47 à 20:15, trafic du soir) ;
- 🔧 **la saisie de la carte par l'équipage est refusée deux fois sur trois** (`PATCH mutuelle-card` :
  172 × 400 pour 96 × 200, `us-ambulance`) : **motif journalisé depuis le 22/11 à 17:47 (`bba6eca`)**
  (« Saisie de carte refusée|azp=…|motif=… ») — premier relevé : 1 refus, « Saisie vide » (17:50) ;
  relever sur une journée pleine, puis décider avec Jules ;
- **une panne de la base Vector rend 500, pas 503** — aucune panne dans la fenêtre, non revérifié ;
- **`403` sur `JobDetail`** : 691 en 8 jours (~90/jour), rythme inchangé depuis le 05/10 ;
- **clés de protection éphémères** à chacun des 28 démarrages du pool en 8 jours, surtout la nuit ;
- **chez Orders** : les étapes de mission vides (~3 883 annoncées avant le 23/07).

## Itération 2 — Retirer le repli de la lecture par lot

| | |
|---|---|
| **Nature** | Échafaudage à démonter |
| **Effort** | **Fait dans le code le 22/11** — reste : publier, puis constater une passe de la facturation |
| **Bloqué par** | Rien — le repli ne s'est **jamais déclenché** en octobre ni en novembre (aucun « ne porte pas batch-refs » au journal) |

Retirés : `ReadRefsOneByOneAsync` et sa lecture bornée, le **corps par défaut** de
`IErpReadApiClient.GetMissionBatchRefsAsync` (devenue non nullable) et le traitement 404/405 du client —
une route absente est une panne, la tranche sort en `Error`. Les doubles de test implémentent la méthode ;
le double du lecteur lève sur `/full` et la commande. 373 tests verts.

**Après publication** : `POST api/missions/field-data` en 200 de `erp-billinggateway-api`, aucun
`batch-refs impossible` hors panne d'Orders.

## Itération 3 — Publier et lire la recette 🆕 *[relevé le 21/09, cible du 14/11]*

| | |
|---|---|
| **Nature** | Outillage de publication |
| **Effort** | Quelques minutes |
| **Bloqué par** | **Où vit la recette** : `\\192.168.1.118\DEV_API` comme les autres modules ? `Test-Path` à faire depuis le poste. Et **son URL publique** |

Le 14/11, `deploy.ps1 DEV` a échoué : `\\192.168.1.112\dev_api` inaccessible. Vector vise encore
`.112` dans `deploy.ps1` (l. 8, 35, 196), `IIS-DevServer.pubxml` et `FolderProfile.pubxml`. Et
`VectorVersionUrl` est **vide** pour la recette : la publication n'y lit pas `api/version`.

## Itération 4 — Fermer les routes anonymes de la carte *[ex-M9]*

| | |
|---|---|
| **Nature** | Échafaudage à démonter — donnée de santé servie sans jeton |
| **Effort** | **Fait dans le code le 22/11** : `[AllowAnonymous]` retiré, `MutuelleCardRead` posé sur les deux, surface anonyme des écrans amont vide (tests). **Publié le 22/11 (`1668136`) et constaté** : `401` sans jeton sur les deux routes ; la présence par mission sert toujours les écrans (10 appels dans les 4 premières minutes). ✅ **Validé le 22/11 sur 4 h de trafic** (16:25 → 20:15) : aucun `401` sur les deux routes fermées hors les deux sondes du constat (16:24) ; 184 présences par mission de `us-regulation` en 200, une 503 (Orders hors délai, 18:12). À verser dans `delivered.md` au prochain « compact devplan » |
| **Bloqué par** | **Plus rien** : Jules confirme le 22/11 (J-095) que la régulation et la certification n'appellent plus que `POST api/missions/mutuelle-card/presence`. Mesure relancée le 22/11 : **zéro appel** aux deux routes anonymes du 23/10 au 22/11 (31 journaux), contre des milliers par jour pour la route par mission |

`POST api/mutuelle-card/presence` et `GET api/beneficiaries/{id}/mutuelle-card/image` : **zéro appel**
du 21/09 au 04/10. Relancer la mesure, puis retirer `[AllowAnonymous]`, poser `MutuelleCardRead`, et
vider `OuverturesPourLesEcransAmont` dans `AnonymousSurfaceTests`.

## Itération 5 — Allumer la projection de la signature 🆕 *[PRJ-2]*

| | |
|---|---|
| **Nature** | Interrupteur, puis rattrapage |
| **Effort** | **Allumé dans le code le 22/11** (`FieldProjection:Enabled = true`, `appsettings.json` versionné) — reste : publier, constater, puis jouer le rattrapage |
| **Bloqué par** | **Plus rien côté Orders** : 1.19.1 (`dad58a3`) en production le 22/11 sert la route — un `GET` sans jeton sur `missions/{id}/field` rend **405**, contre 404 pour une route inexistante. Contrat vérifié dans le code d'Orders : `signatureExists`, `waiver { code, at }`, motif porté sans être jugé (30 caractères) |

**Dans l'ordre** :
1. **Publier** Vector, puis constater au journal « projection signature **active** » au démarrage du
   dispatcher, et les premiers `PUT missions/{id}/field` en 204 ;
2. **lire les erreurs** les premiers jours : un 404 abandonne l'entrée (mission inconnue), un 400/409
   s'abandonne avec son motif (DR-001) ;
3. **puis seulement**, jouer `docs/exploitation/PRJ2_rattrapage_signatures.sql` (depuis le 13/10, 10
   entrées par seconde) — désormais précédé de `USE [BD_ERP_MOBILE_APP]` / `GO`.

## Itération 6 — La dette de la revue du 11/11

| | |
|---|---|
| **Nature** | Hygiène — aucun changement de contrat |
| **Effort** | **Fait dans le code le 22/11** pour DR-004, AR-001, DR-002, CQ-002 — et CQ-004 au passage. Reste : publier |
| **Bloqué par** | Rien |

Le détail est au registre, rubrique « Dette ». Fait le 22/11, 375 tests verts, contrats inchangés :
- **DR-004** : `ClWithdrawSignatureWaiverUseCase` (garde, `ClResult`, `CommitAsync`) ; le contrôleur rend
  toujours 204 ;
- **AR-001 + DR-002** : le cycle de l'outbox passe dans `ClProjectionOutboxDispatcher` (Infrastructure),
  validé par l'`IUnitOfWork` ; le worker ne garde que sa boucle. Une annulation à l'arrêt du pool n'est
  plus comptée comme tentative ni journalisée en erreur ;
- **CQ-002** : `PutProjectionAsync` commun aux deux projections, `Failure` pour les cinq fins d'échec —
  texte des journaux inchangé ;
- **CQ-004** : le motif projeté passe par `ToDomain`, plus de second `SpecifyKind`.

**Après publication** : la projection (jalons et signature) se vit au journal comme avant — aucune
« erreur de cycle », abandons et relances au même format.

Restent au registre, sans urgence : CQ-003, CQ-005 à CQ-007, DR-003.

⚖️ `ClApplicationResult` reste différé (`decided.md`, 04/10) ; les alias de compatibilité restent
(rubrique « Fonctionnalités envisagées en Vn »).

## Itération 7 — Le kilométrage dans le dossier transmis *[ex-E1, MOB-10]*

| | |
|---|---|
| **Nature** | Champ attendu par la facturation, toujours vide |
| **Effort** | Une session si le km véhicule suffit ; **plusieurs** s'il faut un relevé par mission |
| **Bloqué par** | 🔴 **Un arbitrage avec la facturation** |

Km du véhicule, ou relevé début/fin par mission ? On repart de zéro : `api/Kilometers` est retirée
depuis le 04/10.

## Itération 8 — Positions et statuts des véhicules *[ex-F3, MOB-16]*

| | |
|---|---|
| **Nature** | Connecteurs portés, jamais recâblés |
| **Effort** | Plusieurs sessions |
| **Bloqué par** | Rien de connu — non réexaminé depuis juillet |

GpsGate (positions, REST) et Sirus (statuts, UDP) sont injectés mais ne servent à rien.

## Itération 9 — Protéger les données du patient *[ex-G7, P4]*

| | |
|---|---|
| **Nature** | RGPD — dette assumée |
| **Effort** | Plusieurs sessions, un seul lot |
| **Bloqué par** | Rien |

Documents, carte mutuelle et anomalies servis par une API exposée : **rétention et purge** (3 ans —
l'historique des cartes s'accumule sans limite), chiffrement au repos, **audit des accès** (le journal
des lectures de carte, sans identifiant de patient, est un début). Depuis le 14/11, l'équipage **écrit**
aussi l'identité et le contact du patient — chez Order, qui en porte l'audit (`setBy`). ⚠️ Si la
lecture automatique est activée, ce lot porte aussi le sort de l'image envoyée.

## Itération 11 — Le verrou « Vu » 🆕 *[22/11]*

Règles arrêtées dans `Erp.Order/docs/features/featDesc_VerrouVu.md` (D10 à D13). Fiche Jules :
[`docs/sous-traitance-ui/2026-11-22-verrou-vu.md`](docs/sous-traitance-ui/2026-11-22-verrou-vu.md) (J-172, envoyée le 22/11 ; réponses de Jules le 22/11).

| Reste | Note |
|---|---|
| **Publier le contrôle de périmètre du « vu » et la fermeture de son effacement** | Codés le 22/11 : `PATCH api/JobList` → 403 « Mission hors de vos équipages » (mission inconnue comprise) ; `DELETE api/Time/{id}/seen` → 400. Aucun client n'appelle l'effacement du « vu » ; us-ambulance et us-mobile envoient le « vu » avec le jeton de l'ambulancier |
| **Refuser les écritures sur une mission non vue** | Après le déploiement de la nouvelle app : d'abord journalisé sans bloquer, puis activé par réglage |
| **Publier l'heure du clic du « Vu »** | **Tranché le 22/11 par l'utilisateur : l'API accepte l'heure du clic.** Codé : `SeenAt` facultatif sur `PATCH api/JobList`, refusé au-delà de 5 min d'avance ou 24 h de retard, heure locale (convention `ClHorloge`). Fiche J-172 complétée (« Complément du 22/11 ») : déblocage hors réseau, file locale, rejeu sans `SeenAt` sur 400. Q2 de Jules : rien d'autre à couvrir. **Publié le 22/11 (`1668136`)** ; les « Vu » sans heure répondent toujours 200 après publication. Reste : envoyer le complément à Jules, puis constater le premier `SeenAt` reçu |

## Itération 12 — Le mode formation 🆕 *[22/11]*

Missions fictives pour former les équipages et faire avancer l'app sans compte Keycloak. Codé le 22/11 :
`Infrastructure/Simulation/`, `SimulationConfinementFilter`, `api/simulation` (`Status`, `personas`, `reset`),
purge de la nuit. Le pourquoi est dans `decided.md` (22/11).

| Reste | Note |
|---|---|
| **Publier sur le serveur de dev et constater** | `Simulation:Enabled` + `AnonymousPersonas` posés dans `appsettings.Staging.json` : **vérifier que le serveur de dev tourne bien en `Staging`**, sinon rien ne s'ouvre. À constater : `GET api/simulation/personas` → 5 personas, `api/crew/mine` avec `Bearer simulation-persona-1` → équipage `FORMATION` |
| **Fiche Jules** *(J-175, envoyée le 22/11, `docs/sous-traitance-ui/2026-11-22-mode-formation.md`)*. Réponses de Jules le 22/11 : bouton dans « Moi », section Formation (Q1) ; au redémarrage l'app repart en mode normal et purge le cache de la séance (Q2, il propose de garder la règle) ; pas d'appel à `/api/EmergencyPlatform` (Q3) ; plus aucune écriture en file (Q4) ; aucune notification de mission dans l'app (Q5) ; bandeau orange `#FF6B00`, texte presque noir, capitales, **validé par l'utilisateur le 22/11** (Q6) | Bouton « Mode formation » qui pose `X-Vector-Simulation: 1` sur **chaque** appel, bandeau visible tant qu'il est actif, écran de choix d'un persona en dev, `POST api/simulation/reset`. Le 409 « mode formation non ouvert » est à afficher |
| **Ouvrir en production** | Décision d'exploitation : `Simulation:Enabled=true` dans `appsettings.Production.json`. Les personas y restent interdits (refus au démarrage) |
| **Lecture automatique des cartes de formation** | Le worker OCR ne distingue pas une carte de formation : à exclure avant d'activer la lecture, si une vraie carte peut être photographiée en formation |
| **L'état du faux Orders est en mémoire** | Perdu à un recyclage du pool (statut et context reviennent à l'origine ; les saisies en base Mobile restent). Gêne admise ; à revoir si les formations s'en plaignent |

## Itération 10 — Ce qui attend ailleurs *[ex-A3, B, C1, C3, E4, F4, E2]*

*Rien à coder ici tant que l'autre partie n'a pas bougé. Non revérifié à cette édition sauf mention,
et **c'est dit**.*

| Entrée | Qui doit bouger | Dernier relevé | En deux mots |
|---|---|---|---|
| **Le front : saisie du patient, « Autre », motif de signature** 🆕 | **Jules** | **14/11** | [`note_front_jules_saisie_patient_et_autre.md`](note_front_jules_saisie_patient_et_autre.md), après [`note_front_jules_vector.md`](note_front_jules_vector.md) (04/10). Sa bascule débloque « Fermer les routes anonymes de la carte » |
| **`OtherTextRequiredForField` chez Order** 🆕 | Order | 14/11 | Devait rester faux tant que Vector ne relayait pas `OtherText` — **c'est fait depuis le 14/11**, à leur dire. Leur réserve : un formulaire renvoyé en entier peut tomber en 400 sur un champ « Autre » sans texte |
| **La route `PUT missions/{id}/field`** 🆕 | Orders | 10/11 | Débloque « Allumer la projection de la signature » |
| **Le client Keycloak de l'écran de la certification** | exploitation Keycloak | 05/10 | Son `azp` → **`Keycloak:FrontClientIds`** (ex-`ScreenAzp`), **puis redémarrer le pool** : lu au démarrage |
| **Basculer les heures en UTC** *[ex-E2]* | **Vector, Orders, facturation ensemble** | 04/10 | Convention figée et déclarée (`ClHorloge`, `endPoint.md`) ; la bascule se fait en une publication coordonnée des trois, avec une date pour l'historique (`decided.md`, 04/10) |
| **Règle d'applicabilité des types** *[B9]* | Orders + décision métier | 04/10 | 11 types proposés partout, tables de restriction vides |
| **Exiger un jeton du terrain** | Orders | 04/10 | ⚠️ `POST /missions/batch-refs` répond 200 sans jeton |
| **La cause des coupures** | exploitation / réseau | 04/10 | 22/09, 02/10 : SQL, Orders et DNS ensemble ; 04/10 : redémarrage du SQL de la base Vector |
| **Rattachement des comptes** *[C1]* | Orders, Identity, **RH** | 13/09 | `PER_KEYCLOAK_MAP` non alimenté par Employee ; 273 personnels sans fiche. [Consigne](docs/auth/consigne-rattachement-ambulancier.md) à transmettre |
| **Composer les équipages avant la prise de service** *[C3]* | 🔴 régulation — décision | 13/09 | Sans quoi l'accès anticipé de 30 min ne sert à rien |
| **`REFERENCE` et `URGENT`** *[B10]* | décision métier | 19/09 | Absents du catalogue |
| **`Billed` : l'écrire, ou retirer le palier** *[B4, E4]* | 🔴 décision | 13/09 | La facturation est en lecture seule par décision de son module |
| **Tests du transfert côté Orders** *[B6]* · **Relance des missions terminées non clôturées** *[B7]* | Orders | 13/09 | Des dossiers n'arrivent jamais en facturation |
| **Présence : qui est connecté** *[F4]* | 🔴 décision + cadrage **RH/RGPD** | 13/09 | [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md) |

## Dette

Prochains identifiants : CQ-008 · DR-005 · AR-002

*Revérifiée dans le code le 15/11. **Soldées et versées dans `delivered.md`** : CQ-001, DR-001 ; DR-002
pour l'essentiel — il n'en reste que le worker. Rapport d'origine : revue code-quality de `8af6ff7`
(11/11) ; les points DR et AR viennent de sa section « hors périmètre », non revus par un agent
design-review ou architecture.*

| ID | P | Fichier | Constat |
|---|---|---|---|
| CQ-002 | P2 | ✅ **corrigé dans le code le 22/11, à publier** — `CaSoft.Erp.USVector.Infrastructure/ErpApi/HttpErpWriteApiClient.cs:26-91` | `ProjectFieldAsync` recopie `ProjectOperationalAsync` (règle 404) ; fin d'échec log + `HttpRequestException` ×5 (l. 57, 91, 122, 152, 187) → helpers privés |
| CQ-003 | P3 | `CaSoft.Erp.USVector.Infrastructure/Repositories/FieldDataReader.cs:202` | `ClFieldSignatureWaiverDtoOut` construit inline, seul bloc hors méthode d'extension (règle MAPPING) |
| CQ-004 | P3 | ✅ **corrigé dans le code le 22/11, à publier** — `CaSoft.Erp.USVector.Api/Workers/OperationalOutboxDispatcher.cs:136` | `SpecifyKind(Utc)` du motif en double de `SignatureWaiverMappings.ToDomain` |
| CQ-005 | P3 | `CaSoft.Erp.USVector.Application/UseCases/SignatureWaiver/ClDeclareSignatureWaiverUseCase.vb:14,30,33` | vocabulaire technique en français (`Validateur`, `Refus`) dans du code nouveau |
| CQ-006 | P3 | `…/UseCases/SignatureWaiver/ClSignatureWaiverDtos.vb`, `Domain/Entities/ClSignatureWaiver.vb` | plusieurs types par fichier, mapping hors `ModXxxMapping.vb`, contre la convention du module |
| CQ-007 | P3 | `ErpApi/IErpWriteApiClient.cs:19,33`, `Repositories/Mobile/ProjectionOutbox.cs`, `Api/Workers/OperationalOutboxDispatcher.cs` | « Operational » vs « Projection » pour le même mécanisme (`EnOperationalProjectionOutcome` rendu par `ProjectFieldAsync`) ; table `MOB_OPERATIONAL_OUTBOX` inchangée |
| DR-002 | P3 | ✅ **corrigé dans le code le 22/11, à publier** — `CaSoft.Erp.USVector.Api/Workers/OperationalOutboxDispatcher.cs:101` | reste : le worker appelle `ctx.SaveChangesAsync` au lieu de l'`IUnitOfWork` (repositories et cas d'usage migrés le 14/11) ; à traiter avec AR-001 |
| DR-003 | P3 | `CaSoft.Erp.USVector.Api/Controllers/SignatureWaiverController.cs:29-41` | lectures par `ISignatureWaiverRepository` au lieu d'un `IXxxQueryService` ; le contrôleur compose le libellé (deux appels) |
| DR-004 | P3 | ✅ **corrigé dans le code le 22/11, à publier** — `SignatureWaiverController.cs:51-56` | `Withdraw` appelle le repository et valide lui-même (`CommitAsync`), sans cas d'usage (ni garde, ni `ClResult`) |
| AR-001 | P3 | ✅ **corrigé dans le code le 22/11, à publier** — `OperationalOutboxDispatcher.cs:131-132` | le worker (couche Api) interroge `MobileDbContext` directement (`Signatures`, `SignatureWaivers`) au lieu d'un service d'Infrastructure |

# 3. Fonctionnalités envisagées en Vn

*Non engagé, non planifié — avec la condition qui les remettrait sur la table.*

| Sujet | En deux mots | Ce qui la rouvrirait |
|---|---|---|
| **Historique des photos de la carte** | Toutes gardées ; les écrans ne montrent que la plus récente (04/10) | Un transport contesté où la carte d'alors compte |
| **Activer la lecture automatique des cartes** *[F2, P3]* | En place, **inerte** : une clé à poser, puis l'écran de validation côté web. ~12 cartes/jour, ~1 centime/carte *(20/09)* | 🔴 **Une décision d'exploitation** : envoyer une **donnée de santé** au fournisseur du modèle |
| **Retirer les alias de compatibilité** *[G2]* | `IsAck`, champs historiques du détail, `SelectedDriver` jamais nul, champs typés des lieux. **Conservés délibérément** | **La confirmation du front, champ par champ** — [`note_ui_alex.md`](note_ui_alex.md) |
| **`ClApplicationResult` au lieu de `ClResult`** | Différé le 04/10 (`decided.md`) | Un appui de Vector sur le pipeline du socle |
| **Alerter sur les 500 et les coupures** | La coupure du 22/09 n'a été vue que douze jours après | Une coupure de plus découverte en retard |
| **Sortir les images du SQL** *[Vd-6]* | ~2 Go/an : la base le porte | Le volume, ou une contrainte de sauvegarde |
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
code.* Cette édition y ajoute sept lignes : la saisie du patient passe par la bordure d'Order, l'arbitrage
DR-001, la frontière transactionnelle (DR-002), la validation du socle sans rien lui retirer, les
anciennes clés reprises, l'homonyme `ClKeycloakCallers`, et la vérification commit par commit.

---
# 5. Journal des livraisons

**Dans [`delivered.md`](delivered.md)**, horodaté et constaté. Dernière entrée : la publication de
`653e6d8` le 15/11, schéma 10 sur 10. Cette édition y a aussi versé `29b6cc9` (04/10), que l'édition du
05/10 n'avait pas inscrit.

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
| [`docs/deploiement/configuration-keycloak-iis.md`](docs/deploiement/configuration-keycloak-iis.md) | Clés Keycloak (noms du socle depuis le 14/11), validation du jeton |
| [`CaSoft.Erp.USVector.Api/Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http) | La suite de vérification, à rejouer après chaque publication |
| `note_front_jules_*.md`, `note_web_alexandre_*.md`, `note_ui_alex.md`, `docs/ui-web/*` | Ce qui est promis aux fronts |

**Fin du document**
