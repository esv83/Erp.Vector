using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;
using CaSoft.Erp.USVector.Infrastructure.Mapping;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories;

/// <summary>
/// B5 — Silos terrain de plusieurs missions pour le paquet d'enrichissement : <b>une requête par
/// silo</b>, et <b>aucun binaire</b> dans les projections.
/// </summary>
/// <remarks>
/// Avant le 19/09, chaque paquet passait par les dépôts : six requêtes par mission, dont deux qui
/// sortaient de la base ce que le paquet n'annonce que par URL — l'image entière de la signature
/// (~42 Ko) pour en lire la date, le contenu de chaque document pour en lire les métadonnées.
/// Les projections ci-dessous sont nommées colonne par colonne : une colonne binaire ajoutée demain
/// n'y entrera pas par accident.
/// </remarks>
public sealed class FieldDataQueryService : IFieldDataQueryService
{
    private readonly MobileDbContext _ctx;

    public FieldDataQueryService(MobileDbContext ctx) => _ctx = ctx;

    public ClFieldSilos ReadSilos(IReadOnlyCollection<Guid> missionIds, IReadOnlyCollection<Guid> beneficiaryIds)
    {
        var missions = (missionIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var beneficiaries = (beneficiaryIds ?? Array.Empty<Guid>()).Distinct().ToList();

        return new ClFieldSilos
        {
            Timelines = missions.Count == 0
                ? new Dictionary<Guid, ClJobTimeData>()
                : _ctx.MissionStates.AsNoTracking()
                    .Where(s => missions.Contains(s.MST_MISSION_ID))
                    .ToList()
                    .ToDictionary(s => s.MST_MISSION_ID, s => s.ToJobTimeData()),

            // SIG_DATA (l'image) n'est pas lue : seule la date sert au paquet.
            SignedAt = missions.Count == 0
                ? new Dictionary<Guid, DateTime>()
                : _ctx.Signatures.AsNoTracking()
                    .Where(s => missions.Contains(s.SIG_MISSION_ID))
                    .Select(s => new { s.SIG_MISSION_ID, s.SIG_DATETIME })
                    .ToList()
                    .ToDictionary(s => s.SIG_MISSION_ID, s => s.SIG_DATETIME),

            CurrentMutuelles = ReadCurrentMutuelles(beneficiaries),

            // DOC_CONTENT n'est pas lu : le paquet annonce le document par son URL (D8).
            Documents = missions.Count == 0
                ? Array.Empty<ClDocument>().ToLookup(d => d.MissionId)
                : _ctx.Documents.AsNoTracking()
                    .Where(d => missions.Contains(d.DOC_MISSION_ID))
                    .Select(d => new ClDocument
                    {
                        Id = d.DOC_ID,
                        MissionId = d.DOC_MISSION_ID,
                        Category = (EnDocumentCategory)d.DOC_CATEGORY,
                        ContentType = d.DOC_CONTENT_TYPE,
                        ByteSize = d.DOC_BYTE_SIZE,
                        FileName = d.DOC_FILE_NAME,
                        CapturedAt = d.DOC_CAPTURED_AT,
                        CapturedCrewId = d.DOC_CAPTURED_CREW_ID
                    })
                    .ToList()
                    .OrderByDescending(d => d.CapturedAt)
                    .ToLookup(d => d.MissionId),

            Anomalies = missions.Count == 0
                ? Array.Empty<ClAnomaly>().ToLookup(a => a.MissionId)
                : _ctx.Anomalies.AsNoTracking()
                    .Where(a => missions.Contains(a.ANO_MISSION_ID))
                    .ToList()
                    .Select(a => a.ToDomain())
                    .OrderByDescending(a => a.ReportedAt)
                    .ToLookup(a => a.MissionId)
        };
    }

    /// <summary>
    /// Carte courante de chaque bénéficiaire : la plus récemment capturée. Toutes les cartes des
    /// bénéficiaires du lot sont lues en une requête, le tri se fait ici — une poignée de lignes par
    /// patient, historique compris.
    /// </summary>
    private Dictionary<Guid, ClMutuelleCard> ReadCurrentMutuelles(List<Guid> beneficiaries)
    {
        if (beneficiaries.Count == 0) return new Dictionary<Guid, ClMutuelleCard>();

        // Mêmes colonnes que MutuelleCardRepository.GetCurrentMetadata — l'image, jamais.
        return _ctx.MutuelleCards.AsNoTracking()
            .Where(c => beneficiaries.Contains(c.MMC_BENEFICIARY_ID))
            .Select(c => new ClMutuelleCard
            {
                Id = c.MMC_ID,
                BeneficiaryId = c.MMC_BENEFICIARY_ID,
                ContentType = c.MMC_CONTENT_TYPE,
                ByteSize = c.MMC_BYTE_SIZE,
                CapturedAt = c.MMC_CAPTURED_AT,
                CapturedCrewId = c.MMC_CAPTURED_CREW_ID,
                MissionId = c.MMC_MISSION_ID,
                MutuelleName = c.MMC_MUTUELLE_NAME,
                AmcCode = c.MMC_AMC_CODE,
                Concentrateur = c.MMC_CONCENTRATEUR,
                Teletransmission = c.MMC_TELETRANSMISSION,
                OcrStatus = c.MMC_OCR_STATUS,
                OcrValidatedAt = c.MMC_OCR_VALIDATED_AT
            })
            .ToList()
            .GroupBy(c => c.BeneficiaryId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CapturedAt).First());
    }
}
