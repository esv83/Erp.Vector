# 🧭 Traçabilité — des saisies Vector aux 91 colonnes du fichier de facturation

> **Objet** : établir, saisie par saisie, ce que le terrain écrit dans Vector, **par quel chemin**
> cela atteint `BillingGateway`, et **dans quelle colonne** du fichier AidesNSoft cela finit — ou
> pourquoi cela n'y finit pas.
>
> **Nature** : relevé de l'existant, pas un plan. Aucune décision n'est prise ici ; les écarts
> constatés sont listés en §6 pour arbitrage.
>
> **Établi le** 2026-08-26 par lecture croisée de `esv83/Erp.Vector` @ `1d06b1f` et
> `esv83/Erp.BillingGateway` @ `b4eec90`.
>
> Se lit avec [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md), qui propose de
> changer ce chemin, et [`devplan.md`](devplan.md), qui porte les décisions `D1`→`D16`.

---

# 1. En un paragraphe

**Les attributs remontent.** Selon le drapeau `ContextOrder:UseOrderAttributes`, ils transitent soit
par Order (chemin **A**), soit par le magasin Vector et le paquet `field-data` (chemin **B**) ; les
deux sont lus à l'export et fusionnés, **Order primant à nom égal**. Ils alimentent cinq colonnes :
`C41`, `C51`, `C54`, `C60`, `C89`.

**Quatre autres blocs ne remontent pas.** Le paquet `field-data` porte aussi la carte mutuelle, les
documents, les anomalies et les jalons `AckAt`/`ReadAt` : la copie possédée côté facturation ne les
**déclare pas**, et un *tolerant reader* jette en silence ce qu'il ne déclare pas. Aucune erreur,
aucune trace de journal.

Sur les 91 colonnes du contrat, **49 sont renseignées** et 42 partent vides à chaque ligne.

---

# 2. Le chemin, bout à bout

```
SAISIE (app terrain)          MAGASIN                      TRANSPORT HTTP                BILLINGGATEWAY
─────────────────────         ───────────────────────      ────────────────────────      ──────────────────────
Attributs de mission  ──ON──► Order (valeurs de       ────► GET /missions/for-export ──┐
PATCH api/JobEdit/{id}        contexte, portées par         1 appel / JOURNÉE (bulk)   │  prime à nom égal
                      │       la COMMANDE)                                             ├─► ModUpstreamTranslator
                      └─OFF─► MOB_JOB_ATTRIBUTE_VALUE ─┐                               │   .To_LigneExport
                              (overlay, par MISSION)   │                               │        │
Jalons opérationnels   ─────► MOB_JOB_TIME             ├──► GET api/missions/{id}/     │        ▼
PATCH api/time/{id}                                    │    field-data                 │   ClLigneExport
                                                       │    1 appel / MISSION ─────────┘   91 colonnes
Signature patient      ─────► MOB_SIGNATURE            │    (14,7 s pour 284)              │
POST api/Signature/{id}                                │         │ ImageUrl                ▼
                                                       │         ▼                     ClAidesNSoftCsvSerializer
Carte mutuelle         ─────► MOB_MUTUELLE_CARD        │    GET api/Signature/{id} ───►  fichier .txt
Documents · anomalies         MOB_DOCUMENT             │    image, si signée             « #ExportRegul 001 »
POST multipart                MOB_ANOMALY  ────────────┘         │
                                                                 ▼
                                        ⛔ mutuelle · documents · anomalies · AckAt · ReadAt
                                           JETÉS à la désérialisation (non déclarés dans
                                           ClFieldEnrichmentDto, copie possédée)

Troisième amont, hors Vector : CERTIFICATION (1 appel/journée) — passe DEVANT Vector
sur C17, C18, C26, C90, C91.
```

**Points de passage, avec leur fichier :**

