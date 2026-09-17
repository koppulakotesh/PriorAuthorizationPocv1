using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PriorAuthorization.Infrastructure.Data;

/// <summary>
/// Used by `dotnet ef migrations` at design time.
/// </summary>
public class PADbContextFactory : IDesignTimeDbContextFactory<PADbContext>
{
    public PADbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PADbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PriorAuthorizationPOC;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new PADbContext(options);
    }
}
