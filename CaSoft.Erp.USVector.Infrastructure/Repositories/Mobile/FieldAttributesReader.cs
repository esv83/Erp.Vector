using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;

/// <summary>
/// OC-7 — Compose le bloc <c>attributes</c> du paquet terrain à partir du seul magasin Vector.
///
/// <para><b>Le comportement est celui d'avant OC-3b, à l'identique.</b> C'est délibéré : la
/// composition reste déléguée à l'overlay, mais à une instance <b>privée de résolveur de type</b> —
/// montée comme telle à la racine de composition. Recopier la logique ici aurait produit deux
/// versions d'une même règle, condamnées à diverger ; l'injecter sans résolveur garantit la parité
/// sans duplication.</para>
///
/// <para>Conséquence directe : le paquet ne fait <b>aucun appel réseau</b> pour ses attributs, que
/// les drapeaux de bascule soient armés ou non. Armer la bascule ne ralentit pas la facturation.</para>
/// </summary>
public sealed class FieldAttributesReader : IFieldAttributesReader
{
    private static readonly Dictionary<string, IEnumerable<string>> NoBaseline = new();

    private readonly IJobAttributeOverlay _storeOnlyOverlay;

    /// <param name="storeOnlyOverlay">
    /// Overlay <b>sans résolveur de type</b> : il lit <c>MOB_JOB_CONTRACT</c> et le catalogue local,
    /// sans jamais interroger Orders.Api. La garantie est posée au câblage, pas ici.
    /// </param>
    public FieldAttributesReader(IJobAttributeOverlay storeOnlyOverlay)
        => _storeOnlyOverlay = storeOnlyOverlay;

    public ClFieldAttributesDto Read(Guid missionId)
    {
        // Pas de baseline ERP : le paquet transporte ce que le terrain a saisi, pas les coordonnées
        // que la facturation lit déjà dans la fiche du bénéficiaire.
        var contract = _storeOnlyOverlay.BuildContractType(missionId, NoBaseline);

        var values = contract?.Attributs?.Values
            .Where(a => !string.IsNullOrEmpty(a.Value))
            .Select(a => new ClFieldAttributeValueDto { Name = a.Name, Value = a.Value })
            .ToList() ?? new List<ClFieldAttributeValueDto>();

        return new ClFieldAttributesDto
        {
            // ⚠️ Identifiant du catalogue Vector, et lu par personne aujourd'hui — ni la facturation
            // ni la certification ne s'en servent. On le sert tel quel plutôt que de le faire changer
            // d'espace d'identifiants au gré d'un drapeau : un id qui veut dire autre chose selon la
            // configuration est un piège qu'aucun consommateur ne verrait venir.
            ContractId = contract?.Id ?? 0,
            ContractDisplay = contract?.Display,
            Values = values
        };
    }
}
