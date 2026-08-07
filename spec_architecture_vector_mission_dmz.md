# Spécification d’architecture — Module Vector en DMZ et intégrations Mission / Order / Crew / Certification / Billing

## 1. Objectif du document

Ce document formalise l’architecture cible du module **Vector**, destiné aux ambulanciers terrain, en tenant compte de la contrainte suivante :

> **Vector sera hébergé en DMZ, car il doit être accessible depuis les applications mobiles. Il ne doit donc pas avoir accès aux bases de données des autres modules internes.**

Le module Vector doit permettre :

- la consultation des missions attribuées aux équipages ;
- la saisie des statuts terrain ;
- la saisie de données administratives terrain ;
- l’indication de la récupération ou de l’absence de prescription médicale ;
- la photographie de la carte mutuelle ;
- la transmission des informations utiles à la régulation et aux équipages retour ;
- la conservation des données terrain pour audit ;
- le traitement quasi temps réel via intégration sécurisée.

Principe fondamental :

> Les données remontées par Vector sont utiles, historisées et exploitables opérationnellement, mais elles restent non fiables par défaut. Elles peuvent être corrigées, ignorées ou comparées aux données certifiées.

---

## 2. Nouvelle contrainte DMZ

### 2.1 Principe réseau

Vector est exposé aux applications mobiles. Il doit donc être isolé du réseau interne.

Architecture réseau retenue :

```text
Internet / Mobiles
   ↓ HTTPS
DMZ
   ├── Vector API
   ├── VectorDb
   ├── stockage temporaire documents Vector
   └── Vector Outbox

Réseau interne / LAN
   ├── OrderDb
   ├── CrewDb
   ├── CertificationDb
   ├── BillingDb
   ├── IntegrationDb
   ├── RabbitMQ interne
   ├── Workers internes
   └── SignalR régulation
```

### 2.2 Règle de sécurité principale

```text
Vector ne lit jamais directement les bases internes.
Vector n’écrit jamais directement dans les bases internes.
```

Bases interdites à Vector :

```text
OrderDb
CrewDb
CertificationDb
BillingDb
IntegrationDb interne
```

Vector accède uniquement à :

```text
VectorDb
Vector Outbox
stockage documentaire temporaire DMZ
services techniques strictement nécessaires
```

---

## 3. Architecture générale retenue

L’architecture globale conserve le principe d’une base par module, avec l’exception déjà validée :

```text
OrderDb contient Order + Mission.
```

Bases principales :

```text
VectorDb        // DMZ
OrderDb         // LAN
CrewDb          // LAN
CertificationDb // LAN
BillingDb       // LAN
IntegrationDb   // LAN
```

Communication inter-modules :

- événements d’intégration ;
- RabbitMQ interne ;
- pattern Outbox / Inbox ;
- projections locales ;
- worker de pont DMZ / LAN ;
- SignalR pour la régulation et les équipages concernés.

---

## 4. Découpage des responsabilités

## 4.1 VectorDb — DMZ

`VectorDb` est la base locale du module Vector.

Elle contient uniquement les données nécessaires au fonctionnement mobile :

- missions attribuées sous forme de projection ;
- données minimales nécessaires à l’affichage terrain ;
- statuts saisis par les ambulanciers ;
- données administratives terrain saisies ;
- indicateurs prescription récupérée / prescription manquante ;
- références de documents temporaires ;
- Outbox locale Vector ;
- historique terrain Vector.

`VectorDb` ne contient pas la commande complète.

Elle contient une **copie projetée, filtrée et limitée** des missions utiles aux équipages.

### Tables recommandées dans VectorDb

```text
VectorMissionProjection
VectorMissionStatusHistory
VectorFieldAdministrativeData
VectorDocumentStaging
VectorOutboxMessage
VectorInboxMessage
```

---

## 4.2 OrderDb — LAN

`OrderDb` reste la source de vérité des commandes et la source opérationnelle des missions.

Elle contient :

- les commandes ;
- les missions ;
- l’affectation mission-équipage ;
- les statuts courants mission ;
- les horaires provisoires retenus ;
- les corrections régulation ;
- les projections utiles issues de Vector ;
- les données terrain utiles à l’exploitation, sans modification directe de la commande initiale.

`OrderDb` publie vers Vector uniquement les données nécessaires aux ambulanciers.

---

## 4.3 CrewDb — LAN

`CrewDb` est la source de vérité des équipages.

Elle contient :

- composition d’équipage ;
- véhicule affecté ;
- disponibilité courante.