| Étape | Où |
|---|---|
| Écriture des attributs, aiguillage du drapeau | `CaSoft.Erp.USVector.Api/Controllers/JobEditController.cs:38` |
| Chemin A — relais vers Order | `…/Infrastructure/Repositories/Erp/ContextOrderAttributeService.cs` |
| Chemin B — overlay BD Mobile | `…/Infrastructure/Repositories/Mobile/JobAttributeOverlayRepository.cs` |
| Composition du paquet terrain | `…/Infrastructure/Repositories/FieldDataReader.cs` |
| Bloc `attributes` du paquet (sans appel réseau) | `…/Infrastructure/Repositories/Mobile/FieldAttributesReader.cs` |
| Exposition du paquet (`AllowAnonymous`, DEC-6 non fait) | `…/Api/Controllers/FieldDataController.cs:26` |
| Lecture du paquet côté facturation | `BillingGateway.Infrastructure/Upstream/Vector/ClVectorFieldDataClient.vb` |
| Lecture de l'image de signature | `…/Upstream/Vector/ClVectorSignatureClient.vb` |
| Boucle d'acquisition (1 appel par mission) | `…/Upstream/ClCompositeMissionSourceProvider.vb` |
| Traduction vers les 91 colonnes | `…/Upstream/ModUpstreamTranslator.vb` |
| Écriture du fichier | `BillingGateway.Application/Serialization/ClAidesNSoftCsvSerializer.vb` |

---

# 3. Attributs → colonnes

Règle unique, dans `AppliquerAttributs` (`ModUpstreamTranslator.vb:441`). Tout attribut non nommé
explicitement part en commentaire sous la forme `NOM: valeur` — c'est le sort de `REFERENCE`,
`URGENT`, `PMT`, `BT`, `SMUR_DE`, `PHONES`, `MAILS`.

| Attribut saisi | Colonne | État | Règle |
|---|---|---|---|
| `NUM_CENTAURE` | **C41** | ✅ arrive | Numéro SAMU repris tel quel (Centaure = logiciel de régulation SAMU). N'écrase jamais une valeur déjà posée. |
| `SAMU` | **C41** ou **C60** | ✅ arrive | C41 seulement si strictement numérique après retrait des tirets/espaces ; sinon commentaire nu. |
| `AMC` · `MUTUELLE` | **C54** + **C60** | ✅ arrive | Écrit la colonne *et* une mention « Mutuelle: … ». ⚠️ **sans garde** — cf. §6. |
| `NIR` · `NUMSECU` | **C51** → **C88** | 🟡 repli | La fiche bénéficiaire d'Orders fait foi (règle du 22/08). L'attribut ne sert que si C51 est vide. C51 est ensuite recopié dans la clé `BeNumSecuComplet` de C88. |
| `DDN` | **C89** + **C60** | 🟡 repli | Même règle. En repli, normalisée `jj/mm/aaaa` et doublée d'une mention « DDN: … ». |
| `NOM_ASSISTANCE` + `NUM_DOSSIER` | **C60** | ✅ arrive | Concaténés en **une** mention « Assistance: nom - dossier », même si un seul est renseigné. |
| `NOM_CENTRALE` | **C60** | ✅ arrive | Mention « Centrale: … ». |
| `COMMENTS` · `COMMENTAIRE(S)` | **C60** | ✅ arrive | Repris nu, sans préfixe : c'est déjà du commentaire libre. |
| *tout autre nom* | **C60** | ✅ arrive | « NOM: valeur ». Les sauts de ligne deviennent `¤$£` à la sérialisation. |
| `ETABLISSEMENT_PAYEUR` · `COMMUNE` | — | ⛔ écarté | Décision métier du 16/08 : ni colonne, ni commentaire. Explicite, pas un oubli. |
| valeur `[]` ou `{}` | — | ⛔ filtré | `EstSansContenu` — sinon « PHONES: [] » polluait chaque ligne (206 lignes du premier dépôt). |
| type de contrat Vector | C49 · C50 | ⛔ abandonné | Colonnes plus écrites depuis le 16/08 (M-4) : le contrat MOB n'avait aucune utilité en facturation. |

