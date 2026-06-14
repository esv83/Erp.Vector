using System.Text.Json;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// MOB-13 — Overlay des attributs de mission en BD Mobile (aucune écriture ERP).
///
/// <para><b>Lecture</b> : assemble le <see cref="ClContractType"/> de la mission = attributs
/// « core » + attributs du contrat sélectionné (contrat actif par défaut sinon), valeurs overlay
/// fusionnées. Chaque attribut porte son type de contrôle (<c>FieldType</c>) et, pour une liste de
/// choix, ses options. Pour les champs multi-valués (téléphones/e-mails), la valeur affichée =
/// baseline ERP ∪ items overlay (dédoublonnés).</para>
///
/// <para><b>Écriture</b> : upsert dans <c>MOB_JOB_ATTRIBUTE_VALUE</c>. Multi-valué : on ne stocke
/// que les items hors baseline ERP (on ne peut pas modifier ceux de l'ERP, doublons écartés).</para>
/// </summary>
public class JobAttributeOverlayRepository : IJobAttributeOverlay
{
    private static readonly StringComparer Cmp = StringComparer.OrdinalIgnoreCase;

    private readonly MobileDbContext _ctx;

    public JobAttributeOverlayRepository(MobileDbContext ctx) => _ctx = ctx;

    public ClContractType BuildContractType(
        Guid missionId, IDictionary<string, IEnumerable<string>> erpBaselines)
    {
        erpBaselines ??= new Dictionary<string, IEnumerable<string>>();

        int? contractId = _ctx.JobContracts
            .Where(c => c.JCT_MISSION_ID == missionId)
            .Select(c => (int?)c.JCT_CONTRACT_ID)
            .FirstOrDefault()
            ?? _ctx.ContractTypes
                .Where(t => t.CTT_ACTIVE)
                .OrderBy(t => t.CTT_ID)
                .Select(t => (int?)t.CTT_ID)
                .FirstOrDefault();

        var display = contractId is null
            ? string.Empty
            : _ctx.ContractTypes.Where(t => t.CTT_ID == contractId).Select(t => t.CTT_DISPLAY).FirstOrDefault() ?? string.Empty;

        // Attributs globaux (tous contrats) + attributs liés au contrat sélectionné, triés par index.
        var defs = _ctx.ContractAttributes
            .Where(a => a.CAT_IS_GLOBAL
                || _ctx.ContractAttributeContracts.Any(l =>
                       l.CAC_ATTRIBUTE_ID == a.CAT_ID && l.CAC_CONTRACT_ID == contractId))
            .OrderBy(a => a.CAT_INDEX)
            .ToList();

        var optionsByAttr = LoadOptions(defs.Select(d => d.CAT_ID).ToList());

        var overlay = _ctx.JobAttributeValues
            .Where(v => v.JAV_MISSION_ID == missionId)
            .ToDictionary(v => v.JAV_ATTRIBUTE_NAME, v => v.JAV_VALUE, Cmp);

        var collection = new ClAttributCollection();
        foreach (var def in defs)
        {
            overlay.TryGetValue(def.CAT_NAME, out var overlayValue);

            var attr = new ClContractAttribut(def.CAT_ID)
            {
                Name = def.CAT_NAME,
                Label = def.CAT_LABEL,
                FieldType = def.CAT_FIELD_TYPE,
                Index = def.CAT_INDEX,
                Required = def.CAT_REQUIRED,
                PlaceHolder = def.CAT_PLACEHOLDER,
                InstantUpdate = def.CAT_INSTANT_UPDATE,
                IsMulti = def.CAT_IS_MULTI,
                IsList = string.Equals(def.CAT_FIELD_TYPE, "list", StringComparison.OrdinalIgnoreCase),
            };

            if (optionsByAttr.TryGetValue(def.CAT_ID, out var opts))
                attr.Options = opts;

            if (def.CAT_IS_MULTI)
            {
                // Valeur affichée = baseline ERP (verrouillée) ∪ items overlay, dédoublonnés.
                var baseline = erpBaselines.TryGetValue(def.CAT_NAME, out var b) ? b : Enumerable.Empty<string>();
                var merged = Dedup(baseline.Concat(ParseJsonArray(overlayValue)));
                attr.Value = JsonSerializer.Serialize(merged);
            }
            else
            {
                attr.Value = overlayValue ?? string.Empty;
            }

            collection[def.CAT_NAME] = attr;
        }

        return new ClContractType(contractId ?? 0, display, collection);
    }

