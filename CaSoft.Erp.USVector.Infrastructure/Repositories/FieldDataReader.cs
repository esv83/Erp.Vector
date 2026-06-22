using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories;

/// <summary>
/// TRF-6 — Assemble le paquet d'enrichissement terrain consolidé (<see cref="ClFieldEnrichmentDtoOut"/>)
/// à partir des silos BD Mobile + des données de référence ERP (mission → commande → bénéficiaire,
/// lues via <see cref="IErpReadApiClient"/>). Tiré par Certification au transfert en facturation.
/// </summary>
public sealed class FieldDataReader : IFieldDataReader
{
    private readonly IErpReadApiClient _erp;
    private readonly IJobTimeRepository _jobTime;
    private readonly ISignatureRepository _signature;
    private readonly IJobAttributeOverlay _overlay;
    private readonly IMutuelleCardRepository _mutuelle;
    private readonly IDocumentRepository _documents;
    private readonly IAnomalyRepository _anomalies;

    public FieldDataReader(
        IErpReadApiClient erp,
        IJobTimeRepository jobTime,
        ISignatureRepository signature,
        IJobAttributeOverlay overlay,
        IMutuelleCardRepository mutuelle,
        IDocumentRepository documents,
        IAnomalyRepository anomalies)
    {
        _erp = erp;
        _jobTime = jobTime;
        _signature = signature;
        _overlay = overlay;
        _mutuelle = mutuelle;
        _documents = documents;
        _anomalies = anomalies;
    }

    public async Task<ClFieldEnrichmentDtoOut> GetAsync(Guid missionId, CancellationToken ct)
    {
        var full = await _erp.GetMissionFullAsync(missionId, ct);
        if (full is null) return null!;   // mission introuvable côté ERP

        // Bénéficiaire via la commande parente (pour rattacher la carte mutuelle).
        Guid? beneficiaryId = null;
        var order = await _erp.GetOrderAsync(full.OrderId, ct);
        if (order?.Order is not null) beneficiaryId = order.Order.BeneficiaryId;

        // Timeline opérationnelle (BD Mobile).
        var time = _jobTime.GetJobTimeData(missionId);
        var timeline = new ClFieldTimelineDto
        {
            AckAt = time?.AckTime,
            ReadAt = time?.ReadTime,
            GoAt = time?.GoTime,
            OnsiteAt = time?.OnSiteTime,
            TerminateAt = time?.TerminateTime
        };

        // Signature (présence + horodatage ; binaire servi par api/Signature/{id}).
        var sigExists = _signature.Exists(missionId);
        DateTime? signedAt = sigExists ? _signature.Fetch(missionId)?.DateTime : null;
        var signature = new ClFieldSignatureDto
        {
            Exists = sigExists,
            SignedAt = signedAt,
            ImageUrl = sigExists ? $"api/Signature/{missionId}" : null
        };

        // Attributs de facturation dynamiques (overlay MOB-13, valeurs terrain seules).
        var contract = _overlay.BuildContractType(missionId, new Dictionary<string, IEnumerable<string>>());
        var values = contract?.Attributs?.Values
            .Where(a => !string.IsNullOrEmpty(a.Value))
            .Select(a => new ClFieldAttributeValueDto { Name = a.Name, Value = a.Value })
            .ToList() ?? new List<ClFieldAttributeValueDto>();
        var attributes = new ClFieldAttributesDto
        {
            ContractId = contract?.Id ?? 0,
            ContractDisplay = contract?.Display,
            Values = values
        };

        // Carte mutuelle courante du bénéficiaire.
        ClMutuelleCardDtoOut? mutuelle = null;
        if (beneficiaryId.HasValue)
            mutuelle = _mutuelle.GetCurrent(beneficiaryId.Value)?.ToDtoOut();

        // Documents + anomalies (mission-scoped).
        var documents = _documents.ListByMission(missionId).Select(d => d.ToDtoOut()).ToList();
        var anomalies = _anomalies.ListByMission(missionId).Select(a => a.ToDtoOut()).ToList();

        // Watermark global = max des horodatages présents.
        var stamps = new List<DateTime?>
        {
            time?.AckTime, time?.ReadTime, time?.GoTime, time?.OnSiteTime, time?.TerminateTime,
            signedAt, mutuelle?.CapturedAt
        };
        stamps.AddRange(documents.Select(d => (DateTime?)d.CapturedAt));
        stamps.AddRange(anomalies.Select(a => (DateTime?)a.ReportedAt));
        var present = stamps.Where(s => s.HasValue).Select(s => s!.Value).ToList();
        DateTime? updatedAt = present.Count == 0 ? null : present.Max();

        return new ClFieldEnrichmentDtoOut
        {
            MissionId = missionId,
            OrderId = full.OrderId,
            SchemaVersion = 1,
            UpdatedAt = updatedAt,
            Timeline = timeline,
            Signature = signature,
            Attributes = attributes,
            Mutuelle = mutuelle,
            Kilometers = null,   // crew/véhicule-scoped (cf. TRF-9), surfacé séparément
            Documents = documents,
            Anomalies = anomalies
        };
    }
}
