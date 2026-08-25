using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// OC-5 — Adapte les attributs de mission servis par Order au contrat mobile.
///
/// <para><b>Une traduction, rien d'autre.</b> Le DTO d'Order est le miroir du modèle mobile : ce
/// service recopie champ pour champ et ajoute les deux qui manquaient (le verrou par champ et son
/// motif). Aucune règle métier ici — la validation du NIR, le refus d'une date future, le partage
/// de la case PMT entre l'aller et le retour vivent côté Order, là où la donnée vit.</para>
///
/// <para><b>L'écriture renvoie le formulaire entier, sans trier.</b> C'est délibéré : Order ignore
/// une valeur reposée à l'identique, y compris sur un champ verrouillé, et ne refuse que les
/// <i>modifications</i>. Trier côté Vector nous obligerait à dupliquer la connaissance du verrou —
/// et à la voir dériver le jour où Order en change la règle.</para>
/// </summary>
public sealed class ContextOrderAttributeService : IContextOrderAttributeService
{
    private readonly IErpReadApiClient _read;
    private readonly IErpWriteApiClient _write;

    public ContextOrderAttributeService(IErpReadApiClient read, IErpWriteApiClient write)
    {
        _read = read;
        _write = write;
    }

    public async Task<List<ClMobileAppFieldModel>?> GetFormStructureAsync(Guid missionId, CancellationToken ct)
    {
        var fields = await _read.GetContextOrderFormStructureAsync(missionId, ct);
        if (fields is null) return null;

        return fields
            .OrderBy(f => f.Index)
            .Select(f => new ClMobileAppFieldModel
            {
                Name = f.Name,
                Label = f.Label,
                Index = f.Index,
                Type = f.Type,
                Required = f.Required,
                InstantUpdate = f.InstantUpdate,
                PlaceHolder = f.PlaceHolder,
                IsMulti = f.IsMulti,
                // Le modèle mobile porte les options en Object et ne les sert que pour une liste ;
                // on garde cette convention pour ne pas faire apparaître un « options: {} » là où le
                // front n'en attendait jamais.
                Options = IsList(f.Type) ? ToMobileOptions(f.Options) : null,
                Value = f.Value,
                IsReadOnly = f.IsReadOnly,
                ReadOnlyReason = f.ReadOnlyReason
            })
            .ToList();
    }

    public async Task<EnContextOrderValuesOutcome> SaveValuesAsync(
        Guid missionId, List<ClAttributValueModel> values, string? setBy, CancellationToken ct)
    {
        var payload = (values ?? new List<ClAttributValueModel>())
            .Where(v => !string.IsNullOrWhiteSpace(v.AttributName))
            .Select(v => (Name: v.AttributName, Value: v.AttributValue?.ToString()))
            .ToList();

        // Lot vide : rien à écrire, et surtout pas un aller-retour réseau pour le dire.
        if (payload.Count == 0) return EnContextOrderValuesOutcome.Applied;

        var outcome = await _write.SetContextOrderValuesAsync(missionId, payload, setBy, ct);

        return outcome switch
        {
            EnContextOrderValuesWriteOutcome.Applied => EnContextOrderValuesOutcome.Applied,
            EnContextOrderValuesWriteOutcome.FieldLocked => EnContextOrderValuesOutcome.FieldLocked,
            EnContextOrderValuesWriteOutcome.Invalid => EnContextOrderValuesOutcome.Invalid,
            EnContextOrderValuesWriteOutcome.MissionNotFound => EnContextOrderValuesOutcome.MissionNotFound,
            _ => EnContextOrderValuesOutcome.Invalid
        };
    }

    private static bool IsList(string? type)
        => string.Equals(type, "list", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Order sert les options en tableau ordonné <c>[{ key, label }]</c> ; le contrat mobile les
    /// attend en objet <c>{ "0": "Non", "1": "Oui" }</c> — c'est la forme que le front parse depuis
    /// toujours, et la bascule ne doit pas la changer (D14).
    /// <para>
    /// L'ordre de réception est conservé : <c>System.Text.Json</c> sérialise un dictionnaire dans
    /// l'ordre d'insertion, et cet ordre est celui qu'Order a voulu. Une clé en double est écartée
    /// plutôt que de faire échouer le formulaire entier pour un doublon de catalogue.
    /// </para>
    /// </summary>
    private static Dictionary<int, string>? ToMobileOptions(List<ErpContextOrderOptionDto>? options)
    {
        if (options is null || options.Count == 0) return null;

        var mapped = new Dictionary<int, string>(options.Count);
        foreach (var option in options)
            mapped[option.Key] = option.Label ?? string.Empty;

        return mapped;
    }
}
