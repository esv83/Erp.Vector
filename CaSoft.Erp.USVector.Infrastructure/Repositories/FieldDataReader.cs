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
    private readonly IFieldAttributesReader _attributes;
    private readonly IMutuelleCardRepository _mutuelle;
    private readonly IDocumentRepository _documents;
    private readonly IAnomalyRepository _anomalies;

    public FieldDataReader(
        IErpReadApiClient erp,
        IJobTimeRepository jobTime,
        ISignatureRepository signature,
        IFieldAttributesReader attributes,
        IMutuelleCardRepository mutuelle,
        IDocumentRepository documents,
        IAnomalyRepository anomalies)
    {
        _erp = erp;
        _jobTime = jobTime;
        _signature = signature;
        _attributes = attributes;
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

        // Attributs de facturation dynamiques — OC-7 : lus dans le seul magasin Vector, sans appel
        // réseau. Depuis la bascule du référentiel, la facturation lit ces valeurs directement chez
        // Order et les fait primer ; ce bloc ne sert plus qu'à combler les trous pour les missions
        // saisies avant. Les faire transiter par ici en interrogeant Order serait un troisième chemin
        // vers la même donnée, payé deux appels par mission sur un traitement déjà lent.
        var attributes = _attributes.Read(missionId);

        // Carte mutuelle courante du bénéficiaire.
        ClMutuelleCardDtoOut? mutuelle = null;
        if (beneficiaryId.HasValue)
            // Métadonnées seules : le paquet annonce l'image par son URL, il ne la transporte pas
            // (D8, l'aval tire les octets). Lire le binaire ici l'aurait sorti de la base une fois
            // par mission, pour rien.
            mutuelle = _mutuelle.GetCurrentMetadata(beneficiaryId.Value)?.ToDtoOut();

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