L’affectation mission-équipage reste portée par `OrderDb`.

`CrewDb` ne reçoit pas directement les statuts Vector. Il reçoit des informations consolidées depuis l’intégration interne.

---

## 4.4 CertificationDb — LAN

`CertificationDb` est la source finale des horaires validés et des dossiers de certification verrouillés.

Elle reçoit les valeurs candidates via l’intégration interne, jamais directement depuis Vector.

---

## 4.5 BillingDb — LAN

`BillingDb` est dédié à la préparation et au calcul de facturation.

Il peut utiliser :

- `CertificationDb` ;
- `OrderDb` ;
- des données non certifiées.

Chaque donnée utilisée doit être tracée avec son niveau de fiabilité.

---

## 4.6 IntegrationDb — LAN

`IntegrationDb` centralise côté interne :

- messages inter-modules ;
- Inbox consommateurs ;
- erreurs d’intégration ;
- suivi de publication ;
- rejeu des messages ;
- supervision globale en V2.

---

## 5. Sources de vérité

| Domaine | Source de vérité |
|---|---|
| Commande | `OrderDb` |
| Mission opérationnelle | `OrderDb` |
| Équipage | `CrewDb` |
| Disponibilité équipage | `CrewDb` |
| Donnée terrain déclarée | `VectorDb` |
| Horaire opérationnel provisoire | `OrderDb` |
| Correction régulation | `OrderDb` |
| Horaire certifié | `CertificationDb` |
| Dossier de certification | `CertificationDb` |
| Facturation | `BillingDb` |

Règle générale :

```text
VectorDb = vérité terrain déclarative, non fiable
OrderDb = vérité opérationnelle de la régulation
CrewDb = vérité des équipages et disponibilités
CertificationDb = vérité certifiée
BillingDb = vérité de facturation
```

---

## 6. Flux interne vers Vector : publication des missions attribuées

Vector ne peut pas lire `OrderDb`. Il faut donc alimenter `VectorDb` avec des projections.

Flux recommandé :

```text
OrderDb
   ↓
Order / Mission Outbox
   ↓
RabbitMQ interne
   ↓
VectorPublicationWorker interne
   ↓ HTTPS sortant LAN → DMZ ou canal sécurisé dédié
Vector API DMZ
   ↓
VectorDb.VectorMissionProjection
   ↓
Mobile Vector
```

### 6.1 Données publiées vers Vector

La projection envoyée à Vector doit être limitée au strict nécessaire.

Exemple de projection :

```text
VectorMissionProjection
```

| Champ | Rôle |
|---|---|
| MissionId | Identifiant mission interne |
| OrderId | Identifiant commande si nécessaire |
| CrewId | Équipage affecté |
| MissionType | Aller, retour, AR, etc. |
| PickupAddressLabel | Adresse de prise en charge affichable |
| DestinationAddressLabel | Adresse destination affichable |
| ScheduledPickupTime | Heure prévue |
| TransportMode | Ambulance, VSL, etc. |
| PatientDisplayName | Nom affichable selon règle métier |
| SensitiveDataMaskingMode | Niveau de masquage |
| CanEditFieldData | Droit de saisie terrain |
| AccessUntilStatus | Clôturé |
| ProjectionVersion | Version projection |
| PublishedAt | Date publication |

### 6.2 Données à éviter dans la projection

Ne pas pousser inutilement :

```text
Historique complet commande
Données de facturation
Données de certification
Données RH équipage non utiles
Données patient non nécessaires
Données techniques internes
```

---

## 7. Flux Vector vers modules internes : remontées terrain

Vector ne modifie jamais `OrderDb` directement.

Flux recommandé :

```text
Mobile Vector
   ↓ HTTPS
Vector API DMZ
   ↓
VectorDb + VectorOutbox
   ↓
VectorBridgeWorker interne
   ↓ lecture contrôlée DMZ → LAN ou appel API sécurisé
RabbitMQ interne
   ↓
Inbox consommateurs internes
   ↓
OrderDb / CrewDb / CertificationDb
   ↓
SignalR
   ↓
Régulation / équipages concernés
```

### 7.1 Règle de sens des flux

Recommandation réseau :

```text
Le LAN peut initier des connexions contrôlées vers la DMZ.
La DMZ ne doit pas initier de connexion directe vers les bases du LAN.
```

Deux options techniques possibles :

