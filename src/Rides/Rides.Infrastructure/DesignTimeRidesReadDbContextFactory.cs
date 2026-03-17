using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Rides.Infrastructure.Persistence;

namespace Rides.Infrastructure;

public class DesignTimeRidesReadDbContextFactory : IDesignTimeDbContextFactory<RidesReadDbContext>
{
    public RidesReadDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RidesReadDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=MyRideReadDb;User Id=sa;Password=MyRide@123;TrustServerCertificate=True;")
            .Options;

        return new RidesReadDbContext(options);
    }
}
