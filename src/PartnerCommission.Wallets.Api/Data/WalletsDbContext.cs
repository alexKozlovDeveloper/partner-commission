using Microsoft.EntityFrameworkCore;
using PartnerCommission.Wallets.Api.Entities;

namespace PartnerCommission.Wallets.Api.Data;

public class WalletsDbContext(DbContextOptions<WalletsDbContext> options)
    : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletEntry> WalletEntries => Set<WalletEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(e =>
        {
            e.ToTable("wallets");
            e.HasKey(x => x.UserExternalId);

            e.Property(x => x.UserExternalId).HasMaxLength(64);
            e.Property(x => x.Balance).HasPrecision(18, 4);
        });

        modelBuilder.Entity<WalletEntry>(e =>
        {
            e.ToTable("wallet_entries");
            e.HasKey(x => x.CommissionId);

            e.Property(x => x.CommissionId).ValueGeneratedNever();

            e.Property(x => x.UserExternalId).HasMaxLength(64);
            e.Property(x => x.EventExternalId).HasMaxLength(64);
            e.Property(x => x.Amount).HasPrecision(18, 4);

            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(16);

            e.HasIndex(x => new { x.UserExternalId, x.Status });
            e.HasIndex(x => new { x.UserExternalId, x.PaidAtUtc });
            e.HasIndex(x => x.Status);
        });
    }
}
