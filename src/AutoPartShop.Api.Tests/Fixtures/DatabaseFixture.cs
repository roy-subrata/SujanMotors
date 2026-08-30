using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace AutoPartShop.Api.Tests.Fixtures;

/// <summary>
/// Shared xUnit fixture that starts a SQL Server 2025 container, applies all EF Core migrations,
/// and seeds deterministic test data for report calculation tests. All test classes that need
/// the database implement IClassFixture&lt;DatabaseFixture&gt; to share the container.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
#pragma warning disable CS0618 // Obsolete parameterless constructor
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2025-latest")
        .WithPassword("Test!Passw0rd#2026")
        .Build();
#pragma warning restore CS0618

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await ApplyMigrationsAsync();
        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AutoPartDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AutoPartDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AutoPartDbContext(options);
    }

    private async Task ApplyMigrationsAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    private async Task SeedAsync()
    {
        await using var db = CreateContext();
        await TestSeedData.SeedAsync(db);
    }
}
