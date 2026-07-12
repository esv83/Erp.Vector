using System;
using System.Collections.Generic;
using CaSoft.Erp.USVector.Domain;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Règle domaine <see cref="ClCrew.IsSelectableAt"/> : un équipage n'est sélectionnable au terrain
/// que s'il est <b>commencé</b>, <b>non clôturé</b> (fin de service passée ou déclarée) et
/// <b>non obsolète</b> (durée &gt; 18 h = vacation probablement oubliée).
/// </summary>
public class ClCrewSelectableTests
{
    private static readonly DateTime Now = new(2026, 7, 12, 10, 0, 0);

    private static ClCrew Crew(DateTime start, DateTime? end = null, bool serviceEnded = false)
        => new(Guid.NewGuid(), new List<ClEmployee>(), start,
               new ClVehicle(Guid.Empty, string.Empty, new ClKilometers()), end)
        {
            IsServiceEnded = serviceEnded
        };

    [Fact]
    public void Commence_ouvert_recent_est_selectionnable()
        => Crew(Now.AddHours(-2)).IsSelectableAt(Now).Should().BeTrue();

    [Fact]
    public void Fenetre_en_cours_est_selectionnable()
        => Crew(Now.AddHours(-2), end: Now.AddHours(2)).IsSelectableAt(Now).Should().BeTrue();

    [Fact]
    public void Pas_encore_commence_non_selectionnable()
        => Crew(Now.AddHours(1)).IsSelectableAt(Now).Should().BeFalse();

    [Fact]
    public void Fin_de_service_passee_non_selectionnable()
        => Crew(Now.AddHours(-4), end: Now.AddHours(-1)).IsSelectableAt(Now).Should().BeFalse();

    [Fact]
    public void Service_declare_termine_non_selectionnable()
        => Crew(Now.AddHours(-2), serviceEnded: true).IsSelectableAt(Now).Should().BeFalse();

    [Fact]
    public void Obsolete_plus_de_18h_non_selectionnable()
        => Crew(Now.AddHours(-19)).IsSelectableAt(Now).Should().BeFalse();

    [Fact]
    public void Juste_sous_18h_est_selectionnable()
        => Crew(Now.AddHours(-18).AddMinutes(1)).IsSelectableAt(Now).Should().BeTrue();
}
