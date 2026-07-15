using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// DET-1 — mapping d'un lieu ERP (<see cref="ErpStageDto"/>) vers le lieu structuré mobile
/// (<c>ClJobLocation</c>), via <see cref="JobRepository.GetJob"/>.
///
/// <para>Deux contrats sous test : le <b>service</b> est porté par son champ dédié (et non plus
/// concaténé dans <c>BatEtage</c>), et les <b>coordonnées</b> ne sont exposées que si l'ERP a
/// réellement géocodé le lieu — une coordonnée absente doit rester absente, jamais 0/0.</para>
/// </summary>
public class JobLocationMappingTests
{
    private static readonly Guid JobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ── Fakes ─────────────────────────────────────────────────────────────────
    private sealed class FakeErp : IErpReadApiClient
    {
        public ErpStageDto? Pickup;

        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpMissionFullDto?>(new ErpMissionFullDto
            {
                Id = id,
                OrderId = OrderId,
                MissionDate = new DateOnly(2026, 7, 15),
                SchedulingTime = new TimeOnly(9, 30),
                Pickup = Pickup,
                Dropoff = null
            });

        public Task<ErpOrderEditDto?> GetOrderAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpOrderEditDto?>(new ErpOrderEditDto { Order = new ErpOrderBodyDto() });

        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<ErpBeneficiaryDetailDto?>(null);

