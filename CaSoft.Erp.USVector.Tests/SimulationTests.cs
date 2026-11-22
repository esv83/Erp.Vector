using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CaSoft.Erp.USVector.Api.Controllers;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using CaSoft.Erp.USVector.Infrastructure.Simulation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;
using static CaSoft.Erp.USVector.Infrastructure.Simulation.ClSimulationIds;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Mode formation : la plage d'identifiants réservée sépare les deux mondes. Ces tests tiennent les trois
/// promesses du mode — une mission fictive n'atteint jamais Orders, une requête de formation ne touche
/// jamais une vraie mission, et la purge SQL lit les identifiants comme le code les écrit.
/// </summary>
public class SimulationTests
{
    private static readonly DateOnly Today = new(2026, 11, 22);

    // ── Plage d'identifiants ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Un_identifiant_simule_se_relit_entierement()
    {
        var owner = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
        var id = Create(EnKind.Mission, owner, Today, 4);

        IsSimulated(id).Should().BeTrue();
        KindOf(id).Should().Be(EnKind.Mission);
        DayOf(id).Should().Be(Today);
        SlotOf(id).Should().Be(4);
        OwnerOf(id).Should().Equal(owner);
        id.ToString().Should().StartWith("5e5e5e5e-");
    }

    [Fact]
    public void Un_vrai_identifiant_n_est_jamais_simule()
    {
        IsSimulated(Guid.NewGuid()).Should().BeFalse();
        IsSimulated(Guid.Empty).Should().BeFalse();
        IsSimulated((Guid?)null).Should().BeFalse();
    }

    [Fact]
    public void Le_stagiaire_garde_le_meme_personnel_fictif_et_ne_croise_pas_un_autre()
    {
        var sub = Guid.NewGuid();
        TraineeFor(sub).Should().Be(TraineeFor(sub));
        TraineeFor(sub).Should().NotBe(TraineeFor(Guid.NewGuid()));
        PersonaNumberOf(TraineeFor(sub)).Should().BeNull();
        PersonaNumberOf(Persona(3)).Should().Be(3);
    }

    /// <summary>La purge SQL lit le jour en positions 15 à 18 de la forme texte : c'est ce qu'écrit le code.</summary>
    [Fact]
    public void La_purge_SQL_lit_le_jour_la_ou_le_code_l_ecrit()
    {
        var text = Create(EnKind.Mission, new byte[7], Today, 2).ToString().ToUpperInvariant();

        text.Should().StartWith(SqlPrefix);
        Convert.ToInt32(text.Substring(14, 4), 16).Should().Be(DayNumber(Today));
    }

    [Fact]
    public void Le_motif_de_remise_a_zero_couvre_toutes_les_natures_d_un_proprietaire_et_d_un_jour_seulement()
    {
        var personnel = Persona(2);
        var pattern = SqlPatternFor(OwnerOf(personnel), Today);
        var like = new Regex("^" + Regex.Escape(pattern).Replace("_", ".") + "$");

        foreach (var kind in new[] { EnKind.Mission, EnKind.Crew, EnKind.Beneficiary })
            like.IsMatch(Create(kind, OwnerOf(personnel), Today, 3).ToString().ToUpperInvariant()).Should().BeTrue();

        like.IsMatch(Create(EnKind.Mission, OwnerOf(personnel), Today.AddDays(-1), 3).ToString().ToUpperInvariant()).Should().BeFalse();
        like.IsMatch(Create(EnKind.Mission, OwnerOf(Persona(3)), Today, 3).ToString().ToUpperInvariant()).Should().BeFalse();
    }

    // ── Faux Orders ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Un_persona_a_un_equipage_du_jour_avec_tout_le_jeu_de_missions()
    {
        var erp = new ClSimulatedOrdersReadClient(new ClSimulationState());
        var personnel = await erp.ResolvePersonnelIdByKeycloakAsync(Persona(1));

        var crews = await erp.ListCrewIdsAsync(personnel!.Value, Today, 500);
        var crew = await erp.GetCrewFullAsync(crews.Single());
        var missions = await erp.ListMissionsByCrewAsync(crew!.Id);

        crew.Members.Select(m => m.Id).Should().Contain(personnel.Value);
        missions.Should().HaveCount(ClSimulationScenarios.All.Count);
        missions.Should().OnlyContain(m => IsSimulated(m.Id) && m.AssignedCrewId == crew.Id && m.MissionDate == Today);
    }