> ⚠️ Rappel `B10` de [`devplan.md`](devplan.md) : au catalogue Order, `COMMENTS`, `PHONES`, `MAILS`,
> `PMT`, `SMUR_DE`, `COMMUNE`, `NOM_CENTRALE` sont **définis mais rattachés à rien** — ils
> n'atteignent aucune mission. Le tableau ci-dessus dit où ils *iraient* ; il ne dit pas qu'ils
> sortent aujourd'hui.

---

# 4. Le reste de ce que le terrain produit

| Saisie Vector | Colonne | État | Ce qui se passe |
|---|---|---|---|
| Jalons « En route » / « Sur place » | **C17** | 🟡 2ᵉ rang | Cascade à trois étages : Certification, puis Vector (`OnsiteAt` puis `GoAt`), puis horaire **prévu** d'Orders. Converti UTC → local. |
| Jalon « Disponible » | **C18** | 🟡 2ᵉ rang | Repli de `ADestinationUtc` de la Certification. Converti UTC → local. |
| Signature (présence) | **C61** | ✅ arrive | L'image est tirée à part sur `api/Signature/{id}` via l'`ImageUrl` du paquet. Image indisponible → drapeau `SIGNE` (hors contrat, assumé) ; pas de signature → `NPS`. |
| Horodatage de signature | **C62** | ✅ arrive | Seul horodatage Vector **non** converti : écrit en heure locale là où les jalons sont en UTC. À défaut, C62 recopie C18. |
| Jalons « Pris en compte » / « Lu » | — | ⛔ jeté | Servis par Vector, absents de `ClFieldTimelineDto` côté facturation. Aucune colonne ne les attend non plus. |
| Carte mutuelle (photo + champs) | — | ⛔ jeté | Le paquet la porte, la copie possédée ne la déclare pas. **C54 vient uniquement de l'attribut `AMC`**, jamais de la carte. |
| Documents et photos terrain (TRF-10) | — | ⛔ jeté | Idem. Le contrat AidesNSoft n'offre d'ailleurs aucune colonne pour eux. |
| Anomalies terrain (TRF-8) | — | ⛔ jeté | Idem — alors que la spec les annonce « arbitrées par la facturation ». |
| Kilométrage | C26 | ⚪ inerte | Le repli existe en 3ᵉ rang, mais Vector renvoie toujours `Nothing` : le km est porté par l'odomètre de l'équipage, pas par la mission. |
| Watermark `UpdatedAt` | — | ⚪ inutilisé | Déclaré dans la copie possédée, lu par personne : aucune re-synchronisation ne s'appuie dessus. |

---

# 5. Les 91 colonnes — d'où vient chaque champ

Ordre normatif du contrat `@Structure Import Prestations_V4`. **En gras** : ce que Vector alimente,
seul ou en repli.

