# Livré — Vector (module terrain ambulanciers)

> **Mis à jour le** 2026-09-13 · **En production** : `c5eedca` (`main`), rechargé le 2026-09-13 à 20:04,
> vérifié par sourcelink.
>
> Ce document porte **ce qui est livré** : ce que le module fait, le journal daté des livraisons,
> les décisions appliquées, la configuration qui a déjà cassé la production, les pistes retirées.
> [`devplan.md`](devplan.md) ne porte que **les sujets ouverts**.
>
> **Il se met à jour au prompt « compact devplan »**, avant la régénération du plan : ce qui est
> livré depuis la dernière édition entre ici, daté. **Horodatage** : le jour de la mise en
> production quand elle est constatée, sinon le jour du commit — l'entrée le précise.
>
> **Première édition (2026-09-13)** : reprend tout ce que `devplan.md` portait comme livré (§1,
> étapes terminées du §3, §4 à §6), recoupé avec `git log` et vérifié dans le code quand c'était
> vérifiable.

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

Ses missions lui sont visibles **30 minutes avant sa prise de service**. Il ne voit que les missions
**engagées** par la régulation : une mission simplement affectée au planning ne remonte pas.

## 1.3 Il voit son plan de travail et le fait avancer

- La **liste des missions du jour** : patient, mode de transport, sens, lieux, horaires.
- Le **détail d'une mission** : identité et coordonnées du patient, adresses résolues, horaires, sens
  et fréquence, service médical destinataire. L'affichage des lieux est **composé par le serveur**.
- Le marqueur **« mission vue »** : la régulation voit l'heure de prise de connaissance.
- Les **cinq jalons de progression**, horodatés, **annulables** — le retour arrière remonte à la
  régulation.
- La **signature du patient**, avec un indicateur de présence visible dès la liste.
- Le **conducteur** de l'équipage, consultable et modifiable.

Chaque geste est **projeté vers la régulation** en temps quasi réel ; les envois en échec sont mis en
attente et rejoués automatiquement, sans jamais bloquer la saisie.

## 1.4 Il complète le dossier depuis le terrain

- **Type de mission et attributs de facturation**, servis par Order : formulaire dynamique dont le jeu
  de champs dépend du type ; champs pré-remplis et verrouillés quand la fiche patient les connaît ;
  saisie validée par Order, tout ou rien.
