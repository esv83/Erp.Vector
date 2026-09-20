using System.Reflection;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>
/// G8 — Ce que le binaire sait de lui-même : sa version, le **commit** d'où il a été construit, et
/// si l'arbre était **modifié** à ce moment-là.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pourquoi l'arbre compte autant que le commit.</b> Le sourcelink du <c>.pdb</c> donne le
/// <c>HEAD</c> du build ; il ne dit pas si des fichiers non commités ont été compilés avec. Le 13/09,
/// la production annonçait ainsi un commit qui ne contenait pas le code servi. <c>Tree</c> répond à
/// cette question-là, et à elle seule.
/// </para>
/// <para>
/// <b>Rien ne lève ici, et rien n'est obligatoire.</b> Sans git au build — une archive, un serveur —
/// le commit est vide et <c>Tree</c> vaut <c>unknown</c> : l'API le dit plutôt que de refuser de
/// démarrer. Un renseignement absent se lit, une API muette ne se lit pas.
/// </para>
/// </remarks>
public static class BuildInfo
{
    /// <summary>Le service, tel qu'il se nomme auprès des autres modules.</summary>
    public const string Service = "Vector.Api";

    private static readonly Assembly Entry = Assembly.GetEntryAssembly() ?? typeof(BuildInfo).Assembly;

    /// <summary>Version d'assembly, commit compris (<c>1.0.0+&lt;sha&gt;</c>).</summary>
    public static readonly string InformationalVersion =
        Entry.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Entry.GetName().Version?.ToString()
        ?? "unknown";

    /// <summary>Version sémantique seule.</summary>
    public static readonly string Version = InformationalVersion.Split('+')[0];

    /// <summary>Commit complet injecté au build (<c>SourceRevisionId</c>), vide si git était absent.</summary>
    public static readonly string Commit = ReadCommit(InformationalVersion);

    /// <summary>Les 7 premiers caractères du commit — ceux qu'on lit dans `git log`.</summary>
    public static readonly string ShortCommit = Commit.Length >= 7 ? Commit[..7] : Commit;

    /// <summary><c>clean</c>, <c>modified</c> (des fichiers non commités ont été compilés), ou <c>unknown</c>.</summary>
    public static readonly string Tree = Metadata("VectorTree") ?? "unknown";

    /// <summary>Instant du build, en UTC. Vide si l'assemblage ne le porte pas.</summary>
    public static readonly string BuildUtc = Metadata("VectorBuildUtc") ?? string.Empty;

    /// <summary>Vrai quand le binaire a été construit depuis un arbre modifié : ce qu'il annonce est incomplet.</summary>
    public static bool TreeWasModified => string.Equals(Tree, "modified", StringComparison.Ordinal);

    /// <summary>
    /// Le commit est ce qui suit le <c>+</c>. Un suffixe qui n'est pas un SHA (build horodaté, par
    /// exemple) n'en est pas un : on rend le vide plutôt qu'un « commit » qui n'en désigne aucun.
    /// </summary>
    private static string ReadCommit(string informationalVersion)
    {
        var plus = informationalVersion.IndexOf('+');
        if (plus < 0) return string.Empty;

        var suffix = informationalVersion[(plus + 1)..];
        return suffix.Length == 40 && suffix.All(Uri.IsHexDigit) ? suffix : string.Empty;
    }

    private static string? Metadata(string key)
        => Entry.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.Ordinal))?.Value;
}
