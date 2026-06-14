using CaSoft.Erp.USVector.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaSoft.Erp.USVector.Infrastructure.Persistence;

/// <summary>
/// DbContext de la BD Mobile dédiée. Cible base <c>BD_ERP_MOBILE_APP</c>.
/// Tables : <c>MOB_SESSION</c>, <c>MOB_MISSION_STATE</c>, <c>MOB_SIGNATURE</c> ;
/// overlay attributs (MOB-13) : <c>MOB_CONTRACT_TYPE</c>, <c>MOB_CONTRACT_ATTRIBUTE</c>,
/// <c>MOB_CONTRACT_ATTRIBUTE_OPTION</c>, <c>MOB_JOB_CONTRACT</c>, <c>MOB_JOB_ATTRIBUTE_VALUE</c>.
/// Les colonnes <c>SES_CREW_ID</c> / <c>*_MISSION_ID</c> référencent les entités ERP
/// (CRW_CREW / ORD_MISSION) par id, sans FK cross-database.
/// </summary>
public class MobileDbContext : DbContext
{
    public MobileDbContext(DbContextOptions<MobileDbContext> options) : base(options) { }

    public DbSet<MOB_SESSION> Sessions => Set<MOB_SESSION>();
    public DbSet<MOB_MISSION_STATE> MissionStates => Set<MOB_MISSION_STATE>();
    public DbSet<MOB_SIGNATURE> Signatures => Set<MOB_SIGNATURE>();

    // ── Overlay attributs de mission (MOB-13) ───────────────────────────────────
    public DbSet<MOB_CONTRACT_TYPE> ContractTypes => Set<MOB_CONTRACT_TYPE>();
    public DbSet<MOB_CONTRACT_ATTRIBUTE> ContractAttributes => Set<MOB_CONTRACT_ATTRIBUTE>();
    public DbSet<MOB_CONTRACT_ATTRIBUTE_CONTRACT> ContractAttributeContracts => Set<MOB_CONTRACT_ATTRIBUTE_CONTRACT>();
    public DbSet<MOB_CONTRACT_ATTRIBUTE_OPTION> ContractAttributeOptions => Set<MOB_CONTRACT_ATTRIBUTE_OPTION>();
    public DbSet<MOB_JOB_CONTRACT> JobContracts => Set<MOB_JOB_CONTRACT>();
    public DbSet<MOB_JOB_ATTRIBUTE_VALUE> JobAttributeValues => Set<MOB_JOB_ATTRIBUTE_VALUE>();

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

        // ── Overlay attributs de mission (MOB-13) ───────────────────────────────
        modelBuilder.Entity<MOB_CONTRACT_TYPE>(b =>
        {
            b.ToTable("MOB_CONTRACT_TYPE");
            b.HasKey(t => t.CTT_ID);
            b.Property(t => t.CTT_ACTIVE).HasDefaultValue(true);
            b.HasIndex(t => t.CTT_CODE, "UX_MOB_CONTRACT_TYPE_CODE").IsUnique();
        });

        modelBuilder.Entity<MOB_CONTRACT_ATTRIBUTE>(b =>
        {
            b.ToTable("MOB_CONTRACT_ATTRIBUTE");
            b.HasKey(a => a.CAT_ID);
            b.HasIndex(a => a.CAT_NAME, "UX_MOB_CAT_NAME").IsUnique();
        });

        modelBuilder.Entity<MOB_CONTRACT_ATTRIBUTE_CONTRACT>(b =>
        {
            b.ToTable("MOB_CONTRACT_ATTRIBUTE_CONTRACT");
            b.HasKey(c => new { c.CAC_ATTRIBUTE_ID, c.CAC_CONTRACT_ID });
        });

        modelBuilder.Entity<MOB_CONTRACT_ATTRIBUTE_OPTION>(b =>
        {
            b.ToTable("MOB_CONTRACT_ATTRIBUTE_OPTION");
            b.HasKey(o => o.CAO_ID);
            b.HasIndex(o => new { o.CAO_ATTRIBUTE_ID, o.CAO_KEY }, "UX_MOB_CAO_ATTR_KEY").IsUnique();
        });

        modelBuilder.Entity<MOB_JOB_CONTRACT>(b =>
        {
            b.ToTable("MOB_JOB_CONTRACT");
            b.HasKey(c => c.JCT_MISSION_ID);
            b.Property(c => c.JCT_MISSION_ID).ValueGeneratedNever();
            b.Property(c => c.JCT_UPDATED_AT).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<MOB_JOB_ATTRIBUTE_VALUE>(b =>
        {
            b.ToTable("MOB_JOB_ATTRIBUTE_VALUE");
            b.HasKey(v => new { v.JAV_MISSION_ID, v.JAV_ATTRIBUTE_NAME });
            b.Property(v => v.JAV_UPDATED_AT).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
