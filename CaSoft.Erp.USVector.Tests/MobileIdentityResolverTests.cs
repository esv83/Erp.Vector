using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Garde-fou d'appartenance : Orders.Api peut remonter des équipages qui ne partagent que le
/// VÉHICULE (bug de jointure côté ERP) et non l'appartenance réelle du personnel. Le resolver doit
/// revérifier que le personnel est bien MEMBRE de chaque équipage (ErpCrewFullDto.Members).
/// </summary>
public class MobileIdentityResolverTests
{
    // Fake minimal : seuls ListCrewIdsAsync + GetCrewFullAsync sont exercés ici.
    private sealed class FakeErp : IErpReadApiClient
    {
        private readonly IReadOnlyList<Guid> _crewIds;
        private readonly Dictionary<Guid, ErpCrewFullDto> _crews;
        public FakeErp(IReadOnlyList<Guid> crewIds, Dictionary<Guid, ErpCrewFullDto> crews)
        {
            _crewIds = crewIds;
            _crews = crews;
        }

        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid personnelId, DateOnly onDate, int take, CancellationToken ct = default)
            => Task.FromResult(_crewIds);

        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default)
            => Task.FromResult(_crews.TryGetValue(crewId, out var c) ? c : null);

        // Non sollicités par ces tests.
        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid missionId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(DateTime from, DateTime to, int take, IReadOnlyCollection<Guid>? assignedCrewIds = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpOrderEditDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid beneficiaryId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid keycloakSub, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int?> GetMissionTransferStatusAsync(Guid missionId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static ErpCrewFullDto Crew(Guid id, params Guid[] memberIds)
        => new()
        {
            Id = id,
            Members = memberIds.Select(m => new ErpCrewMemberDto { Id = m }).ToList()
        };

    [Fact]
    public void Ecarte_un_crew_partageant_seulement_le_vehicule()
    {
        // Cas Jeremy Lautard : Orders.Api remonte 2 crews non clôturés partageant le VÉHICULE ;
        // Jeremy n'est membre que du premier → le crew « véhicule seul » doit être écarté.
        var jeremy = Guid.NewGuid();
        var coequipier = Guid.NewGuid();
        var autre = Guid.NewGuid();
        var crewDeJeremy = Guid.NewGuid();
        var crewMemeVehicule = Guid.NewGuid();

        var erp = new FakeErp(
            new[] { crewDeJeremy, crewMemeVehicule },
            new Dictionary<Guid, ErpCrewFullDto>
            {
                [crewDeJeremy] = Crew(crewDeJeremy, jeremy, coequipier),
                [crewMemeVehicule] = Crew(crewMemeVehicule, coequipier, autre), // pas Jeremy
            });

        var result = new MobileIdentityResolver(erp).ResolveActiveCrewIds(jeremy, new DateOnly(2026, 7, 6));

        result.Should().ContainSingle().Which.Should().Be(crewDeJeremy);
    }

    [Fact]
    public void Conserve_tous_les_crews_dont_le_personnel_est_reellement_membre()
    {
        var jeremy = Guid.NewGuid();
        var crewMatin = Guid.NewGuid();
        var crewApresMidi = Guid.NewGuid();

        var erp = new FakeErp(
            new[] { crewMatin, crewApresMidi },
            new Dictionary<Guid, ErpCrewFullDto>
            {
                [crewMatin] = Crew(crewMatin, jeremy),
                [crewApresMidi] = Crew(crewApresMidi, jeremy),
            });

        var result = new MobileIdentityResolver(erp).ResolveActiveCrewIds(jeremy, new DateOnly(2026, 7, 6));

        result.Should().BeEquivalentTo(new[] { crewMatin, crewApresMidi });
    }
}
