using System.Security.Cryptography;

namespace CaSoft.Erp.USVector.Infrastructure.Simulation;

/// <summary>
/// Plage d'identifiants RÉSERVÉE au mode formation : tout ce qui est simulé (personnel, équipage, mission,
/// commande, patient) porte un GUID de cette plage, et rien d'autre ne la porte. C'est ce qui sépare les
/// deux mondes, partout — y compris dans le worker de projection, qui n'a pas de requête HTTP : un
/// identifiant suffit à savoir s'il va au catalogue simulé ou au vrai Orders.
/// <para>
/// Disposition : <c>5E5E5E5E-KKKK-DDDD-SSOO-OOOOOOOOOOOO</c> — préfixe fixe, nature (K), jour (D, en jours
/// depuis le 01/01/2026), emplacement dans le jeu du jour (S), propriétaire sur 7 octets (O). Tout se
/// déduit de l'identifiant : le catalogue n'a rien à stocker, il survit à un recyclage du pool.
/// </para>
/// <para>
/// La purge SQL lit la même disposition (<see cref="SqlPrefix"/>, jour en 15e position de la forme texte) :
/// la changer impose de changer <see cref="ClSimulationPurge"/> dans le même commit.
/// </para>
/// </summary>
public static class ClSimulationIds
{
    private const int Prefix = 0x5E5E5E5E;

    /// <summary>Début de la forme texte (majuscules, comme la rend <c>CONVERT(char(36), …)</c> de SQL Server).</summary>
    public const string SqlPrefix = "5E5E5E5E-";

    private static readonly DateOnly Origin = new(2026, 1, 1);

    public enum EnKind : short
    {
        Personnel = 1,
        Teammate = 2,
        Crew = 3,
        Mission = 4,
        Order = 5,
        Beneficiary = 6,
        Vehicle = 7
    }

    public static bool IsSimulated(Guid id) => id != Guid.Empty && ReadPrefix(id) == Prefix;

    public static bool IsSimulated(Guid? id) => id.HasValue && IsSimulated(id.Value);

    public static EnKind KindOf(Guid id) => (EnKind)BitConverter.ToInt16(id.ToByteArray(), 4);

    public static DateOnly DayOf(Guid id) => Origin.AddDays(BitConverter.ToInt16(id.ToByteArray(), 6));

    public static int SlotOf(Guid id) => id.ToByteArray()[8];

    /// <summary>Les 7 octets du propriétaire : un stagiaire (dérivé de son compte) ou un persona.</summary>
    public static byte[] OwnerOf(Guid id) => id.ToByteArray()[9..16];

    public static int DayNumber(DateOnly day) => day.DayNumber - Origin.DayNumber;

    public static Guid Create(EnKind kind, byte[] owner, DateOnly day = default, int slot = 0)
    {
        if (owner.Length != 7) throw new ArgumentException("Le propriétaire tient sur 7 octets.", nameof(owner));
        var dayNumber = day == default ? 0 : DayNumber(day);
        return new Guid(Prefix, (short)kind, (short)dayNumber, (byte)slot,
            owner[0], owner[1], owner[2], owner[3], owner[4], owner[5], owner[6]);
    }

    /// <summary>Même jour, même emplacement, même propriétaire, autre nature (mission → commande → patient).</summary>
    public static Guid Related(Guid id, EnKind kind) => Create(kind, OwnerOf(id), DayOf(id), SlotOf(id));

    /// <summary>
    /// Personnel fictif d'un ambulancier en formation, dérivé de son compte Keycloak : stable d'un jour et
    /// d'une session à l'autre, sans rien stocker, et sans appel à Orders.
    /// </summary>
    public static Guid TraineeFor(Guid keycloakSub)
        => Create(EnKind.Personnel, SHA256.HashData(keycloakSub.ToByteArray())[..7]);

    /// <summary>Personnel d'un persona sans compte (développement de l'app), numéroté.</summary>
    public static Guid Persona(int number)
        => Create(EnKind.Personnel, new byte[] { 0xA0, (byte)number, 0, 0, 0, 0, 0 });

    /// <summary>Numéro du persona, ou null si le personnel est celui d'un stagiaire.</summary>
    public static int? PersonaNumberOf(Guid personnelId)
    {
        var owner = OwnerOf(personnelId);
        return owner[0] == 0xA0 && owner[2..].All(b => b == 0) ? owner[1] : null;
    }

    /// <summary>
    /// Motif SQL <c>LIKE</c> des lignes d'un propriétaire pour un jour, toutes natures confondues
    /// (remise à zéro d'une session).
    /// </summary>
    public static string SqlPatternFor(byte[] owner, DateOnly day)
        => $"{SqlPrefix}____-{(ushort)DayNumber(day):X4}-__{owner[0]:X2}-{Convert.ToHexString(owner[1..])}";

    private static int ReadPrefix(Guid id) => BitConverter.ToInt32(id.ToByteArray(), 0);
}
