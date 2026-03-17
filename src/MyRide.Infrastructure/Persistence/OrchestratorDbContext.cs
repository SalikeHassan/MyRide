using Microsoft.EntityFrameworkCore;
using MyRide.Domain.Sagas;

namespace MyRide.Infrastructure.Persistence;

public class OrchestratorDbContext : DbContext
{
    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : base(options) { }

    public DbSet<StartRideSagaState> StartRideSagas => Set<StartRideSagaState>();
    public DbSet<CompleteRideSagaState> CompleteRideSagas => Set<CompleteRideSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StartRideSagaState>(entity =>
        {
            entity.ToTable("StartRideSagas", "orchestrator");
            entity.HasKey(s => s.SagaId);
            entity.Property(s => s.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.DriverName).HasMaxLength(200);
            entity.Property(s => s.FareCurrency).HasMaxLength(10);
            entity.Property(s => s.FareAmount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Status).HasConversion<string>();
            entity.Property(s => s.FailureReason).HasMaxLength(500);
        });

        modelBuilder.Entity<CompleteRideSagaState>(entity =>
        {
            entity.ToTable("CompleteRideSagas", "orchestrator");
            entity.HasKey(s => s.SagaId);
            entity.Property(s => s.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.FareCurrency).HasMaxLength(10);
            entity.Property(s => s.FareAmount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Status).HasConversion<string>();
            entity.Property(s => s.FailureReason).HasMaxLength(500);
        });
    }
}
