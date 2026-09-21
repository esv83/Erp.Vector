using System.Reflection;
using System.Text.RegularExpressions;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Schema;

/// <summary>
/// G4 — Les scripts de schéma que la base Vector <b>doit</b> porter, lus dans l'assemblage lui-même.
/// </summary>
/// <remarks>
/// <para>
/// <b>Le catalogue n'est pas une liste écrite à la main</b> : il se déduit des fichiers embarqués
/// (<c>Sql\MOB_*.sql</c>). Un script ajouté demain est attendu sans que personne y pense — c'est
/// exactement ce qui a manqué le 06/08, quand prod et dev ont divergé en sens inverse.
/// </para>
/// <para>
/// <b>Deux scripts ne sont plus attendus</b>, et c'est un choix : <c>MOB_002</c> et <c>MOB_007</c>
/// posaient l'overlay d'attributs, que <c>MOB_008</c> a supprimé. Rien dans la base ne peut plus
/// témoigner de leur passage, et réclamer un script dont l'effet a été défait ne ferait qu'un signal
/// faux de plus. Leur histoire vit dans <c>delivered.md</c> §5.1.
/// </para>
/// </remarks>
public static class SchemaCatalogue
{
    /// <summary>Scripts annulés par un script ultérieur : hors du catalogue, avec leur raison.</summary>
    public static readonly IReadOnlyDictionary<string, string> Annules = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["MOB_002_JobAttributes"] = "overlay d'attributs, supprimé par MOB_008",
        ["MOB_007_FixArt80Display"] = "libellé d'un catalogue supprimé par MOB_008"
    };

    private static readonly Regex Motif = new(
        @"\.Sql\.(?<id>MOB_(?<numero>\d{3})_[^.]+)\.sql$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Assembly Assemblage = typeof(SchemaCatalogue).Assembly;

    /// <summary>Identifiants attendus, par ordre de numéro (<c>MOB_001_Initial</c>, …).</summary>
    public static IReadOnlyList<string> Attendus { get; } = Charger();

    private static IReadOnlyList<string> Charger()
    {
        var ids = Assemblage.GetManifestResourceNames()
            .Select(nom => Motif.Match(nom))
            .Where(m => m.Success)
            .Select(m => m.Groups["id"].Value)
            .Where(id => !Annules.ContainsKey(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            // Un catalogue vide déclarerait toute base à jour, y compris une base vide : le pire des
            // verdicts, puisqu'il rassure. Mieux vaut échouer ici, au premier appel.
            throw new InvalidOperationException(
                $"Aucun script de schéma embarqué dans {Assemblage.GetName().Name}. "
                + "Vérifier l'ItemGroup EmbeddedResource \"Sql\\*.sql\" du csproj.");
        }

        return ids;
    }
}
