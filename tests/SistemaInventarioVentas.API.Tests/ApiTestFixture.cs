using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SistemaInventarioVentas.API.Tests;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private const string JwtIssuer = "SistemaInventarioVentas.Api.Tests";
    private const string JwtAudience = "SistemaInventarioVentas.Api.Tests.Client";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("catalog_api_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    private readonly string _jwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private sealed record RegisterResponse(Guid UserId, bool IsActive);

    private sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);

    private ApiWebApplicationFactory? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();
        }

        _factory = new ApiWebApplicationFactory(_database.GetConnectionString(), _jwtSigningKey);
        Client = _factory.CreateClient();
    }

    public string JwtSigningKey => _jwtSigningKey;

    public async Task DisposeAsync()
    {
        Client.Dispose();
        _factory?.Dispose();
        await _database.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        await using var dbContext = CreateDbContext();
        await dbContext.Products.ExecuteDeleteAsync();
        await dbContext.Categories.ExecuteDeleteAsync();
        await dbContext.Users.ExecuteDeleteAsync();
    }

    public async Task ResetAsAsync(UserRole role, bool includeUserRole = false)
    {
        await ResetAsync();

        var email = $"test-{Guid.NewGuid():N}@example.com";
        using var registrationResponse = await Client.PostAsJsonAsync(
            "/auth/register",
            new { name = "API Test User", email, password = "test-password" });
        registrationResponse.EnsureSuccessStatusCode();
        var registration = await registrationResponse.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions)
            ?? throw new InvalidOperationException("The test user registration response was empty.");

        await using (var dbContext = CreateDbContext())
        {
            var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == registration.UserId);
            user.Activate();
            if (role == UserRole.Admin)
            {
                user.AssignRole(UserRole.Admin);
                if (!includeUserRole)
                {
                    user.RemoveRole(UserRole.User);
                }
            }

            await dbContext.SaveChangesAsync();
        }

        using var loginResponse = await Client.PostAsJsonAsync(
            "/auth/login",
            new { email, password = "test-password" });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions)
            ?? throw new InvalidOperationException("The test user login response was empty.");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    public CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options;

        return new CatalogDbContext(options);
    }

    private sealed class ApiWebApplicationFactory(
        string connectionString,
        string jwtSigningKey) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:InventoryDatabase", connectionString);
            builder.UseSetting("Authentication:Jwt:Issuer", JwtIssuer);
            builder.UseSetting("Authentication:Jwt:Audience", JwtAudience);
            builder.UseSetting("Authentication:Jwt:SigningKey", jwtSigningKey);
            builder.UseSetting("Authentication:Jwt:AccessTokenLifetimeMinutes", "5");
        }
    }
}
