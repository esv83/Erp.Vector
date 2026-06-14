using CaSoft.Erp.USVector.Domain;
using CaSoft.Orders.Application;

namespace CaSoft.Erp.USVector.Infrastructure.Mapping;

/// <summary>
/// MOB-3b — Mappings des données de référence ERP (DTO publics d'Orders.Application)
/// vers le domaine mobile. On consomme les DTO des query services ERP (contrat
/// applicatif stable), pas les entités EF.
///
/// Identités alignées en Guid des deux côtés (décision MOB-3a) : aucune conversion.
/// Le mapping riche Job/Mission/Beneficiary est porté par MOB-5 (liste) / MOB-6 (détail),
/// là où la forme cible (ClJob + adresses + enums transport) est réellement exercée.
/// </summary>
public static class ErpReferenceMappings
{
    /// <summary>ORD_VEHICLE (DTO) → ClVehicle. Le kilométrage vit en BD Mobile (MOB-10) : vide ici.</summary>
    public static ClVehicle ToMobileVehicle(this ClVehicleDtoOut dto)
    {
        return new ClVehicle(dto.Id, dto.RegistrationPlate, new ClKilometers());
    }

    /// <summary>PER_PERSONNEL (DTO détail) → ClEmployee.</summary>
    public static ClEmployee ToMobileEmployee(this ClPersonnelDtoOut dto)
    {
        return new ClEmployee(dto.Id, dto.FirstName, dto.LastName);
    }

    /// <summary>
    /// Membre de crew (DTO résumé) → ClEmployee. Le résumé ne porte qu'un FullName ;
    /// on le place en Name (LastName vide) — suffisant pour l'affichage équipage.
    /// </summary>
    public static ClEmployee ToMobileEmployee(this ClCrewMemberDtoOut dto)
    {
        return new ClEmployee(dto.PersonnelId, dto.FullName, string.Empty);
    }

    /// <summary>Conducteur courant (DTO) → ClLastDriver.</summary>
    public static ClLastDriver ToMobileLastDriver(this ClDriverAssignmentDtoOut dto)
    {
        return new ClLastDriver(new ClEmployee(dto.PersonnelId, dto.FullName, string.Empty), dto.StartedAt);
    }

    /// <summary>
    /// CRW_CREW (DTO détail) → ClCrew. Le véhicule est résolu en amont par le caller
    /// (appel séparé à IVehicleQueryService via <c>VehicleId</c>) car le DTO crew ne
    /// porte que l'id du véhicule, pas son détail.
    /// </summary>
    public static ClCrew ToMobileCrew(this ClCrewDtoOut dto, ClVehicle? vehicle)
    {
        var employees = dto.ActiveMembers.Select(m => m.ToMobileEmployee()).ToList();

        var crew = new ClCrew(dto.Id, employees, dto.VacationStart, vehicle)
        {
            HasVehicle = vehicle is not null
        };

        if (dto.CurrentDriver is not null)
            crew.SetLastDriver(dto.CurrentDriver.ToMobileLastDriver());

        return crew;
    }
}