        // Membres non exercés par le détail mission.
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsAsync(DateTime f, DateTime t, int take, IReadOnlyCollection<Guid>? crews = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid p, DateOnly d, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid sub, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int?> GetMissionTransferStatusAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeJobTime : IJobTimeRepository
    {
        public void Save(Guid gJobId, ClJobTimeData timeData) { }
        public ClJobTimeData GetJobTimeData(Guid gJobId) => null!;
    }

    private sealed class FakeSignature : ISignatureRepository
    {
        public HashSet<Guid> ExistingFor(IEnumerable<Guid> ids) => new();
        public ClSignatureDto Fetch(Guid id) => throw new NotSupportedException();
        public void Insert(Guid id, string d) => throw new NotSupportedException();
        public void Update(Guid id, string d) => throw new NotSupportedException();
        public void Delete(Guid id, string d) => throw new NotSupportedException();
        public bool Exists(Guid id) => false;
    }

    private sealed class FakeOverlay : IJobAttributeOverlay
    {
        public ClContractType BuildContractType(Guid missionId, IDictionary<string, IEnumerable<string>> erpBaselines) => null!;
        public void Save(Guid missionId, ClContractType contractType, IDictionary<string, IEnumerable<string>> erpBaselines) { }
        public IReadOnlyList<ClContractType> GetContracts() => throw new NotSupportedException();
        public int? GetSelectedContractId(Guid missionId) => null;
        public void SelectContract(Guid missionId, int contractId) { }
    }

    private static ClJob BuildJob(ErpStageDto stage)
    {
        var erp = new FakeErp { Pickup = stage };
        var repo = new JobRepository(erp, new FakeJobTime(), new FakeSignature(), new FakeOverlay(), NullLogger<JobRepository>.Instance);
        return repo.GetJob(JobId);
    }

    private static ClJobLocation MapPickup(ErpStageDto stage) => BuildJob(stage).PickupLocation;

    /// <summary>Lieu tel que le mobile le reçoit réellement (domaine → DTO exposé).</summary>
    private static ClJobDetailModel.ClJobLocationDto PickupDto(ErpStageDto stage)
        => new ClJobDetailAdapter(BuildJob(stage)).PickupLocation;

    // ── Le service quitte BatEtage (cœur de DET-1) ────────────────────────────

    [Fact]
    public void Etablissement_avec_service_et_ligne3_les_separe()
    {
        var loc = MapPickup(new ErpStageDto
        {
            LocationName = "CHITS CH SAINTE MUSSE",
            ServiceLabel = "CARDIOLOGIE",
            AddressLine1 = "54 R HENRI SAINTE CLAIRE DEVILLE",
            AddressLine3 = "CS 31412",
            PostalCode = "83000",
            City = "Toulon"
        });

        loc.Service.Should().Be("CARDIOLOGIE");
        // La régression que ce test verrouille : BatEtage ne doit plus absorber le service.
        loc.BatEtage.Should().Be("CS 31412");
        loc.Commune.Should().Be("83000 Toulon");
    }

    [Fact]
    public void Adresse_beneficiaire_na_pas_de_service()
    {
        var loc = MapPickup(new ErpStageDto
        {
            LocationName = "Domicile",
            ServiceLabel = null,
            AddressLine1 = "12 rue des Lilas",
            AddressLine3 = "Bat B",
            PostalCode = "83100",
            City = "Toulon"
        });

        loc.Service.Should().BeEmpty();
        loc.BatEtage.Should().Be("Bat B");
    }

    // ── Coordonnées ───────────────────────────────────────────────────────────

    [Fact]
    public void Lieu_geocode_expose_ses_coordonnees()
    {
        var loc = MapPickup(new ErpStageDto
        {
            LocationName = "CHITS CH SAINTE MUSSE",
            AddressLine1 = "54 R HENRI SAINTE CLAIRE DEVILLE",
            Latitude = 43.1242,
            Longitude = 5.9280
        });

        loc.Latitude.Should().Be(43.1242);
        loc.Longitude.Should().Be(5.9280);
    }

    [Fact]
    public void Lieu_non_geocode_na_pas_de_coordonnees()
    {
        var loc = MapPickup(new ErpStageDto
        {
            LocationName = "EHPAD LES TAMARIS",
            AddressLine1 = "3 chemin du Puits",
            Latitude = null,
            Longitude = null
        });

        // Absentes, surtout pas 0/0 (golfe de Guinée).
        loc.Latitude.Should().BeNull();
        loc.Longitude.Should().BeNull();
    }

    // ── Repli label figé ──────────────────────────────────────────────────────

    [Fact]
    public void Sans_aucun_champ_structure_le_label_fige_atterrit_dans_Nom()
    {
        var loc = MapPickup(new ErpStageDto
        {
            Label = "EHPAD LES TAMARIS - CHAM 38 RDC - La Valette-du-Var"
        });

        loc.Nom.Should().Be("EHPAD LES TAMARIS - CHAM 38 RDC - La Valette-du-Var");
        loc.Adresse.Should().BeEmpty();
    }

    [Fact]
    public void Un_service_seul_suffit_a_ne_pas_declencher_le_repli()
    {
        // Service est un champ structuré : il ne doit pas être écrasé par le label figé.
        var loc = MapPickup(new ErpStageDto
        {
            ServiceLabel = "URGENCES",
            Label = "LABEL FIGE A NE PAS UTILISER"
        });

        loc.Service.Should().Be("URGENCES");
        loc.Nom.Should().BeEmpty();
    }

    // ── Contrat exposé au mobile (DTO) ────────────────────────────────────────

    [Fact]
    public void Dto_geocode_porte_le_sous_objet_Coordinates()
    {
        var dto = PickupDto(new ErpStageDto
        {
            LocationName = "CHITS CH SAINTE MUSSE",
            ServiceLabel = "CARDIOLOGIE",
            AddressLine3 = "CS 31412",
            Latitude = 43.1242,
            Longitude = 5.9280
        });

        dto.Service.Should().Be("CARDIOLOGIE");
        dto.BatEtage.Should().Be("CS 31412");
        dto.Coordinates.Should().NotBeNull();
        dto.Coordinates!.Latitude.Should().Be(43.1242);
        dto.Coordinates.Longitude.Should().Be(5.9280);
    }

    [Fact]
    public void Dto_non_geocode_omet_le_sous_objet_Coordinates()
    {
        var dto = PickupDto(new ErpStageDto
        {
            LocationName = "EHPAD LES TAMARIS",
            Latitude = null,
            Longitude = null
        });

        // Absent plutôt que { 0, 0 } : l'UI teste la présence pour savoir si le lieu est cartographiable.
        dto.Coordinates.Should().BeNull();
    }

    // ── DET-2 : affichage piloté serveur (sections de lignes) ─────────────────

    /// <summary>Affichage tel que le mobile le reçoit (domaine → graphe piloté serveur).</summary>
    private static ClLocationDisplayDto PickupDisplay(ErpStageDto stage)
        => new ClJobDetailAdapter(BuildJob(stage)).PickupDisplay;

    [Fact]
    public void Display_compose_la_section_identite_puis_la_section_adresse()
    {
        var d = PickupDisplay(new ErpStageDto
        {
            LocationName = "CHITS CH SAINTE MUSSE",
            ServiceLabel = "CARDIOLOGIE",
            AddressLine1 = "54 R HENRI SAINTE CLAIRE DEVILLE",
            AddressLine3 = "CS 31412",
            PostalCode = "83000",
            City = "Toulon"
        });

        d.Blocks.Should().HaveCount(2);
        // Section identité : nom en gras (index 1) puis service labellisé (index 2).
        d.Blocks[0][0].Value.Should().Be("CHITS CH SAINTE MUSSE");
        d.Blocks[0][0].IsBold.Should().BeTrue();
        d.Blocks[0][0].Index.Should().Be(1);
        d.Blocks[0][1].Label.Should().Be("Service");
        d.Blocks[0][1].Value.Should().Be("CARDIOLOGIE");
        // Section adresse : les champs non vides.
        d.Blocks[1].Select(l => l.Value).Should()
            .Contain(new[] { "54 R HENRI SAINTE CLAIRE DEVILLE", "CS 31412", "83000 Toulon" });
    }

    [Fact]
    public void Display_omet_les_lignes_vides()
    {
        // Adresse bénéficiaire (pas de service) → la section identité n'a que le nom, pas de ligne Service.
        var d = PickupDisplay(new ErpStageDto
        {
            LocationName = "Domicile",
            ServiceLabel = null,
            AddressLine1 = "12 rue des Lilas",
            PostalCode = "83100",
            City = "Toulon"
        });

        d.Blocks[0].Should().HaveCount(1);
        d.Blocks[0][0].Value.Should().Be("Domicile");
        d.Blocks.SelectMany(b => b).Should().NotContain(l => l.Label == "Service");
    }

    [Fact]
    public void Display_geocode_porte_le_sous_objet_Coordinates()
    {
        var d = PickupDisplay(new ErpStageDto
        {
            LocationName = "CHITS CH SAINTE MUSSE",
            Latitude = 43.1242,
            Longitude = 5.9280
        });

        d.Coordinates.Should().NotBeNull();
        d.Coordinates!.Latitude.Should().Be(43.1242);
        d.Coordinates.Longitude.Should().Be(5.9280);
    }

    [Fact]
    public void Display_non_geocode_omet_les_Coordinates()
    {
        var d = PickupDisplay(new ErpStageDto
        {
            LocationName = "EHPAD LES TAMARIS",
            Latitude = null,
            Longitude = null
        });

        d.Coordinates.Should().BeNull();
    }
}
