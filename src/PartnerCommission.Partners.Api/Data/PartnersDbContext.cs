using Microsoft.EntityFrameworkCore;
using PartnerCommission.Partners.Api.Entities;

namespace PartnerCommission.Partners.Api.Data;

public class PartnersDbContext(DbContextOptions<PartnersDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.ExternalId).IsUnique();
            e.Property(x => x.ExternalId).HasMaxLength(64);

            e.HasOne(x => x.Parent)
             .WithMany()
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}