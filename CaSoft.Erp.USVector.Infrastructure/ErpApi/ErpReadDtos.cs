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
}

/// <summary>GET /missions (liste légère)</summary>
public sealed class ErpMissionListItemDto
{
    public Guid Id { get; set; }
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
