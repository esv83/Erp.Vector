using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence;

/// <summary>
/// DbContext de la BD Mobile dédiée. Cible base <c>BD_ERP_MOBILE_APP</c>.
/// Tables : <c>MOB_SESSION</c>, <c>MOB_MISSION_STATE</c>, <c>MOB_SIGNATURE</c>, carte mutuelle,
/// anomalies, documents, outbox de projection.
/// Les colonnes <c>SES_CREW_ID</c> / <c>*_MISSION_ID</c> référencent les entités ERP
/// (CRW_CREW / ORD_MISSION) par id, sans FK cross-database.
/// <para>
/// L'overlay d'attributs (MOB-13 : <c>MOB_CONTRACT_*</c>, <c>MOB_JOB_CONTRACT</c>,
/// <c>MOB_JOB_ATTRIBUTE_VALUE</c>) n'est plus mappé depuis le 2026-09-13 (OC-8) : le référentiel est
/// chez Order. Les tables se suppriment par <c>MOB_008_DropContractOverlay.sql</c>.
/// </para>
/// </summary>
public class MobileDbContext : DbContext
{
    public MobileDbContext(DbContextOptions<MobileDbContext> options) : base(options) { }

    public DbSet<MOB_SESSION> Sessions => Set<MOB_SESSION>();
    public DbSet<MOB_MISSION_STATE> MissionStates => Set<MOB_MISSION_STATE>();
    public DbSet<MOB_SIGNATURE> Signatures => Set<MOB_SIGNATURE>();

    // Carte mutuelle (P1)
    public DbSet<MOB_MUTUELLE_CARD> MutuelleCards => Set<MOB_MUTUELLE_CARD>();

    // Anomalies terrain (TRF-8)
    public DbSet<MOB_ANOMALY> Anomalies => Set<MOB_ANOMALY>();

    // Documents/photos terrain (TRF-10)
    public DbSet<MOB_DOCUMENT> Documents => Set<MOB_DOCUMENT>();

    // Outbox de projection opérationnelle (synchro régulation garantie + debounce)
    public DbSet<MOB_OPERATIONAL_OUTBOX> OperationalOutbox => Set<MOB_OPERATIONAL_OUTBOX>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MOB_SESSION>(b =>
        {
            b.ToTable("MOB_SESSION");
            b.HasKey(s => s.SES_ID);
            b.Property(s => s.SES_ID).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(s => s.SES_STARTED_AT).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(s => s.SES_ENDED_AT).HasPrecision(0);

            b.HasIndex(s => s.SES_TOKEN, "UX_MOB_SESSION_TOKEN").IsUnique();
            b.HasIndex(s => s.SES_CREW_ID, "UX_MOB_SESSION_CREW_ACTIVE")
             .IsUnique()
             .HasFilter("[SES_ENDED_AT] IS NULL");
        });

        modelBuilder.Entity<MOB_MISSION_STATE>(b =>
        {
            b.ToTable("MOB_MISSION_STATE");
            b.HasKey(m => m.MST_MISSION_ID);
            b.Property(m => m.MST_MISSION_ID).ValueGeneratedNever();
            b.Property(m => m.MST_ACK_AT).HasPrecision(0);
            b.Property(m => m.MST_READ_AT).HasPrecision(0);
            b.Property(m => m.MST_GO_AT).HasPrecision(0);
            b.Property(m => m.MST_ONSITE_AT).HasPrecision(0);
            b.Property(m => m.MST_TERMINATED_AT).HasPrecision(0);
            b.Property(m => m.MST_UPDATED_AT).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<MOB_SIGNATURE>(b =>
        {
            b.ToTable("MOB_SIGNATURE");
            b.HasKey(s => s.SIG_MISSION_ID);
            b.Property(s => s.SIG_MISSION_ID).ValueGeneratedNever();
            b.Property(s => s.SIG_DATA).IsRequired();
            b.Property(s => s.SIG_DATETIME).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ── Carte mutuelle (P1) ─────────────────────────────────────────────────
        modelBuilder.Entity<MOB_MUTUELLE_CARD>(b =>
        {
            b.ToTable("MOB_MUTUELLE_CARD");
            b.HasKey(c => c.MMC_ID);
            b.Property(c => c.MMC_ID).ValueGeneratedNever();   // Guid généré côté application
            b.Property(c => c.MMC_IMAGE).IsRequired();
            b.Property(c => c.MMC_CAPTURED_AT).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(c => c.MMC_OCR_VALIDATED_AT).HasPrecision(0);
            // MOB_010 — proposition de la lecture automatique : décimal borné (0 à 1), daté à la seconde.
            b.Property(c => c.MMC_OCR_CONFIDENCE).HasPrecision(4, 3);
            b.Property(c => c.MMC_OCR_EXTRACTED_AT).HasPrecision(0);
            b.HasIndex(c => new { c.MMC_BENEFICIARY_ID, c.MMC_CAPTURED_AT }, "IX_MOB_MUTUELLE_CARD_BENEFICIARY");
        });

        // ── Outbox projection opérationnelle (synchro régulation garantie) ───────
        modelBuilder.Entity<MOB_OPERATIONAL_OUTBOX>(b =>
        {
            b.ToTable("MOB_OPERATIONAL_OUTBOX");
            b.HasKey(o => o.OOB_MISSION_ID);
            b.Property(o => o.OOB_MISSION_ID).ValueGeneratedNever();
            b.Property(o => o.OOB_DISPATCH_AFTER).HasPrecision(0);
            b.Property(o => o.OOB_UPDATED_AT).HasPrecision(0);
            b.HasIndex(o => o.OOB_DISPATCH_AFTER, "IX_MOB_OPERATIONAL_OUTBOX_DISPATCH");
        });

        // ── Anomalies terrain (TRF-8) ───────────────────────────────────────────
        modelBuilder.Entity<MOB_ANOMALY>(b =>
        {
            b.ToTable("MOB_ANOMALY");
            b.HasKey(a => a.ANO_ID);
            b.Property(a => a.ANO_ID).ValueGeneratedNever();   // Guid généré côté application
            b.Property(a => a.ANO_REPORTED_AT).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasIndex(a => new { a.ANO_MISSION_ID, a.ANO_REPORTED_AT }, "IX_MOB_ANOMALY_MISSION");
        });

        // ── Documents/photos terrain (TRF-10) ───────────────────────────────────
        modelBuilder.Entity<MOB_DOCUMENT>(b =>
        {
            b.ToTable("MOB_DOCUMENT");
            b.HasKey(d => d.DOC_ID);
            b.Property(d => d.DOC_ID).ValueGeneratedNever();   // Guid généré côté application
            b.Property(d => d.DOC_CONTENT).IsRequired();
            b.Property(d => d.DOC_CAPTURED_AT).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasIndex(d => new { d.DOC_MISSION_ID, d.DOC_CAPTURED_AT }, "IX_MOB_DOCUMENT_MISSION");
        });
    }
}
