namespace CaSoft.Erp.USVector.Infrastructure.Ocr;

/// <summary>
/// P3 — Réglages de la lecture automatique des cartes mutuelle (section <c>MutuelleCardOcr</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Incomplet, tout est inerte</b> : sans clé, le worker ne démarre pas et rien n'appelle de modèle.
/// C'est le même contrat que le compte de service (C2) — Vector peut être déployé avant que la
/// décision « où tourne l'appel » soit prise, sans rien changer à son comportement.
/// </para>
/// <para>
/// <b>La clé ne vit jamais dans un fichier suivi</b> : <c>web.config</c> du serveur, variable
/// <c>MutuelleCardOcr__ApiKey</c>.
/// </para>
/// </remarks>
public sealed class MutuelleCardOcrOptions
{
    public const string SectionName = "MutuelleCardOcr";

    /// <summary>Valeur de remplacement des fichiers suivis : vaut « non configuré ».</summary>
    public const string Placeholder = "__SET_VIA_ENV__";

    public string? ApiKey { get; set; }

    /// <summary>Modèle de vision. Par défaut le plus capable : une carte mal lue coûte plus qu'un jeton.</summary>
    public string Model { get; set; } = "claude-opus-5";

    /// <summary>Cartes lues par cycle. Volume constaté : une douzaine par jour (relevé du 20/09).</summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>Délai entre deux cycles, en secondes.</summary>
    public int PollSeconds { get; set; } = 60;

    /// <summary>
    /// Tentatives avant d'abandonner une carte. Au-delà, elle passe en <c>error</c> avec son motif :
    /// la file de projection a relancé 55 450 fois une mission qui ne reviendrait jamais.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && ApiKey != Placeholder;
}
