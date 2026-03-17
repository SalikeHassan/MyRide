using Drivers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Drivers.Infrastructure;

public class DesignTimeDriversReadDbContextFactory : IDesignTimeDbContextFactory<DriversReadDbContext>
{
    public DriversReadDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DriversReadDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=MyRideReadDb;User Id=sa;Password=MyRide@123;TrustServerCertificate=True;")
            .Options;

        return new DriversReadDbContext(options);
    }
}
