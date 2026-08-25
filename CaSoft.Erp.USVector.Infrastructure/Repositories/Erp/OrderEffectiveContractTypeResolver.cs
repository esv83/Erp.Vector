using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// OC-3b — Implémentation d'<see cref="IEffectiveContractTypeResolver"/> au-dessus d'Orders.Api.
///
/// <para><b>Pont sync/async assumé.</b> Le contrat legacy <c>IJobAttributeOverlay</c> est synchrone
/// et sert un chemin lui-même synchrone (<c>JobRepository.GetJob</c>, déjà bâti sur ce pont). On
/// concentre ici l'unique attente bloquante plutôt que de la propager dans le dépôt d'attributs —
/// et de rendre async une interface promise à la suppression avec OC-5.</para>
///
/// <para><b>Coût.</b> Un appel HTTP de plus sur le détail mission. Il est du même ordre que les
/// trois que ce chemin fait déjà (mission, commande, bénéficiaire), et il disparaîtra avec OC-5 où
/// la structure du formulaire viendra d'Order en même temps que le type.</para>
/// </summary>
public sealed class OrderEffectiveContractTypeResolver : IEffectiveContractTypeResolver
{
    private readonly MobileDbContext _ctx;
    private readonly IErpReadApiClient _erp;
    private readonly ContextOrderOptions _options;
    private readonly ILogger<OrderEffectiveContractTypeResolver> _logger;

    public OrderEffectiveContractTypeResolver(
        MobileDbContext ctx,
        IErpReadApiClient erp,
        ContextOrderOptions options,
        ILogger<OrderEffectiveContractTypeResolver> logger)
    {
        _ctx = ctx;
        _erp = erp;
        _options = options;
        _logger = logger;
    }

    public EffectiveContractType? Resolve(Guid missionId)
    {
        // Drapeau désarmé : le type vit encore dans MOB_JOB_CONTRACT, on ne s'en mêle pas.
        if (!_options.UseOrderCatalog) return null;

        ErpMissionContextOrderDto? context;
        try
        {
            context = _erp.GetMissionContextOrderAsync(missionId, CancellationToken.None)
                          .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // On s'abstient au lieu de rendre « aucun type ». La différence compte : rendre « aucun
            // type » retirerait des champs de l'écran que l'ambulancier est peut-être en train de
            // remplir, sur une simple panne réseau. L'abstention lui laisse le formulaire qu'il
            // avait avant la bascule. Aucune donnée n'est mise en danger au passage : les valeurs
            // saisies sont stockées par NOM d'attribut, jamais sous l'id d'un type.
            _logger.LogWarning(ex,
                "OC-3b : type effectif indisponible pour la mission {MissionId}, formulaire servi selon le chemin historique.",
                missionId);
            return null;
        }

        // Mission inconnue d'Order : cas limite (le détail mission aurait déjà échoué en amont).
        // On s'abstient plutôt que de vider le formulaire sur une incohérence de référentiel.
        if (context is null) return null;

        // Type non renseigné côté Order : état valide depuis OC-3b, et non plus « prendre le
        // premier actif ». Le formulaire se réduit aux attributs communs.
        if (string.IsNullOrWhiteSpace(context.ContextOrderCode))
            return new EffectiveContractType(null);

        var vectorCode = ContextOrderCodeAliases.ToVector(context.ContextOrderCode);

        // Pas de filtre sur CTT_ACTIVE : un type désactivé au catalogue Vector ne doit pas retirer
        // ses champs d'une mission qui le porte déjà. L'activité gouverne ce qu'on peut choisir,
        // pas ce qu'on peut relire.
        var contractTypeId = _ctx.ContractTypes.AsNoTracking()
            .Where(t => t.CTT_CODE == vectorCode)
            .Select(t => (int?)t.CTT_ID)
            .FirstOrDefault();

        if (contractTypeId is null)
        {
            // Attendu : le catalogue Order compte sept types, celui de Vector deux. Une mission en
            // « Centre 15 » n'a pas de jeu d'attributs Vector — attributs communs seulement, plutôt
            // que ceux d'un type voisin choisi au hasard.
            _logger.LogInformation(
                "OC-3b : type Order {Code} sans équivalent au catalogue Vector (mission {MissionId}), attributs communs seuls.",
                context.ContextOrderCode, missionId);
        }

        return new EffectiveContractType(contractTypeId);
    }
}