| # | Champ | Source |
|---|---|---|
| C1 | Série | Orders — aller simple / aller-retour |
| C2 | Type de transport | Orders — mode traduit AMB/TAP/DIV |
| C3 | Type de véhicule | Orders — mode traduit |
| C4 | Libellé type véhicule | Orders — mode traduit |
| C5 | ID véhicule | Orders |
| C6 | Immatriculation | Orders |
| C7 | ID chauffeur | Orders |
| C8 | Nom chauffeur | Orders |
| C9 | Prénom chauffeur | Orders |
| C10 | ID accompagnateur | Orders |
| C11 | Nom accompagnateur | Orders |
| C12 | Prénom accompagnateur | Orders |
| C13 | ID bénéficiaire | Orders |
| C14 | Nom bénéficiaire | Orders — fiche |
| C15 | Prénom bénéficiaire | Orders — fiche |
| C16 | Téléphone bénéficiaire | Orders — fiche |
| C17 | Départ (réalisé) | Certification › **Vector jalons** › horaire prévu |
| C18 | Arrivée (réalisée) | Certification › **Vector `TerminateAt`** |
| C19 | Id aller-retour | Orders |
| C20 | Id commande | Orders |
| C21 | Sens | Orders |
| C22 | INSEE départ | Référentiel communes |
| C23 | Commune départ | Orders |
| C24 | INSEE arrivée | Référentiel communes |
| C25 | Commune arrivée | Orders |
| C26 | Distance | Certification › Orders routier › **Vector km (inerte)** |
| C27 | ID mission | Orders |
| C28 | Libellé départ | Orders — nom du lieu |
| C29 | Libellé arrivée | Orders — nom du lieu |
| C30 | Nombre de patients | *jamais alimentée* |
| C31 | Info covoiturage | *jamais alimentée* |
| C32 | Facture diverse | *jamais alimentée* |
| C33 | ID tiers soignant | *jamais alimentée* |
| C34 | Nom tiers soignant | *jamais alimentée* |
| C35 | ID donneur d'ordre | *jamais alimentée* |
| C36 | Nom donneur d'ordre | *jamais alimentée* |
| C37 | ID tiers payeur | *jamais alimentée* |
| C38 | Nom tiers payeur | *jamais alimentée* |
| C39 | Code postal départ | Orders |
| C40 | Code postal arrivée | Orders |
| C41 | Numéro SAMU | **Attribut `NUM_CENTAURE` ou `SAMU`** |
| C42 | ID prescripteur | *jamais alimentée* |
| C43 | FINESS prescripteur | *jamais alimentée* |
| C44 | Nom prescripteur | *jamais alimentée* |
| C45 | Service départ | Orders |
| C46 | Service arrivée | Orders |
| C47 | ID motif transport | *jamais alimentée* |
| C48 | Motif transport | *jamais alimentée* |
| C49 | Id contrat | abandonnée le 16/08 (portait le contrat Vector) |
| C50 | Libellé contrat | abandonnée le 16/08 |
| C51 | Numéro de sécurité sociale | Orders fiche › **attribut `NIR` (repli)** |
| C52 | ID caisse urgence | *jamais alimentée* |
| C53 | Taux caisse | *jamais alimentée* |
| C54 | ID mutuelle urgence | **Attribut `AMC` / `MUTUELLE`** — pas la carte |
| C55 | Taux mutuelle | *jamais alimentée* |
| C56 | Nature assurance | *jamais alimentée* |
| C57 | Code exonération | *jamais alimentée* |
| C58 | Numéro AT | *jamais alimentée* |
| C59 | Information valide | *jamais alimentée* |
| C60 | Commentaire | **Tous les attributs non nommés ailleurs** |
| C61 | Signature | **Image Vector** › `SIGNE` › `NPS` |
| C62 | Date/heure signature | **Vector `SignedAt`** › C18 |
| C63 | Montant compteur | *jamais alimentée* |
| C64 | Montant péage | *jamais alimentée* |
| C65 | Type supplément 1 | *jamais alimentée* |
| C66 | Montant supplément 1 | *jamais alimentée* |
| C67 | Type supplément 2 | *jamais alimentée* |
| C68 | Montant supplément 2 | *jamais alimentée* |
| C69 | Type supplément 3 | *jamais alimentée* |
| C70 | Montant supplément 3 | *jamais alimentée* |
| C71 | Complément départ | Orders |
| C72 | Complément arrivée | Orders |
| C73 | Détail adresse | constante — toujours `1` |
| C74 | Adresse départ ligne 1 | Orders |
| C75 | Adresse arrivée ligne 1 | Orders |
| C76 | Adresse départ ligne 2 | *jamais alimentée* |
| C77 | Adresse arrivée ligne 2 | *jamais alimentée* |
| C78 | ID tiers départ | *jamais alimentée* |
| C79 | Nom tiers départ | *jamais alimentée* |
| C80 | Type tiers départ | *jamais alimentée* |
| C81 | ID tiers arrivée | *jamais alimentée* |
| C82 | Nom tiers arrivée | *jamais alimentée* |
| C83 | Type tiers arrivée | *jamais alimentée* |
| C84 | Course confrère | *jamais alimentée* |
| C85 | ID tiers confrère | *jamais alimentée* |
| C86 | Nom tiers confrère | *jamais alimentée* |
| C87 | Adresse patient (JSON) | Orders — fiche ; vide si aucune adresse |
| C88 | Équipage (JSON) | Orders + C51 (donc **l'attribut `NIR` en repli**) |
| C89 | Date de naissance | Orders fiche › **attribut `DDN` (repli)** |
| C90 | Géoloc départ | Certification › Orders (adresse théorique) |
| C91 | Géoloc arrivée | Certification › Orders (adresse théorique) |

---

# 6. Écarts constatés — à arbitrer

| Réf | Constat | Où |
|---|---|---|
| **T1** | **Quatre blocs jetés sans bruit.** La copie possédée ne déclare ni `Mutuelle`, ni `Documents`, ni `Anomalies`, ni `AckAt`/`ReadAt`. Même piège que `ContextOrderId` puis `ImageUrl` — trois fois la même cause, notée dans ce fichier même. | `ClVectorFieldDataClient.vb:16-27` |
| **T2** | **Le plan mutuelle dit l'inverse du code.** `MUTUELLE_CARD_devplan.md:101` annonce que la facturation tire le bloc mutuelle et son `imageUrl` — « c'est fait ». Le client HTTP existe, mais le bloc n'est pas désérialisé et aucune colonne ne le lit. À corriger d'un côté ou de l'autre. | [`MUTUELLE_CARD_devplan.md:101`](MUTUELLE_CARD_devplan.md) |
| **T3** | **Le drapeau qui vide le chemin B.** Armer `UseOrderAttributes` détourne les saisies vers Order : le magasin Vector cesse d'être alimenté, le bloc `attributes` se construit **vide** sans erreur. Le garde-fou est un commentaire, pas un contrôle au démarrage — assumé, mais à connaître. | `ContextOrderOptions.cs:58-63` |
| **T4** | **C54 n'a pas de garde.** Contrairement à C41, C51 et C89, l'écriture par `AMC`/`MUTUELLE` ne teste pas si la colonne est déjà remplie : deux noms pour un même sens, la dernière valeur lue gagne. | `ModUpstreamTranslator.vb:496-498` |
| **T5** | **Le watermark ne sert à rien.** Vector calcule `UpdatedAt` = max des horodatages du paquet, précisément pour permettre un re-tirage ; la facturation le désérialise puis l'ignore. Une mission enrichie après publication ne se re-tire pas. | `FieldDataReader.cs:87-96` |
| **T6** | **Le chemin unitaire coûte cher.** Un appel `field-data` par mission, plus un appel image par mission signée — 14,7 s pour 284 missions, sur un clic. C'est l'objet de `PRJ`. | [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md) |

---

# 7. Documents voisins

| Document | Ce qu'il apporte |
|---|---|
| [`PROJECTION_TERRAIN_devplan.md`](PROJECTION_TERRAIN_devplan.md) | La cible : Order seul amont métier, Vector réduit aux octets. Rend `T6` caduc s'il est engagé. |
| [`devplan.md`](devplan.md) | Décisions `D1`→`D16`, et les manques `B9`/`B10` sur l'applicabilité du catalogue d'attributs. |
| [`MUTUELLE_CARD_devplan.md`](MUTUELLE_CARD_devplan.md) | Le plan carte mutuelle, à recaler sur `T2`. |
| [`endPoint.md`](endPoint.md) | Contrat détaillé de ce que Vector attend d'Orders. |
