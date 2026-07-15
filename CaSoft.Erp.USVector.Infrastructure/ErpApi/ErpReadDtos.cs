namespace CaSoft.Erp.USVector.Infrastructure.ErpApi;

// DTO miroir des réponses JSON d'Orders.Api (camelCase, enums en entiers).
// On ne mappe QUE les champs réellement consommés par les adaptateurs mobiles —
// les champs absents du JSON sont ignorés à la désérialisation (case-insensitive).

public sealed class ErpStageDto
{
    public string? LocationName { get; set; }
    public string? ServiceLabel { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Complement { get; set; }
    public string? Label { get; set; }

    /// <summary>Latitude WGS84 du lieu source. null tant que l'ERP n'a pas géocodé.</summary>
    public double? Latitude { get; set; }
    /// <summary>Longitude WGS84 du lieu source. null tant que l'ERP n'a pas géocodé.</summary>
    public double? Longitude { get; set; }
}

/// <summary>GET /missions/{id}/full</summary>
public sealed class ErpMissionFullDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public DateOnly MissionDate { get; set; }
    public TimeOnly SchedulingTime { get; set; }
    public TimeOnly? AppointmentTime { get; set; }
    public bool IsAsap { get; set; }
    public int DelayMaxInMinutes { get; set; }
    public Guid? AssignedCrewId { get; set; }
    public ErpStageDto? Pickup { get; set; }
    public ErpStageDto? Dropoff { get; set; }
    /// <summary>Commentaire opérationnel de la mission (ex. « C15 CHUTE MECA DLR LOMBAIRE »).</summary>
    public string? Comment { get; set; }
}

/// <summary>GET /missions (liste légère)</summary>
public sealed class ErpMissionListItemDto
{
    public Guid Id { get; set; }

    /// <summary>Statut mission Orders.Api : 1=Engagé/À faire, 2=EnCours, 3=Terminé, 4=Clôturé
    /// (visualStatus « Closed »). Le mobile affiche jusqu'à « Terminé » ; ≥ 4 (clôturé) est masqué (spec §14).</summary>
    public int Status { get; set; }
    public Guid? AssignedCrewId { get; set; }
    public string? BeneficiaryDisplayName { get; set; }
    public int TransportModeId { get; set; }
    public DateOnly MissionDate { get; set; }
    public TimeOnly SchedulingTime { get; set; }
    public TimeOnly? AppointmentTime { get; set; }
    public string? PickupLabel { get; set; }
    public string? DropoffLabel { get; set; }
}

/// <summary>GET /orders/{id} — enveloppe { order: { … } }</summary>
public sealed class ErpOrderEditDto
{
    public ErpOrderBodyDto? Order { get; set; }
}

public sealed class ErpOrderBodyDto
{
    public Guid BeneficiaryId { get; set; }
    public int TransportModeId { get; set; }
    /// <summary>Sous-catégorie de transport (secondaire) : null si absente. Label ex. « Bariatrique », « TPMR ».</summary>
    public int? TransportSubCategoryId { get; set; }
    public string? TransportSubCategoryLabel { get; set; }
    public bool HasReturn { get; set; }
    public int Frequency { get; set; }
}

/// <summary>GET /beneficiaries/{id}</summary>
public sealed class ErpBeneficiaryDetailDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Nir { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? PrimaryPhone { get; set; }
}

/// <summary>GET /crews (liste) — on ne lit que l'id.</summary>
public sealed class ErpCrewListItemDto
{
    public Guid Id { get; set; }
}

/// <summary>
/// GET /crews/{crewId} — détail complet d'un équipage (MOB-4/MOB-11) : membres (avec Id),
/// conducteur actif, véhicule, fenêtre de service. Endpoint additif côté Orders.Api.
/// </summary>
public sealed class ErpCrewFullDto
{
    public Guid Id { get; set; }
    public DateTime ServiceStart { get; set; }
    public DateTime? ServiceEnd { get; set; }
    public ErpCrewVehicleDto? Vehicle { get; set; }
    /// <summary>Conducteur actif, ou null si aucun n'a encore été désigné.</summary>
    public ErpCrewDriverDto? ActiveDriver { get; set; }
    public List<ErpCrewMemberDto> Members { get; set; } = new();
}

/// <summary>Membre d'équipage (personnel) — Id = PER_ID.</summary>
public sealed class ErpCrewMemberDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public sealed class ErpCrewVehicleDto
{
    public Guid Id { get; set; }
    public string? Immatriculation { get; set; }
}

/// <summary>Conducteur actif d'un équipage : le personnel désigné et depuis quand.</summary>
public sealed class ErpCrewDriverDto
{
    public Guid PersonnelId { get; set; }
    public DateTime From { get; set; }
}

/// <summary>GET /missions/{id} — on ne lit que le statut de transfert (gel terrain, TRF-7).</summary>
public sealed class ErpMissionStatusDto
{
    public Guid Id { get; set; }
    public int TransferStatus { get; set; }
}
