using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.Ocr;

/// <summary>
/// P3 — Lit une carte mutuelle avec un modèle de vision (Claude) et <b>propose</b> ses quatre champs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sortie structurée imposée.</b> Le modèle répond selon un schéma JSON, pas en prose : c'est ce
/// qui rend la réponse exploitable sans reconnaissance de texte, et ce qui permet de dire « champ
/// absent » plutôt que d'inventer. Un champ qu'il n'a pas lu revient <c>null</c> — jamais deviné.
/// </para>
/// <para>
/// <b>Une carte illisible n'est pas une panne.</b> Photo floue, carte coupée, mauvais document : le
/// modèle rend quatre champs vides, et la file marque la carte comme lue sans proposition. Seul un
/// échec technique (réseau, service indisponible) lève — c'est alors à la file de retenter.
/// </para>
/// <para>
/// ⚠️ <b>Donnée de santé.</b> L'image part chez Anthropic : c'est la décision à prendre côté
/// exploitation avant d'activer la clé (P4 / RGPD), pas une propriété de ce code.
/// </para>
/// </remarks>
public sealed class ClaudeMutuelleCardOcrService : IMutuelleCardOcrService
{
    /// <summary>Ce qu'on demande au modèle. Court, et sans exemple : une carte n'a pas deux formes.</summary>
    private const string Consigne =
        "Tu lis la photo d'une carte de mutuelle santé française. Rends les quatre champs demandés, "
        + "exactement tels qu'ils sont imprimés. Un champ que tu ne lis pas clairement sur l'image "
        + "vaut null : ne devine jamais, ne complète jamais un numéro partiel. "
        + "amcCode est le numéro AMC (organisme d'assurance maladie complémentaire), 8 chiffres le "
        + "plus souvent ; teletransmission est le numéro de télétransmission ; concentrateur est le "
        + "nom du concentrateur technique (Viamedis, Almerys, SP Santé…) quand il figure. "
        + "confidence dit ta confiance globale dans cette lecture, de 0 à 1.";

    private static readonly IReadOnlyDictionary<string, JsonElement> Schema = BuildSchema();

    private readonly AnthropicClient _client;
    private readonly MutuelleCardOcrOptions _options;
    private readonly ILogger<ClaudeMutuelleCardOcrService> _logger;

    public ClaudeMutuelleCardOcrService(AnthropicClient client, MutuelleCardOcrOptions options,
        ILogger<ClaudeMutuelleCardOcrService> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public async Task<ClMutuelleCardOcrProposal> ExtractAsync(byte[] image, string contentType, CancellationToken ct)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = 1024,
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = Schema }
            },
            Messages =
            [
                new MessageParam
                {
                    Role = Role.User,
                    Content = new List<ContentBlockParam>
                    {
                        new ImageBlockParam
                        {
                            Source = new Base64ImageSource
                            {
                                Data = Convert.ToBase64String(image),
                                MediaType = MediaType(contentType)
                            }
                        },
                        new TextBlockParam { Text = Consigne }
                    }
                }
            ]
        }, cancellationToken: ct);

        var json = response.Content.OfType<TextBlock>().Select(b => b.Text).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning("Lecture de carte mutuelle : réponse sans texte exploitable.");
            return new ClMutuelleCardOcrProposal();
        }

        return Parse(json);
    }

    /// <summary>
    /// Le JSON est imposé par le schéma, mais il traverse un réseau : un corps illisible rend une
    /// proposition vide, jamais une exception — la carte sera marquée lue sans proposition, et un
    /// opérateur la saisira comme avant.
    /// </summary>
    internal ClMutuelleCardOcrProposal Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var racine = doc.RootElement;
            if (racine.ValueKind != JsonValueKind.Object) return new ClMutuelleCardOcrProposal();

            return new ClMutuelleCardOcrProposal
            {
                MutuelleName = Texte(racine, "mutuelleName"),
                AmcCode = Texte(racine, "amcCode"),
                Concentrateur = Texte(racine, "concentrateur"),
                Teletransmission = Texte(racine, "teletransmission"),
                Confidence = Confiance(racine)
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Lecture de carte mutuelle : réponse non-JSON, proposition ignorée.");
            return new ClMutuelleCardOcrProposal();
        }
    }

    private static string? Texte(JsonElement racine, string nom)
        => racine.TryGetProperty(nom, out var valeur) && valeur.ValueKind == JsonValueKind.String
           && !string.IsNullOrWhiteSpace(valeur.GetString())
            ? valeur.GetString()!.Trim()
            : null;

    private static decimal? Confiance(JsonElement racine)
    {
        if (!racine.TryGetProperty("confidence", out var valeur) || valeur.ValueKind != JsonValueKind.Number)
            return null;

        // Bornée à [0,1] : la colonne est un decimal(4,3), et une confiance hors bornes ne veut rien
        // dire de toute façon.
        var lue = valeur.GetDecimal();
        return lue < 0m ? 0m : lue > 1m ? 1m : Math.Round(lue, 3);
    }

    /// <summary>
    /// L'API n'accepte que quatre types d'image. Une carte arrivée en HEIC ou en PDF n'est pas une
    /// panne : on l'annonce en JPEG, et si le service refuse, la file retiendra le motif.
    /// </summary>
    private static string MediaType(string? contentType) => contentType?.ToLowerInvariant() switch
    {
        "image/png" => "image/png",
        "image/gif" => "image/gif",
        "image/webp" => "image/webp",
        _ => "image/jpeg"
    };

    private static Dictionary<string, JsonElement> BuildSchema() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            mutuelleName = new { type = new[] { "string", "null" }, description = "Nom de la mutuelle, tel qu'imprimé." },
            amcCode = new { type = new[] { "string", "null" }, description = "Numéro AMC de l'organisme complémentaire." },
            concentrateur = new { type = new[] { "string", "null" }, description = "Nom du concentrateur technique, s'il figure." },
            teletransmission = new { type = new[] { "string", "null" }, description = "Numéro de télétransmission." },
            confidence = new { type = "number", description = "Confiance globale dans la lecture, de 0 à 1." }
        }),
        ["required"] = JsonSerializer.SerializeToElement(
            new[] { "mutuelleName", "amcCode", "concentrateur", "teletransmission", "confidence" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false)
    };
}
