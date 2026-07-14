using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Repli d'affichage d'un lieu non structuré : <see cref="JobRepository.SplitLabel"/> éclate le label figé
/// en lignes. Séparateurs : sauts de ligne et « - » (espace-tiret-espace). Trim et morceaux vides retirés.
/// </summary>
public class SplitLabelTests
{
    [Fact]
    public void Eclate_sur_le_separateur_espace_tiret_espace()
        => JobRepository.SplitLabel("EHPAD LES TAMARIS - CHAM 38 RDC - La Valette-du-Var")
            .Should().Equal("EHPAD LES TAMARIS", "CHAM 38 RDC", "La Valette-du-Var");

    [Fact]
    public void Preserve_les_tirets_sans_espaces() // « La Valette-du-Var » ne doit pas être coupé
        => JobRepository.SplitLabel("La Valette-du-Var")
            .Should().Equal("La Valette-du-Var");

    [Fact]
    public void Eclate_aussi_sur_les_sauts_de_ligne()
        => JobRepository.SplitLabel("Foo\nBar - Baz")
            .Should().Equal("Foo", "Bar", "Baz");

    [Fact]
    public void Trim_chaque_ligne()
        => JobRepository.SplitLabel("  Foo - Bar  ")
            .Should().Equal("Foo", "Bar");

    [Fact]
    public void Retire_les_morceaux_vides() // séparateur en fin → pas de ligne vide
        => JobRepository.SplitLabel("A - ")
            .Should().Equal("A");

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Vide_ou_null_donne_une_liste_vide(string? label)
        => JobRepository.SplitLabel(label).Should().BeEmpty();
}
