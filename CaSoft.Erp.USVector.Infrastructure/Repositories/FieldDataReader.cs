using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories;

/// <summary>
/// TRF-6 — Assemble le paquet d'enrichissement terrain consolidé (<see cref="ClFieldEnrichmentDtoOut"/>)
/// à partir des silos de la base Vector et des données de référence d'Orders (mission → commande →
/// bénéficiaire, lues via <see cref="IErpReadApiClient"/>). Tiré par la facturation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Un seul chemin pour l'unité et le lot (B5, 19/09).</b> La facturation tirait un paquet par
/// mission ; Vector plafonnait vers 40 appels/s et pesait 12,8 s sur les 18,3 s de son acquisition
/// d'une journée. Le lot lit chaque silo en une requête (<see cref="IFieldDataQueryService"/>) et ne
/// demande chaque commande qu'une fois — l'aller et le retour la partagent.
/// </para>
/// <para>
/// <b>Ce qui reste par mission : l'appel à Orders</b> pour la mission (existence, commande). Orders
/// n'a pas de lecture par liste d'identifiants ; ces appels partent en parallèle, bornés à
/// <see cref="OrdersConcurrency"/> — la même charge que la facturation s'imposait déjà.
/// </para>
/// <para>
/// <b>Un échec n'emporte pas le lot.</b> Une mission dont la lecture échoue chez Orders sort en
/// <c>Error</c>, les autres en <c>Found</c> ou <c>NotFound</c>.
/// </para>
/// </remarks>
public sealed class FieldDataReader : IFieldDataReader
{
    /// <summary>Appels simultanés vers Orders pour un lot.</summary>
    public const int OrdersConcurrency = 8;

    private readonly IErpReadApiClient _erp;
    private readonly IFieldDataQueryService _silos;
    private readonly ILogger _logger;

    public FieldDataReader(IErpReadApiClient erp, IFieldDataQueryService silos,
        ILogger<FieldDataReader>? logger = null)
    {
        _erp = erp;
        _silos = silos;
        _logger = (ILogger?)logger ?? NullLogger.Instance;
    }

    public async Task<ClFieldEnrichmentDtoOut> GetAsync(Guid missionId, CancellationToken ct)
    {
        var item = (await GetManyAsync(new[] { missionId }, ct))[0];

        return item.Status switch
        {
            ClFieldEnrichmentBatchItemDtoOut.StatusFound => item.Data,
            ClFieldEnrichmentBatchItemDtoOut.StatusNotFound => null!,   // mission inconnue d'Orders → 404
            // À l'unité, une panne reste une panne (500), comme avant le lot.
            _ => throw new InvalidOperationException(item.Error)
        };
    }

    public async Task<IReadOnlyList<ClFieldEnrichmentBatchItemDtoOut>> GetManyAsync(
        IReadOnlyCollection<Guid> missionIds, CancellationToken ct)
    {
        var ids = (missionIds ?? Array.Empty<Guid>()).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<ClFieldEnrichmentBatchItemDtoOut>();

        // 1. Orders : la mission (existence, commande), puis chaque commande une seule fois.
        var missions = await ForEachBoundedAsync(ids, id => _erp.GetMissionFullAsync(id, ct), ct);

        var orderIds = missions.Values
            .Where(r => r.Error is null && r.Value is not null)
            .Select(r => r.Value!.OrderId)
            .Distinct()
            .ToList();
        var orders = await ForEachBoundedAsync(orderIds, id => _erp.GetOrderAsync(id, ct), ct);

        // 2. Base Vector : une requête par silo pour tout le lot.
        var found = ids.Where(id => missions[id].Error is null && missions[id].Value is not null).ToList();
        var beneficiaries = found
            .Select(id => orders.TryGetValue(missions[id].Value!.OrderId, out var o) ? o.Value?.Order?.BeneficiaryId : null)
            .Where(b => b.HasValue)
            .Select(b => b!.Value)
            .Distinct()
            .ToList();
        var silos = _silos.ReadSilos(found, beneficiaries);

        // 3. Une entrée par mission demandée, dans l'ordre de la demande.
        return ids.Select(id =>
        {
            var mission = missions[id];
            if (mission.Error is not null)
                return ClFieldEnrichmentBatchItemDtoOut.Failed(id, $"Lecture de la mission chez Orders impossible : {mission.Error.Message}");
            if (mission.Value is null)
                return ClFieldEnrichmentBatchItemDtoOut.NotFound(id);

            var order = orders[mission.Value.OrderId];
            if (order.Error is not null)
                return ClFieldEnrichmentBatchItemDtoOut.Failed(id, $"Lecture de la commande chez Orders impossible : {order.Error.Message}");

            return ClFieldEnrichmentBatchItemDtoOut.Found(
                Assemble(id, mission.Value.OrderId, order.Value?.Order?.BeneficiaryId, silos));
        }).ToList();
    }

