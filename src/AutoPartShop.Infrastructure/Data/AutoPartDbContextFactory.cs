using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoPartShop.Infrastructure.Data;

/// <summary>
/// Design-time factory used by EF Core tooling (migrations) so they can run against the
/// Infrastructure project alone, without building/launching the API host. Used at design time
/// only — never at runtime. Reads the connection string from the ConnectionStrings__AutoPartDb
/// environment variable, falling back to the local dev SQL Server.
/// </summary>
public class AutoPartDbContextFactory : IDesignTimeDbContextFactory<AutoPartDbContext>
{
    public AutoPartDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AutoPartDb")
            ?? "Server=127.0.0.1,1433;User ID=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;Initial Catalog=AutoPartShopDb";

        var options = new DbContextOptionsBuilder<AutoPartDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AutoPartDbContext(options);
    }
}