    [Fact]
    public async Task La_chaine_d_acces_reelle_autorise_le_stagiaire_sur_ses_missions_sans_jamais_appeler_Orders()
    {
        var state = new ClSimulationState();
        var router = new ClSimulationRoutingErpReadApiClient(new ForbiddenOrders(), new ClSimulatedOrdersReadClient(state));
        var resolver = new MobileIdentityResolver(router);
        var trainee = TraineeFor(Guid.NewGuid());
        var crewId = (await router.ListCrewIdsAsync(trainee, Today, 500)).Single();
        var mission = (await router.ListMissionsByCrewAsync(crewId)).First();

        (await resolver.IsMissionAccessibleAsync(trainee, mission.Id, CancellationToken.None)).Should().BeTrue();
        (await resolver.IsMissionAccessibleAsync(TraineeFor(Guid.NewGuid()), mission.Id, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Une_vraie_mission_part_chez_Orders_et_un_lot_mixte_se_partage()
    {
        var real = new RecordingOrders();
        var router = new ClSimulationRoutingErpReadApiClient(real, new ClSimulatedOrdersReadClient(new ClSimulationState()));
        var realMission = Guid.NewGuid();
        var simulatedMission = Create(EnKind.Mission, new byte[7], Today, 0);

        await router.GetMissionFullAsync(realMission);
        var refs = await router.GetMissionBatchRefsAsync(new[] { realMission, simulatedMission });

        real.Seen.Should().Equal(realMission, realMission);
        refs!.Select(r => r.MissionId).Should().Contain(simulatedMission);
    }

    [Fact]
    public async Task La_projection_terrain_fait_avancer_le_statut_sans_jamais_le_faire_reculer()
    {
        var state = new ClSimulationState();
        var write = new ClSimulatedOrdersWriteClient(state);
        var read = new ClSimulatedOrdersReadClient(state);
        var crew = Create(EnKind.Crew, new byte[7], Today);
        var mission = Create(EnKind.Mission, new byte[7], Today, 0);

        (await write.ProjectOperationalAsync(mission, null, null, DateTime.Now, null, null, crew)).Should().Be(EnOperationalProjectionOutcome.Applied);
        (await read.ListMissionsByCrewAsync(crew)).Single(m => m.Id == mission).Status.Should().Be(ClSimulationScenarios.InProgress);

        await write.ProjectOperationalAsync(mission, DateTime.Now, null, null, null, null, crew);
        (await read.ListMissionsByCrewAsync(crew)).Single(m => m.Id == mission).Status.Should().Be(ClSimulationScenarios.InProgress);
    }

    [Fact]
    public async Task Le_faux_Orders_oppose_les_memes_refus_que_le_vrai()
    {
        var write = new ClSimulatedOrdersWriteClient(new ClSimulationState());
        var imposed = Create(EnKind.Mission, new byte[7], Today, IndexOf(s => s.ImposedContextOrderId is not null));
        var locked = Create(EnKind.Mission, new byte[7], Today, IndexOf(s => s.IsBirthDateLocked));

        (await write.SetMissionContextOrderAsync(imposed, 9002)).Should().Be(EnContextOrderWriteOutcome.LockedByRegulator);
        (await write.SetContextOrderValuesAsync(locked, new[] { ("DateNaissance", (string?)"2000-01-01", (string?)null) }))
            .Outcome.Should().Be(EnContextOrderValuesWriteOutcome.FieldLocked);
        (await write.SetCrewDriverAsync(Create(EnKind.Crew, new byte[7], Today), Guid.NewGuid(), DateTime.Now))
            .Outcome.Should().Be(EnCrewDriverWriteOutcome.Refused);
    }

    [Fact]
    public async Task Le_mode_formation_rattache_le_compte_a_son_personnel_fictif_et_seulement_sur_demande()
    {
        var sub = Guid.NewGuid();
        var realPersonnel = Guid.NewGuid();
        var isTraining = false;
        var resolver = new ClSimulationIdentityResolver(new FixedIdentity(realPersonnel), () => isTraining);

        (await resolver.ResolvePersonnelIdAsync(sub, CancellationToken.None)).Should().Be(realPersonnel);
        isTraining = true;
        (await resolver.ResolvePersonnelIdAsync(sub, CancellationToken.None)).Should().Be(TraineeFor(sub));
        (await resolver.ResolvePersonnelIdAsync(Persona(1), CancellationToken.None)).Should().Be(Persona(1));
    }

    // ── Personas ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Un_jeton_de_persona_donne_un_personnel_fictif_avec_l_azp_mobile()
    {
        var persona = VectorJwtBearerEvents.PersonaFor("Bearer simulation-persona-2", "us-ambulance");

        persona!.Identity!.IsAuthenticated.Should().BeTrue();
        persona.FindFirst("sub")!.Value.Should().Be(Persona(2).ToString());
        persona.FindFirst(ClKeycloakCallers.AzpClaim)!.Value.Should().Be("us-ambulance");
        persona.FindFirst(SimulationRequest.PersonaClaim).Should().NotBeNull();

        VectorJwtBearerEvents.PersonaFor("Bearer eyJhbGciOi.vrai.jeton", "us-ambulance").Should().BeNull();
        VectorJwtBearerEvents.PersonaFor("Bearer simulation-persona-x", "us-ambulance").Should().BeNull();
    }

    // ── Cantonnement ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Une_requete_de_formation_ne_touche_jamais_une_vraie_mission()
    {
        var context = Executing(typeof(TimeController), training: true, ("gJobId", Guid.NewGuid()));

        await Filter(enabled: true).OnActionExecutionAsync(context, Next);

        context.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Une_requete_de_formation_passe_sur_une_mission_fictive()
    {
        var context = Executing(typeof(TimeController), training: true, ("gJobId", Create(EnKind.Mission, new byte[7], Today, 1)));

        await Filter(enabled: true).OnActionExecutionAsync(context, Next);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task Un_lot_d_identifiants_dans_le_corps_est_controle_lui_aussi()
    {
        var body = new FieldDataController.FieldDataBatchQuery { MissionIds = new List<Guid> { Create(EnKind.Mission, new byte[7], Today, 1), Guid.NewGuid() } };
        var context = Executing(typeof(FieldDataController), training: true, ("query", body));

        await Filter(enabled: true).OnActionExecutionAsync(context, Next);

        context.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task La_plateforme_d_urgence_est_fermee_en_formation()
    {
        var context = Executing(typeof(EmergencyPlatformController), training: true, ("strImmat", "FORMATION"));

        await Filter(enabled: true).OnActionExecutionAsync(context, Next);

        context.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Le_mode_coupe_refuse_l_en_tete_au_lieu_de_servir_les_vraies_missions()
    {
        var context = Executing(typeof(TimeController), training: true, ("gJobId", Guid.NewGuid()));

        await Filter(enabled: false).OnActionExecutionAsync(context, Next);

        context.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Une_requete_ordinaire_n_est_pas_concernee()
    {
        var context = Executing(typeof(TimeController), training: false, ("gJobId", Guid.NewGuid()));

        await Filter(enabled: true).OnActionExecutionAsync(context, Next);

        context.Result.Should().BeNull();
    }

    // ── Outillage ───────────────────────────────────────────────────────────────────────────────────

    private static int IndexOf(Func<ClSimulationScenario, bool> match)
        => ClSimulationScenarios.All.Select((s, i) => (s, i)).First(x => match(x.s)).i;

    private static SimulationConfinementFilter Filter(bool enabled) => new(new SimulationOptions { Enabled = enabled });

    private static Task<ActionExecutedContext> Next() => Task.FromResult<ActionExecutedContext>(null!);

    private static ActionExecutingContext Executing(Type controller, bool training, params (string Name, object Value)[] arguments)
    {
        var http = new DefaultHttpContext();
        if (training) http.Request.Headers[SimulationRequest.HeaderName] = "1";
        var action = new ControllerActionDescriptor { ControllerTypeInfo = controller.GetTypeInfo() };
        return new ActionExecutingContext(
            new ActionContext(http, new RouteData(), action),
            new List<IFilterMetadata>(),
            arguments.ToDictionary(a => a.Name, a => (object?)a.Value),
            controller: null!);
    }

    /// <summary>Un Orders qu'on ne doit jamais appeler : toute mission fictive doit rester au catalogue.</summary>
    private class ForbiddenOrders : IErpReadApiClient
    {
        protected virtual T Fail<T>(Guid id) => throw new InvalidOperationException($"Orders appelé pour {id}.");
        public Task<ErpMissionFullDto?> GetMissionFullAsync(Guid missionId, CancellationToken ct = default) => Fail<Task<ErpMissionFullDto?>>(missionId);
        public Task<IReadOnlyList<ErpMissionListItemDto>> ListMissionsByCrewAsync(Guid crewId, CancellationToken ct = default) => Fail<Task<IReadOnlyList<ErpMissionListItemDto>>>(crewId);
        public Task<ErpOrderEditDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default) => Fail<Task<ErpOrderEditDto?>>(orderId);
        public Task<ErpBeneficiaryDetailDto?> GetBeneficiaryAsync(Guid beneficiaryId, CancellationToken ct = default) => Fail<Task<ErpBeneficiaryDetailDto?>>(beneficiaryId);
        public Task<IReadOnlyList<Guid>> ListCrewIdsAsync(Guid personnelId, DateOnly onDate, int take, CancellationToken ct = default) => Fail<Task<IReadOnlyList<Guid>>>(personnelId);
        public Task<ErpCrewFullDto?> GetCrewFullAsync(Guid crewId, CancellationToken ct = default) => Fail<Task<ErpCrewFullDto?>>(crewId);
        public Task<Guid?> ResolvePersonnelIdByKeycloakAsync(Guid keycloakSub, CancellationToken ct = default) => Fail<Task<Guid?>>(keycloakSub);
        public Task<int?> GetMissionTransferStatusAsync(Guid missionId, CancellationToken ct = default) => Fail<Task<int?>>(missionId);
        public Task<ErpMissionContextOrderDto?> GetMissionContextOrderAsync(Guid missionId, CancellationToken ct = default) => Fail<Task<ErpMissionContextOrderDto?>>(missionId);
        public Task<IReadOnlyList<ErpContextOrderFieldDto>?> GetContextOrderFormStructureAsync(Guid missionId, CancellationToken ct = default) => Fail<Task<IReadOnlyList<ErpContextOrderFieldDto>?>>(missionId);
        public Task<IReadOnlyList<ErpMissionBatchRefDto>> GetMissionBatchRefsAsync(IReadOnlyCollection<Guid> missionIds, CancellationToken ct = default) => Fail<Task<IReadOnlyList<ErpMissionBatchRefDto>>>(missionIds.First());
    }

    /// <summary>Un vrai Orders qui note ce qu'on lui demande et ne connaît rien.</summary>
    private sealed class RecordingOrders : ForbiddenOrders
    {
        public List<Guid> Seen { get; } = new();

        protected override T Fail<T>(Guid id)
        {
            Seen.Add(id);
            object result = typeof(T) == typeof(Task<IReadOnlyList<ErpMissionBatchRefDto>?>)
                ? Task.FromResult<IReadOnlyList<ErpMissionBatchRefDto>?>(new List<ErpMissionBatchRefDto>())
                : Task.FromResult<ErpMissionFullDto?>(null);
            return (T)result;
        }
    }

    private sealed class FixedIdentity : IMobileIdentityResolver
    {
        private readonly Guid _personnel;
        public FixedIdentity(Guid personnel) => _personnel = personnel;
        public Task<Guid?> ResolvePersonnelIdAsync(Guid keyCloakSub, CancellationToken ct) => Task.FromResult<Guid?>(_personnel);
        public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsAsync(Guid personnelId, DateOnly onDate, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsFreshAsync(Guid personnelId, DateOnly onDate, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> IsMissionAccessibleAsync(Guid personnelId, Guid missionId, CancellationToken ct) => throw new NotSupportedException();
    }
}
