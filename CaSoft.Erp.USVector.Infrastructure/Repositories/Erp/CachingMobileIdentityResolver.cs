using CaSoft.Erp.USVector.Application.Port;
using Microsoft.Extensions.Caching.Memory;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;

/// <summary>
/// Décorateur de cache sur <see cref="IMobileIdentityResolver"/> — supprime les appels répétés à
/// Orders.Api dans le chemin chaud (garde-fou crew de chaque requête). Cache calé sur la volatilité
/// de chaque maillon :
/// <list type="bullet">
///   <item><b>sub → personnelId</b> : quasi-immuable → TTL long. Seules les résolutions positives
///   sont mises en cache (un compte pas encore rattaché peut l'être plus tard dans la journée).</item>
///   <item><b>personnel → crews actifs du jour</b> : volatile intra-journée (création de crew,
///   changement d'équipage) → TTL court. La (re)sélection (<see cref="ResolveActiveCrewIdsFresh"/>)
///   contourne le cache et le rafraîchit → un crew créé le jour même apparaît dès l'ouverture du
///   sélecteur, seul endroit où il peut être choisi.</item>
/// </list>
/// <see cref="IsMissionAccessible"/> est délégué tel quel (fréquence faible : ouverture d'un détail).
/// </summary>
public class CachingMobileIdentityResolver : IMobileIdentityResolver
{
    private readonly IMobileIdentityResolver _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _personnelTtl;
    private readonly TimeSpan _activeCrewsTtl;

    public CachingMobileIdentityResolver(
        IMobileIdentityResolver inner, IMemoryCache cache,
        TimeSpan personnelTtl, TimeSpan activeCrewsTtl)
    {
        _inner = inner;
        _cache = cache;
        _personnelTtl = personnelTtl;
        _activeCrewsTtl = activeCrewsTtl;
    }

    public Guid? ResolvePersonnelId(Guid keyCloakSub)
    {
        var key = $"mid:per:{keyCloakSub}";
        if (_cache.TryGetValue<Guid>(key, out var cached))
            return cached;

        var resolved = _inner.ResolvePersonnelId(keyCloakSub);
        // On ne cache QUE le positif : une résolution nulle (compte non rattaché) doit pouvoir
        // se corriger le jour même sans attendre l'expiration.
        if (resolved is not null)
            _cache.Set(key, resolved.Value, _personnelTtl);

        return resolved;
    }

    public IReadOnlyList<Guid> ResolveActiveCrewIds(Guid personnelId, DateOnly onDate)
        => _cache.GetOrCreate(CrewsKey(personnelId, onDate), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _activeCrewsTtl;
            return _inner.ResolveActiveCrewIds(personnelId, onDate);
        })!;

    public IReadOnlyList<Guid> ResolveActiveCrewIdsFresh(Guid personnelId, DateOnly onDate)
    {
        // Lecture fraîche (bypass) puis on rafraîchit l'entrée pour le garde-fou qui suivra.
        var fresh = _inner.ResolveActiveCrewIds(personnelId, onDate);
        _cache.Set(CrewsKey(personnelId, onDate), fresh, _activeCrewsTtl);
        return fresh;
    }

    public bool IsMissionAccessible(Guid personnelId, Guid missionId)
        => _inner.IsMissionAccessible(personnelId, missionId);

    private static string CrewsKey(Guid personnelId, DateOnly onDate)
        => $"mid:crews:{personnelId}:{onDate:yyyyMMdd}";
}
