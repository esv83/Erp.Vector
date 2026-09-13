using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Dto;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Framework;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Sélecteur d'équipage : quand l'équipage est composé mais hors fenêtre, le 404 dit à l'ambulancier
/// ce qui le bloque et quoi faire — plutôt qu'un constat d'absence.
/// </summary>
public class ClGetMyActiveCrewsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 7, 12, 10, 0, 0);

    private static ClCrew Crew(DateTime start, DateTime? end = null, bool serviceEnded = false)
        => new(Guid.NewGuid(), new List<ClEmployee>(), start,
               new ClVehicle(Guid.Empty, string.Empty, new ClKilometers()), end)
        {
            IsServiceEnded = serviceEnded
        };

    private sealed class FakeCrews : ICrewRepository
    {
        private readonly Dictionary<Guid, ClCrew> _crews;
        public FakeCrews(params ClCrew[] crews) => _crews = crews.ToDictionary(c => c.CrewId);
        public ClCrew GetCrew(Guid gCrewID) => _crews[gCrewID];
        public bool IsEmployeeInCrew(Guid gCrewID, Guid gEmployeeId) => throw new NotSupportedException();
        public ClLogDriverModel GetCrewDriver(Guid gVehicleID) => throw new NotSupportedException();
        public List<ClJobListItemModel> FetchJobList(Guid gCrewId) => throw new NotSupportedException();
        public List<ClJobListItemModel> FetchJobList(IReadOnlyCollection<Guid> gCrewIds) => throw new NotSupportedException();
        public List<ClInstructionListItemModel> FetchInstructionList(Guid gCrewId) => throw new NotSupportedException();
        public void Update(ClCrew crew) => throw new NotSupportedException();
        public void AckInstruction(int instructionId) => throw new NotSupportedException();
        public List<Guid> GetCrewIdList(DateOnly id) => throw new NotSupportedException();
    }

    private static ClResult<ClActiveCrewSelectionDtoOut> Select(params ClCrew[] crews)
        => new ClGetMyActiveCrewsUseCase(crews.Select(c => c.CrewId).ToList(), Now, new FakeCrews(crews)).Handle();

    private static string NotFoundMessage(ClResult<ClActiveCrewSelectionDtoOut> result)
    {
        result.IsFail.Should().BeTrue();
        var error = result.InnerError.Should().BeOfType<ClError>().Subject;
        error.IsNotFound.Should().BeTrue("un équipage hors fenêtre reste un 404");
        return error.ErrorText;
    }

    [Fact]
    public void Service_plus_tard_dans_la_journee_donne_l_heure_d_ouverture_de_l_acces()
        => NotFoundMessage(Select(Crew(new DateTime(2026, 7, 12, 14, 0, 0))))
            .Should().Be("Votre service commence à 14:00 : vos missions seront accessibles à partir de 13:30.");

    [Fact]
    public void Service_un_autre_jour_donne_la_date()
        => NotFoundMessage(Select(Crew(new DateTime(2026, 7, 13, 7, 0, 0))))
            .Should().StartWith("Votre service commence le 13/07 à 07:00");

    [Fact]
    public void Plusieurs_services_a_venir_annonce_le_plus_proche()
        => NotFoundMessage(Select(Crew(new DateTime(2026, 7, 12, 18, 0, 0)), Crew(new DateTime(2026, 7, 12, 13, 0, 0))))
            .Should().Contain("à 13:00");

    [Fact]
    public void Service_cloture_le_dit()
        => NotFoundMessage(Select(Crew(Now.AddHours(-4), serviceEnded: true)))
            .Should().StartWith("Votre service est clôturé");

    [Fact]
    public void Service_a_venir_prime_sur_un_service_du_matin_cloture()
        => NotFoundMessage(Select(Crew(Now.AddHours(-4), serviceEnded: true), Crew(new DateTime(2026, 7, 12, 14, 0, 0))))
            .Should().StartWith("Votre service commence à 14:00");

    [Fact]
    public void Vacation_expiree_demande_d_appeler_la_regulation()
        => NotFoundMessage(Select(Crew(Now.AddHours(-19))))
            .Should().Contain($"plus de {ClCrew.MaxServiceDurationHours} h");

    [Fact]
    public void Un_equipage_selectionnable_suffit()
        => Select(Crew(Now.AddHours(-4), serviceEnded: true), Crew(Now.AddHours(-1))).IsSucces.Should().BeTrue();

    [Fact]
    public void Le_motif_de_cloture_prime_sur_la_fenetre_anticipee()
        => Crew(Now.AddMinutes(10), serviceEnded: true).UnselectableReasonAt(Now).Should().Be(EnCrewUnselectableReason.Closed);
}
