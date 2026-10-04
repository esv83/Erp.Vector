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
/// <b>Orders aussi, par lot (04/10)</b> : <c>POST /missions/batch-refs</c> rend mission → commande →
/// bénéficiaire pour 200 missions en un appel, au lieu de 200 <c>/full</c> — sa lecture la plus chère —
/// puis une par commande. Tant qu'Orders ne la porte pas en production (avant 1.8.5), le lot se
/// <b>replie</b> sur ces appels unitaires, en parallèle bornés à <see cref="OrdersConcurrency"/> :
/// Vector se publie donc avant ou après Orders, indifféremment.
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

    /// <summary>Plafond de <c>POST /missions/batch-refs</c> chez Orders (contrat du 21/09).</summary>
    public const int OrdersBatchSize = 200;

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

        // 1. Orders : mission → commande → bénéficiaire. Par lot quand Orders porte batch-refs ; sinon
        //    mission par mission — repli à retirer une fois la 1.8.5 d'Orders constatée en production.
        var refs = await ReadRefsByBatchAsync(ids, ct) ?? await ReadRefsOneByOneAsync(ids, ct);

        // 2. Base Vector : une requête par silo pour tout le lot.
        var found = ids.Where(id => refs[id].Error is null && refs[id].Value is not null).ToList();
        var beneficiaries = found
            .Select(id => refs[id].Value!.BeneficiaryId)
            .Where(b => b.HasValue)
            .Select(b => b!.Value)
            .Distinct()
            .ToList();
        var silos = _silos.ReadSilos(found, beneficiaries);

        // 3. Une entrée par mission demandée, dans l'ordre de la demande.
        return ids.Select(id =>
        {
            var lecture = refs[id];
            if (lecture.Error is not null)
                return ClFieldEnrichmentBatchItemDtoOut.Failed(id, lecture.Error);
            if (lecture.Value is null)
                return ClFieldEnrichmentBatchItemDtoOut.NotFound(id);

            return ClFieldEnrichmentBatchItemDtoOut.Found(
                Assemble(id, lecture.Value.OrderId, lecture.Value.BeneficiaryId, silos));
        }).ToList();
    }

    /// <summary>Ce que le paquet demande à Orders : la commande, et son bénéficiaire.</summary>
    private sealed record Refs(Guid OrderId, Guid? BeneficiaryId);

    /// <summary>
    /// Références d'une mission. <c>Value</c> et <c>Error</c> nuls = mission inconnue d'Orders ;
    /// <c>Error</c> = le motif, prêt pour l'entrée <c>Error</c> du lot.
    /// </summary>
    private sealed record RefsLecture(Refs? Value, string? Error);

    /// <summary>
    /// <c>POST /missions/batch-refs</c>, par tranches de <see cref="OrdersBatchSize"/>. <c>null</c> si
    /// Orders ne porte pas la route : tout le lot passe alors par le repli. Une panne n'est PAS un
    /// repli — relancer 200 lectures unitaires sur un Orders à terre ne le relèverait pas ; la tranche
    /// sort en <c>Error</c>, que la facturation retente.
    /// </summary>
    private async Task<Dictionary<Guid, RefsLecture>?> ReadRefsByBatchAsync(List<Guid> ids, CancellationToken ct)
    {
        var result = new Dictionary<Guid, RefsLecture>();

        foreach (var tranche in ids.Chunk(OrdersBatchSize))
        {
            IReadOnlyList<ErpMissionBatchRefDto>? rows;
            try
            {
                rows = await _erp.GetMissionBatchRefsAsync(tranche, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "field-data en lot : batch-refs impossible chez Orders ({Count} missions).", tranche.Length);
                foreach (var id in tranche)
                    result[id] = new RefsLecture(null, $"Lecture des références chez Orders impossible : {ex.Message}");
                continue;
            }

            if (rows is null)
            {
                _logger.LogInformation("field-data en lot : Orders ne porte pas batch-refs, lecture mission par mission.");
                return null;
            }

            // Une mission absente de la réponse est inconnue d'Orders : c'est le contrat.
            var parMission = rows.ToDictionary(r => r.MissionId);
            foreach (var id in tranche)
                result[id] = parMission.TryGetValue(id, out var r)
                    ? new RefsLecture(new Refs(r.OrderId, r.BeneficiaryId), null)
                    : new RefsLecture(null, null);
        }

        return result;
    }

    /// <summary>
    /// Repli : <c>/missions/{id}/full</c> pour chaque mission, puis chaque commande une seule fois —
    /// l'aller et le retour la partagent. Le chemin d'avant batch-refs, inchangé.
    /// </summary>
    private async Task<Dictionary<Guid, RefsLecture>> ReadRefsOneByOneAsync(List<Guid> ids, CancellationToken ct)
    {
        var missions = await ForEachBoundedAsync(ids, id => _erp.GetMissionFullAsync(id, ct), ct);

        var orderIds = missions.Values
            .Where(r => r.Error is null && r.Value is not null)
            .Select(r => r.Value!.OrderId)
            .Distinct()
            .ToList();
        var orders = await ForEachBoundedAsync(orderIds, id => _erp.GetOrderAsync(id, ct), ct);

        return ids.ToDictionary(id => id, id =>
        {
            var mission = missions[id];
            if (mission.Error is not null)
                return new RefsLecture(null, $"Lecture de la mission chez Orders impossible : {mission.Error.Message}");
            if (mission.Value is null)
                return new RefsLecture(null, null);

            var order = orders[mission.Value.OrderId];
            if (order.Error is not null)
                return new RefsLecture(null, $"Lecture de la commande chez Orders impossible : {order.Error.Message}");

            return new RefsLecture(new Refs(mission.Value.OrderId, order.Value?.Order?.BeneficiaryId), null);
        });
    }

    private static ClFieldEnrichmentDtoOut Assemble(Guid missionId, Guid orderId, Guid? beneficiaryId, ClFieldSilos silos)
    {
        silos.Timelines.TryGetValue(missionId, out var time);
        var timeline = new ClFieldTimelineDtoOut
        {
            AckAt = time?.AckTime,
            ReadAt = time?.ReadTime,
            GoAt = time?.GoTime,
            OnsiteAt = time?.OnSiteTime,
            TerminateAt = time?.TerminateTime
        };

        // Signature : présence + horodatage ; les octets sont servis par api/Signature/{id}.
        DateTime? signedAt = silos.SignedAt.TryGetValue(missionId, out var at) ? at : null;
        var signature = new ClFieldSignatureDtoOut
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