    private static ClFieldEnrichmentDtoOut Assemble(Guid missionId, Guid orderId, Guid? beneficiaryId, ClFieldSilos silos)
    {
        silos.Timelines.TryGetValue(missionId, out var time);
        var timeline = new ClFieldTimelineDto
        {
            AckAt = time?.AckTime,
            ReadAt = time?.ReadTime,
            GoAt = time?.GoTime,
            OnsiteAt = time?.OnSiteTime,
            TerminateAt = time?.TerminateTime
        };

        // Signature : présence + horodatage ; les octets sont servis par api/Signature/{id}.
        DateTime? signedAt = silos.SignedAt.TryGetValue(missionId, out var at) ? at : null;
        var signature = new ClFieldSignatureDto
        {
            Exists = signedAt.HasValue,
            SignedAt = signedAt,
            ImageUrl = signedAt.HasValue ? $"api/Signature/{missionId}" : null
        };

        // Carte mutuelle courante du bénéficiaire (métadonnées : l'image est annoncée par son URL).
        ClMutuelleCardDtoOut? mutuelle = null;
        if (beneficiaryId.HasValue && silos.CurrentMutuelles.TryGetValue(beneficiaryId.Value, out var card))
            mutuelle = card.ToDtoOut();

        var documents = silos.Documents[missionId].Select(d => d.ToDtoOut()).ToList();
        var anomalies = silos.Anomalies[missionId].Select(a => a.ToDtoOut()).ToList();

        // Watermark global = max des horodatages présents.
        var stamps = new List<DateTime?>
        {
            time?.AckTime, time?.ReadTime, time?.GoTime, time?.OnSiteTime, time?.TerminateTime,
            signedAt, mutuelle?.CapturedAt
        };
        stamps.AddRange(documents.Select(d => (DateTime?)d.CapturedAt));
        stamps.AddRange(anomalies.Select(a => (DateTime?)a.ReportedAt));
        var present = stamps.Where(s => s.HasValue).Select(s => s!.Value).ToList();

        return new ClFieldEnrichmentDtoOut
        {
            MissionId = missionId,
            OrderId = orderId,
            SchemaVersion = 1,
            UpdatedAt = present.Count == 0 ? null : present.Max(),
            Timeline = timeline,
            Signature = signature,
            // OC-8 — toujours null : le magasin d'attributs Vector est retiré (2026-09-13). Les valeurs
            // en vigueur sont chez Order, où la facturation les lit ; elle tolère ce null.
            Attributes = null,
            Mutuelle = mutuelle,
            Kilometers = null,   // crew/véhicule-scoped (cf. TRF-9), surfacé séparément
            Documents = documents,
            Anomalies = anomalies
        };
    }

    /// <summary>Résultat d'une lecture chez Orders : la valeur, ou l'échec qui l'a empêchée.</summary>
    private sealed record Lecture<T>(T? Value, Exception? Error);

    /// <summary>
    /// Lit chaque clé chez Orders, <see cref="OrdersConcurrency"/> à la fois. Un échec est capturé
    /// pour sa clé, journalisé, et n'interrompt pas les autres ; une annulation, elle, remonte.
    /// </summary>
    private async Task<Dictionary<Guid, Lecture<T>>> ForEachBoundedAsync<T>(
        IReadOnlyCollection<Guid> keys, Func<Guid, Task<T?>> read, CancellationToken ct) where T : class
    {
        var results = new Dictionary<Guid, Lecture<T>>();
        if (keys.Count == 0) return results;

        using var gate = new SemaphoreSlim(OrdersConcurrency);
        var tasks = keys.Select(async key =>
        {
            await gate.WaitAsync(ct);
            try
            {
                return (key, new Lecture<T>(await read(key), null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "field-data en lot : lecture Orders impossible pour {Key}.", key);
                return (key, new Lecture<T>(null, ex));
            }
            finally
            {
                gate.Release();
            }
        }).ToList();

        foreach (var (key, lecture) in await Task.WhenAll(tasks))
            results[key] = lecture;

        return results;
    }
}
