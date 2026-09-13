using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Incident du 2026-09-13 : depuis qu'Orders pose une fin théorique (début + 10 h) à chaque vacation,
/// « fin de service dépassée » ne veut plus dire « clôturé ». Des ambulanciers encore en service après
/// 18:00 recevaient « Votre service est clôturé ». La clôture se lit sur le statut, et sur lui seul.
/// </summary>
public class ErpCrewMappingsTests
{
    private static readonly DateTime Soir = new(2026, 9, 13, 19, 30, 0);

    private static ErpCrewFullDto Vacation(int status) => new()
    {
        Id = Guid.NewGuid(),
        ServiceStart = new DateTime(2026, 9, 13, 8, 0, 0),
        ServiceEnd = new DateTime(2026, 9, 13, 18, 0, 0),   // fin théorique : début + 10 h
        Status = status
    };

    [Theory]
    [InlineData(0)]   // Draft
    [InlineData(1)]   // Ready
    public void Une_vacation_ouverte_apres_sa_fin_theorique_reste_selectionnable(int status)
    {
        var crew = Vacation(status).ToDomain();

        crew.IsServiceEnded.Should().BeFalse();
        crew.IsSelectableAt(Soir).Should().BeTrue();
    }

    [Fact]
    public void Une_vacation_cloturee_chez_Orders_n_est_plus_selectionnable()
    {
        var crew = Vacation(ErpCrewFullDto.ClosedStatus).ToDomain();

        crew.IsServiceEnded.Should().BeTrue();
        crew.UnselectableReasonAt(Soir).Should().Be(EnCrewUnselectableReason.Closed);
    }
}
