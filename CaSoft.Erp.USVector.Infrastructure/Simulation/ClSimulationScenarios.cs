using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Simulation;

/// <summary>Une mission du jeu de formation, et le cas d'écran qu'elle sert à montrer.</summary>
public sealed record ClSimulationScenario(
    string Purpose,
    int Status,
    int TransportModeId,
    int Kind,
    TimeOnly SchedulingTime,
    TimeOnly? AppointmentTime,
    bool IsAsap,
    int DelayMaxInMinutes,
    ErpStageDto Pickup,
    ErpStageDto Dropoff,
    string? Comment,
    ClSimulationPatient? Patient,
    ErpBeneficiaryIdentificationDto? Identification,
    int? TransportSubCategoryId,
    string? TransportSubCategoryLabel,
    int? ImposedContextOrderId,
    int TransferStatus,
    bool IsBirthDateLocked);

public sealed record ClSimulationPatient(string FirstName, string LastName, string Nir, DateOnly BirthDate, string Phone);

/// <summary>
/// Le jeu de missions servi chaque jour à chaque équipage de formation. Un cas d'écran par mission : la
/// liste se lit comme une check-list de ce que l'app doit savoir afficher. Patients et lieux sont
/// fictifs ; les noms portent « (FICTIF) » pour qu'aucune capture ne passe pour un vrai dossier.
/// <para>Ajouter un cas : l'ajouter EN FIN de liste — l'emplacement fait partie de l'identifiant.</para>
/// </summary>
public static class ClSimulationScenarios
{
    public const int Ambulance = 1, Vsl = 2, ProduitsSanguins = 4;
    public const int Aller = 1, Retour = 2;
    public const int ToDo = 1, InProgress = 2, Done = 3;

    public static readonly IReadOnlyList<ErpContextOrderChoiceDto> ContextOrders = new[]
    {
        new ErpContextOrderChoiceDto { Id = 9001, Code = "CPAM", Display = "Assurance maladie", Index = 1 },
        new ErpContextOrderChoiceDto { Id = 9002, Code = "MUTUELLE", Display = "Mutuelle", Index = 2 },
        new ErpContextOrderChoiceDto { Id = 9003, Code = "PRIVE", Display = "Particulier", Index = 3 },
    };

    private static ErpStageDto Place(string name, string? service, string line1, string postalCode, string city,
        double? latitude, double? longitude, string? complement = null) => new()
    {
        LocationName = name, ServiceLabel = service, AddressLine1 = line1, PostalCode = postalCode, City = city,
        Complement = complement, Latitude = latitude, Longitude = longitude,
        Label = $"{name} — {line1}, {postalCode} {city}"
    };

    private static readonly ErpStageDto Domicile1 = Place("Domicile", null, "12 rue des Lauriers", "83000", "Toulon", 43.1242, 5.9280, "Digicode 4512, 3e étage sans ascenseur");
    private static readonly ErpStageDto Domicile2 = Place("Domicile", null, "5 avenue des Pins", "83200", "Toulon", 43.1290, 5.8990);
    private static readonly ErpStageDto Domicile3 = Place("Domicile", null, "48 chemin du Collet", "83130", "La Garde", 43.1250, 6.0110);
    private static readonly ErpStageDto Hopital = Place("Centre hospitalier (fictif)", "Cardiologie", "1 boulevard de la Santé", "83100", "Toulon", 43.1330, 5.9520);
    private static readonly ErpStageDto Clinique = Place("Clinique du Littoral (fictive)", "Dialyse", "20 rue du Port", "83500", "La Seyne-sur-Mer", 43.1010, 5.8820);
    private static readonly ErpStageDto Cabinet = Place("Cabinet de radiologie (fictif)", null, "7 place de la Liberté", "83000", "Toulon", 43.1255, 5.9300);
    private static readonly ErpStageDto Efs = Place("Site de transfusion (fictif)", "Délivrance", "3 rue de l'Hôpital", "83000", "Toulon", 43.1270, 5.9330);

