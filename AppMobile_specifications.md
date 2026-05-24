\# SPECIFICATION FONCTIONNELLE

\# MODULE MOBILE TERRAIN — ERP TRANSPORT SANITAIRE



Version : 1.0  

Destination : Équipe développement  

Type : Application Web Mobile indépendante  

Contexte : ERP Transport Sanitaire  



\---



\# 1. OBJECTIF DU MODULE



Le module Mobile Terrain permet aux équipages ambulanciers :



\- de consulter leur plan de travail en temps réel ;

\- de recevoir les missions attribuées par la régulation ;

\- de suivre l’état d’avancement des missions ;

\- de transmettre des informations terrain ;

\- de notifier la régulation des changements de statut ;

\- de transmettre des documents administratifs ;

\- de recueillir la signature du patient ;

\- d’accuser réception des modifications du planning.



Le module est une application web mobile indépendante.



Il ne s’intègre pas au shell WinForms existant.



\---



\# 2. PRINCIPES D’ARCHITECTURE FONCTIONNELLE



\## 2.1 Séparation Régulation / Terrain



Le système distingue :



\### Données officielles régulation



Données créées et maintenues par la régulation :



\- patient ;

\- horaires prévus ;

\- adresses ;

\- mode de transport ;

\- affectation équipage ;

\- planification.



\### Données terrain



Données constatées ou saisies par l’équipage :



\- statuts terrain ;

\- horaires réels ;

\- informations complémentaires ;

\- observations ;

\- signature ;

\- documents ;

\- données de contact réellement utilisées.



Les données terrain ne doivent pas écraser automatiquement les données officielles.



\---



\# 3. OBJECTIFS MÉTIER



Le module doit permettre :



\- un suivi temps réel des équipages ;

\- une transmission fiable des ordres de mission ;

\- la traçabilité des prises de connaissance ;

\- la traçabilité des statuts terrain ;

\- la remontée des documents nécessaires à la facturation ;

\- la remontée des anomalies terrain ;

\- la sécurisation des échanges entre régulation et terrain.



\---



\# 4. UTILISATEURS



\## 4.1 Ambulancier



Accède uniquement :



\- à ses missions ;

\- au plan de travail de son équipage ;

\- aux informations nécessaires à l’exécution des missions.



\## 4.2 Régulateur



Visualise :



\- l’état du plan de travail ;

\- les accusés de réception ;

\- les statuts terrain ;

\- les documents remontés ;

\- les signatures ;

\- les anomalies signalées.



\---



\# 5. PLAN DE TRAVAIL TERRAIN



\## 5.1 Concept



Le plan de travail représente :



\- la liste ordonnée des missions ;

\- affectées à un équipage ;

\- pour une période donnée.



L’ordre est décidé par la régulation.



\---



\# 6. GESTION DES MODIFICATIONS DU PLAN



\## 6.1 Modifications possibles



La régulation peut :



\- ajouter une mission ;

\- supprimer une mission ;

\- modifier l’ordre des missions ;

\- modifier les horaires ;

\- modifier certaines informations de mission.



Chaque modification crée une nouvelle version du plan.



\---



\# 7. ACCUSÉ DE RÉCEPTION OBLIGATOIRE



\## 7.1 Objectif



Toute modification du plan doit être :



\- notifiée à l’équipage ;

\- validée explicitement comme reçue.



\---



\## 7.2 Fonctionnement attendu



Lorsqu’une modification intervient :



\- une notification apparaît sur le mobile ;

\- le détail des changements est affiché ;

\- l’utilisateur doit confirmer réception.



Exemple :



\- mission ajoutée ;

\- mission annulée ;

\- ordre des missions modifié.



\---



\## 7.3 Statuts de réception



Le système doit distinguer :



\- plan envoyé ;

\- plan reçu ;

\- plan non confirmé ;

\- plan confirmé.



\---



\# 8. LISTE DES MISSIONS



\## 8.1 Affichage



La liste des missions doit être :



\- triée selon l’ordre défini par la régulation ;

\- lisible rapidement ;

\- adaptée à un usage terrain.



\---



\## 8.2 Informations affichées



Chaque mission affiche :



\- ordre dans la tournée ;

\- nom du patient ;

\- heure prévue ;

\- lieu de départ ;

\- lieu d’arrivée ;

\- mode de transport ;

\- statut terrain actuel.



\---



\## 8.3 Mise en évidence



Les missions doivent être visuellement différenciées :



\- mission en cours ;

\- mission urgente ;

\- mission modifiée ;

\- mission annulée ;

\- mission non lue.



\---



\# 9. PRISE DE CONNAISSANCE D’UNE MISSION



\## 9.1 Validation de lecture



L’ambulancier doit explicitement confirmer :



\- qu’il a pris connaissance de la mission.



Cette action doit être tracée.



\---



\## 9.2 Informations affichées



Premier écran de la mission :



\### Patient



\- nom ;

\- prénom ;

\- âge ;

\- date de naissance.



\### Transport



\- mode de transport ;

\- heure de prise en charge ;

\- heure de rendez-vous.



\### Adresses



\- départ ;

\- destination.



\---



\# 10. STATUTS TERRAIN



\## 10.1 Statuts principaux



L’application doit permettre :



\- EN ROUTE ;

\- SUR PLACE ;

\- DISPONIBLE.



D’autres statuts pourront être ajoutés ultérieurement.



