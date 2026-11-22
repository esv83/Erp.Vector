using System.Collections.Concurrent;

namespace CaSoft.Erp.USVector.Infrastructure.Simulation;

/// <summary>
/// Ce que le terrain a changé chez le « faux Orders » au cours d'une formation : statut des missions,
/// conducteur désigné, context choisi, attributs saisis. Singleton, en mémoire.
/// <para>
/// Perdu à un recyclage du pool : une mission revient alors « à faire » côté Orders simulé. Gêne admise —
/// la liste terrain superpose de toute façon l'état de la base Mobile (<c>MOB_MISSION_STATE</c>), qui, lui,
/// est conservé jusqu'à la purge de la nuit.
/// </para>
/// </summary>
public sealed class ClSimulationState
{
    private readonly ConcurrentDictionary<Guid, int> _missionStatus = new();
    private readonly ConcurrentDictionary<Guid, (Guid PersonnelId, DateTime From)> _crewDriver = new();
    private readonly ConcurrentDictionary<Guid, int> _contextOrder = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, (string? Value, string? OtherText)>> _values = new();

    public int? MissionStatus(Guid missionId) => _missionStatus.TryGetValue(missionId, out var s) ? s : null;

    /// <summary>Le statut ne recule jamais : une projection rejouée ou en retard n'efface pas l'avancement.</summary>
    public void AdvanceMissionStatus(Guid missionId, int status)
        => _missionStatus.AddOrUpdate(missionId, status, (_, current) => Math.Max(current, status));

    public (Guid PersonnelId, DateTime From)? CrewDriver(Guid crewId)
        => _crewDriver.TryGetValue(crewId, out var d) ? d : null;

    public void SetCrewDriver(Guid crewId, Guid personnelId, DateTime from) => _crewDriver[crewId] = (personnelId, from);

    public int? ContextOrder(Guid missionId) => _contextOrder.TryGetValue(missionId, out var c) ? c : null;

    public void SetContextOrder(Guid missionId, int contextOrderId) => _contextOrder[missionId] = contextOrderId;

    public (string? Value, string? OtherText)? Value(Guid missionId, string name)
        => _values.TryGetValue(missionId, out var fields) && fields.TryGetValue(name, out var v) ? v : null;

    public void SetValue(Guid missionId, string name, string? value, string? otherText)
        => _values.GetOrAdd(missionId, _ => new(StringComparer.OrdinalIgnoreCase))[name] = (value, otherText);

    /// <summary>Oublie tout ce qui vérifie <paramref name="match"/> (remise à zéro, purge de la nuit).</summary>
    public void Forget(Func<Guid, bool> match)
    {
        foreach (var key in _missionStatus.Keys.Where(match)) _missionStatus.TryRemove(key, out _);
        foreach (var key in _crewDriver.Keys.Where(match)) _crewDriver.TryRemove(key, out _);
        foreach (var key in _contextOrder.Keys.Where(match)) _contextOrder.TryRemove(key, out _);
        foreach (var key in _values.Keys.Where(match)) _values.TryRemove(key, out _);
    }
}
