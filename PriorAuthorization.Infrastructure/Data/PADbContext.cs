using Microsoft.EntityFrameworkCore;
using PriorAuthorization.Domain.Entities;

namespace PriorAuthorization.Infrastructure.Data;

public class PADbContext : DbContext
{
    public PADbContext(DbContextOptions<PADbContext> options) : base(options)
    {
    }

    public DbSet<PriorAuthorizationRecord> PriorAuthorizations => Set<PriorAuthorizationRecord>();
    public DbSet<PAAudit> PAAudits => Set<PAAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriorAuthorizationRecord>(entity =>
        {
            entity.ToTable("PriorAuthorizations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PatientId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PatientName).HasMaxLength(256);
            entity.Property(x => x.ProviderId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ProviderName).HasMaxLength(256);
            entity.Property(x => x.InsuranceId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.InsuranceName).HasMaxLength(256);
            entity.Property(x => x.ProcedureCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ProcedureDescription).HasMaxLength(256);
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ExternalReferenceId).HasMaxLength(64);
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<PAAudit>(entity =>
        {
            entity.ToTable("PAAudits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.OldStatus).HasMaxLength(32);
            entity.Property(x => x.NewStatus).HasMaxLength(32);
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
        });
    }
}