- **Anomalies** constatées en mission : non bloquantes, arbitrées par la facturation.
- **Documents et photos** rattachés à la mission.
- **Carte mutuelle** du patient — photo **depuis la mission** et saisie des quatre champs
  ([`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md)).

Principe constant : **le terrain n'écrase jamais la donnée officielle de l'ERP.**

## 1.5 Le dossier part en facturation

- Une mission **clôturée par le régulateur** devient automatiquement transférable.
- Les modules d'aval disposent d'une **file des missions à traiter**.
- Ils récupèrent **un paquet unique et versionné** (jalons, signature, attributs, mutuelle,
  documents, anomalies) et tirent les **pièces jointes à la demande**. Ce paquet est **réellement
  consommé** par la facturation, mission par mission.
- Une fois la mission transférée, **le dossier est gelé côté terrain**.

## 1.6 Ce qui tient tout cela debout

- **Un contrat mobile préservé** : les ajouts sont additifs, les anciens champs restent servis.
- **Une authentification à un seul point de passage**, avec cache.
- **Une API fermée par défaut** : quatre sorties anonymes nommées, justifiées et figées par
  `AnonymousSurfaceTests`.
- **Un outil de diagnostic** de la chaîne d'identité, réservé au dev.
- **Un code applicatif homogène** : tous les cas d'usage renvoient un `ClResult` typé ; l'ancien
  mécanisme à présentateurs est entièrement retiré.
- **Un déploiement outillé** : recette et production, confirmation à taper, pré-vol du partage,
  contrôle de ce qui est arrivé.

---

# 2. Journal des livraisons

*Du plus récent au plus ancien.*

## 2026-09-13 — Carte mutuelle : la capture passe par la mission

*En production — première publication à 14:40 depuis un arbre non commité (§8), **republiée à 15:05
depuis `main`** (`a6c2aba`, fusion de `e942967`), vérifiée par sourcelink à 15:09.*

- **Cause de la table vide trouvée** : 1 344 paquets terrain sur 1 344 sans carte (24→27/08, mesure
  facturation). La capture demandait l'identifiant du patient, qu'aucun DTO terrain ne porte
  (`ClPatientDto`). Ce n'était pas un défaut d'usage.
- `POST`/`GET api/missions/{missionId}/mutuelle-card` : patient résolu côté serveur (mission →
  commande → bénéficiaire, `MissionBeneficiaryQueryService`), mission tracée d'office, validation et
  stockage repris de la capture existante. Premiers cas d'usage asynchrones (`HandleAsync`).
- 8 tests ; **suite complète : 112 verts**.
- Note au dev web écrite : [`note_web_alexandre_carte_mutuelle.md`](note_web_alexandre_carte_mutuelle.md).
- Plan mutuelle recalé : `M7` (facturation : la carte ne va jamais en `C54`), `M8` (capture par
  mission), `M9` (route image anonyme jusqu'à P4) ; écart `T2` de la traçabilité levé.
- ⚠️ Publiée **avant** le commit (§8).

## 2026-09-13 — Incident : des ambulanciers en service exclus à la fin théorique de leur vacation

*Corrigé en production — `c5eedca` (`main`), rechargé à 20:04:18, vérifié par sourcelink et journal.*

- **Symptôme** : « l'application ne fonctionne plus » remonté par les utilisateurs. Le sélecteur
  d'équipage répondait **« Votre service est clôturé »** à des ambulanciers encore en service :
  137 réponses 404 entre 18 h et 19 h, puis **125 refus pour 9 ambulanciers** de 18:59 à 19:57
  (équipages A209, A307, A111, A603 — **ouverts** chez Orders, fin à 18:00). Les 15 autres
  personnels connectés passaient.
- **Cause** : Orders `7984ec0` (13/09, 15:55) donne à toute vacation une **fin théorique** dès sa
  création (début + 10 h, `finDeServiceSource` = 2). Vector tenait depuis le 12/07 (`b6d5dec`)
  « fin de service dépassée » pour une clôture — juste tant que la fin n'était posée qu'à la clôture
  réelle. Aucun des deux modules n'était faux isolément ; c'est leur rencontre qui a exclu le terrain.
- **Écarté** : le renommage du client Keycloak `us-vector-api` → `erp-vector-api` soupçonné en
  premier. Journal : 316 jetons mobiles validés, 0 rejeté, appels à Orders en 200.
- **Correctif** : la clôture se lit sur le **statut** de la vacation (`status` = 2, `Closed`),
  jamais sur l'heure de fin — dans la règle de sélection, l'affichage du sélecteur (« en cours »,
  « clôturé ») et le diagnostic. La fin reste affichée ; l'expiration à 18 h reste le filet.
  3 tests reproduisent le cas du soir ; 140 verts.
- **Constaté après publication** : plus aucun refus « service clôturé » ; le premier ambulancier
  bloqué qui a réessayé (A307, 57 refus avant) obtient son équipage.
- **Leçon** : le même jour, le refus d'Orders sur le conducteur disait déjà « La vacation s'est
  terminée … à 18:00 » — le signe était dans le journal une heure avant les appels des utilisateurs.

## 2026-09-13 — Vector présente son jeton de service à Orders (C2, sortant)

*En production — `bc105c1` (`main`), rechargé à 18:59:53, vérifié par sourcelink, `web.config` et
journal.*

- **Client Keycloak `erp-vector-api`** créé dans le realm `delesse` (d'abord sous `us-vector-api`,
  renommé pour suivre la convention `erp-<module>-api`) ; secret posé dans le `web.config`.
- **Constaté** : « Jeton de service obtenu pour erp-vector-api » trois secondes après le démarrage ;
  les appels à Orders passent avec le jeton (Orders ne le vérifie pas encore). Jetons de 5 minutes,
  renouvelés une minute avant l'échéance.
- **Le client de la facturation n'avait jamais existé** : `us-facturation` n'était qu'un nom de
  configuration de BillingGateway, où Keycloak est désactivé. Vector attend désormais
  **`erp-billinggateway-api`** (`Keycloak:ServiceAzp`).
- ⚠️ **Deux accrocs pendant la mise en place, à retenir** : une première publication faite avant le
  commit (`.pdb` à `f5b2996`, republiée depuis `main`) ; deux variables du `web.config` écrites
  **sans leur chevron ouvrant**, donc ignorées sans erreur par IIS — le compte de service ne
  s'activait pas et rien ne le signalait.
- Guide : [`docs/deploiement/keycloak-compte-service-vector.md`](docs/deploiement/keycloak-compte-service-vector.md).

## 2026-09-13 — Vector accepte le jeton de la facturation et sait présenter le sien (C2, partie Vector)

*En production — `8ace89f` (`main`), rechargé à 18:12:59, vérifié par sourcelink, configuration
déployée et journal.*

- **Entrant** — l'authentification accepte les jetons des modules déclarés (`Keycloak:ServiceAzp` =
  `us-facturation`), et la **politique de repli exige désormais l'azp mobile** : un jeton de service
  n'ouvre aucune route du terrain. `ClKeycloakCallers.ServiceOrMobilePolicy` est enregistrée pour la
  fermeture future des quatre routes anonymes, qui restent ouvertes.
- **Sortant** — `ServiceAccountTokenHandler` sur les deux clients Orders.Api : jeton
  `client_credentials` dès que `OrdersApi:ServiceAccount` est renseigné. **Déployé inerte** (section
  vide). Un realm indisponible ne bloque pas l'appel.
- **Constaté après publication** : jetons mobiles validés (`azp=us-ambulance`) et **admis par la
  nouvelle politique** — les requêtes atteignent le sélecteur, 0 jeton rejeté, 0 réponse 403, 0
  erreur. Au passage, le message C3 hors fenêtre est vu en direct : « Votre service est clôturé… ».
- 20 tests ; 137 verts.
- Reste hors Vector : client Keycloak de Vector, jeton de BillingGateway, puis fermeture (devplan C2).

## 2026-09-13 — Le sélecteur dit quoi faire, la file de projection cesse de s'acharner (C3, G9)

*En production — `8193fbf` (`main`), publié à 16:54, vérifié par sourcelink, chaînes des DLL et
journal du redémarrage.*

- **C3 — le 404 du sélecteur dit quoi faire.** Équipage pas encore composé : « Votre équipage n'est
  pas encore composé par la régulation. Réessayez dans quelques minutes ; si rien ne change, appelez
  la régulation. » Équipage composé mais hors fenêtre — ce 404 partait **sans corps** : il annonce
  l'heure d'ouverture de l'accès (« Votre service commence à 14:00 : vos missions seront accessibles à
  partir de 13:30 »), la clôture, ou la vacation expirée. Motif calculé par
  `ClCrew.UnselectableReasonAt`, seule source de la règle. Même code 404, texte seul (D14).
  **Constaté** : nouveaux textes présents dans les DLL, ancien absent.
- **G9 — une mission inconnue d'Orders est abandonnée.** `ProjectOperationalAsync` distingue le 404
  (`EnOperationalProjectionOutcome.MissionNotFound`) d'une panne ; le worker retire l'entrée avec un
  `WARN`, les pannes gardent le backoff sans plafond. **Constaté** : au redémarrage (16:54:51), la
  mission `745c9f76` est abandonnée deux secondes plus tard, **après 55 450 tentatives** ; 0 relance
  et 0 erreur ensuite.
- 117 tests verts (106 + 8 sélecteur + 3 outbox).

## 2026-09-13 — Les journaux de production reprennent (G9)

*Constaté à 16:22 : `logs/usvector-api-2026-09-13.log`, première ligne à 16:18:01.*

- NLog et stdout s'étaient arrêtés ensemble le 24/08 vers 02:17 : trois semaines sans aucune trace
  en production, alors que l'API servait normalement.
- Rétablis côté serveur, hors dépôt ; la cause n'est pas documentée ici.
- Premier constat qu'ils permettent : la file de projection opérationnelle relance sans fin une
  mission qu'Orders ne connaît plus (404, tentative n° 55 414) — ouvert au devplan.

## 2026-09-13 — Le 404 du « second membre » est diagnostiqué : ce n'est pas un défaut (C3)

*Analyse — aucun code livré. Journaux de production du 04/07 au 24/08, rejoués contre Orders.Api.*

- **Le symptôme était réel** : 1 344 réponses 404 « sans équipage actif » au sélecteur, sur 119
  couples (personnel, jour), jusqu'à 184 par jour.
- **Classement de chaque 404** contre l'état de l'équipage chez Orders :

  | Cause | Couples | 404 |
  |---|---|---|
  | Aucun équipage ce jour-là (1 seul réussit ensuite) | 90 | 982 |
  | **Équipage composé après la tentative** — médiane 23 min, 9 cas > 1 h, 21 équipages | 24 | 340 |
  | Membre remplacé ou retiré de l'équipage | 4 | 21 |
  | Échec isolé, réussite 6 s plus tard | 1 | 1 |

- **Les deux causes suspectées sont écartées** sur les 122 équipages à plusieurs membres des 7
  derniers jours : 0 membre dont `crews?personnelId=` ne rend pas l'équipage (cause 1), 0 membre actif
  absent de `members` (cause 2).
- **Piège de lecture évité** : `joinedAt` est en **UTC** chez Orders, les journaux Vector et les
  vacations en heure de Paris. Sans conversion, 18 cas paraissaient inexpliqués ; convertis, le membre
  est ajouté **entre le dernier échec et la réussite**. Jusqu'au 03/09, l'adhésion fondatrice était
  datée de l'instant de composition (Order `88aa425`) — c'est ce qui rend la mesure possible sur cette
  période, et plus au-delà.
- Côté identité : 8 membres d'équipage sur 244 n'ont pas de compte Keycloak rattaché (403, pas 404).
- Suite à décider : message terrain et organisation de la régulation (devplan C3).

## 2026-09-13 — Routes mortes retirées du contrat, magasin d'attributs supprimé (A5, A6)

*En production — `0122b7a` (`main`), publié à 15:43, vérifié par sourcelink, Swagger et paquets
terrain dans la foulée.*

- **A5** — `ContactController`, `MecanicLogController`, `AnalyzeController` (toutes leurs routes
  répondaient 500) et `ReferenceDataController` (listes codées en dur de la main courante) retirés,
  avec les stubs, trois ports, les cas d'usage `MechanicLog` et leurs orphelins. `MOB-14` abandonné.
  **Constaté** : aucune de ces routes dans le Swagger de production (29 routes servies).
- Les mappings de la timeline, rangés par erreur dans le module mécanique, déplacés dans
  `Time/Model/ModJobTimeMapping.vb` — la compilation l'a révélé.
- **A6** — lecture de l'overlay d'attributs retirée (lecteur, repository, ports, six entités EF).
  **Constaté** sur 5 missions récentes : `field-data` répond 200 avec `Attributes: null`, timeline,
  signature, mutuelle et documents présents.
- **`MOB_008` joué** en production le 2026-09-13 : les six tables de l'overlay sont supprimées,
  confirmé en base par l'exploitant (le poste de dev n'y a pas accès, compte `ErpAccount` refusé).
- 106 tests verts (112 − 7 tests de l'overlay + 1). Note au dev web écrite :
  [`note_web_alexandre_routes_retirees.md`](note_web_alexandre_routes_retirees.md).

## 2026-09-13 — L'écran affiche les refus du contexte de mission (A1)

*Confirmé par le dev web, en réponse aux notes du 25 et du 26/08.*

- Depuis la bascule du 25/08, l'API peut refuser : choix du type de mission (`409` — type imposé par
  la régulation ou inadapté à la commande), enregistrement du questionnaire (`409` — champ verrouillé ;
  `400` — valeur invalide, n° de sécurité sociale à clé fausse, date future).
- **L'UI affiche l'information de refus** : l'ambulancier sait que sa saisie n'est pas enregistrée.
  Ferme le dernier risque de la bascule côté écran ; restent les améliorations d'A3.

## 2026-08-27 — Le contexte n'a plus qu'un chemin, et la prod redevient traçable

- **Retrait du second chemin du contexte** (A1/A2, `4e6de8b`, publié à 00:21) : drapeaux, cas
  d'usage du catalogue local, résolveur de type, aiguillage des contrôleurs. Trois contrôles : section
  `ContextOrder` absente de la configuration déployée, API répondante, **paquet terrain identique à
  l'octet près**.
- Le paquet terrain lit le même magasin que l'overlay (`c0b5b8f`) : premier verrou d'A6 levé.
- *(Order)* Les messages de verrou nomment la facturation, plus AidesNSoft : **8 missions sur 25**
  servent le nouveau texte, **0** mentionne encore AidesNSoft.
- **Prod reproductible depuis git** : Vector à `4e6de8b`, Orders.Api à `28bb9b5`, vérifiés par
  sourcelink.
- Relevé de traçabilité des saisies jusqu'aux 91 colonnes de facturation
  ([`TRACABILITE_SAISIES_VECTOR_EXPORT.md`](TRACABILITE_SAISIES_VECTOR_EXPORT.md), fusion `86b5b28`).

## 2026-08-26 — Nettoyage de la bascule, et mesures en production

- **OC-9** (`a5fd995`) : retrait des quatre ports sans consommateur, de leurs enregistrements, de
  deux présentateurs, un cas d'usage, trois DTO et d'adaptateurs jamais instanciés. **126 tests
  verts**, aucune route touchée.
- **A3 vérifié côté API** (`542f953`) : le verrou par champ traverse en recopie, aucune règle rejouée.
  Mesuré sur 40 missions : `DDN` renseignée 34/40 et **verrouillée dans les 34 cas** ; `NIR` 0/40.
- **A6 tranché : abandon pur** des 2 132 valeurs d'attributs de test ; `MOB_008` écrit, non joué.
- Note au dev web demandant la confirmation qui ferme la bascule (`14b4952`) ; le front sert les
  bons attributs, le risque des ids en dur tombe (`3dbb3cf`).

## 2026-08-25 — Le référentiel de contexte bascule vers Order

- **En service en production** (`3fa5598` OC-3b/OC-5/OC-7, armement versionné `0c3ad34`) : sélection
  du type (A1) et questionnaire d'attributs (A2) viennent d'Order ; le paquet terrain lit le magasin
  Vector sans appel réseau et devient plus rapide qu'avant (A4).
- *(Order)* **OC-28 déployé** : surchargeabilité portée par le type, **0 mission verrouillée sur 15**
  (contre 20 sur 25 la veille). Script `063` joué sur `109` et `118`.
- **API fermée par défaut** (`f3238bc`) : jusque-là seuls cinq endpoints étaient protégés et une date
  de naissance de patient a été relevée servie sans authentification. Quatre sorties restent, figées
  par `AnonymousSurfaceTests`.
- Options des attributs de type liste lues comme un tableau (`bc220f1`).
- Note au dev web : les DTO du contexte tels qu'ils répondent (`4548d3d`).

## 2026-08-24 — Lecture et écriture du contexte chez Order

- OC-1/OC-2 : lecture et écriture HTTP du contexte (`92e0aef`, `e5fb3a2`) ; **les trois refus
  vérifiés contre l'API de production** (`780c38e`).
- OC-3a : verrou et provenance lisibles par le terrain (`554f655`, `d07c273`).
- OC-4 : relais de la sélection terrain vers Order, tranche inerte (`a2c63f9`) ; `STANDARD`
  s'enregistre en CPAM, libellé `ART80` corrigé — `MOB_007` (`9f1e73e`).
- Devplan unique, décision **D14** (`cf747e5`). 13 tables `MOB_*` vérifiées sur `BD_ERP_MOBILE_APP`.

## 2026-08-07 — Spec d'architecture DMZ et réorganisation de la solution (`e2bd655`)

## 2026-08-06 — Le paquet terrain est réellement consommé

- Mesuré par la facturation : **284 missions acquises**.
- Divergence de schéma entre prod et dev (`MOB_004/005/006`) résolue pour la production — la cause
  demeure (devplan G4).

## 2026-08-02 — Accès anticipé et authentification réellement validée en production

- **CREW-1** : missions visibles 30 min avant la prise de service (`ba5a6ec`).
- **Jetons Keycloak réellement validés en prod** ; **KC-1** : Keycloak piloté par la configuration,
  garde-fou de démarrage (`5ec3bb7`).
- **DEP-2** : `appsettings.json` aligné sur la prod, écarts dans les overlays (`e5b9b4d`).
- Cible PROD pour `deploy.ps1` et garde-fous de publication (`0e76a70`).
- Décision métier : les vacations de nuit ne sont pas concernées par CREW-2.

## 2026-07-12 → 2026-07-15 — Lieux, identité, diagnostic

- **DET-1** : service médical en champ dédié (`db949e9`) ; **DET-2** : service du lieu (`54da11f`) ;
  lignes d'affichage harmonisées et journal des lieux non structurés — DET-3 (`81bb134`).
- Joblist : sens de transport exposé (`c891469`).
- Diagnostic de la chaîne d'identité, endpoint et page (`da5b122`).
- Sélecteur d'équipage limité aux équipages actifs (`b6d5dec`) ; claim `per_id` écarté (`d198b43`).
- Validation de l'`azp` au lieu de l'`aud` (`44bdfec`) ; Keycloak activé par appsettings
  d'environnement (`83c0908`) ; guide Keycloak & IIS (`6f3d6d8`).
- Jalon « disponible » en minuscule (`d912145`).
- Spec de présence des utilisateurs connectés, sans code (`44aff1f`).

## 2026-07-06 → 2026-07-08 — Joblist par équipage, missions engagées

- Joblist filtrée par équipage seul (`dfd2210`) ; **missions engagées uniquement** (`b6c9b3f`), test
  de non-régression (`8f55893`).
- Claims JWT bruts conservés (`0b2c722`) ; équipages actifs filtrés sur l'appartenance réelle
  (`18b04b4`) ; multi-équipage et cache d'identité (`5ac0d6b`).
- Signature en `POST`, upsert idempotent (`f913a27`).
- CaSoft.Framework 2.3.5 (`87fdcc9`).

## 2026-07-05 — Result pattern complet, synchro régulation garantie

- **Result pattern, vague 1** : 31 cas d'usage migrés par lots, parité HTTP vérifiée
  (`2b2743a` → `958bf35`). **Vague 2** : straggler `ClSetDriverUseCase` migré, `ClUseCaseHandler`,
  services et échafaudage legacy supprimés (`b29e974`, `49c3cf3`). *Constaté dans le code le
  2026-09-13 : plus aucune référence à `ClUseCaseBase` ni aux présentateurs.*
- **Outbox de projection opérationnelle** + worker, debounce 5 s et retry (`f2bd5fe`).
- **Retour arrière d'un jalon** (`12057f8`), contrat `null = effacé` côté Orders ; `api/time`
  cumulatif (`e1cbee5`).
- JobDetail enrichi : horaire formaté, mode secondaire, lieux structurés (`df5c12b`) ; joblist masque
  les missions clôturées (`62d9fe3`) ; alias `IsAck` (`52fbd97`) ; « Mission vue » retenue (`5816534`).
- **MOB-11** : feature conducteur, `SelectedDriver` jamais nul (`c8b131c`, `f27c924`, `4bde58d`).
- Login Keycloak de bout en bout ; vérification bin↔UNC de `deploy.ps1` fiabilisée (`61db015`).

## 2026-07-04 — Premiers correctifs de production

- 500 sur la joblist : slash final d'`OrdersApi:BaseUrl`, 404 équipage toléré (`6028ab9`).
- Publication portable, sans RID `win-x64` : SqlClient de nouveau fonctionnel sur IIS (`aaffa14`).
- MOB-13 : `PER_ID` résolu par l'endpoint `by-keycloak` (`e5a7d79`) ; script de déploiement (`bb383be`).

## 2026-06-25 — Authentification MOB-4a et uploads multipart (`b8b5d90`)

## 2026-06-22 → 2026-06-23 — Transfert terrain → facturation

- **TRF-1 → TRF-11** (`cc8db5e`) : transférabilité automatique, file des non-transférées, paquet
  `field-data`, gel au transfert. 24 tests ; schéma Orders `034` appliqué le 22/06.

## 2026-06-15 — Carte mutuelle, découplage HTTP

- **Carte mutuelle P1** (capture, stockage) et **P2** (restitution, saisie manuelle) — `9f85d4f`,
  `a7ab043`, 16 tests.
- **Découplage Vector ↔ Orders (4a)** : lecture ERP via Orders.Api en HTTP (`0f90bf2`) ; isolation de
  build prouvée.
- Overlay attributs : validation et tests (`d34696a`, `7421d71`) ; profil de publication (`b3b9fe2`).

## 2026-06-14 — Socle mobile

- Login Keycloak → missions de l'équipage (`2f69b8d`) ; attributs de mission en overlay (`9fabc67`).
- Rebrand `CaSoft.Erp.Mobile.*` → `CaSoft.Erp.USVector.*` (`549cdbb`) ; `web.config` géré
  manuellement, secrets hors git (`ca78bb9`) ; Swagger compatible sous-chemin IIS (`592780b`).

---

# 3. Repères de validation

| Lot | Preuve |
|---|---|
| Socle mobile | 25 routes du contrat legacy exposées ; joblist, détail, signature, timeline validés sur missions réelles |
| Authentification | login Keycloak bout-en-bout 2026-07-05 ; jetons réellement validés en prod et accès anticipé 30 min depuis 2026-08-02 |
| Découplage HTTP | isolation de build prouvée (reconstruction sans Orders) |
| Result pattern | 31 cas d'usage migrés, parité HTTP vérifiée ; legacy retiré |
| Terrain (attributs 11, mutuelle 16, lieux 12) | validés en base 2026-06-14 / 06-15 / 07-14 |
| Transfert (Orders + Vector) | 24 tests ; schéma appliqué 2026-06-22 |
| Consommation réelle du paquet terrain | 2026-08-06 : 284 missions acquises par la facturation |
| Contexte de mission | 58 tests ; les trois refus constatés en production le 2026-08-24 |
| Carte mutuelle par mission | 8 tests (2026-09-13) |
| Suite complète | 126 verts (2026-08-25) → **112 verts (2026-09-13)**, après le retrait OC-9 et l'ajout mutuelle |

---

# 4. Décisions appliquées — ne pas les rejouer

## 4.1 Décisions structurantes

| # | Décision |
|---|---|
| D1 | **Contrat mobile préservé** : on remplace l'implémentation des repositories, pas les routes ni les DTO. Les ajouts sont additifs. |
| D2 | **Séparation officiel ↔ terrain** : la donnée terrain est déclarative et non fiable par construction ; elle n'écrase jamais l'ERP. |
| D3 | **Accès aux autres modules par API HTTP uniquement** (posture DMZ), base propre à Vector sur le LAN derrière firewall. Le durcissement événementiel est une option V2. |
| D4 | **Identités de référence en Guid** (équipage / véhicule / personnel), alignées sur l'ERP. |
| D5 | **Grain de transfert = la mission**, avec le rattachement à sa commande conservé dans le paquet. |
| D6 | **Transfert automatique** des missions clôturées ; l'aval contrôle via la file des non-transférées. |
| D7 | **Gel au transfert**, pas à la clôture. |
| D8 | **L'aval tire les octets** (signature, photos, documents) depuis Vector.Api — pas de stockage partagé. |
| D9 | **Anomalies non bloquantes** : transférées comme donnée, arbitrées en facturation. |
| D10 | **Temps réel régulateur = persistance + polling** au MVP ; le push est V2. |
| D11 | **`Closed` reste la main du régulateur** : le mobile n'écrit jamais la clôture administrative. |
| D12 | **Photos et documents en base** en V1 ; sortie vers un stockage fichier planifiée V2. |
| D13 | **Le claim `per_id` dans le jeton est écarté** : c'est le cache HTTP qui s'invalide, pas le jeton. |
| **D14** | **On code neutre ou additif — jamais de rupture du contrat consommé par l'appli web en production.** L'app web n'est pas déployée en même temps que l'API : on ajoute à côté plutôt qu'on ne remplace ; les alias ne se retirent que sur confirmation du front ; une évolution non additive se coordonne avant livraison (`note_web_alexandre_*.md`). |
| D15 | **La surchargeabilité d'un type de mission est une propriété du catalogue**, pas une décision commande par commande. |

## 4.2 Décisions de la bascule du contexte

- **A1** — Le lien type ↔ attributs est refait par le code à chaque lecture, pas par une écriture en
  double. Une panne d'Orders rend une **liste vide**, jamais le catalogue local. L'identifiant reçu
  est vérifié contre les types réellement proposés.
- **A1/A2** — Le second chemin a été retiré **sans attendre la confirmation du dev web** : le chemin
  de désarmement ne posait jamais `IsReadOnly` et ne validait aucune valeur ; le rebrancher aurait
  rouvert 34 dates de naissance verrouillées sur 40. Conséquence assumée : un incident se corrige par
  un redéploiement, plus par une clé de configuration.
- **A2** — Le formulaire est renvoyé entier, sans trier les champs verrouillés (Order ignore une
  valeur reposée à l'identique). Aucune règle métier n'est rejouée côté Vector.
- **A4** — Le paquet terrain ne proxifie pas les attributs vers Order : la facturation les lit déjà
  chez Orders et les fait primer ; un troisième chemin coûterait un appel par mission.
- **A6** — Abandon pur de l'historique d'attributs antérieur au 2026-08-25 (données de test, 26/08) ;
  confirmé le 2026-09-13 : la facturation n'en a plus besoin.
- **A5** *(2026-09-13)* — Les routes adossées à des stubs (recherche et modification de bénéficiaire,
  main courante mécanicien et ses analyses) **sortent du contrat mobile** plutôt que d'être
  implémentées. Elles répondaient 500 ; `MOB-14` est abandonné sous sa forme mobile.
- **C1** *(2026-09-13)* — **Vector n'héberge ni ne duplique l'écran de rattachement des comptes.**
  L'écran existe dans Employee et écrit dans le carnet d'Identity ; Vector attend la **bascule
  d'Orders** sur ce carnet (Identity itération 7, Orders itération 24), elle-même bloquée par la RH
  (273 personnels actifs d'Orders sans fiche Employee). Ni lecture de repli du carnet depuis Vector
  (lien `EMP_EMPLOYEE.PersonnelId` vide, 273 absents non couverts), ni écran côté Orders (troisième
  chemin d'écriture). **Consigne transitoire** : rattacher aussi l'ambulancier côté Orders.
- **A0** *(2026-09-13)* — Quand le terrain remplace un type de mission proposé par la régulation, la
  proposition est **perdue** (écrasement en place, aucune trace). **Perte assumée** : aucun audit à
  construire, ni côté Vector ni côté Order.

---

# 5. Où vit quoi

| Donnée | Emplacement | Autorité |
|---|---|---|
| Missions, commandes, équipages, véhicules, personnel, bénéficiaires | Orders (`BD_ERP_SANITAIRE_DEV`), lu par API HTTP | **Orders** |
| Jalons terrain détaillés, signature, anomalies, documents, carte mutuelle, file de projection | Base Vector (`BD_ERP_MOBILE_APP`, tables `MOB_*`) | **Vector** |
| Avancement opérationnel projeté + statut de transfert | Orders (`ORD_MISSION_OPERATIONAL`, `MIS_TRANSFER_STATUS` / `MIS_TRANSFERRED_AT` / `MIS_BILLED_AT`) | **Orders** (Vector pousse, Certification écrit le statut) |
| Type de mission (contexte) et attributs de facturation | Orders (`ORD_ORDER_CONTEXT*`) — basculé le 2026-08-25 | **Orders** |
| Rattachement compte Keycloak ↔ ambulancier | `PER_KEYCLOAK_MAP` (Orders) → cible : module Identity | **Identity** |

## 5.1 Migrations SQL appliquées

| Base | Script | Contenu | État |
|---|---|---|---|
| Orders | `026_AddKeycloakMap.sql` | `PER_KEYCLOAK_MAP` | appliqué |
| Orders | `034_AddMissionOperationalAndTransfer.sql` | `ORD_MISSION_OPERATIONAL` + `MIS_TRANSFER_STATUS` | appliqué 2026-06-22 *(référencé `027` dans l'historique)* |
| Orders | `063` | `OCT_FIELD_OVERRIDABLE` | joué 2026-08-25 sur `109` et `118` — *ne pas rejouer `062`* |
| Vector | `MOB_001` → `MOB_006` | session/timeline/signature · catalogue contrat + overlay · carte mutuelle · anomalies · documents · file de projection | appliqués (13 tables vérifiées le 2026-08-24) |
| Vector | `MOB_007` | libellé `ART80` → « Article 80 » | appliqué |
| Vector | `MOB_008` | suppression de l'overlay d'attributs (`MOB_CONTRACT_*`, `MOB_JOB_CONTRACT`, `MOB_JOB_ATTRIBUTE_VALUE`) | joué le 2026-09-13, confirmé en base |

---

# 6. Configuration & déploiement

*Procédure complète :* [`docs/deploiement/configuration-keycloak-iis.md`](docs/deploiement/configuration-keycloak-iis.md).

**Clés lues** : `ConnectionStrings:MobileDb` (`OrdersDb` inutilisé) · `OrdersApi:BaseUrl` ·
`AddressApi:BaseUrl` · `Keycloak:{Enabled, Authority, Audience, DisableValidation,
RequireHttpsMetadata, AdminClientId, AdminClientSecret}` · `Diagnostics:Enabled` ·
`MobileIdentityCache:{PersonnelMinutes=30, ActiveCrewsMinutes=15}` · secrets GpsGate/Sirus
`__SET_VIA_ENV__`.

## 6.1 Les quatre pièges avérés

| Piège | Conséquence | Règle |
|---|---|---|
| `OrdersApi:BaseUrl` **sans slash final** | dernier segment perdu → 500 sur la joblist (juillet 2026) | terminer par `/`, PathBase IIS inclus |
| Publication **RID `win-x64`** | SqlClient « PlatformNotSupported » → SQL injoignable | publier **portable** |
| `Keycloak:DisableValidation=true` hors dev | jetons décodés sans vérification | **`false` en prod** ; `Authority` vide ou placeholder empêche le démarrage |
| `Diagnostics:Enabled=true` en prod | expose la résolution d'identité et les comptes Keycloak | dev/staging seulement ; ailleurs `/api/diag*` rend 404 |

> `Audience` sert aussi d'**`azp` attendu** : l'audience n'est volontairement pas validée (Keycloak
> émet `aud=account`) ; signature, issuer et expiration restent validés.

## 6.2 Trois couches de configuration

**`web.config`** (serveur, manuel, hors git) porte l'environnement et les secrets et survit à toute
publication · **`appsettings.json`** est publié, donc porte la **valeur de la prod** ·
**`appsettings.{Environment}.json`** est publié **et** prioritaire : il décrit les **déviations**.
Jamais une valeur éditée à la main sur le serveur.

## 6.3 Déploiement

`.\deploy.ps1 dev` · `.\deploy.ps1 prod` (confirmation à taper ; `-Force` en non-interactif) →
`\\192.168.1.112\{dev_api,prod_api}\Vector.Api`. Pré-vol du partage, vérification bin↔UNC et de
`appsettings.json`. `app_offline.htm` → **courte coupure de l'API** à chaque publication.
Prérequis : `net use \\192.168.1.112\prod_api /user:192.168.1.112\DeployApi *`.

**Quel commit tourne en production** (sourcelink du `.pdb`) :

```
grep -a -o "esv83/Erp.Vector/[0-9a-f]\{40\}" \\192.168.1.112\prod_api\Vector.Api\CaSoft.Erp.USVector.Api.pdb
```

⚠️ Il donne le `HEAD` au moment de la construction, **pas l'état de l'arbre** : une publication
depuis un arbre modifié annonce un commit qui ne contient pas le code servi (§8, 13/09).

---

# 7. Pistes retirées — obsolètes ou abandonnées

| Ce qui a disparu | Motif |
|---|---|
| **Accès in-process aux projets Orders** et **schéma DMZ strict comme cible V1** | Vector consomme `Orders.Api` en HTTP à travers un firewall : isolation obtenue, le durcissement événementiel devient V2. |
| **Table `MOB_CREW_MAP`** (équipage `int` ↔ `Guid`) | Toutes les identités de référence passent en Guid. |
| **Accusé de réception distinct** (`MST_ACK_AT`, `ClAckJobUseCase`) | Remplacé par « Mission vue ». `IsAck` survit comme alias. |
| **Login déclaratif `api/login`** et jeton `MOB_SESSION` | Remplacés par Keycloak. |
| **Claim `per_id` dans le jeton** | Écarté le 2026-07-12 : non invalidable sous turnover. |
| **Table `MOB_KM`** telle que planifiée | Le kilométrage est équipage/véhicule-scoped. |
| **Catalogue autonome de contrats** (`MOB_CONTRACT_*`), son seed et la purge des valeurs orphelines | Le référentiel est chez Order. |
| **Interfaces legacy `IContractTypeRepository` / `IAttributsRepository` / `IInvoicingRepository`** | Retirées le 2026-08-26 (OC-9). |
| **`FetchInstructionList` / `AckInstruction` / `GetCrewIdList(date)` / `GetCrewDriver(vehicleId)`** | Aucun équivalent ERP ; laissés en `NotImplementedException`. |
| **Blocages « migrations à exécuter en db_owner »** et « projection du statut de fin différée » | Résolus. |
| **Documents « source PDF ERP »** | Documents et photos stockés et servis par Vector. |
| **Lot Certification TRF-12..15 tel que planifié** | Partage Certification / facturation, livré. |
| **Arbitrages** 4a/4b · 2a/2b/2c · « défaut = premier contexte actif » | **4a**, **2b**, **supprimé**. |
| **A4 tel que planifié** (attributs du paquet lus chez Order) | Mauvaise cible, cf. §4.2. |
| **Écran Siège `UcEmployeeKeycloakAccount`** comme hôte du mapping Keycloak | Siège archivé ; la correspondance passe à Identity. |
| **Choisir l'hôte d'un écran de rattachement** (ex-C1 : Identity ou endpoints Orders) | Sans objet le 2026-09-13 : l'écran existe dans Employee et écrit dans le carnet d'Identity. Vector attend la bascule d'Orders sur ce carnet (§4.2). |
| **Historique de portage legacy** et dettes C4, C5, C6, KC-1, DEP-2, DET-1, DET-2, DET-3 | Résolues ; l'information vit dans `git log`. |
| **Result pattern, vague 2** (G1 du devplan) | Livrée le 2026-07-05 ; le plan ne l'avait pas enregistré. |
| **`MOB-14` — logs mécaniques et analyses** (tables `MOB_MECANIQUE_*`, référentiels, repositories) | Abandonné le 2026-09-13 avec A5 : les routes sortent du contrat mobile au lieu d'être implémentées. |
| **Trace de la proposition de type écrasée par le terrain** (A0) | Perte assumée le 2026-09-13 (§4.2). |
| **Ticket Orders « chaîne équipage du 2ᵉ membre »** (ex-B3 : jointure `crews?personnelId=` ou `Members` incomplet) | Écarté le 2026-09-13 : aucune des deux causes n'existe dans les données ; le 404 vient d'équipages composés après la tentative (C3). |

---

# 8. Incidents de production

| Date | Incident | Ce qui l'a révélé | Suite |
|---|---|---|---|
| **2026-09-13** *(18:00 → 20:04)* | **Ambulanciers en service exclus de l'application** (« Votre service est clôturé ») : 9 personnels, 125 refus en une heure. Orders pose désormais une fin théorique à chaque vacation (`7984ec0`), que Vector lisait comme une clôture | appels des utilisateurs ; le refus du conducteur disait déjà « vacation terminée à 18:00 » | clôture lue sur le statut (`c5eedca`), publié à 20:04 |
| **2026-09-13** | **Publication en production depuis un arbre non commité** : binaires à 14:40, commit `e942967` à 14:42. Le `.pdb` annonce `86b5b28`, qui ne contient pas la capture par mission. | comparaison des horodatages et des chaînes des DLL au moment de rédiger ce document | republié depuis `main` à 15:05 (`a6c2aba`), vérifié par sourcelink ; blocage des publications non commitées au plan (G8) |
| 2026-08-24 → 2026-09-13 | **Journaux de production muets** : NLog et stdout arrêtés le 24/08 vers 02:17, l'API servant normalement | en cherchant les 404 du sélecteur pour C3 | rétablis le 13/09 à 16:18, côté serveur |
| 2026-08-25 | Binaire de production publié depuis un arbre non commité | par hasard, en cherchant l'origine d'un champ | reproductible depuis git le 27/08 |
| 2026-08-25 | Drapeaux de bascule armés sur le serveur, désarmés dans le fichier versionné | lecture du fichier déployé | armement versionné (`0c3ad34`), puis drapeaux retirés |
| 2026-08-25 | Correctif joué sur la mauvaise base Orders (`115` au lieu de `109`), sans erreur | une erreur SQL ultérieure | devplan G8 |
| 2026-08-06 | Schémas prod et dev divergents en sens inverse → 500 opaque, journée sans données terrain | constat de la facturation | résolu pour la prod ; devplan G4 |
| juillet 2026 | `OrdersApi:BaseUrl` sans slash final → 500 sur la joblist | panne | règle §6.1 |
| 2026-07-04 | Publication RID `win-x64` → SQL injoignable | panne | publication portable |

---

**Fin du document**
