using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Framework.Application;
using CaSoft.Framework.Infrastructure.Persistence;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>DR-002 — l'enregistrement appartient au cas d'usage : les tests le lui fournissent.</summary>
internal static class UnitOfWorkDoubles
{
    /// <summary>Le vrai : enregistre le DbContext des repositories (base en mémoire).</summary>
    public static IUnitOfWork For(MobileDbContext ctx) => new ClEfUnitOfWork<MobileDbContext>(ctx);

    /// <summary>
    /// Le vrai, sur le DbContext d'un repository déjà construit — pour les harnais qui ne gardent que le
    /// repository. Lu par réflexion : les repositories n'exposent pas leur contexte, et ne le doivent pas.
    /// </summary>
    public static IUnitOfWork Of(object repository)
        => For((MobileDbContext)repository.GetType()
            .GetField("_ctx", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(repository)!);

    /// <summary>Pour les repositories factices, qui gardent déjà ce qu'on leur donne : compte les validations.</summary>
    public sealed class Counting : IUnitOfWork
    {
        public int Commits { get; private set; }

        public Task<int> CommitAsync(CancellationToken ct = default)
        {
            Commits++;
            return Task.FromResult(0);
        }
    }
}