    public void Save(
        Guid missionId, ClContractType contractType, IDictionary<string, IEnumerable<string>> erpBaselines)
    {
        if (contractType?.Attributs is null) return;
        erpBaselines ??= new Dictionary<string, IEnumerable<string>>();

        var existing = _ctx.JobAttributeValues
            .Where(v => v.JAV_MISSION_ID == missionId)
            .ToDictionary(v => v.JAV_ATTRIBUTE_NAME, v => v, Cmp);

        foreach (var attr in contractType.Attributs.Values)
        {
            string? toStore;
            if (attr.IsMulti)
            {
                // On ne persiste que les items AJOUTÉS (hors baseline ERP), sans doublon.
                var baseline = erpBaselines.TryGetValue(attr.Name, out var b)
                    ? new HashSet<string>(b ?? Enumerable.Empty<string>(), Cmp)
                    : new HashSet<string>(Cmp);
                var added = Dedup(ParseJsonArray(attr.Value).Where(x => !baseline.Contains(x)));
                toStore = JsonSerializer.Serialize(added);
            }
            else
            {
                toStore = attr.Value;
            }

            if (existing.TryGetValue(attr.Name, out var row))
            {
                row.JAV_VALUE = toStore;
                row.JAV_UPDATED_AT = DateTime.UtcNow;
            }
            else
            {
                _ctx.JobAttributeValues.Add(new MOB_JOB_ATTRIBUTE_VALUE
                {
                    JAV_MISSION_ID = missionId,
                    JAV_ATTRIBUTE_NAME = attr.Name,
                    JAV_VALUE = toStore,
                    JAV_UPDATED_AT = DateTime.UtcNow,
                });
            }
        }

        _ctx.SaveChanges();
    }

    public IReadOnlyList<ClContractType> GetContracts()
        => _ctx.ContractTypes
            .Where(t => t.CTT_ACTIVE)
            .OrderBy(t => t.CTT_ID)
            .AsEnumerable()
            .Select(t => new ClContractType(t.CTT_ID, t.CTT_DISPLAY, new ClAttributCollection()))
            .ToList();

    public int? GetSelectedContractId(Guid missionId)
        => _ctx.JobContracts
            .Where(c => c.JCT_MISSION_ID == missionId)
            .Select(c => (int?)c.JCT_CONTRACT_ID)
            .FirstOrDefault();

    public void SelectContract(Guid missionId, int contractId)
    {
        var exists = _ctx.ContractTypes.Any(t => t.CTT_ID == contractId && t.CTT_ACTIVE);
        if (!exists)
            throw new InvalidOperationException($"Type de contrat {contractId} inconnu ou inactif.");

        var row = _ctx.JobContracts.SingleOrDefault(c => c.JCT_MISSION_ID == missionId);
        if (row is null)
        {
            _ctx.JobContracts.Add(new MOB_JOB_CONTRACT
            {
                JCT_MISSION_ID = missionId,
                JCT_CONTRACT_ID = contractId,
                JCT_UPDATED_AT = DateTime.UtcNow,
            });
        }
        else
        {
            row.JCT_CONTRACT_ID = contractId;
            row.JCT_UPDATED_AT = DateTime.UtcNow;
        }

        _ctx.SaveChanges();
    }

    private Dictionary<int, IDictionary<int, string>> LoadOptions(List<int> attributeIds)
    {
        if (attributeIds.Count == 0) return new();
        return _ctx.ContractAttributeOptions
            .Where(o => attributeIds.Contains(o.CAO_ATTRIBUTE_ID))
            .OrderBy(o => o.CAO_KEY)
            .AsEnumerable()
            .GroupBy(o => o.CAO_ATTRIBUTE_ID)
            .ToDictionary(
                g => g.Key,
                g => (IDictionary<int, string>)g.ToDictionary(x => x.CAO_KEY, x => x.CAO_LABEL));
    }

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            if (arr is not null) return arr;
        }
        catch (JsonException)
        {
            // Tolérance : une valeur simple non-JSON est traitée comme un item unique.
        }
        return new List<string> { json.Trim() };
    }

    private static List<string> Dedup(IEnumerable<string> items)
    {
        var seen = new HashSet<string>(Cmp);
        var result = new List<string>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            var trimmed = item.Trim();
            if (seen.Add(trimmed)) result.Add(trimmed);
        }
        return result;
    }
}
