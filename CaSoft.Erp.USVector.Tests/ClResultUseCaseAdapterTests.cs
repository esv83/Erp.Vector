using CaSoft.Framework;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Phase 0 (refactor Result pattern) — le pont legacy←Result (<see cref="ClResultUseCaseAdapter{T}"/>)
/// doit produire une réponse identique à ce qu'attend ClWebApiPresenter, garantissant la
/// non-régression HTTP : Ok(valeur)→Data (200) ; Ok(null)/NotFound→Data null (404) ;
/// erreur métier→HasError (400) ; exception→HasError.
/// </summary>
public class ClResultUseCaseAdapterTests
{
    private sealed class StubResultUseCase<T> : IResultUseCase<T>
    {
        private readonly Func<ClResult<T>> _handle;
        public StubResultUseCase(Func<ClResult<T>> handle) => _handle = handle;
        public ClResult<T> Handle() => _handle();
    }

    private sealed class CapturingHandler : IResponseHandler
    {
        public ClUseCaseResponseBase? Captured { get; private set; }
        public void Handle(ClUseCaseResponseBase response) => Captured = response;
    }

    private static ClUseCaseResponseBase Run<T>(Func<ClResult<T>> handle)
    {
        var handler = new CapturingHandler();
        new ClResultUseCaseAdapter<T>(new StubResultUseCase<T>(handle)).Execute(handler);
        return handler.Captured!;
    }

    [Fact]
    public void Succes_avec_valeur_expose_la_donnee() // -> 200
    {
        var r = Run(() => ClResult<string>.Ok("payload"));

        r.IsSuccess.Should().BeTrue();
        r.Data.Should().Be("payload");
    }

    [Fact]
    public void Succes_sans_valeur_donne_data_null() // -> 404 via presenter
    {
        var r = Run(() => ClResult<string>.Ok(null!));

        r.IsSuccess.Should().BeTrue();
        r.Data.Should().BeNull();
    }

    [Fact]
    public void NotFound_est_traduit_en_data_null() // -> 404 (parité legacy)
    {
        var r = Run(() => ClResult<string>.Fail(ClError.NotFound("absent")));

        r.IsSuccess.Should().BeTrue();
        r.Data.Should().BeNull();
    }

    [Fact]
    public void Erreur_metier_est_traduite_en_erreur() // -> 400
    {
        var r = Run(() => ClResult<string>.Fail(ClError.Application("boom")));

        r.HasError.Should().BeTrue();
        r.ErrorText.Should().Contain("boom");
    }

    [Fact]
    public void Exception_dans_Handle_est_capturee_en_erreur()
    {
        var r = Run<string>(() => throw new InvalidOperationException("kaboom"));

        r.HasError.Should().BeTrue();
        r.ErrorText.Should().Contain("kaboom");
    }
}
