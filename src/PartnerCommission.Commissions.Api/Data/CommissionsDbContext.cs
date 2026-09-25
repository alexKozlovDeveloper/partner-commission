using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Entities;

namespace PartnerCommission.Commissions.Api.Data;

public class CommissionsDbContext(DbContextOptions<CommissionsDbContext> options) 
    : DbContext(options)
{
    public DbSet<ProfitEvent> ProfitEvents => Set<ProfitEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}
