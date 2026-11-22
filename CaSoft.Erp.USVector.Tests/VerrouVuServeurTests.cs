using System.Security.Claims;
using CaSoft.Erp.USVector.Api.Controllers;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Contracts;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Framework;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// Verrou « Vu », côté serveur (22/11) : on ne déclare « vue » que la mission d'un de ses équipages,
/// et le « vu » ne s'efface plus (« un seul Vu »).
/// </summary>
public class VerrouVuServeurTests
{
    private static readonly Guid Sub = Guid.NewGuid();
    private static readonly Guid Personnel = Guid.NewGuid();

    private sealed class Identite : IMobileIdentityResolver
    {
        public bool Accessible;
        public Guid? MissionDemandee;
        public Task<Guid?> ResolvePersonnelIdAsync(Guid keyCloakSub, CancellationToken ct)
            => Task.FromResult<Guid?>(keyCloakSub == Sub ? Personnel : null);
        public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        public Task<IReadOnlyList<Guid>> ResolveActiveCrewIdsFreshAsync(Guid personnelId, DateOnly onDate, CancellationToken ct)
            => ResolveActiveCrewIdsAsync(personnelId, onDate, ct);
        public Task<bool> IsMissionAccessibleAsync(Guid personnelId, Guid missionId, CancellationToken ct)
        {
            MissionDemandee = missionId;
            return Task.FromResult(Accessible && personnelId == Personnel);
        }
    }

    private sealed class Missions : IJobService
    {
        public int Vues;
        public Task<ClResult<bool>> MarkMissionSeenAsync(Guid gJobId, DateTimeOffset? seenAt, CancellationToken ct)
        {
            Vues++;
            return Task.FromResult(ClResult<bool>.Ok(true));
        }
        public Task<ClResult<ClJobTimeModel>> GetJobTimeAsync(Guid gJobId, CancellationToken ct) => throw new NotSupportedException();
        public Task<ClResult<ClJobTimelineDtoOut>> GetJobTimelineAsync(Guid gJobId, CancellationToken ct) => throw new NotSupportedException();
        public Task<ClResult<bool>> SetJobTimeAsync(Guid gJobId, ClJobTimeModel jobTime, CancellationToken ct) => throw new NotSupportedException();
        public Task<ClResult<bool>> ClearJobTimeAsync(Guid gJobId, string jalon, CancellationToken ct) => throw new NotSupportedException();
    }

    private static JobListController Controleur(Identite identite, Missions missions, bool avecJeton = true)
    {
        var http = new DefaultHttpContext();
        if (avecJeton)
        {
            http.Request.Headers.Authorization = "Bearer x";
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", Sub.ToString()) }, "test"));
        }
        return new JobListController(null!, missions, identite, NullLogger<JobListController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task Une_mission_hors_de_ses_equipages_ne_se_declare_pas_vue()
    {
        var missions = new Missions();
        var mission = Guid.NewGuid();

        var reponse = await Controleur(new Identite { Accessible = false }, missions)
            .PatchSeen(new ClReadJobModel { IsJob = true, JobId = mission }, CancellationToken.None);

        reponse.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        missions.Vues.Should().Be(0, "aucun état « vu » ne doit naître, même orphelin");
    }

    [Fact]
    public async Task Une_mission_de_ses_equipages_se_declare_vue()
    {
        var missions = new Missions();
        var identite = new Identite { Accessible = true };
        var mission = Guid.NewGuid();

        var reponse = await Controleur(identite, missions)
            .PatchSeen(new ClReadJobModel { IsJob = true, JobId = mission }, CancellationToken.None);

        ((reponse as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode ?? 200).Should().Be(200);
        identite.MissionDemandee.Should().Be(mission);
        missions.Vues.Should().Be(1);
    }

    [Fact]
    public async Task Sans_jeton_le_vu_est_refuse_en_401()
    {
        var missions = new Missions();

        var reponse = await Controleur(new Identite { Accessible = true }, missions, avecJeton: false)
            .PatchSeen(new ClReadJobModel { IsJob = true, JobId = Guid.NewGuid() }, CancellationToken.None);

        reponse.Should().BeOfType<UnauthorizedObjectResult>();
        missions.Vues.Should().Be(0);
    }

    private sealed class Jalons : IJobRepository
    {
        public ClJobTimeData Stocke = ClJobTimeData.GetBuilder().WithId(Guid.NewGuid())
            .WithReadTime(new DateTime(2026, 11, 22, 8, 0, 0)).WithGoTime(new DateTime(2026, 11, 22, 8, 5, 0)).Build();
        public int Lectures, Enregistrements;
        public Task<ClJob> GetJobAsync(Guid gJobId, CancellationToken ct) => throw new NotSupportedException();
        public Task<ClJobTimeData> GetJobTimeAsync(Guid jobId, CancellationToken ct) { Lectures++; return Task.FromResult(Stocke); }
        public Task SaveJobTimeAsync(ClJobTimeData jobTime, CancellationToken ct) { Enregistrements++; Stocke = jobTime; return Task.CompletedTask; }
    }

    [Theory]
    [InlineData("seen")]
    [InlineData("read")]
    [InlineData(" SEEN ")]
    public async Task Le_vu_ne_s_efface_pas(string jalon)
    {
        var jalons = new Jalons();
        var unitOfWork = new UnitOfWorkDoubles.Counting();

        var result = await new ClClearJobTimeUseCase(Guid.NewGuid(), jalon, jalons, unitOfWork).HandleAsync(CancellationToken.None);

        result.IsFail.Should().BeTrue();
        result.InnerError!.ErrorText.Should().Be("Le « vu » d'une mission ne s'efface pas.");
        jalons.Enregistrements.Should().Be(0);
        unitOfWork.Commits.Should().Be(0);
        jalons.Stocke.ReadTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Les_autres_jalons_s_effacent_toujours()
    {
        var jalons = new Jalons();
        var unitOfWork = new UnitOfWorkDoubles.Counting();

        var result = await new ClClearJobTimeUseCase(Guid.NewGuid(), "go", jalons, unitOfWork).HandleAsync(CancellationToken.None);

        result.IsSucces.Should().BeTrue();
        jalons.Stocke.GoTime.Should().BeNull();
        jalons.Stocke.ReadTime.Should().NotBeNull("effacer « en route » ne touche pas au « vu »");
        unitOfWork.Commits.Should().Be(1);
    }
}