| Option | Principe | Recommandation |
|---|---|---|
| Pull LAN → DMZ | Un worker interne lit les messages prêts dans VectorDb ou via Vector API | Recommandé |
| Push DMZ → LAN RabbitMQ | Vector publie directement vers RabbitMQ interne | Possible mais moins strict côté sécurité |

Option recommandée en V1 :

```text
Vector écrit dans son Outbox DMZ.
Un worker interne vient récupérer les messages et les publie dans RabbitMQ interne.
```

---

## 8. Pattern Outbox / Inbox adapté DMZ

### 8.1 Outbox Vector en DMZ

Vector doit écrire dans une Outbox locale dans la même transaction que l’action terrain.

```text
Transaction VectorDb :
- écriture action terrain
- écriture VectorOutboxMessage
```

Exemple :

```text
Ambulancier clique "Sur place"
↓
VectorMissionStatusHistory ajouté
↓
VectorOutboxMessage ajouté
```

### 8.2 Bridge interne

Un worker interne, hébergé côté LAN, récupère les messages Vector.

Rôle :

- lire les messages DMZ non traités ;
- vérifier signature / origine / version ;
- publier dans RabbitMQ interne ;
- marquer le message comme récupéré ou publié ;
- gérer les erreurs et rejeux.

Nom recommandé :

```text
VectorDmzBridgeWorker
```

### 8.3 Inbox consommateurs

Chaque module interne consommateur doit avoir une Inbox :

```text
OrderMissionInbox
CrewInbox
CertificationInbox
BillingInbox si nécessaire
```

Objectifs :

- idempotence ;
- rejeu ;
- traçabilité ;
- protection contre les doublons.

---

## 9. Temps réel

SignalR reste le canal principal de temps réel, mais il est alimenté depuis le LAN, pas directement depuis Vector.

Délai acceptable : **1 minute**.

La régulation ne voit pas les événements Vector bruts. Elle voit uniquement l’état consolidé.

Flux :

```text
Vector API DMZ
   ↓
VectorOutbox DMZ
   ↓
VectorDmzBridgeWorker LAN
   ↓
RabbitMQ interne
   ↓
MissionProjectionWorker
   ↓
OrderDb : état consolidé
   ↓
SignalR interne
   ↓
Régulation / équipages concernés
```

Notifications autorisées :

```text
Mission 123 : statut terrain = Sur place
Mission 123 : prescription récupérée = Oui
Mission 123 : données retour disponibles = Oui
Mission 123 : anomalie horaire détectée = Oui
```

Notifications interdites :

```text
Numéro de sécurité sociale
Téléphone patient
Mutuelle
Photo carte mutuelle
Prescription détaillée
Adresse complète si non nécessaire
```

---

## 10. Statuts terrain

Cycle terrain V1 :

```text
Mission vue
En route
Sur place
Patient pris en charge
Arrivé destination
Disponible
```

Décisions :

| Règle | Décision |
|---|---|
| Mission vue | Oui |
| Mission acquittée | Non |
| En route | Oui |
| Sur place | Oui |
| Patient pris en charge | Oui |
| Arrivé destination | Oui |
| Disponible | Oui |
| Retour base | Non |
| Disponible clôture la mission terrain | Oui |
| Retour arrière dans les statuts | Oui |
| Saut de statut | Oui |
| Plusieurs fois le même statut | Non |

Tables recommandées :

```text
VectorDb.VectorMissionStatusHistory
OrderDb.MissionFieldStatusCurrent
OrderDb.MissionFieldStatusHistory
```

---

## 11. Horaires

Horaires conservés :

| Horaire | Conservé |
|---|---|
| Mission vue | Oui |
| En route | Oui |
| Arrivée sur place | Oui |
| Patient pris en charge | Oui |
| Départ lieu de prise en charge | Oui |
| Arrivée destination | Oui |
| Patient déposé | Oui |
| Disponible | Oui |
| Retour base | Non |

Règles :

- les horaires terrain sont toujours non fiables ;
- les horaires régulation sont provisoires ;
- les horaires certifiés sont les seuls définitifs ;
- la régulation peut ignorer un horaire terrain ;
- la régulation peut corriger tous les horaires ;
- en V1, tout le monde peut corriger après clôture ;
- pas de comparaison OSRM en V1 ;
- détection automatique des horaires incohérents.

Dans cette architecture DMZ :

```text
VectorDb conserve l’horaire saisi ou généré côté terrain.
OrderDb conserve l’horaire opérationnel provisoire retenu.
CertificationDb conserve l’horaire certifié final.
```

Tables recommandées dans `OrderDb` :

```text
MissionOperationalTime
MissionOperationalTimeHistory
```

