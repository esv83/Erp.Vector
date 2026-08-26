# 📱 devplan — Vector (module terrain ambulanciers)

> **Plan unique du module.** Il sert à trois choses : savoir **ce que le module fait**, savoir **ce
> qui reste**, et **ne pas rejouer les décisions déjà tranchées**.
>
> **§1 se lit sans connaître le code.** **§3 est écrit pour celui qui va coder** : il est découpé en
> **chapitres homogènes** — un chapitre = une nature de travail, une étape = une unité livrable avec
> son état, son préalable et son critère de fin. Une fonctionnalité livrée quitte §3 et enrichit §1 ;
> une piste abandonnée va au **§6** avec son motif, pour ne pas être ré-instruite.
>
> **Statut** : 🟡 en service, chantiers en cours — boucle ambulancier livrée (hors écran de
> rattachement Keycloak), chaîne terrain→facturation livrée et consommée, **référentiel de**
> **contexte basculé et en service depuis le 2026-08-25** (§3.A).
> **Prod** : `\\192.168.1.112\prod_api\Vector.Api` (IIS `/vector`) — trafic servi, jetons Keycloak
> réellement validés depuis le 2026-08-02.
> **Dépôt** : `github.com/esv83/Erp.Vector` (`USVector.sln`) · **126 tests verts** (vérifié 2026-08-25).
> **Dernière mise à jour** : 2026-08-25.

