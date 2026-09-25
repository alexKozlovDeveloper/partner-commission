using Microsoft.EntityFrameworkCore;
using PartnerCommission.Wallets.Api.Entities;

namespace PartnerCommission.Wallets.Api.Data;

public class WalletsDbContext(DbContextOptions<WalletsDbContext> options)
    : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}