using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Mapping;

/// <summary>
/// MOB-4/MOB-11 — mapping du détail d'équipage ERP (<see cref="ErpCrewFullDto"/>) vers le
/// domaine (<see cref="ClCrew"/>). Membres → <see cref="ClEmployee"/>, véhicule → <see cref="ClVehicle"/>,
/// conducteur actif → <c>LastDriver</c> (résolu parmi les membres ; ignoré si absent de l'équipage).
/// </summary>
internal static class ErpCrewMappings
{
    public static ClCrew ToDomain(this ErpCrewFullDto dto)
    {
        var employees = dto.Members
            .Select(m => new ClEmployee(m.Id, m.FirstName ?? string.Empty, m.LastName ?? string.Empty))
            .ToList();

        // ClVehicle exige un ClKilometers ; on n'a pas besoin du kilométrage ici → instance vide.
        var vehicle = dto.Vehicle is not null
            ? new ClVehicle(dto.Vehicle.Id, dto.Vehicle.Immatriculation ?? string.Empty, new ClKilometers())
            : new ClVehicle(Guid.Empty, string.Empty, new ClKilometers());

        var crew = new ClCrew(dto.Id, employees, dto.ServiceStart, vehicle)
        {
            HasVehicle = dto.Vehicle is not null
        };

        // Conducteur actif : uniquement s'il fait partie des membres (cohérence).
        if (dto.ActiveDriver is not null)
        {
            var driver = employees.FirstOrDefault(e => e.Id == dto.ActiveDriver.PersonnelId);
            if (driver is not null)
                crew.SetLastDriver(driver, dto.ActiveDriver.From);
        }

        return crew;
    }
}