    /// <summary>Lieu ni structuré ni géocodé : seul le libellé figé est connu (repli d'affichage, pas de carte).</summary>
    private static readonly ErpStageDto NonStructure = new() { Label = "Parking du stade, entrée nord, près du kiosque" };

    public static readonly IReadOnlyList<ClSimulationScenario> All = new[]
    {
        new ClSimulationScenario("Consultation programmée avec heure de rendez-vous, context à choisir",
            ToDo, Ambulance, Aller, new TimeOnly(8, 0), new TimeOnly(8, 45), false, 15, Domicile1, Hopital,
            "Consultation cardiologie, patient sous oxygène", new("Jeanne", "DUPONT (FICTIF)", "247058300000199", new DateOnly(1947, 5, 12), "0600000001"),
            null, null, null, null, 0, false),

        new ClSimulationScenario("Urgence au plus tôt, commentaire long, patient anonyme",
            ToDo, Vsl, Aller, new TimeOnly(9, 0), null, true, 30, NonStructure, Hopital,
            "C15 CHUTE MECA DLR LOMBAIRE — patient retrouvé au sol par un voisin, conscient, douleur lombaire importante, "
            + "ne peut pas se relever seul. Prévoir le matelas coquille. Le voisin attend sur place pour ouvrir.",
            null, new ErpBeneficiaryIdentificationDto { State = 1, Description = "Homme, 70 ans environ, veste bleue", IdentityEditableByField = true },
            null, null, null, 0, false),

        new ClSimulationScenario("Mission en cours, retour à domicile, sous-catégorie TPMR, context imposé par la régulation",
            InProgress, Vsl, Retour, new TimeOnly(10, 30), null, false, 20, Hopital, Domicile2,
            null, new("Marcel", "BERNARD (FICTIF)", "138028300000299", new DateOnly(1938, 2, 3), "0600000002"),
            null, 3, "TPMR", 9001, 0, false),

        new ClSimulationScenario("Dialyse, patient à vérifier, arrivée non géocodée",
            ToDo, Vsl, Aller, new TimeOnly(12, 15), new TimeOnly(13, 0), false, 15, Domicile3, Clinique,
            "Dialyse le mardi, jeudi et samedi", new("Paul", "MOREAU (FICTIF)", "155048300000399", new DateOnly(1955, 4, 21), "0600000003"),
            new ErpBeneficiaryIdentificationDto { State = 2, Description = null, IdentityEditableByField = true },
            null, null, null, 0, false) with { Dropoff = Place("Clinique du Littoral (fictive)", "Dialyse", "20 rue du Port", "83500", "La Seyne-sur-Mer", null, null) },

        new ClSimulationScenario("Bariatrique, date de naissance figée par la régulation, motif « Autre » à préciser",
            ToDo, Ambulance, Aller, new TimeOnly(14, 0), new TimeOnly(14, 30), false, 15, Domicile2, Cabinet,
            "Brancard bariatrique et deux équipiers", new("Sylvie", "LEROY (FICTIF)", "262078300000499", new DateOnly(1962, 7, 30), "0600000004"),
            null, 1, "Bariatrique", null, 0, true),

        new ClSimulationScenario("Mission terminée et déjà transférée en facturation : gelée",
            Done, Ambulance, Retour, new TimeOnly(7, 0), null, false, 15, Clinique, Domicile1,
            null, new("Louise", "FOURNIER (FICTIF)", "241098300000599", new DateOnly(1941, 9, 9), "0600000005"),
            null, null, null, null, 2, false),

        new ClSimulationScenario("Transport de produits sanguins : aucun patient",
            ToDo, ProduitsSanguins, Aller, new TimeOnly(16, 0), null, false, 10, Efs, Hopital,
            "2 poches CGR, glacière fournie", null, null, null, null, null, 0, false),
    };

    public static ClSimulationScenario? At(int slot) => slot >= 0 && slot < All.Count ? All[slot] : null;
}