---

## 12. Données administratives terrain

Vector peut saisir ou indiquer :

| Donnée | Décision |
|---|---|
| Téléphone patient | Oui |
| Numéro de sécurité sociale | Oui |
| Mutuelle | Oui |
| Prescription récupérée | Oui |
| Prescription manquante | Oui |
| Photo prescription | Non |
| Photo carte mutuelle | Oui |
| Consignes retour | Oui |

Règles :

- les données terrain ne modifient pas directement la commande ;
- elles ne doivent pas forcément être validées par la régulation ;
- la certification porte seulement sur les horaires et les adresses / géolocalisation ;
- les anciennes valeurs doivent être conservées.

### 12.1 Stockage DMZ puis intégration interne

Les données saisies dans Vector sont d’abord stockées dans `VectorDb`.

Puis elles sont intégrées dans `OrderDb` via le flux sécurisé :

```text
VectorFieldAdministrativeData DMZ
   ↓
VectorOutboxMessage
   ↓
VectorDmzBridgeWorker
   ↓
RabbitMQ interne
   ↓
Order / Mission Consumer
   ↓
OrderDb.MissionFieldAdministrativeData
```

---

## 13. Photo de carte mutuelle

La photo de carte mutuelle ne doit pas être stockée en base SQL.

Flux recommandé :

```text
Mobile Vector
   ↓ upload HTTPS
Stockage temporaire DMZ
   ↓ référence dans VectorDb.VectorDocumentStaging
   ↓ événement MutualCardPhotoUploaded
VectorDmzBridgeWorker
   ↓ copie contrôlée vers stockage interne sécurisé
   ↓ suppression ou expiration de la copie DMZ
OrderDb.MissionDocument = référence interne
```

Règles V1 :

- stockage hors base ;
- pas de coffre documentaire en V1 ;
- pas de traçage fin des consultations en V1 ;
- purge automatique à 3 ans ;
- suppression de la copie DMZ dès transfert interne si possible.

---

## 14. Partage avec les autres équipages

Règles retenues :

- les équipages retour peuvent voir les données saisies à l’aller ;
- les équipages non affectés ne peuvent pas voir les données ;
- les autres équipages de la même agence ne peuvent pas voir les données ;
- le numéro de sécurité sociale doit être masqué partiellement ;
- la mutuelle est visible par l’équipage retour ;
- le téléphone est visible par l’équipage retour ;
- la prescription est visible par l’équipage retour ;
- pas de traçage systématique de chaque consultation de donnée sensible en V1 ;
- l’accès est autorisé jusqu’au statut clôturé ;
- après clôture, l’accès Vector est interdit.

Dans l’architecture DMZ, cette visibilité doit être calculée côté interne puis projetée vers Vector.

```text
OrderDb / Mission
   ↓ calcule les droits de visibilité
   ↓ publie une projection filtrée
VectorDb
   ↓ affiche seulement les données autorisées
```

Vector ne doit pas recalculer seul les droits métier complexes à partir des bases internes.

---

## 15. Crew

`CrewDb` contient :

- composition d’équipage ;
- véhicule affecté ;
- état courant de disponibilité.

L’affectation mission-équipage est stockée dans `OrderDb`.

Vector reçoit uniquement une projection des missions attribuées à un équipage.

Vector ne lit jamais `CrewDb`.

---

## 16. Certification

`CertificationDb` est la source finale des horaires validés.

Décisions :

| Règle | Décision |
|---|---|
| CertificationDb reçoit toutes les valeurs candidates | Oui |
| CertificationDb copie toutes les données utiles depuis OrderDb | Pas forcément |
| Photo complète des données au moment de certification | Non |
| Certification mission par mission | Oui |
| Certification par lot | Oui |
| Certification malgré horaires terrain incohérents | Oui |
| Certification sans prescription | Oui |
| Dossier de certification verrouillé | Oui |
| Modification mission après certification | Non |
| Réouverture admin après certification | Oui |

Objet métier recommandé :

```text
CertifiedMissionFile
```

Champs recommandés :

```text
MissionId
OrderId
CertifiedTimes
CertifiedAddresses
CertifiedDistance
PrescriptionStatus
CertificationStatus
CertificationMode
CertifiedByUserId
CertifiedByWorkerName
CertifiedAt
CertificationRuleVersion
ReopenedAt
ReopenedByUserId
ReopenReason
```

---

## 17. Facturation

Décisions :

