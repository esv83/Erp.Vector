# CaSoft.Erp.USVector

API mobile ambulanciers (terrain ↔ régulation), **reconnectée à l'ERP** après la perte de
la base legacy `BD_REGULATION_prod`. Remplace l'ancienne `WebApi` de la solution
`E:\VB_Projects\MobileApp` en préservant le contrat (routes + DTOs) → l'app mobile est
re-pointée sans modification.

> Plan de développement détaillé : `mobile_devplan.md` (itérations MOB-0 → MOB-16).

## Architecture (Clean Architecture, .NET 8)

| Projet | Langage | Rôle |
|---|---|---|
| `CaSoft.Erp.USVector.Domain` | VB | Entités métier mobile (← `MobApp.Domaine`) |
| `CaSoft.Erp.USVector.Contracts` | VB | DTOs = contrat mobile (← `MobApp.Modeles`) |
| `CaSoft.Erp.USVector.Application` | VB | Use cases, services, **interfaces repos** (← `MobApp.Application`) |
| `CaSoft.Erp.USVector.Infrastructure` | C# | Couche data : `MobileDbContext` (BD Mobile `MOB_*`) + repos lisant l'ERP **in-process** + mapping |
| `CaSoft.Erp.USVector.Api` | C# | API REST (controllers) — sous-app IIS `/mobile` |
| `CaSoft.Erp.USVector.Framework` | VB | Socle legacy (`ClBusinessBase`, use cases, presenters — ← `0-Framework`, RootNamespace `CaSoft.Framework`) |
| `GpsGate.Connector` | VB | Client REST GpsGate (géoloc) — porté tel quel |
| `EmergencyPlatformConnector` | VB | Orchestration Sirus (UDP régulation) + GpsGate — porté tel quel |

### Flux de données
- **Données de référence** (missions, équipages, véhicules, personnel, bénéficiaires) :
  lues **in-process** via références projet vers `Orders.Application` / `Orders.Infrastructure`
  (ERP), **pas** via `Orders.Api` HTTP.
- **Données purement mobiles** (timeline statuts opérationnels, signature, kilométrage,
  logs mécaniques) : **BD Mobile dédiée** (`MOB_*`), référencent les entités ERP par id.
- **Sirus** (régulation UDP) + **GpsGate** (géoloc REST) : statu quo, connecteurs portés tels quels.

## Références ERP (in-process)
`CaSoft.Erp.USVector.Infrastructure` référence :
- `..\..\Erp.Orders\Orders.Application\Orders.Application.vbproj`
- `..\..\Erp.Orders\Orders.Infrastructure\Orders.Infrastructure.csproj`

## État
**MOB-0 → MOB-3 + MOB-5 livrés** (MOB-4 reporté) :
- BD dédiée `BD_ERP_MOBILE_APP` (serveur dev `192.168.1.109,1440`), tables `MOB_SESSION` /
  `MOB_MISSION_STATE` / `MOB_SIGNATURE`, entités Database First + `MobileDbContext`.
- Legacy intégralement porté : Framework (`CaSoft.Erp.USVector.Framework`), Domain, Contracts,
  Application, connecteurs (`GpsGate.Connector`, `EmergencyPlatformConnector` avec Sirus),
  16 controllers. Secrets en config (`__SET_VIA_ENV__`, user-secrets en dev).
- Repos BD Mobile **réels** : signature, timeline statuts, sessions ; le reste stubbé (MOB-4+).
- **Identités de référence (crew/vehicle/personnel) en Guid**, alignées sur l'ERP (décision MOB-3a,
  pas de table de correspondance). Mappings ERP→mobile (`ErpReferenceMappings`) consommant les DTO
  d'Orders.Application.
- **Accès ERP in-process** câblé (`AddOrdersInfrastructure`) : `GET api/joblist/{crewId}` renvoie
  les missions du jour de l'équipage (ERP) avec flags ack/terminé overlay depuis la BD Mobile —
  validé sur données réelles.
- L'API expose les **25 routes du contrat legacy** ; `api/Signature`, `api/Time`, `api/joblist`
  validés de bout en bout.

Prochaine étape : **MOB-4** (Login — résolution équipage ERP + token `MOB_SESSION`), puis MOB-6 (détail mission).

## Build
```powershell
dotnet build USVector.sln
```
