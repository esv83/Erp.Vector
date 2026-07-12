using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Mapping;

namespace CaSoft.Erp.USVector.Infrastructure.Diagnostics;

/// <summary>
/// Outil de diagnostic développeur (dev-only) : déroule la chaîne
/// <c>sub Keycloak → PER_ID → crews candidats → membre ? → actif ?</c> en exécutant le VRAI
/// client Orders.Api et la VRAIE règle domaine <see cref="ClCrew.IsSelectableAt"/>, et expose
/// le résultat de chaque maillon (dont le motif d'exclusion par équipage). Lecture fraîche : on
/// appelle directement <see cref="IErpReadApiClient"/> (pas le cache) pour refléter l'état réel.
/// </summary>
public sealed class CrewChainDiagnostic
{
    private readonly IErpReadApiClient _erp;

    public CrewChainDiagnostic(IErpReadApiClient erp) => _erp = erp;

    public async Task<CrewChainResult> DiagnoseAsync(Guid sub, DateTime at, CancellationToken ct = default)
    {
        var result = new CrewChainResult { Sub = sub, At = at };
        try
        {
            // Maillon 1 : sub → PER_ID (GET /personnel/by-keycloak/{sub}).
            var personnelId = await _erp.ResolvePersonnelIdByKeycloakAsync(sub, ct);
            result.PersonnelId = personnelId;
            result.Step1Ok = personnelId is not null;
            if (personnelId is null)
                return result; // compte non rattaché → la chaîne s'arrête ici.

            // Maillon 2 : PER_ID → crews candidats du jour (GET /crews?personnelId=&date=).
            var candidateIds = await _erp.ListCrewIdsAsync(personnelId.Value, DateOnly.FromDateTime(at), 500, ct);
            result.CandidateCount = candidateIds.Count;

            foreach (var id in candidateIds)
            {
                var dto = await _erp.GetCrewFullAsync(id, ct);
                var crewInfo = new CrewChainCrew { CrewId = id, Found = dto is not null };

                if (dto is not null)
                {
                    var crew = dto.ToDomain(); // même mapping que la prod

                    crewInfo.VehicleImmat = dto.Vehicle?.Immatriculation;
                    crewInfo.ServiceStart = dto.ServiceStart;
                    crewInfo.ServiceEnd = dto.ServiceEnd;
                    crewInfo.IsMember = dto.Members.Any(m => m.Id == personnelId.Value);
                    crewInfo.Started = dto.ServiceStart <= at;
                    crewInfo.NotClosed = !(dto.ServiceEnd.HasValue && dto.ServiceEnd.Value < at);
                    crewInfo.NotObsolete = at - dto.ServiceStart <= TimeSpan.FromHours(ClCrew.MaxServiceDurationHours);
                    crewInfo.Selectable = crew.IsSelectableAt(at); // règle domaine réelle
                    crewInfo.Members = dto.Members
                        .Select(m => new CrewChainMember
                        {
                            PersonnelId = m.Id,
                            Name = $"{m.FirstName} {m.LastName}".Trim(),
                            IsTarget = m.Id == personnelId.Value
                        })
                        .ToList();
                    crewInfo.Verdict = Verdict(crewInfo);
                }
                else
                {
                    crewInfo.Verdict = "Introuvable côté ERP";
                }

                result.Crews.Add(crewInfo);
            }

            // Ce que le sélecteur /api/crew/mine retiendrait réellement : membre ET sélectionnable.
            result.SelectableCrewIds = result.Crews
                .Where(c => c.IsMember && c.Selectable)
                .Select(c => c.CrewId)
                .ToList();
        }
        catch (Exception ex)
        {
            result.Error = $"{ex.GetType().Name} : {ex.Message}";
        }

        return result;
    }

    private static string Verdict(CrewChainCrew c)
    {
        if (!c.IsMember) return "Exclu : le personnel n'est pas membre de cet équipage";
        if (!c.Started) return "Exclu : service pas encore démarré";
        if (!c.NotClosed) return "Exclu : service clôturé (fin de service dépassée)";
        if (!c.NotObsolete) return $"Exclu : obsolète (durée > {ClCrew.MaxServiceDurationHours} h)";
        return "Sélectionnable";
    }
}

public sealed class CrewChainResult
{
    public Guid Sub { get; set; }
    public DateTime At { get; set; }
    public Guid? PersonnelId { get; set; }
    public bool Step1Ok { get; set; }
    public int CandidateCount { get; set; }
    public string? Error { get; set; }
    public List<CrewChainCrew> Crews { get; set; } = new();
    public List<Guid> SelectableCrewIds { get; set; } = new();
}

public sealed class CrewChainCrew
{
    public Guid CrewId { get; set; }
    public bool Found { get; set; }
    public string? VehicleImmat { get; set; }
    public DateTime? ServiceStart { get; set; }
    public DateTime? ServiceEnd { get; set; }
    public bool IsMember { get; set; }
    public bool Started { get; set; }
    public bool NotClosed { get; set; }
    public bool NotObsolete { get; set; }
    public bool Selectable { get; set; }
    public string Verdict { get; set; } = string.Empty;
    public List<CrewChainMember> Members { get; set; } = new();
}

public sealed class CrewChainMember
{
    public Guid PersonnelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTarget { get; set; }
}
