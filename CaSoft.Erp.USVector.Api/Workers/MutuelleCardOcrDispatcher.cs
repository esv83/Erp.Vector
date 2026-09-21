using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Ocr;

namespace CaSoft.Erp.USVector.Api.Workers;

/// <summary>
/// P3 — Lit les cartes mutuelle en attente, une par une, et enregistre ce que le modèle propose.
/// </summary>
/// <remarks>
/// <para>
/// <b>Asynchrone par construction</b> : la capture ne doit jamais attendre le modèle. L'ambulancier
/// photographie, la carte part en <c>pending</c>, et la lecture se fait après — sur le modèle de la
/// file de projection, qui tourne depuis juillet.
/// </para>
/// <para>
/// <b>Il n'écrit jamais dans les champs de facturation</b> (M5) : la proposition vit à côté, et c'est
/// la validation humaine qui recopie.
/// </para>
/// <para>
/// <b>Il abandonne</b> après <see cref="MutuelleCardOcrOptions.MaxAttempts"/> tentatives, en gardant le
/// motif. La file de projection a relancé 55 450 fois une mission qui ne reviendrait jamais : une
/// carte que le modèle refuse ne doit pas coûter un appel toutes les minutes jusqu'au redémarrage.
/// </para>
/// </remarks>
public sealed class MutuelleCardOcrDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MutuelleCardOcrOptions _options;
    private readonly ILogger<MutuelleCardOcrDispatcher> _logger;

    public MutuelleCardOcrDispatcher(IServiceScopeFactory scopeFactory, MutuelleCardOcrOptions options,
        ILogger<MutuelleCardOcrDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MutuelleCardOcrDispatcher démarré (modèle {Modele}, poll {Poll}s, {Lot} cartes par cycle).",
            _options.Model, _options.PollSeconds, _options.BatchSize);

        var intervalle = TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReadPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Un cycle qui tombe ne doit pas emporter le worker : la carte suivante peut passer.
                _logger.LogError(ex, "MutuelleCardOcrDispatcher — erreur de cycle.");
            }

            try
            {
                await Task.Delay(intervalle, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task ReadPendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var cartes = scope.ServiceProvider.GetRequiredService<IMutuelleCardRepository>();
        var lecture = scope.ServiceProvider.GetRequiredService<IMutuelleCardOcrService>();

        foreach (var cardId in cartes.ListPendingOcr(_options.BatchSize))
        {
            if (ct.IsCancellationRequested) return;

            var image = cartes.GetImage(cardId);
            if (image is null)
            {
                cartes.MarkOcrFailure(cardId, "Image introuvable.", maxAttempts: 1);
                continue;
            }

            try
            {
                var proposition = await lecture.ExtractAsync(image.Bytes, image.ContentType, ct);

                if (!proposition.HasValue)
                {
                    // Le modèle a répondu, et n'a rien lu : c'est un résultat, pas un échec. La carte
                    // sort de la file — une photo floue ne deviendra pas lisible à la dixième relance.
                    cartes.SaveOcrProposal(cardId, proposition);
                    _logger.LogInformation("Carte {Carte} : aucune valeur lue, rien n'est proposé.", cardId);
                    continue;
                }

                cartes.SaveOcrProposal(cardId, proposition);
                _logger.LogInformation("Carte {Carte} : {Champs} champ(s) proposé(s), confiance {Confiance}.",
                    cardId, Comptes(proposition), proposition.Confidence);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Échec technique : on retente, jusqu'à MaxAttempts. Le motif reste sur la carte.
                var issue = cartes.MarkOcrFailure(cardId, ex.Message, _options.MaxAttempts);
                _logger.LogWarning(ex, "Carte {Carte} : lecture impossible (tentative {Tentative}/{Max}){Suite}.",
                    cardId, issue.Attempts, _options.MaxAttempts, issue.GaveUp ? " — abandon" : string.Empty);
            }
        }
    }

    private static int Comptes(Application.ClMutuelleCardOcrProposal p)
        => new[] { p.MutuelleName, p.AmcCode, p.Concentrateur, p.Teletransmission }
            .Count(v => !string.IsNullOrWhiteSpace(v));
}
