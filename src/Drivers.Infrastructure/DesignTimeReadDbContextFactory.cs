using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Drivers.Infrastructure;

public class DesignTimeReadDbContextFactory : IDesignTimeDbContextFactory<ReadDbContext>
{
    public ReadDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=MyRideReadDb;User Id=sa;Password=MyRide@123;TrustServerCertificate=True;")
            .Options;

        return new ReadDbContext(options);
    }
}
