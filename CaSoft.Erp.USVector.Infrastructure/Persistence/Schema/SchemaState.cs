namespace CaSoft.Erp.USVector.Infrastructure.Persistence.Schema;

/// <summary>G4 — Ce que la base répond quand on lui demande quels scripts elle porte.</summary>
public sealed class SchemaState
{
    private SchemaState() { }

    /// <summary>Faux quand la base n'a pas répondu : le schéma n'est alors ni bon ni mauvais, il est inconnu.</summary>
    public bool Reachable { get; private init; }

    /// <summary>Message brut de SQL Server. <b>Journalisé, jamais servi</b> : il nomme le compte de connexion.</summary>
    public string? Error { get; private init; }

    /// <summary>Faux quand `__VectorSchema` est absente : base jamais initialisée, ou montée hors des scripts.</summary>
    public bool JournalPresent { get; private init; }

    /// <summary>Scripts inscrits dans la base, du plus ancien au plus récent.</summary>
    public IReadOnlyList<AppliedScript> Applied { get; private init; } = Array.Empty<AppliedScript>();

    /// <summary>Attendus par le catalogue, absents de la base.</summary>
    public IReadOnlyList<string> Missing { get; private init; } = Array.Empty<string>();

    /// <summary>Inscrits dans la base, inconnus du catalogue — la base est en avance sur le dépôt.</summary>
    public IReadOnlyList<string> Unknown { get; private init; } = Array.Empty<string>();

    public bool UpToDate => Reachable && JournalPresent && Missing.Count == 0 && Unknown.Count == 0;

    /// <summary>Le dernier script appliqué, ou vide.</summary>
    public string LastApplied
        => Applied.Select(s => s.ScriptId).OrderBy(id => id, StringComparer.Ordinal).LastOrDefault() ?? string.Empty;

    /// <summary>Une phrase, celle du journal comme celle de la route — sans jamais de détail SQL.</summary>
    public string Statement()
    {
        if (!Reachable) return "Schéma : base injoignable — détail au journal d'application.";

        if (!JournalPresent)
        {
            return $"Schéma : la table __VectorSchema est ABSENTE — base jamais initialisée, ou montée "
                 + $"hors des scripts. Attendus : {SchemaCatalogue.Attendus.Count} scripts (jouer MOB_009).";
        }

        var phrases = new List<string> { $"Schéma : {Applied.Count} script(s) inscrit(s) sur {SchemaCatalogue.Attendus.Count} attendus" };
        if (Missing.Count > 0) phrases.Add($"MANQUANTS : {string.Join(", ", Missing)}");
        if (Unknown.Count > 0) phrases.Add($"INCONNUS du dépôt (base en avance) : {string.Join(", ", Unknown)}");
        if (Missing.Count == 0 && Unknown.Count == 0) phrases.Add($"à jour (dernier : {LastApplied})");

        return string.Join(" · ", phrases) + ".";
    }

    // ── Fabriques ────────────────────────────────────────────────────────────

    public static SchemaState Unreachable(string? error) => new() { Reachable = false, Error = error };

    public static SchemaState WithoutJournal() => new() { Reachable = true, JournalPresent = false };

    public static SchemaState From(IReadOnlyList<AppliedScript> applied)
    {
        var inscrits = applied.Select(s => s.ScriptId).ToHashSet(StringComparer.Ordinal);

        return new SchemaState
        {
            Reachable = true,
            JournalPresent = true,
            Applied = applied,
            Missing = SchemaCatalogue.Attendus.Where(id => !inscrits.Contains(id)).ToList(),
            // Un script annulé (MOB_002, MOB_007) inscrit sur une base ancienne n'est pas une
            // anomalie : il a vraiment été joué. Il ne manque pas, il n'est pas inconnu.
            Unknown = inscrits
                .Where(id => !SchemaCatalogue.Attendus.Contains(id) && !SchemaCatalogue.Annules.ContainsKey(id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList()
        };
    }
}

/// <summary>Un script inscrit dans `__VectorSchema`.</summary>
/// <param name="ScriptId">Nom du fichier, sans extension.</param>
/// <param name="AppliedAt">Quand il a été inscrit.</param>
/// <param name="Origin">`script` (posé en s'appliquant) ou `constat` (déduit de ses objets par MOB_009).</param>
public sealed record AppliedScript(string ScriptId, DateTimeOffset AppliedAt, string Origin);
