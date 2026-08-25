namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// Codes du catalogue Vector sans homonyme côté Order, et leur équivalent décidé.
///
/// <para><c>STANDARD</c> — « Transport standard » — n'existe pas au catalogue Order : un transport
/// standard y est un transport <c>CPAM</c>. Arbitrage métier du 2026-08-24. Sans cette entrée, le
/// type par défaut du sélecteur mobile serait le seul à ne pas pouvoir être enregistré.</para>
///
/// <para>La table est <b>volontairement minuscule et locale au code</b> : elle ne survit pas à la
/// bascule, où le sélecteur n'offrira plus que des types du catalogue Order. En faire une table de
/// base la rendrait permanente.</para>
///
/// <para>Elle est lue dans les <b>deux sens</b>, par deux chemins distincts qui doivent rester
/// d'accord : l'écriture traduit le code Vector coché vers son code Order (<see cref="ToOrder"/>),
/// et la lecture du type effectif fait le trajet inverse pour retrouver le jeu d'attributs Vector
/// correspondant (<see cref="ToVector"/>). Deux tables séparées finiraient par diverger.</para>
/// </summary>
internal static class ContextOrderCodeAliases
{
    private static readonly Dictionary<string, string> VectorToOrder =
        new(StringComparer.OrdinalIgnoreCase) { ["STANDARD"] = "CPAM" };

    private static readonly Dictionary<string, string> OrderToVector =
        VectorToOrder.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>Code Order visé par un code Vector — le code lui-même s'il n'a pas d'alias.</summary>
    public static string ToOrder(string vectorCode)
        => VectorToOrder.TryGetValue(vectorCode, out var alias) ? alias : vectorCode;

    /// <summary>Code Vector visé par un code Order — le code lui-même s'il n'a pas d'alias.</summary>
    public static string ToVector(string orderCode)
        => OrderToVector.TryGetValue(orderCode, out var alias) ? alias : orderCode;
}