| | Sens |
|---|---|
| 🟢 | livré, vérifié |
| ⏳ | à faire, rien ne bloque |
| ⛔ | bloqué (décision, contenu métier, ou livrable d'un autre module) |
| ⚪ | différé V2 |
| ⚠️ | dette / garde-fou |

---

# 1. Ce que le module fait aujourd'hui

## 1.1 L'application terrain est reconnectée à l'ERP

Après la perte de la base historique, l'API mobile a été reconstruite sur l'ERP **sans toucher au
contrat consommé par le terminal** : mêmes routes, mêmes formats — l'application n'a eu qu'à être
re-pointée. Les données de référence (missions, équipages, véhicules, personnel, patients) viennent
de l'ERP ; tout ce qui est propre au terrain vit dans une base dédiée à Vector.

Vector ne touche plus aux bases des autres modules : **il dialogue avec eux par leurs API**. Un
chantier en cours ailleurs ne casse plus ni le build ni le déploiement de l'app mobile.

## 1.2 L'ambulancier se connecte avec son compte d'entreprise

Il s'authentifie avec son **compte Keycloak** ; l'application retrouve seule le ou les équipages dont
il fait partie ce jour-là, lui fait **choisir celui qu'il occupe** quand il y en a plusieurs, et ne
lui montre que les missions de cet équipage — celle d'un autre est refusée. Un compte non rattaché
reçoit un message explicite l'invitant à contacter la régulation.

Ses missions lui sont visibles **30 minutes avant sa prise de service**, pour qu'il prépare sa
journée avant de démarrer sa vacation. Il ne voit que les missions **engagées** par la régulation :
une mission simplement affectée au planning ne remonte pas au terrain.

## 1.3 Il voit son plan de travail et le fait avancer

- La **liste des missions du jour** : patient, mode de transport, lieux, horaires.
- Le **détail d'une mission** : identité et coordonnées du patient, adresses de départ et d'arrivée
  résolues, horaires, sens et fréquence, service médical destinataire. L'affichage des lieux est
  **composé par le serveur** : l'UI rend ce qu'elle reçoit, sans règle de mise en forme.
- Le marqueur **« mission vue »** : d'un geste il signale qu'il a pris connaissance de la mission ;
  l'icône disparaît et la régulation voit l'heure de prise de connaissance.
- Les **cinq jalons de progression**, horodatés au fil de la course, **annulables** — un jalon posé
  par erreur peut être retiré, et le retour arrière remonte à la régulation.
- La **signature du patient**, avec un indicateur de présence visible dès la liste.
- Le **conducteur** de l'équipage, consultable et modifiable.

Chaque geste est **projeté vers la régulation** en temps quasi réel : le statut de la mission s'y met
à jour tout seul. Cette synchronisation est **garantie** — les envois en échec sont mis en attente et
rejoués automatiquement, sans jamais bloquer la saisie de l'ambulancier.

## 1.4 Il complète le dossier depuis le terrain

- **Attributs de mission** : commentaires, coordonnées du patient, type de contrat et champs de
  facturation, sous forme de **formulaire dynamique** — le jeu de champs dépend du type retenu.
- **Anomalies** constatées en mission : elles ne bloquent rien, elles partent avec le dossier et
  c'est la facturation qui arbitre.
- **Documents et photos** rattachés à la mission.
- **Carte mutuelle** du patient — photo et saisie des informations
  ([`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md)).

Principe constant : **le terrain n'écrase jamais la donnée officielle de l'ERP.** Ces saisies sont
une couche déclarative que la facturation relit et corrige.

## 1.5 Le dossier part en facturation

- Une mission **clôturée par le régulateur** devient automatiquement transférable.
- Les modules d'aval disposent d'une **file des missions à traiter** — donc de la visibilité sur ce
  qui reste en attente.
- Ils récupèrent **un paquet unique et versionné** rassemblant tout l'enrichissement terrain de la
  mission (jalons, signature, attributs, mutuelle, documents, anomalies) et tirent les **pièces
  jointes à la demande**. **Ce paquet est réellement consommé** : la facturation l'interroge mission
  par mission pour construire la journée transmise à AidesNSoft, et la certification des trajets
  s'appuie sur la même file.
- Une fois la mission transférée, **le dossier est gelé côté terrain** : toute tentative de
  modification est refusée avec un message explicite.

## 1.6 Ce qui tient tout cela debout

- **Un contrat mobile préservé** malgré la reconstruction : les ajouts sont additifs, les anciens
  champs restent servis le temps que l'UI web bascule.
- **Une authentification à un seul point de passage**, avec cache : le chemin chaud ne refait plus
  d'appels réseau pour retrouver l'ambulancier et son équipage.
- **Une API fermée par défaut** : une route est protégée sauf mention contraire, et les quelques
  sorties sont nommées, justifiées et figées par un test (§3.C2).
- **Un outil de diagnostic** qui rejoue la chaîne d'identité maillon par maillon, réservé au dev,
  pour trancher une panne d'accès sans lire les logs à l'aveugle.
- **Un code applicatif homogène** : les cas d'usage renvoient un résultat typé plutôt que d'écrire
  dans un présentateur, ce qui rend les erreurs traduisibles en codes HTTP sans logique dans les
  contrôleurs.
- **Un déploiement outillé** : une commande pour la recette, une pour la production, avec
  confirmation à taper, vérification préalable du partage réseau et contrôle que ce qui est publié
  est bien arrivé.

## 1.7 Repères de validation

| Lot | Preuve |
|---|---|
| Socle mobile | 25 routes du contrat legacy exposées ; joblist, détail, signature, timeline validés sur missions réelles |
| Authentification | login Keycloak bout-en-bout 2026-07-05 ; jetons réellement validés en prod 2026-08-02 ; accès anticipé 30 min en prod depuis la même date |
| Découplage HTTP | isolation de build prouvée (reconstruction sans Orders) |
| Result pattern, vague 1 | 31 cas d'usage migrés, parité HTTP vérifiée |
| Terrain (attributs 11, mutuelle 16, lieux 12) | validés en base 2026-06-14 / 06-15 / 07-14 |
| Transfert (Orders + Vector) | 24 tests ; schéma appliqué 2026-06-22 |
| Consommation réelle du paquet terrain | mesurée le 2026-08-06 par la facturation : 284 missions acquises |
| Contexte de mission (type, attributs, paquet terrain) | 58 tests ; **les trois refus constatés en production** le 2026-08-24 |
| Suite complète | **126 tests verts** (2026-08-25) |

## 1.8 ⚠️ Livré mais pas encore exploité

- **La carte mutuelle n'est jamais remplie en production** (table vide au 23/08/2026) : la chaîne
  serveur fonctionne, mais aucune photo n'arrive. Ce qui coince est en aval, pas dans l'API (§3.F1).
- **Le kilométrage n'est pas transmis** avec le dossier terrain : la facturation attend ce champ
  pour activer son contrôle (§3.E1).
- **Le repère de fraîcheur du paquet** (`updatedAt`) est servi mais aucun consommateur ne s'en sert :
  ils re-tirent à chaque construction. À garder, sans y investir davantage.
- ⚠️ **Le référentiel de contexte est basculé et armé en production** (2026-08-25) : le type de
  mission et ses attributs viennent d'Order. Une conséquence n'a pas été traitée — le dev web n'a
  **pas** été prévenu des nouveaux refus (`409` sur la sélection du type, `409`/`400` sur la saisie),
  là où ces appels réussissaient toujours (§3.A1).

---

# 2. Décisions structurantes (toujours en vigueur)

| # | Décision |
|---|---|
| D1 | **Contrat mobile préservé** : on remplace l'implémentation des repositories, pas les routes ni les DTO. Les ajouts sont additifs. |
| D2 | **Séparation officiel ↔ terrain** : la donnée terrain est déclarative et non fiable par construction ; elle n'écrase jamais l'ERP. |
| D3 | **Accès aux autres modules par API HTTP uniquement** (posture DMZ), base propre à Vector sur le LAN derrière firewall. L'architecture actuelle est conforme ; le durcissement événementiel est une option V2. |
| D4 | **Identités de référence en Guid** (équipage / véhicule / personnel), alignées sur l'ERP. |
| D5 | **Grain de transfert = la mission**, avec le rattachement à sa commande conservé dans le paquet. |
| D6 | **Transfert automatique** des missions clôturées ; l'aval contrôle via la file des non-transférées. Pas de validation régulateur préalable : la facturation agrège et corrige. |
| D7 | **Gel au transfert**, pas à la clôture : tant que la mission est seulement transférable, le terrain peut encore corriger. |
| D8 | **L'aval tire les octets** (signature, photos, documents) depuis Vector.Api — pas de stockage partagé. |
| D9 | **Anomalies non bloquantes** : transférées comme donnée, arbitrées en facturation. |
| D10 | **Temps réel régulateur = persistance + polling** au MVP ; le push est V2. |
| D11 | **`Closed` reste la main du régulateur** : le mobile n'écrit jamais la clôture administrative. |
| D12 | **Photos et documents en base** en V1 ; sortie vers un stockage fichier planifiée V2. |
| D13 | **Le claim `per_id` dans le jeton est écarté** : un turnover le rendrait non invalidable. C'est le cache HTTP qui s'invalide, pas le jeton. |
| **D14** | **On code neutre ou additif — jamais de rupture du contrat consommé par l'appli web en production.** L'app web ambulancier tourne contre `Vector.Api` et **n'est pas déployée en même temps que l'API** : un champ retiré, renommé, ou un type de réponse changé (tableau → objet) casse le terrain immédiatement, sans filet. Corollaires : on ajoute à côté plutôt qu'on ne remplace ; les alias de compatibilité (§3.G2) ne se retirent **que** sur confirmation que le front a basculé ; un renommage de type .NET est neutre (le nom n'est pas sur le fil), un renommage de propriété ne l'est pas. Quand une évolution ne **peut** pas être additive, elle se coordonne avec le dev web avant livraison (note `note_web_alexandre_*.md`). |
| D15 | **La surchargeabilité d'un type de mission est une propriété du catalogue**, pas une décision prise commande par commande : le régulateur saisit un type, c'est le type qui dit si le terrain peut le corriger. |

---

# 3. Ce qui reste — chapitres d'étapes codables

> **Comment lire.** Sept chapitres, homogènes par **nature de travail** : on peut entrer dans un
> chapitre et y rester. Chaque étape garde sa référence historique (celle des commits et des autres
> plans) et porte : **état · préalable · contenu · fin**. Les étapes sans préalable sont attaquables
> aujourd'hui.

| Chapitre | Nature | Attaquable maintenant ? |
|---|---|---|
| **A** — Contexte de mission (`OC`) | bascule d'un référentiel, contrat mobile en jeu | A1, A2, A4 **en service**, A5 fait ; A3 et A6 attendent la même chose — la réponse du dev web |
| **B** — Dépendances amont Orders | rien à coder ici : suivre, réclamer, ou coder dans l'autre dépôt | — |
| **C** — Identité & authentification | sécurité, chaîne de connexion | oui (C2, C3) ; C1 sur décision |
| **D** — Robustesse des appels sortants | plomberie HTTP, aucun contrat touché | **oui, isolé** |
| **E** — Chaîne facturation | contrat du paquet terrain, coordination aval | oui (E1, E2) |
| **F** — Fonctions terrain restantes | features à part entière, indépendantes | oui |
| **G** — Dette & hygiène | refactor, zéro changement de contrat | **oui, en continu** |
| **H** — Différé V2 | — | non |

---

## A. Contexte de mission — bascule vers Order (`OC`)

Le référentiel des types de mission a migré côté Order ; Vector en est devenu consommateur. **La
bascule est en service en production depuis le 2026-08-25** : la sélection du type (A1) et le
questionnaire d'attributs (A2) viennent d'Order, le paquet terrain a suivi (A4), et le code qu'elle
remplaçait a été retiré (A5).

**Ce qui reste tient en une phrase.** La bascule est encore réversible par configuration, et on ne
retire ce filet qu'une fois le dev web confirmé capable d'absorber les nouveaux refus. Sa réponse
débloque A3 et A6 ; rien d'autre n'attend.
Notes envoyées : [le contrat](note_web_alexandre_context_mission_dto.md) (25/08) ·
[la demande de confirmation](note_web_alexandre_context_mission_confirmation.md) (26/08).

> ⚠️ **Deux numérotations `OC-` coexistent.** Ici, un `OC-x` nu désigne la tâche **Vector** ; la tâche
> Order s'écrit toujours `Order OC-x`
> ([plan Order](../Erp.Order/feature_order_context_devplan.md) §7, où `Order OC-11` = *tout* ce chapitre).

> ⚠️ **Les identifiants ont changé d'espace le jour de la bascule.** Les deux catalogues n'ont jamais
> partagé leurs ids — `4` désignait « Article 80 » côté Vector et « Centre 15 » côté Order. Un id
> resté en dur dans le front affiche donc faux **sans lever d'erreur** : c'est le principal risque qui
> subsiste côté écran.

### ✅ Dépendance levée : le verrou est devenu intentionnel (`Order OC-28`)

Le verrou n'était pas décidé, il était **dérivé de l'auteur de l'écriture** : toute valeur posée par
la régulation gelait la mission, sans que personne l'ait voulu — les 4 099 assignations de production
viennent toutes de la régulation. Depuis le correctif déployé le 2026-08-25, la surchargeabilité est
une propriété du **type**, configurée une fois, et tout est surchargeable par défaut : plus aucune
mission n'arrive verrouillée, et « la régulation propose, l'ambulancier garde la main » est devenu le
cas courant. *(Côté Order : ne pas rejouer le script `062`, défait par `063`.)*

**À trancher** : quand le terrain écrase une proposition de la régulation, celle-ci est **perdue** —
écrasement en place, aucun audit. Si la facturation ou l'arbitrage d'un litige doit pouvoir la relire,
il faut une trace. Sinon, on assume la perte.

### A1 — 🟢 Sélection du contexte (`OC-3b` + `OC-4`) — en service depuis le 2026-08-25

En production, la liste des types de mission vient d'Order et le choix de l'ambulancier y retourne.
Le front n'a changé ni de route ni de format de réponse.

**Ce que le terrain voit de différent**
- Aucun type n'est présélectionné : « non renseigné » est l'état de départ normal, là où l'API
  cochait le premier de la liste.
- Enregistrer un type peut désormais échouer — la régulation l'a imposé, ou il ne convient pas à la
  commande — alors que l'appel réussissait toujours.
- Si l'ERP ne répond pas, la liste arrive **vide** plutôt que fausse : les identifiants du catalogue
  local ne désignent pas les mêmes types côté Order.

**Trois décisions à ne pas rejouer**
1. Le lien type ↔ attributs est refait par le code à chaque lecture, plutôt que par une écriture en
   double dans l'ancienne table — sans quoi on aurait recréé le doublon que tout ce chapitre supprime.
2. Une panne d'Orders rend une liste vide, jamais le catalogue local : ne rien proposer vaut mieux
   qu'enregistrer un type pour un autre.
3. L'identifiant reçu est vérifié contre les types réellement proposés. C'était le filet du jour de
   la bascule, pour le client resté sur l'ancienne liste.

**Ce qu'il reste à faire — une seule chose**

La bascule est **encore réversible par configuration** : l'ancien chemin vit donc toujours dans le
code, à côté du nouveau. On ne le retire qu'une fois le front confirmé capable d'absorber les refus —
un filet de compatibilité ne se retire pas d'office (D14). Le contrat lui a été décrit le 25/08, la
demande de confirmation envoyée le 26/08
([note](note_web_alexandre_context_mission_confirmation.md)) : **en attente de sa réponse.**

**Constaté le 2026-08-26, sans attendre sa réponse** : l'écran fonctionne — on choisit un type, et le
questionnaire qui revient est bien celui de ce type. Ce n'est pas un détail de confort : le jeu de
champs est dérivé du type **enregistré chez Order**, donc un identifiant mal apparié ramènerait le
mauvais formulaire. **Le risque des identifiants en dur est donc levé par l'observation**, et avec lui
le seul risque qui portait sur la donnée.

Le doute restant ne porte plus que sur les **chemins de refus**, qu'aucun usage normal ne déclenche
aujourd'hui — donc que le bon fonctionnement observé ne teste pas.

**Ce que le désarmement rebrancherait, vérifié le 2026-08-26** : le chemin d'avant n'a **ni verrou par
champ ni validation de valeur** — le cas d'usage historique ne pose jamais `IsReadOnly`, et n'inspecte
aucune valeur. Désarmer aujourd'hui rouvrirait donc à la saisie les **34 dates de naissance
verrouillées sur 40** (A3), et accepterait un NIR à clé fausse sans rien dire, dans un magasin que la
facturation ne lit pas.

⚠️ **Le filet est donc moins sûr que ce qu'il protège.** Le seul défaut qu'on reproche au chemin armé
est de rendre un message d'erreur là où le front n'en attendait pas ; le désarmement, lui, rouvre une
donnée de santé non corrigeable. Ce n'est plus un retour arrière, c'est une régression.

⚠️ Tant que ce double chemin existe, **A6 ne peut pas s'exécuter** — les tables qu'elle supprime sont
exactement ce que le désarmement rebranche.

**En cas d'incident d'ici là** : désarmer par variable d'environnement (`ContextOrder__UseOrderCatalog=false`
dans le `web.config`), effet immédiat sans coupure. Ne pas repasser le fichier versionné à `false` :
il faudrait redéployer, donc couper l'API. ⚠️ Ne pas armer en dev sans avoir d'abord redirigé
`OrdersApi:BaseUrl` — sa valeur de référence pointe la production, et un enregistrement y serait réel.

### A2 — 🟢 Attributs pilotés par Order (`OC-5`) — en service depuis le 2026-08-25

Le questionnaire d'attributs et les valeurs saisies viennent d'Order. Le front garde sa route et son
format ; deux propriétés s'ajoutent, qui disent qu'un champ est verrouillé et pourquoi.

**Ce que le terrain voit de différent**
- Une valeur saisie à l'aller vaut pour le retour : elle vit au niveau de la commande, plus de la
  mission. Un bon de transport coché à l'aller arrive coché au retour.
- Date de naissance et n° de sécurité sociale arrivent pré-remplis et **verrouillés** dès que la
  fiche bénéficiaire les connaît.
- La saisie devient **tout ou rien** et peut être refusée — valeur invalide, ou champ déjà scellé —
  là où elle réussissait toujours.

**Deux décisions à ne pas rejouer**
1. On renvoie le formulaire entier sans trier les champs verrouillés : Order ignore une valeur
   reposée à l'identique. Trier ici obligerait à y recopier sa règle du verrou, donc à la voir
   diverger le jour où il la change.
2. Aucune règle métier n'est rejouée côté Vector — clé de contrôle du NIR, refus d'une date future,
   partage de la case entre l'aller et le retour. Elle vit là où vit la donnée.

**Garde-fou de démarrage** : armer les attributs sans la sélection **empêche l'API de démarrer**.
Sans le premier cran, l'ambulancier choisirait un type dans un catalogue et verrait les champs d'un
autre.

**Ce qu'il reste à faire** : le retrait du double chemin, exactement comme en A1 et sous la même
condition — la réponse du dev web.

### A3 — 🟡 Règles métier portées par Order (`OC-6`) — côté API : vérifié

Les règles vivent chez Order, Vector les transmet sans les rejouer. Vérifié dans le code le
2026-08-26 : le verrou par champ traverse en recopie, aucune validation n'est refaite ici, et le
verrou du **type** n'est consulté nulle part dans le formulaire — un type imposé laisse donc la
saisie ouverte, par construction et non par convention.

**Mesuré en production le 2026-08-26**, sur 40 missions des trois derniers jours, en interrogeant
Order directement :

| | |
|---|---|
| Champs servis sur toutes les missions | `DDN`, `NIR`, `PHONES`, `MAILS`, `COMMENTS` — puis selon le type : `PMT` (21/40), `BT` (11), `MUTUELLE` (10), `NUM_CENTAURE` (7) |
| `DDN` renseignée | **34 / 40 — et verrouillée dans les 34 cas, sans une exception** |
| `NIR` renseigné | **0 / 40** — donc jamais verrouillé, et ouvert à la saisie sur *toutes* les missions |

La règle « renseigné ⇒ verrouillé » est donc appliquée à la lettre, motif affichable compris. Le NIR,
lui, n'est **jamais** pré-rempli : c'est toujours le terrain qui le pose, et personne ne peut le
corriger ensuite. C'est ce qui met sa relecture à la saisie au-dessus du reste de la liste.

*(Au passage : interrogé en rafale, l'endpoint a rendu 16 `503` d'affilée avant de se stabiliser —
démarrage à froid probable, 40/40 en `200` au tir suivant. Pas de conclusion, mais c'est noté.)*

**Ce qu'il reste à faire** : rien côté API. Côté écran, afficher le motif du verrou plutôt qu'un
champ grisé sans explication, et faire relire le numéro de sécurité sociale à la saisie — il n'est
corrigeable dans aucun module une fois posé. Demandé au dev web le 26/08, avec le reste.

### A4 — 🟢 Le paquet terrain ne proxifie pas les attributs (`OC-7`) — livré le 2026-08-25

**Le plan visait la mauvaise cible.** Il demandait que le bloc `attributes` du paquet terrain vienne
d'Order ; vérification faite, la facturation lit **déjà** ces valeurs chez Orders et les fait primer.
Les faire transiter par Vector aurait construit un troisième chemin vers une donnée que le
consommateur possède avant même de nous appeler — au prix d'un appel HTTP par mission, sur un
traitement déclenché par un clic et déjà mesuré à 14,7 s pour 284 missions.

**Ce qui a été fait à la place** : le paquet lit le magasin Vector et rien d'autre, sans passer par le
résolveur de type. Il est donc **plus rapide qu'avant la bascule**, à contenu identique.

**Pourquoi ce bloc survit** : les valeurs saisies *avant* la bascule n'existent que côté Vector, et
elles comblent les trous de l'historique. Son retrait n'est pas un sujet de code — il vient avec A6.

### A5 — 🟢 Nettoyage du code que la bascule remplace (`OC-9`) — fait le 2026-08-26

Retirés : les quatre ports qui n'avaient plus aucun consommateur et dont les seules implémentations
levaient une exception, leurs enregistrements, et tout ce qui n'était joignable que par eux — deux
présentateurs, un cas d'usage, trois DTO, et des adaptateurs jamais instanciés. **126 tests verts,
aucune route touchée.**

**La « fin » annoncée — supprimer `NotImplementedStubs.cs` — n'est pas atteignable en l'état.** Il en
reste trois, injectés par les routes de recherche de bénéficiaire et de main courante mécanicien. Ces
routes répondent **500**, et le faisaient déjà : ce n'est pas une régression, c'est une dette
ancienne que ce nettoyage met au jour.

**Ce qu'il reste à faire** : décider du sort de ces trois routes — les implémenter ou les retirer du
contrat mobile. La question est partie au dev web le 26/08 : est-ce que le front les appelle ?

### A6 — 🟡 Dépréciation des tables `MOB_*` du contrat (`OC-8`) — décidée, exécution bloquée

✅ **Tranché le 2026-08-26 : abandon pur.** Les 2 132 valeurs d'attributs et les 0 sélection de type
présentes en base sont des données de test ; aucune reprise ne sera écrite. Ce qu'on assume : la
saisie terrain antérieure au 2026-08-25 ne sera plus relisible — elle n'a jamais alimenté la
facturation, qui lit les valeurs chez Order.

Le script de suppression est écrit et **volontairement non joué** :
[`MOB_008_DropContractOverlay.sql`](CaSoft.Erp.USVector.Infrastructure/Sql/MOB_008_DropContractOverlay.sql).

⛔ **Deux verrous, pas un — et le second m'avait échappé.**

1. **Le chemin de désarmement.** Ces tables *sont* le chemin d'avant : tant qu'on peut revenir en
   arrière par configuration, les supprimer transformerait ce retour arrière en panne.
2. **Le paquet terrain.** `FieldAttributesReader` (A4) compose le bloc `attributes` depuis ce même
   magasin, **indépendamment des drapeaux** — vérifié le 2026-08-27 en tirant le paquet en
   production, qui sert bien `ContractId: 1 / "Transport standard"`, ids du catalogue Vector. Jouer
   le `DROP` casserait donc le transfert vers la facturation.

**Ce qu'il reste à faire** : le premier verrou tombe avec le retrait des drapeaux d'A1/A2. Le second
n'est pas technique — c'est une question de calendrier à poser à la facturation : *jusqu'à quand
l'historique d'avant le 2026-08-25 doit-il rester servi ?* Elle lit déjà les valeurs d'Order et les
fait primer ; ce bloc ne comble que les trous des missions antérieures.

---

## B. Dépendances amont — rien à coder dans ce dépôt

*Ces points cassent ou dégradent le terrain, mais se corrigent ailleurs. Ils sont ici pour être
suivis, pas traités.*

| Réf | Ce qui manque | Effet visible côté terrain | État |
|---|---|---|---|
| **B1** | ✅ **`Order OC-28` déployé** — mesuré le 2026-08-25 : **0 mission verrouillée sur 15** (contre 20 sur 25 le 24/08) | plus aucun verrou subi ; le préalable d'armement d'A1 est levé | 🟢 |
| **B2** | **Repli sur le snapshot `ORD_ORDER`** dans le chemin de lecture d'Orders — plan complet, code-only, sans migration : [`plan_correctif_vector_fallback_snapshot.md`](plan_correctif_vector_fallback_snapshot.md) | **~3 883 étapes de mission s'affichent vides** (saisies libres orphelines) ; résiduel attendu ~93 | ⏳ à coder dans `Erp.Order` |
| **B3** | **Chaîne équipage pour le 2ᵉ membre** — soit `GET /crews?personnelId=` ne rattache pas le personnel, soit `Members` est incomplet | sur un équipage à 2, **un seul des deux accède à ses missions** (404) | ⏳ diagnostic prêt → §3.C3 |
| **B4** | `Billed` n'a **aucun écrivain** : la facturation est en lecture seule par décision de son module | le palier existe mais reste théorique | ⛔ décision (§3.E4) |
| **B5** | **`field-data` par période** (à l'image de `for-export`) | 14,7 s pour 284 missions, dans un traitement déclenché par un clic | ⏳ non engagé côté demandeur |
| **B6** | **Tests xUnit du transfert côté Orders** : dérivation `MIS_STATUS`, pose automatique de `Transferable`, garde-fous monotones de `MarkTransferred` / `MarkBilled` | aucun aujourd'hui — filet de sécurité manquant | ⏳ |
| **B7** | **Relance de clôture** : alerter les régulateurs des missions terminées mais **non clôturées** — sans quoi elles ne deviennent jamais transférables | dossiers qui n'arrivent jamais en facturation | ⏳ piste : tableau de bord `?status=Done` |
| **B8** | **Adresses « non structurées »** (`DET-3`) : revue de la saisie et de la validation côté Orders / Address.Api | repli mono-ligne, WARNING journalisé côté Vector | ⏳ mesurer l'ampleur avant d'engager |
| **B9** | **Applicabilité agence/mode non configurée.** Le filtre existe et fonctionne des deux côtés — `ContextOrderCatalogQueryService.ListAsync`, le **même service** pour la régulation et pour le terrain — mais les tables de liaison `ORD_ORDER_CONTEXT_AGENCE` et `ORD_ORDER_CONTEXT_MODE` sont **vides**. Convention du code : *un type sans liaison s'applique partout*. Mesuré le 2026-08-25 : 4 agences (1, 2, 50, 60) → **les 7 types à chaque fois**, et `/referentiels/context-orders?agencyId=60&modeId=1` en rend 7 aussi. | l'ambulancier se voit proposer « Secours sur piste » ou « Centre 15 » sur des missions qui n'ont rien à voir — la régulation aussi | ⛔ décision métier : la **matrice d'applicabilité**. ⚠️ **Piège** : la première liaison posée sur un type le restreint aux seules valeurs liées, donc une matrice partielle fait disparaître ce type partout ailleurs. Aucun écran ne gère ces liaisons (`/admin/context-orders` couvre les types et les attributs, pas l'applicabilité) |
| **B10** | **Attributs au catalogue Order mais rattachés à rien.** Relevé sur **30 formulaires servis en production** le 2026-08-25 : `COMMENTS`, `PHONES`, `MAILS`, `PMT`, `SMUR_DE`, `COMMUNE`, `NOM_CENTRALE` sont définis (script `041`) mais **n'atteignent aucune mission** — ni globaux, ni liés à un contexte. Seuls `BT` (26/30), `DDN`, `NIR`, et ponctuellement `NOM_ASSISTANCE` et `NUM_DOSSIER` (1/30) sortent. ⚠️ `NOM_ASSISTANCE` est déclaré `list` au script mais **servi en `text`, sans options** : la base a divergé du script, ou le script n'a pas été appliqué tel quel. | l'ambulancier a perdu, sur **toutes** les missions, le commentaire libre et l'ajout de téléphone/e-mail au dossier patient — ils étaient globaux dans l'ancien catalogue Vector. `PMT` a disparu alors que son jumeau `BT` est partout | ⛔ paramétrage, même famille que **B9** : le catalogue existe, l'applicabilité n'est pas posée. Deux attributs Vector n'ont en revanche **aucun équivalent** et sont à arbitrer : `REFERENCE` (text) et `URGENT` (checkbox) |
| — | ✅ `engagedOnly` sur `GET /crews/{id}/missions` | *résolu* — Orders filtre `MIS_IS_ENGAGED` | 🟢 |
| — | ✅ `null = effacé` sur `PUT /missions/{id}/operational` | *résolu* 2026-07-05 — l'annulation d'un jalon remonte à la régulation | 🟢 |

> Contrat détaillé de tout ce que Vector attend d'Orders : [`endPoint.md`](endPoint.md).

---

## C. Identité & authentification

### C1 — ⛔ Écran de rattachement compte Keycloak ↔ ambulancier (ex-MOB-4b)

Aujourd'hui le rattachement se fait **par INSERT SQL manuel** dans `PER_KEYCLOAK_MAP`. C'est le seul
maillon manuel de la chaîne : sans lui, un ambulancier ne reçoit aucune mission.

**À trancher avant de coder — l'hôte de l'écran :**
- Les **endpoints Orders sont livrés** (`GET`/`PUT`/`DELETE /personnel/{id}/keycloak`, `GET /keycloak/users`).
- Le **module Identity possède désormais cette correspondance** : reprise jouée le **23/08/2026**
  (146 pivots, 105 correspondances) ; Orders doit à terme appeler Identity au lieu d'écrire sa table.
- L'écran Siège envisagé comme hôte **n'existe plus que dans `Archives/`**.

→ **Identity** (cohérent avec la cible) **ou** endpoints Orders le temps de la bascule. Dans les deux
cas : lister les comptes Keycloak, rechercher un `PER_PERSONNEL`, persister **via API** (jamais
d'écriture directe en base), afficher les garde-fous (compte déjà lié, personnel déjà lié, 409).
⚠️ Tant que les trois emplacements du rattachement coexistent, ils divergeront — et l'écart se verra
le jour où un ambulancier ne recevra plus ses missions.

### C2 — ⏳ Authentification de service à service (`DEC-6`) — *les deux sens*

**Sortant.** `Orders.Api` est appelée **sans jeton** : aucun en-tête `Authorization` sur les deux
clients HTTP. Cela tient tant qu'Orders.Api n'est pas protégée. Le jour où elle l'est, il faut un
**client credentials Keycloak** (compte de service dédié à Vector) avec cache et renouvellement, sur
le modèle du module Identity. **Rien ne bloque, et c'est à anticiper** : le symptôme sera une série de
401 sur la joblist, en production, sans autre indice.

**Entrant — nouveau, et c'est désormais la moitié qui presse.** L'API a été **fermée par défaut le
2026-08-25** : jusque-là aucun contrôleur ne portait d'attribut d'autorisation et il n'existait aucune
politique globale, si bien que **seuls les cinq endpoints passant par `CrewAccess` étaient protégés**.
Tout le reste répondait 200 à qui connaissait un identifiant de mission — y compris la structure du
formulaire, **valeurs comprises** : une date de naissance de patient a été relevée servie sans
authentification.

Il reste **quatre ouvertures**, et elles n'ont qu'une justification : la facturation les tire en
serveur-à-serveur, sans jeton, faute de `DEC-6`.

| Route ouverte | Ce qu'elle expose |
|---|---|
| `GET api/missions/{id}/field-data` | le **dossier terrain complet** de la mission |
| `GET api/Signature/{id}` | l'image de la signature du patient |
| `GET api/documents/{id}/content` | les octets d'un document |
| `GET api/mutuelle-card/{id}/image` | ⚠️ **carte mutuelle — donnée de santé** |

Elles se referment **ensemble**, le jour où Vector saura présenter un jeton de service. `DEC-6` cesse
donc d'être une anticipation : c'est ce qui tient ces quatre portes ouvertes, la dernière étant la
plus sensible.

⚠️ **Garde-fou en place** : `AnonymousSurfaceTests` fige la liste exacte de ce qui répond sans jeton.
Toute route anonyme ajoutée fait échouer la suite — une exception doit être une décision, pas un
oubli. Le test rappelle aussi que ces quatre entrées doivent disparaître avec `DEC-6`.

### C3 — ⏳ Trancher le 404 du second membre d'équipage

Le diagnostic est **écrit et prêt** ([`docs/auth/diag-404-second-membre-equipage.md`](docs/auth/diag-404-second-membre-equipage.md)) :
`GET /api/diag/crew-chain` rejoue la vraie règle domaine sans passer par le cache. Le travail restant
n'est pas du code Vector mais une **exécution** : lancer le diag sur les **deux** membres du même
équipage **au même instant**, comparer, et ouvrir le ticket Orders correspondant (§3.B3).
⚠️ Le filtre d'appartenance qui produit le 404 est **volontaire** — il bouche un trou d'autorisation
réel. Il ne doit pas être retiré pour faire disparaître le symptôme.

---

## D. Robustesse des appels sortants (`DEC-7`) — ⏳ isolé, attaquable seul

Les deux clients HTTP sont enregistrés avec la seule `BaseAddress` : **pas de timeout explicite**
(donc 100 s par défaut, ce qui fait pendre une requête mobile), **pas de retry ni de disjoncteur** sur
le chemin de **lecture**. L'écriture est couverte (file de projection + worker avec retry).

**Contenu** : timeout court et explicite sur les deux clients, puis `AddStandardResilienceHandler`
(ou Polly), **en gardant** le comportement de lecture actuel qui tolère un 404 (liste vide plutôt
qu'erreur). **Fin** : aucun appel sortant ne peut pendre au-delà du timeout choisi.

---

## E. Chaîne vers la facturation

Le plan d'origine confiait tout l'aval à Certification ; **la réalité livrée est un partage** :
Certification découvre la file et **écrit le retour** — c'est ce write-back qui arme le gel côté
Vector ; la facturation tire le paquet terrain, agrège et produit le fichier AidesNSoft. Les deux
sont livrés. Reste :

### E1 — ⏳ Le kilométrage dans le paquet (`MOB-10`) — *arbitrage puis code*

Aujourd'hui `kilometers = null`, et la facturation attend ce champ pour activer son contrôle
« kilométrage absent » (une ligne de configuration chez elle). Le km est aujourd'hui
**équipage/véhicule-scoped** : il n'existe pas de km **par mission**.
→ **Arbitrer avec la facturation** : le km véhicule suffit-il, ou faut-il un relevé début/fin par
mission ? La seconde réponse ouvre une étape complète (table + saisie mobile + alimentation du
paquet) ; la première se réduit à alimenter le champ.

### E2 — ⏳ Horodatages : dire l'heure qu'il est

Deux défauts sur le même sujet, à corriger ensemble :
- Les jalons Vector sont en **UTC mais ne le déclarent nulle part** — les consommateurs le devinent.
- **`SIG_DATETIME` est écrit en heure locale** (`DateTime.Now`, deux occurrences), contrairement à
  tout le reste.

**Fin** : signature en UTC comme le reste, et fuseau **déclaré explicitement** dans le contrat du
paquet. Rien ne bloque.

### E3 — ⏳ `field-data` en lot — *voir §3.B5*, non engagé côté demandeur.

### E4 — ⛔ `Billed` : le faire écrire, ou retirer le palier

La facturation est en lecture seule par décision de son module ; le statut `Billed` reste donc
théorique. **À trancher** : le faire poser par la facturation à la publication d'une journée, ou
**retirer le palier** de l'énumération. En l'état il n'est ni faux ni utile.

---

## F. Fonctions terrain restantes — indépendantes les unes des autres

### F1 — ⏳ Carte mutuelle : débloquer l'adoption *(avant tout le reste)*

**Rien à coder côté API.** Confirmer avec le dev web que l'écran ambulancier appelle bien la capture
(multipart) et le `PATCH` de saisie, puis **mesurer le remplissage en production**. C'est le seul
point qui débloque de la valeur immédiate — l'extraction automatique n'a aucun intérêt tant qu'aucune
photo n'arrive. Détail : [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) §3.1.

### F2 — ⏳ Carte mutuelle : extraction automatique (P3)

Seul le **statut** existe en base ; aucun service d'extraction n'est écrit. Pipeline **asynchrone**
(la capture ne doit jamais attendre l'IA) : `pending` à l'upload → worker → **Claude vision** à sortie
structurée imposée (quatre champs + confiance, journalisée) → `extracted` → **écran de validation
humaine** → mêmes champs que la saisie manuelle + `validated`. À cadrer avant de coder : **où tourne
l'appel au modèle** (worker en DMZ, ou service LAN qui tire l'image — la seconde option évite de
donner une clé API à un composant exposé), quotas et coût par carte.
**Préalable** : F1 (§1.8). **Fin** : quatre champs proposés automatiquement, jamais écrits en aveugle.

### F3 — ⏳ Trois chantiers hérités, autonomes et sans dépendance entre eux

| Réf | Objet | Ce qui reste |
|---|---|---|
| `MOB-12` | **Fin de service** | Le contrôleur existe mais **vise la mauvaise chose** : `MOB_SESSION` n'est plus la source d'authentification depuis Keycloak — la clôture doit viser la **vacation d'équipage côté Orders**. Le `TODO` sur les permissions de poster une date de fin depuis la régulation reste ouvert. *Re-cadrage avant code.* |
| `MOB-14` | **Logs mécaniques et analyses** | Trois contrôleurs exposés mais reposant sur des stubs : à faire, les tables `MOB_MECANIQUE_*`, les référentiels (acteurs, natures, contraintes) et les repositories réels. |
| `MOB-16` | **Connecteurs Sirus / GpsGate** | Portés et injectés, **non recâblés fonctionnellement** : positions d'équipage (GpsGate REST) et statuts véhicule (Sirus UDP). Secrets déjà externalisés. |

### F4 — ⏳ Présence : qui est connecté à Vector — *décision d'abord*

Spec complète, **aucun code** : [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md).
L'API est stateless : « connecté » n'est pas une donnée native, il faut la **définir** puis
l'instrumenter. Retenu (à confirmer) : **activité applicative** comme cœur, croisée avec l'**état de
service** déjà exposé par Orders.
**Deux décisions préalables** : la définition retenue, et surtout la **topologie** — un cache par
process ne convient pas en multi-instance, il faut alors une table dédiée ou Redis.
**Découpage** : store + estampillage best-effort au point de passage d'identité → endpoint de
restitution restreint (régulateur/admin) → enrichissement identité + état de service → cadrage
rétention. ⚠️ Donnée de suivi d'activité d'un salarié : **cadrage RH/RGPD requis**.

---

## G. Dette & hygiène — refactor, aucun changement de contrat

*Chapitre attaquable en continu, par petits lots, entre deux chantiers.*

### G1 — ⏳ Result pattern, vague 2 (retrait de l'échafaudage)

Vague 1 faite (**31/32**). Ordre sûr, chaque type supprimé **uniquement** quand un grep confirme
0 référence : le straggler `ClSetDriverUseCase` d'abord (→ 32/32), puis `ClUseCaseHandler` (déjà
0 référence active), puis les contrôleurs en Result direct, puis les services, puis les types de
transition. ⚠️ `Option Strict Off` : des conversions aujourd'hui implicites deviennent explicites —
vérifier chaque retour. ⚠️ Re-builder l'Api en **Release** soi-même (les tests ne compilent pas l'Api).
Détail : [`refactor_result_pattern.md`](refactor_result_pattern.md).

### G2 — ⚠️ Alias de compatibilité — se retirent **sur confirmation du front**, jamais d'office (D14)

| Réf | Alias conservé | Condition de suppression |
|---|---|---|
| C1 | `IsAck`, alias lecture seule de `IsSeen` | UI web migrée sur `IsSeen` |
| C2 | Champs JobDetail legacy en parallèle des nouveaux (`Schedule`, `TransportMode`, `Departure`/`Arrival`) | UI web basculée sur `ScheduleLabel`, `TransportModeLabel`, `PickupLocation`/`DropoffLocation` |
| C3 | `SelectedDriver` jamais null (`Guid.Empty` + `""`) | UI web garde-fou le `null` proprement |
| — | Champs typés des lieux, en parallèle de `PickupDisplay`/`DropoffDisplay` | UI web bascule sur l'affichage piloté serveur |

> ✅ **C4 et C5 sont éteints.** C4 (sur-rapatriement des missions + filtre équipage côté client) est
> résolu autrement que prévu : la joblist lit la route crew-scopée, et `ListMissionsAsync` **n'a plus
> aucun appelant** — candidate à la suppression avec G1. C5 (annulation d'un jalon locale seulement)
> est résolu par `null = effacé` côté Orders.

### G3 — ⏳ Suite de smoke `.http` (`MOB-9` résiduel)

Filet de non-régression manquant : couvrir login → joblist → jobdetail → time → signature dans
`CaSoft.Erp.Mobile.Api.http`. *(Le retrait de l'ancienne `WebApi` est sans objet : la solution legacy
n'est plus sur le disque.)*

### G4 — ⏳ Suivi des migrations SQL — *la dette qui a déjà coûté une journée de production*

`…/Sql/` n'a **aucune table de suivi de schéma**. Constat de la facturation : les bases de prod et de
dev ont divergé **en sens inverse** (l'une avait `MOB_006` sans `MOB_004/005`, l'autre l'inverse) —
résolu pour la production le 06/08/2026, **la cause demeure**. Symptôme : un 500 opaque et une journée
sans données terrain. → table de suivi (modèle `__BillingGatewaySchema`) + **contrôle au démarrage**.
À réconcilier au passage : la migration du transfert est référencée `027` dans l'historique et `034`
dans le dépôt.

Le besoin s'est révélé plus large que le schéma : voir **G8**, qui le généralise à « quel code
tourne, sur quelle base ».

### G5 — ⚠️ Deux dettes de forme, sans urgence

- **Nommage des DTO (`DET-4`)** : suffixe directionnel de la convention (`…DtoIn` / `…DtoOut`) au lieu
  des noms hérités du portage. **Aucun impact JSON** — le nom du type n'apparaît pas sur le fil.
  Priorité basse, par lots.
- **Pont sync/async** : `.GetAwaiter().GetResult()` sur joblist / jobdetail / identité, hérité du
  contrat legacy synchrone. Se lève avec une refonte des interfaces legacy en async.

### G6 — ⏳ Documentation à réconcilier *(rapide, à faire tôt)*

- **`README.md` décrit un état périmé** : accès ERP **in-process**, références projet vers Orders,
  sous-app IIS `/mobile`, « prochaine étape MOB-4 ». Tout cela est faux depuis le découplage.
- **`docs/deploiement/configuration-keycloak-iis.md`** annexe encore « `Authority`/`Audience` codés en
  dur dans `Program.cs` » : **résolu** (`KC-1`), la config pilote réellement les deux.
- **`BUG_DISPLAY.MD` §6** présente `DET-1` comme bloqué par le front : **livré et basculé**. Restent
  ses trois vérifications d'exploitation, elles bien ouvertes : affichage sur une mission **retour**
  (service non inversé), lieu **non référencé** (service référentiel conservé), et **fraîcheur des
  coordonnées** — le re-géocodage sur édition d'adresse bénéficiaire n'est pas implémenté, donc un
  point peut désigner l'ancien bâtiment : **à traiter avant tout usage navigation**.
- **`endPoint.md` §5** dit encore `engagedOnly` non honoré : c'est fait.

### G7 — ⚠️ RGPD (P4)

Données de santé (documents, carte mutuelle, anomalies) servies par une API exposée : rétention et
purge (3 ans), chiffrement au repos, contrôle d'accès fin sur l'image de carte, audit des accès. Un
seul lot pour les trois familles de données.

---

### G8 — ⏳ Savoir ce qui est réellement en service — *généralise G4*

**Aucun module ne sait dire quel code il exécute ni à quelle base il parle.** Pour l'établir, il faut
aujourd'hui ouvrir le partage de déploiement, lire des horodatages de DLL, empiler trois couches de
configuration à la main et deviner laquelle gagne. Chaque diagnostic commence donc par une enquête,
et cette enquête n'est jamais dans le ticket.

**La journée du 2026-08-25 a produit trois occurrences du même manque, en quelques heures :**

| Ce qui a divergé | Comment on s'en est aperçu |
|---|---|
| Le binaire Vector de production avait été **publié depuis un arbre de travail non commité** : git ignorait ce que servait l'API | par hasard, en cherchant l'origine d'un champ inattendu dans une réponse |
| Les drapeaux de bascule étaient **armés sur le serveur, désarmés dans le fichier versionné** — et ce fichier est shippé par la publication, donc le déploiement suivant les éteignait | en lisant le fichier déployé, pas l'application |
| **Deux bases candidates pour Orders** : la vraie est `BD_ERP_SANITAIRE_DEV` sur `109`, l'autre `BD_ERP_SANITAIRE` sur `115`, schémas différents | par une erreur SQL, après avoir joué un correctif au mauvais endroit |

Le troisième cas est le plus instructif : **la correction n'a rien signalé.** Elle a modifié
consciencieusement des lignes que personne ne lit. Une opération qui « réussit » sans effet est pire
qu'une qui échoue — elle ferme le sujet.

**Ce qu'il faut, et rien de plus** : que chaque API expose, sur une route réservée comme l'est déjà le
diagnostic, ce qu'elle est en train de faire —

- le **commit** qu'elle exécute (SHA court + date), injecté au build ;
- l'**environnement** retenu (`ASPNETCORE_ENVIRONMENT`) ;
- la **base réellement résolue** après empilement des trois couches : serveur + nom, **jamais**
  d'identifiants ;
- l'état des **drapeaux** de bascule en vigueur.

Avec la table de suivi de schéma de **G4**, cela fait deux choses à mettre en place et **une seule
lecture** pour répondre à « qu'est-ce qui tourne, où, et sur quoi ». Aujourd'hui la réponse coûte une
demi-heure et reste incertaine.

⚠️ **À gated comme `/api/diag`** : nom de serveur et nom de base ne sont pas des secrets, mais ne se
publient pas non plus sans authentification.

**Fin** : pour chaque module en service, une requête suffit à dire quel commit tourne et sur quelle
base — et un déploiement dont le code n'est pas commité devient visible au lieu d'être découvert.

## H. ⚪ Différé (V2 / hors MVP)

- **`Vd-1` — base `DB_VECTOR` dédiée** (renommage/relogement, secrets séparés) : **pertinent dès
  maintenant**, seul jalon DMZ non conditionné à la V2.
- **`CREW-2` — accès anticipé à cheval sur minuit** : à 23:50, une vacation démarrant à 00:15 ne
  remonte pas. Correctif connu (élargir la requête à J+1 puis dédoublonner), **non prioritaire** —
  décision métier du 2026-08-02 : les vacations de nuit ne sont pas concernées.
- **Durcissement DMZ événementiel** (`Vd-2` à `Vd-4`, `Vd-7`, `Vd-8`) : projection poussée, Outbox
  généralisée + bridge + RabbitMQ, contrats d'événements — et **push temps réel** (SignalR) en
  remplacement du polling régulateur. *Le socle retenu reste l'API HTTP à travers firewall* :
  [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md).
- **`Vd-6` — photos hors SQL** (base = référence + métadonnées, purge à 3 ans, migration des blobs)
  et **`Vd-5` — masquage** (NIR partiel, visibilité de l'équipage retour calculée côté interne).
- **Assembly de contrats partagé `Orders.Contracts`** (option 4b) — anti-dérive du JSON. Le risque de
  drift est **assumé** : il se manifestera par un champ silencieusement null, pas par une erreur.
- **Éviction ciblée du cache d'identité**, **mode offline**, **géolocalisation avancée**, et le
  **renommage** `CaSoft.Erp.USVector.*` → `CaSoft.Erp.Vector.*`.

---

# 4. Où vit quoi

| Donnée | Emplacement | Autorité |
|---|---|---|
| Missions, commandes, équipages, véhicules, personnel, bénéficiaires | Orders (`BD_ERP_SANITAIRE_DEV`), lu par API HTTP | **Orders** |
| Jalons terrain détaillés, signature, anomalies, documents, carte mutuelle, file de projection | Base Vector (`BD_ERP_MOBILE_APP`, tables `MOB_*`) | **Vector** |
| Avancement opérationnel projeté + statut de transfert | Orders (`ORD_MISSION_OPERATIONAL`, `MIS_TRANSFER_STATUS` / `MIS_TRANSFERRED_AT` / `MIS_BILLED_AT`) | **Orders** (Vector pousse, Certification écrit le statut) |
| Type de mission (contexte) et attributs de facturation | Orders (`ORD_ORDER_CONTEXT*`) — **cible**, bascule à faire (§3.A) | **Orders** |
| Rattachement compte Keycloak ↔ ambulancier | `PER_KEYCLOAK_MAP` (Orders) → **cible : module Identity** (§3.C1) | **Identity** |

## 4.1 Migrations SQL

| Base | Script | Contenu | État |
|---|---|---|---|
| Orders | `026_AddKeycloakMap.sql` | `PER_KEYCLOAK_MAP` | 🟢 appliqué |
| Orders | `034_AddMissionOperationalAndTransfer.sql` | `ORD_MISSION_OPERATIONAL` + `MIS_TRANSFER_STATUS` | 🟢 appliqué 2026-06-22 ⚠️ *(référencé `027` dans l'historique — cf. §3.G4)* |
| Orders | `063` | `OCT_FIELD_OVERRIDABLE` (surchargeabilité au catalogue) | 🟢 joué 2026-08-25 sur `109` et `118` — *ne pas rejouer `062`* |
| Vector | `MOB_001` → `MOB_006` | session/timeline/signature · catalogue contrat + overlay · carte mutuelle · anomalies · documents · file de projection | 🟢 appliqués |
| Vector | `MOB_007` | libellé `ART80` corrigé en « Article 80 » | 🟢 appliqué |

> État **vérifié le 2026-08-24** sur `BD_ERP_MOBILE_APP` (192.168.1.109) : les 13 tables `MOB_*` sont
> présentes. ⚠️ Rien ne garantit l'alignement des **autres** environnements — il n'existe aucune table
> de suivi de schéma (§3.G4).

---

# 5. Configuration & déploiement

*Procédure complète (IIS, pool, permissions, service account de diag, checklist) :*
[`docs/deploiement/configuration-keycloak-iis.md`](docs/deploiement/configuration-keycloak-iis.md).
*Ici, seulement ce qui a déjà cassé la production.*

**Clés lues** : `ConnectionStrings:MobileDb` (`OrdersDb` inutilisé depuis le découplage) ·
`OrdersApi:BaseUrl` · `AddressApi:BaseUrl` ·
`Keycloak:{Enabled, Authority, Audience, DisableValidation, RequireHttpsMetadata, AdminClientId, AdminClientSecret}` ·
`Diagnostics:Enabled` · `MobileIdentityCache:{PersonnelMinutes=30, ActiveCrewsMinutes=15}` · secrets
GpsGate/Sirus `__SET_VIA_ENV__`.

## 5.1 Les quatre pièges avérés

| Piège | Conséquence | Règle |
|---|---|---|
| `OrdersApi:BaseUrl` **sans slash final** | le dernier segment est perdu → 500 sur la joblist (panne réelle, juillet 2026) | terminer par `/`, PathBase IIS inclus |
| Publication **RID `win-x64`** | `SqlClient` charge sa façade « PlatformNotSupported » → SQL injoignable | publier **portable** (les profils le sont, c'est voulu) |
| `Keycloak:DisableValidation=true` hors dev | jetons décodés sans vérification | **`false` en prod** ; une `Authority` vide ou restée au placeholder **empêche le démarrage** avec un message explicite, plutôt qu'une série de 401 sans cause lisible |
| `Diagnostics:Enabled=true` en prod | expose la résolution d'identité et la liste des comptes Keycloak | **dev/staging seulement** ; ailleurs `/api/diag*` rend 404, c'est voulu |

> ℹ️ `Audience` sert aussi d'**`azp` attendu** : l'audience n'est volontairement pas validée (Keycloak
> émet `aud=account` sans mapper sur le realm) ; c'est l'`azp` qui est contrôlé, signature, issuer et
> expiration restant validés.

## 5.2 Règle de config des serveurs — trois couches à ne pas confondre

C'est la source des régressions d'authentification et de résolution d'adresses :
**`web.config`** (serveur, manuel, jamais régénéré, hors git) porte l'environnement et les secrets et
survit à toute publication · **`appsettings.json`** fait partie de la sortie de publication, donc
porte la **valeur de référence (celle de la prod)**, jamais une valeur de poste local ·
**`appsettings.{Environment}.json`** est shippé **et** prioritaire : c'est là qu'on décrit les
**déviations**.

> Règle : le défaut sûr est dans la base, les écarts dans l'overlay — **jamais** une valeur éditée à
> la main sur le serveur, qui n'est protégée que par un hasard d'incrémentalité MSBuild.

## 5.3 Déploiement

`.\deploy.ps1 dev` · `.\deploy.ps1 prod` (confirmation à taper ; `-Force` en non-interactif) →
`\\192.168.1.112\{dev_api,prod_api}\Vector.Api`. Le script fait un **pré-vol** (partage accessible),
vérifie l'horodatage bin↔UNC et que `appsettings.json` a bien atterri. `app_offline.htm` est posé puis
retiré → **courte coupure de l'API** à chaque publication.
Prérequis : `net use \\192.168.1.112\prod_api /user:192.168.1.112\DeployApi *`.

---

# 6. Retiré du plan — obsolète ou abandonné

*Conservé uniquement pour ne pas réinstruire ces pistes.*

| Ce qui a disparu | Motif |
|---|---|
| **Accès in-process aux projets Orders** (références projet) et **schéma DMZ strict comme cible V1** | Vector consomme `Orders.Api` en HTTP et joint sa base à travers un firewall : isolation de build obtenue, architecture conforme, le durcissement événementiel devient une option V2 (§3.H). `ConnectionStrings:OrdersDb` est devenu inutilisé. |
| **Table de correspondance `MOB_CREW_MAP`** (équipage `int` ↔ `Guid`) | Tranché : toutes les identités de référence passent en Guid. La table n'a jamais existé. |
| **Accusé de réception distinct** (`MST_ACK_AT`, `ClAckJobUseCase`) | Remplacé par le marqueur **« Mission vue »**. `MST_ACK_AT` reste dormante ; `IsAck` survit comme alias (§3.G2). |
| **Login déclaratif `api/login`** et le jeton Guid de `MOB_SESSION` comme source d'authentification | Remplacés par Keycloak. |
| **Claim `per_id` dans le jeton** | Écarté (2026-07-12) : figé jusqu'au prochain login et non invalidable ; sous turnover, la résolution HTTP cachée est préférée car son cache s'invalide (TTL ramené à 30 min). |
| **Table `MOB_KM`** telle que planifiée | Le kilométrage est équipage/véhicule-scoped ; il n'y a pas de km par mission. Le besoin réel reste à arbitrer (§3.E1). |
| **Catalogue autonome de contrats** (`MOB_CONTRACT_*`), son **seed métier** et la **purge des valeurs orphelines** | Le référentiel passe côté Order (§3.A). Le seed provisoire `STANDARD` + `ART80` ne sera jamais complété ; la purge serait au pire un script ponctuel, pas une fonctionnalité. |
| **Interfaces legacy `IContractTypeRepository` / `IAttributsRepository` / `IInvoicingRepository`** et `JobRepository.UpdateCommande` / `.Invoicing` | Remplacées par un port ciblé, lui-même remplacé par les endpoints ContextOrder. **Retirées le 2026-08-26** (§3.A5). |
| **`FetchInstructionList` / `AckInstruction` / `GetCrewIdList(date)` / `GetCrewDriver(vehicleId)`** | Aucun équivalent ERP ; hors périmètre, laissés en `NotImplementedException`. |
| **Blocages levés** : migrations `MOB_003/004/005` « à exécuter en db_owner » · « projection du statut de fin différée, faute de transition côté Orders » | Résolus : tables présentes en base (§4.1) ; la dérivation existe et Vector la pousse — seule la clôture reste la main du régulateur (D11). |
| **Documents « source PDF ERP »** | Livré autrement : documents et photos stockés côté Vector, servis par Vector.Api. |
| **Lot Certification TRF-12..15 tel que planifié**, dont la **restitution de la carte mutuelle** | La réalité est un partage Certification / facturation, largement livré (§3.E) ; le bloc mutuelle est tiré via le paquet terrain. |
| **Arbitrages tranchés** : 4a vs 4b (découplage) · 2a/2b/2c (champs mutuelle) · « défaut = premier contexte actif » | Respectivement **4a**, **2b**, et **supprimé** (« non renseigné » est un état valide). |
| **Extension de l'écran Siège `UcEmployeeKeycloakAccount`** comme hôte du mapping Keycloak | Le module Siège n'existe plus que dans `Archives/` et la correspondance passe à Identity (§3.C1). |
| **Historique de portage legacy** (divergences framework, namespaces, warnings) et **dettes C4, C5, C6, KC-1, DEP-2, DET-1, DET-2, DET-3** | Portage terminé, dettes résolues et vérifiées ; l'information vit dans `git log`. |

---

# 7. Documents voisins

| Doc | Genre | Ce qu'il apporte |
|---|---|---|
| [`AppMobile_specifications.md`](AppMobile_specifications.md) | Spec fonctionnelle | Le besoin et le vocabulaire (plan de travail, statuts terrain, isolation par équipage, séparation officiel↔terrain) |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Devplan | Carte mutuelle : capture, restitution, OCR (§3.F1-F2) |
| [`VECTOR_ORDERS_DECOUPLING_devplan.md`](VECTOR_ORDERS_DECOUPLING_devplan.md) | Devplan | Découplage HTTP : contrat consommé, auth de service, résilience (§3.C2, §3.D) |
| [`refactor_result_pattern.md`](refactor_result_pattern.md) | Devplan refactoring | Result pattern, vague 2 (§3.G1) |
| [`plan_correctif_vector_fallback_snapshot.md`](plan_correctif_vector_fallback_snapshot.md) | Plan correctif | Repli sur le snapshot `ORD_ORDER` — **à coder dans `Erp.Order`** (§3.B2) |
| [`feadesc_utilisateurs_connectes_vector.md`](feadesc_utilisateurs_connectes_vector.md) | Spec | Présence des utilisateurs connectés (§3.F4) |
| [`spec_architecture_vector_mission_dmz.md`](spec_architecture_vector_mission_dmz.md) | Spec architecture | Cible DMZ événementielle (option V2) |
| [`endPoint.md`](endPoint.md) | Contrat HTTP | Ce que Vector attend d'Orders.Api |
| [`docs/auth/optimisation-chaine-authentification.md`](docs/auth/optimisation-chaine-authentification.md) · [`docs/auth/diag-404-second-membre-equipage.md`](docs/auth/diag-404-second-membre-equipage.md) | Notes | Chaîne d'identité et caches · procédure de diagnostic (§3.C3) |
| [`../Erp.Order/note_vector_orderContext_mission.md`](../Erp.Order/note_vector_orderContext_mission.md) | Note d'intégration | ContextOrder : endpoints, attributs, règles DDN/NIR/PMT/BT |
| [`note_web_alexandre_context_mission_dto.md`](note_web_alexandre_context_mission_dto.md) | Contrat front | **Les DTO de la bascule du contexte, tels qu'ils répondent en production** — la note qui débloque le sélecteur (§3.A1) |
| `note_ui_alex.md`, `note_web_alexandre_*.md`, `docs/ui-web/*` | Contrats front | Ce qui est promis au dev web |
| `docs/deploiement/*`, `BUG_DISPLAY.MD`, `README.md` | Exploitation | ⚠️ périmés par endroits — cf. §3.G6 |

---

**Fin du document**
