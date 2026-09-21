# CaSoft.Erp.USVector — Vector

API du **module terrain des ambulanciers** : l'application mobile y lit ses missions et y écrit ce que
l'équipage fait sur le terrain. Reconstruite sur l'ERP après la perte de la base historique, **sans
changer le contrat consommé par l'application** — mêmes routes, mêmes formats.

> **Ce qui reste à faire** : [`devplan.md`](devplan.md) · **Ce qui est livré, les décisions, la
> configuration, les incidents** : [`delivered.md`](delivered.md).

## Architecture (Clean Architecture, .NET 8)

| Projet | Langage | Rôle |
|---|---|---|
| `CaSoft.Erp.USVector.Domain` | VB | Entités métier du terrain |
| `CaSoft.Erp.USVector.Contracts` | VB | DTO du contrat mobile |
| `CaSoft.Erp.USVector.Application` | VB | Cas d'usage (`ClResult` typé), interfaces des dépôts et des clients ERP |
| `CaSoft.Erp.USVector.Infrastructure` | C# | `MobileDbContext` (base Vector, tables `MOB_*`), clients HTTP vers Orders.Api, mappings |
| `CaSoft.Erp.USVector.Api` | C# | Contrôleurs REST, authentification Keycloak, worker de projection vers Orders |
| `CaSoft.Erp.USVector.Framework` | VB | Socle local (`IResultUseCase`, outils) |
| `CaSoft.Erp.USVector.Tests` | C# | xUnit + FluentAssertions |
| `GpsGate.Connector`, `EmergencyPlatformConnector` | VB | Connecteurs géolocalisation et régulation Sirus — portés, **non recâblés** |

## Flux de données

- **Données de référence** (missions, commandes, équipages, véhicules, personnel, bénéficiaires, type
  de mission et attributs de facturation) : lues chez **Orders.Api, en HTTP** (`OrdersApi:BaseUrl`).
  Aucune référence de projet vers les autres modules : Vector se construit et se déploie seul.
- **Données propres au terrain** (étapes horodatées, signature, anomalies, documents, carte mutuelle,
  file de projection) : **base Vector** `BD_ERP_MOBILE_APP`, tables `MOB_*`.
- **Vers la régulation** : chaque geste est projeté sur Orders par une file rejouée
  (`OperationalOutboxDispatcher`) ; un envoi en échec ne bloque jamais la saisie.
- **Vers la facturation** : elle tire le dossier terrain et les pièces jointes depuis Vector.

## Authentification

- **Ambulancier** : jeton Keycloak du client mobile (`azp` = `Keycloak:Audience`). L'équipage est
  résolu côté serveur, jamais fourni par l'app.
- **Service à service** : Vector accepte les modules déclarés dans `Keycloak:ServiceAzp` et présente
  son propre jeton à Orders (`erp-vector-api`).
- **Fermée par défaut** : les seules routes anonymes sont nommées et justifiées dans
  `AnonymousSurfaceTests`.

Guides : [`docs/deploiement/configuration-keycloak-iis.md`](docs/deploiement/configuration-keycloak-iis.md),
[`docs/deploiement/keycloak-compte-service-vector.md`](docs/deploiement/keycloak-compte-service-vector.md).

## Build, tests, déploiement

**Après une publication**, rejouer [`CaSoft.Erp.USVector.Api/Vector.Api.http`](CaSoft.Erp.USVector.Api/Vector.Api.http) :
jeton, équipage, missions, jalons, signature, lots de la facturation, et ce qui **doit** être refusé.

```powershell
dotnet build USVector.sln
dotnet test USVector.sln
.\deploy.ps1 dev     # \\192.168.1.112\dev_api\Vector.Api
.\deploy.ps1 prod    # \\192.168.1.112\prod_api\Vector.Api — IIS /vector, confirmation à taper
```

⚠️ **Publier depuis `main`, arbre propre** : le script ne l'impose pas encore (devplan, itération 2).
Pièges de configuration avérés : [`delivered.md`](delivered.md) §6.
