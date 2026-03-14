using Drivers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;

namespace ReadDb.Infrastructure;

public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options) { }

    public DbSet<RideReadModel> RideReadModels => Set<RideReadModel>();
    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RideReadModel>(entity =>
        {
            entity.HasKey(r => r.RideId);
            entity.Property(r => r.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(r => r.DriverName).HasMaxLength(200);
            entity.Property(r => r.FareCurrency).HasMaxLength(10);
            entity.Property(r => r.FareAmount).HasColumnType("decimal(18,2)");
            entity.Property(r => r.Status).HasConversion<string>();
            entity.HasIndex(r => new { r.TenantId, r.Status });
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            entity.Property(d => d.Phone).IsRequired().HasMaxLength(20);
            entity.Property(d => d.Status).HasConversion<string>();
            entity.HasIndex(d => new { d.TenantId, d.Status });
        });

        SeedDrivers(modelBuilder);
    }

    private static void SeedDrivers(ModelBuilder modelBuilder)
    {
        var tenantId = "tenant1";

        modelBuilder.Entity<Driver>().HasData(
            Driver.Create(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), tenantId, "James Carter",  "+44 7700 900001"),
            Driver.Create(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), tenantId, "Priya Sharma",  "+44 7700 900002"),
            Driver.Create(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), tenantId, "Mohammed Ali",  "+44 7700 900003"),
            Driver.Create(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"), tenantId, "Sofia Reyes",   "+44 7700 900004"),
            Driver.Create(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005"), tenantId, "Liam O'Brien",  "+44 7700 900005")
        );
    }
}
