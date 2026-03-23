using Microsoft.EntityFrameworkCore;
using Rides.Domain.Aggregates;
using Rides.Domain.ReadModels;

namespace Rides.Infrastructure.Persistence;

public class RidesReadDbContext : DbContext
{
    public RidesReadDbContext(DbContextOptions<RidesReadDbContext> options) : base(options) { }

    public DbSet<RideReadModel> RideReadModels => Set<RideReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RideReadModel>(entity =>
        {
            entity.ToTable("RideReadModels", "rides");
            entity.HasKey(r => r.RideId);
            entity.Property(r => r.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(r => r.DriverName).HasMaxLength(200);
            entity.Property(r => r.FareCurrency).HasMaxLength(10);
            entity.Property(r => r.FareAmount).HasColumnType("decimal(18,2)");
            entity.Property(r => r.Status).HasConversion<string>();
            entity.HasIndex(r => new { r.TenantId, r.Status });
            entity.HasIndex(r => new { r.RiderId, r.TenantId })
                .IsUnique()
                .HasFilter("[Status] IN ('Requested', 'InProgress')")
                .HasDatabaseName("UX_ActiveRidePerRider");
        });
    }
}
