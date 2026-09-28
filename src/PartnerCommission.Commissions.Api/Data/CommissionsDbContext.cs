using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Data;

public class CommissionsDbContext(DbContextOptions<CommissionsDbContext> options) 
    : DbContext(options)
{
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<ProfitEvent> ProfitEvents => Set<ProfitEvent>();
    public DbSet<Setting> Settings => Set<Setting>();   
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProfitEvent>(e =>
        {
            e.ToTable("profit_events");
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.EventExternalId).IsUnique();

            e.HasIndex(x => x.Status);

            e.HasIndex(x => new { x.UserExternalId, x.CreatedAtUtc });

            e.Property(x => x.EventExternalId).HasMaxLength(64);
            e.Property(x => x.UserExternalId).HasMaxLength(64);

            e.Property(x => x.Profit).HasPrecision(18, 4);

            e.Property(x => x.SchemaType)
                .HasConversion<string>()
                .HasMaxLength(16);
        });

        modelBuilder.Entity<Setting>(e =>
        {
            e.ToTable("settings");
            e.HasKey(x => x.Key);

            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.Value).HasMaxLength(64);

        });

        modelBuilder.Entity<Commission>(e =>
        {
            e.ToTable("commissions");
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.ProfitEventId, x.Level }).IsUnique();

            e.HasOne<ProfitEvent>()
                .WithMany()
                .HasForeignKey(x => x.ProfitEventId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.BeneficiaryExternalId).HasMaxLength(64);

            e.Property(x => x.Amount).HasPrecision(18, 4);

            e.Property(x => x.SchemaType)
                .HasConversion<string>()
                .HasMaxLength(16);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.ProcessedAtUtc, x.NextAttemptAtUtc });
            e.Property(x => x.Type).HasMaxLength(128);
            e.Property(x => x.LastError).HasMaxLength(1000);
        });
    }
}
