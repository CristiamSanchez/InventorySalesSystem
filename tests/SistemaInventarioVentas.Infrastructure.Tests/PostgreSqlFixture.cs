using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("catalog_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new CatalogDbContext(options);
    }

    public async Task ClearAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Users.ExecuteDeleteAsync();
        await dbContext.Products.ExecuteDeleteAsync();
        await dbContext.Categories.ExecuteDeleteAsync();
    }
}
