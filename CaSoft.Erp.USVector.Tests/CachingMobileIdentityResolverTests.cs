using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Le cache du garde-fou d'équipage, par lequel passe chaque requête du terrain. Trois promesses :
/// le rattachement se cache, son absence ne se cache pas, et la (re)sélection lit frais.
/// </summary>
public class CachingMobileIdentityResolverTests
{
    private static readonly Guid Sub = Guid.NewGuid();
    private static readonly Guid Personnel = Guid.NewGuid();
    private static readonly DateOnly Jour = new(2026, 10, 4);

    /// <summary>Source simulée : compte ses appels, rend ce qu'on lui dit.</summary>
    private sealed class Source : IMobileIdentityResolver
    {
        public Guid? Rattache;
        public IReadOnlyList<Guid> Equipages = Array.Empty<Guid>();
        public int AppelsPersonnel;
        public int AppelsEquipages;

        public Task<Guid?> ResolvePersonnelIdAsync(Guid keyCloakSub, CancellationToken ct)
        {
            AppelsPersonnel++;
            return Task.FromResult(Rattache);
        }

        public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
        {
            AppelsEquipages++;
            return Task.FromResult(Equipages);
        }

        public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsFreshAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
            => ResolveActiveCrewIdsAsync(personnelId, onDate, ct);

        public Task<bool> IsMissionAccessibleAsync(Guid personnelId, Guid missionId, CancellationToken ct)
            => Task.FromResult(false);
    }

    private static CachingMobileIdentityResolver Cache(Source source)
        => new(source, new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromHours(1), TimeSpan.FromMinutes(1));

    [Fact]
    public async Task Un_rattachement_se_cache()
    {
        var source = new Source { Rattache = Personnel };
        var cache = Cache(source);

        (await cache.ResolvePersonnelIdAsync(Sub, CancellationToken.None)).Should().Be(Personnel);
        (await cache.ResolvePersonnelIdAsync(Sub, CancellationToken.None)).Should().Be(Personnel);

        source.AppelsPersonnel.Should().Be(1);
    }

    [Fact]
    public async Task Un_compte_non_rattache_ne_se_cache_pas_pour_pouvoir_l_etre_le_jour_meme()
    {
        var source = new Source { Rattache = null };
        var cache = Cache(source);

        (await cache.ResolvePersonnelIdAsync(Sub, CancellationToken.None)).Should().BeNull();
        source.Rattache = Personnel;
        (await cache.ResolvePersonnelIdAsync(Sub, CancellationToken.None)).Should().Be(Personnel);

        source.AppelsPersonnel.Should().Be(2);
    }

    [Fact]
    public async Task La_reselection_lit_frais_et_rafraichit_le_garde_fou()
    {
        var matin = Guid.NewGuid();
        var nouveau = Guid.NewGuid();
        var source = new Source { Equipages = new[] { matin } };
        var cache = Cache(source);

        (await cache.ResolveActiveCrewIdsAsync(Personnel, Jour, CancellationToken.None)).Should().Equal(matin);

        // Un équipage composé dans la journée : le garde-fou en cache ne le voit pas encore…
        source.Equipages = new[] { matin, nouveau };
        (await cache.ResolveActiveCrewIdsAsync(Personnel, Jour, CancellationToken.None)).Should().Equal(matin);

        // …la (re)sélection le lit frais, et le garde-fou qui suit le voit.
        (await cache.ResolveActiveCrewIdsFreshAsync(Personnel, Jour, CancellationToken.None)).Should().Equal(matin, nouveau);
        (await cache.ResolveActiveCrewIdsAsync(Personnel, Jour, CancellationToken.None)).Should().Equal(matin, nouveau);

        source.AppelsEquipages.Should().Be(2);
    }
}