| Règle | Décision |
|---|---|
| BillingDb consomme CertificationDb | Oui |
| BillingDb peut lire OrderDb | Oui |
| BillingDb peut utiliser une donnée non certifiée | Oui |
| Mission non certifiée facturable | Oui |
| Snapshot complet BillingDb | Non |
| Données verrouillées après facturation | Oui |
| Recalcul après correction | Oui |
| Trace des données utilisées | Oui |

La facturation est souple en V1, mais chaque donnée utilisée doit être tracée avec son niveau de fiabilité.

Table recommandée :

```text
BillingCalculationTrace
```

---

## 18. Audit qualité

Objectif : mesurer la qualité des données terrain.

Décisions :

| Règle | Décision |
|---|---|
| Mesurer qualité de saisie par équipage | Oui |
| Mesurer qualité de correction par régulation | Non |
| Comparaison principale | Terrain / Certification |
| Score par type d’horaire | Oui |
| Score par ambulancier | Oui |
| Score par agence | Oui |
| Données ignorées dans les statistiques | Non |
| Exclure missions atypiques | Non |
| Alertes sur écarts importants | Oui |

Les données ignorées doivent être conservées techniquement, mais exclues des scores qualité.

---

## 19. Sécurité et confidentialité

Décisions :

| Règle | Décision |
|---|---|
| Chiffrement données sensibles en base | V2 |
| Chiffrement numéro de sécurité sociale | V2 |
| Photos carte mutuelle hors base | Oui |
| Coffre documentaire | Non en V1 |
| Traçage accès photos carte mutuelle | Non en V1 |
| Traçage accès numéro sécurité sociale | Non en V1 |
| Purge automatique données terrain | Oui |
| Conservation longue audit/litige | Non |
| Durée de conservation | 3 ans |

Règles spécifiques DMZ :

- aucune chaîne de connexion LAN dans les applications Vector DMZ ;
- aucun accès SQL direct depuis la DMZ vers `OrderDb`, `CrewDb`, `CertificationDb`, `BillingDb` ;
- secrets DMZ séparés des secrets LAN ;
- filtrage strict des ports entre DMZ et LAN ;
- journalisation des transferts DMZ → LAN ;
- suppression des documents temporaires DMZ après transfert ;
- projections minimales pour limiter l’exposition des données patient.

---

## 20. V1 / V2

### V1

La V1 privilégie une architecture pragmatique mais sécurisée :

- Vector API en DMZ ;
- VectorDb en DMZ ;
- aucune connexion Vector vers les bases internes ;
- projections internes publiées vers Vector ;
- Outbox Vector en DMZ ;
- bridge interne DMZ → LAN ;
- RabbitMQ interne ;
- Outbox / Inbox malgré RabbitMQ ;
- SignalR alimenté depuis le LAN ;
- pas de chiffrement avancé ;
- pas de coffre documentaire ;
- pas de traçage fin des consultations sensibles ;
- certification tentée en temps réel par worker ;
- facturation possible avec des données non certifiées.

### V2

La V2 devra prévoir :

- séparation applicative plus stricte du module Mission ;
- supervision complète des messages ;
- chiffrement des données sensibles ;
- chiffrement du numéro de sécurité sociale ;
- éventuel coffre documentaire ;
- traçabilité renforcée des accès sensibles ;
- restriction progressive des écritures directes inter-bases ;
- meilleure gouvernance des corrections après clôture ;
- éventuelle API Gateway interne dédiée aux échanges DMZ / LAN.

---

## 21. Règles techniques minimales

| Règle | Recommandation |
|---|---|
| Accès Vector aux bases internes | Interdit |
| Lecture missions par Vector | Via `VectorMissionProjection` uniquement |
| Remontées terrain | Via `VectorOutboxMessage` uniquement |
| Publication vers LAN | Via `VectorDmzBridgeWorker` |
| Écriture dans `OrderDb` | Par consumers internes seulement |
| Lecture directe `OrderDb` par autres modules LAN | Possible via vues ou projections si possible |
| Modification mission certifiée | Interdite sauf réouverture admin |
| Facturation non certifiée | Autorisée mais marquée comme non certifiée |
| Données terrain | Toujours historisées |
| Données ignorées | Conservées techniquement, exclues des scores qualité |
| SignalR | Diffuse seulement l’état consolidé |
| Notifications équipage | Jamais de données patient dans le contenu |

---

## 22. Événements d’intégration recommandés

### 22.1 Événements internes vers Vector