\---



\## 10.2 Traçabilité



Chaque changement de statut doit conserver :



\- la date ;

\- l’heure ;

\- l’utilisateur ;

\- éventuellement la géolocalisation.



\---



\# 11. SAISIE DES DONNÉES TERRAIN



\## 11.1 Objectif



L’équipage peut enrichir la mission avec des données terrain.



\---



\## 11.2 Données possibles



Exemples :



\- téléphone utilisé ;

\- email ;

\- numéro de sécurité sociale ;

\- numéro Centaure ;

\- nom du SMUR ;

\- type de mission ;

\- observations.



\---



\# 12. GESTION MODULAIRE DES CHAMPS



\## 12.1 Principe



Les champs disponibles dépendent :



\- du type de mission ;

\- du contexte métier.



\---



\## 12.2 Fonctionnement attendu



Le système doit permettre :



\- d’ajouter de nouveaux champs ;

\- de rendre certains champs obligatoires ;

\- de masquer certains champs ;

\- d’adapter les écrans selon le contexte métier.



Sans modification majeure de l’application.



\---



\# 13. SIGNATURE PATIENT



\## 13.1 Objectif



Le patient doit pouvoir signer directement sur le mobile.



\---



\## 13.2 Exigences



La signature doit être :



\- horodatée ;

\- associée à la mission ;

\- traçable ;

\- consultable par la régulation ;

\- consultable par la facturation.



\---



\# 14. DOCUMENTS ET PHOTOS



\## 14.1 Objectif



L’équipage doit pouvoir transmettre :



\- bons de transport ;

\- prescriptions ;

\- documents administratifs ;

\- photos justificatives.



\---



\## 14.2 Fonctionnement attendu



Le système doit permettre :



\- prise de photo directe ;

\- upload depuis le téléphone ;

\- consultation côté régulation ;

\- consultation côté facturation.



\---



\## 14.3 Catégorisation



Les documents doivent être catégorisables :



\- bon de transport ;

\- prescription ;

\- document administratif ;

\- autre.



\---



\# 15. NOTIFICATIONS TEMPS RÉEL



\## 15.1 Notifications vers le mobile



Le mobile doit recevoir :



\- nouvelles missions ;

\- modification du plan ;

\- annulation ;

\- changement de priorité ;

\- messages régulation.



\---



\## 15.2 Notifications vers la régulation



La régulation doit être informée :



\- mission lue ;

\- changement de statut ;

\- signature reçue ;

\- document reçu ;

\- anomalie signalée ;

\- confirmation de réception du plan.



\---



\# 16. GESTION DES DONNÉES OFFICIELLES



\## 16.1 Principe



Les données officielles ne doivent pas être remplacées automatiquement par les données terrain.



\---



\## 16.2 Exemple



Le téléphone terrain utilisé :



\- ne remplace pas automatiquement le téléphone officiel du patient.



\---



\# 17. GESTION DES ANOMALIES



\## 17.1 Objectif



L’équipage doit pouvoir signaler :



\- erreur de téléphone ;

\- erreur d’adresse ;

\- erreur patient ;

\- problème administratif ;

\- impossibilité de prise en charge.



\---



\## 17.2 Validation



Les anomalies doivent être :



\- visibles par la régulation ;

\- validées ou rejetées ;

\- historisées.



\---



\# 18. MODE HORS LIGNE



\## 18.1 Contraintes métier



Les équipages peuvent intervenir :



\- en zone blanche ;

\- dans des zones à faible couverture réseau.



\---



\## 18.2 Fonctionnement attendu



Le système devra prévoir ultérieurement :



\- cache local ;

\- synchronisation différée ;

\- reprise automatique.



Le mode offline n’est pas prioritaire pour le MVP.



\---



\# 19. TRAÇABILITÉ



Le système doit historiser :



\- lectures missions ;

\- accusés réception ;

\- changements de statut ;

\- signatures ;

\- documents ;

\- anomalies ;

\- modifications importantes.



\---



\# 20. SÉCURITÉ



\## 20.1 Authentification



L’utilisateur doit être authentifié individuellement.



\---



\## 20.2 Isolation des données



Un utilisateur ne doit voir :



\- que les missions de son équipage ;

\- que les données autorisées.



\---



\## 20.3 Traçabilité utilisateur



Toute action importante doit conserver :



\- utilisateur ;

\- date ;

\- heure.



\---



\# 21. OBJECTIFS UX MOBILE



L’application doit être :



\- utilisable avec une seule main ;

\- très lisible ;

\- rapide ;

\- adaptée aux usages terrain ;

\- compatible usage véhicule ;

\- compatible faible luminosité.



\---



\# 22. PRIORITÉS MVP



\## Phase 1



\- authentification ;

\- liste missions ;

\- détail mission ;

\- validation lecture ;

\- statuts terrain ;

\- notifications ;

\- accusé réception planning.



\---



\## Phase 2



\- champs métier modulaires ;

\- signature ;

\- documents ;

\- anomalies.



\---



\## Phase 3



\- mode offline ;

\- géolocalisation avancée ;

\- optimisation UX ;

\- notifications enrichies.



\---



\# 23. OBJECTIFS DE ROBUSTESSE



Le système doit éviter :



\- perte de données ;

\- écrasement d’informations ;

\- absence de traçabilité ;

\- absence de confirmation de réception ;

\- désynchronisation terrain/régulation.



\---