```text
MissionPublishedToVectorIntegrationEvent
MissionUpdatedForVectorIntegrationEvent
MissionRemovedFromVectorIntegrationEvent
MissionAccessClosedForVectorIntegrationEvent
ReturnCrewDataAvailableIntegrationEvent
VectorVisibilityRulesChangedIntegrationEvent
```

### 22.2 Événements Vector vers LAN

```text
MissionSeenIntegrationEvent
CrewEnRouteIntegrationEvent
CrewOnSceneIntegrationEvent
PatientPickedUpIntegrationEvent
PickupLocationDepartedIntegrationEvent
DestinationReachedIntegrationEvent
PatientDroppedOffIntegrationEvent
CrewAvailableIntegrationEvent
PrescriptionCollectedIntegrationEvent
PrescriptionMissingIntegrationEvent
FieldAdministrativeDataProvidedIntegrationEvent
MutualCardPhotoUploadedIntegrationEvent
ReturnInstructionProvidedIntegrationEvent
```

### 22.3 Événements Mission / Order

```text
MissionOperationalStateChangedIntegrationEvent
MissionOperationalTimeCorrectedIntegrationEvent
MissionClosedIntegrationEvent
MissionReopenedIntegrationEvent
MissionCertifiedIntegrationEvent
MissionBillingRequestedIntegrationEvent
```

### 22.4 Événements Crew

```text
CrewAvailabilityChangedIntegrationEvent
CrewCompositionChangedIntegrationEvent
VehicleAssignedToCrewIntegrationEvent
```

### 22.5 Événements Certification

```text
CertificationCandidateReceivedIntegrationEvent
MissionAutoCertifiedIntegrationEvent
MissionCertificationNeedsReviewIntegrationEvent
MissionCertifiedManuallyIntegrationEvent
CertifiedMissionFileCreatedIntegrationEvent
```

### 22.6 Événements Billing

```text
BillingPreparationCreatedIntegrationEvent
ProvisionalBillingCalculatedIntegrationEvent
CertifiedBillingCalculatedIntegrationEvent
BillingLockedIntegrationEvent
BillingRecalculatedIntegrationEvent
```

---

## 23. Organisation projet recommandée

Organisation compatible Clean Architecture et modèle hexagonal :

```text
CaSoft.Erp.Vector.Domain
CaSoft.Erp.Vector.Application
CaSoft.Erp.Vector.Infrastructure
CaSoft.Erp.Vector.Api
CaSoft.Erp.Vector.Worker

CaSoft.Erp.Vector.DmzBridge.Worker

CaSoft.Erp.Order.Domain
CaSoft.Erp.Order.Application
CaSoft.Erp.Order.Infrastructure
CaSoft.Erp.Order.Api

CaSoft.Erp.Mission.Domain
CaSoft.Erp.Mission.Application
CaSoft.Erp.Mission.Infrastructure

CaSoft.Erp.Crew.Domain
CaSoft.Erp.Crew.Application
CaSoft.Erp.Crew.Infrastructure
CaSoft.Erp.Crew.Api

CaSoft.Erp.Certification.Domain
CaSoft.Erp.Certification.Application
CaSoft.Erp.Certification.Infrastructure
CaSoft.Erp.Certification.Worker

CaSoft.Erp.Billing.Domain
CaSoft.Erp.Billing.Application
CaSoft.Erp.Billing.Infrastructure
CaSoft.Erp.Billing.Worker

CaSoft.Erp.Integration.Contracts
CaSoft.Erp.Integration.Infrastructure
CaSoft.Erp.Integration.Worker
```

En V1, `Erp.Mission` peut exister comme solution ou projet préparatoire, même si les tables Mission restent dans `OrderDb`.

---

## 24. Synthèse finale

L’architecture retenue sépare clairement :

- la donnée terrain ;
- la donnée opérationnelle ;
- la donnée certifiée ;
- la donnée facturée ;
- la zone exposée DMZ ;
- la zone interne LAN.

Principe final :

```text
Ne jamais confondre :
- la donnée déclarée,
- la donnée affichée,
- la donnée retenue,
- la donnée certifiée,
- la donnée facturée.

Ne jamais confondre non plus :
- le module exposé en DMZ,
- les bases métier internes,
- les projections nécessaires au mobile,
- les données sources internes.
```

Cette séparation permet :

- un accès mobile sécurisé ;
- aucune exposition directe des bases internes ;
- une régulation informée via état consolidé ;
- une correction opérationnelle des horaires ;
- une certification progressive ;
- une facturation souple mais traçable ;
- un audit qualité par équipage, ambulancier et agence.
