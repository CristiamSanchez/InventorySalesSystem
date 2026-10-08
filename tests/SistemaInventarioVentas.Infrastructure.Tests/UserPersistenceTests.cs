using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Repositories;
using SistemaInventarioVentas.Infrastructure.Security;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class UserPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    private const string TestPassword = "Correct Horse Battery Staple";

    [Fact]
    public async Task UserRepository_PersistsProfileRolesStateAndPasswordHash()
    {
        await database.ClearAsync();
        var passwordHasher = new AspNetPasswordHasher();
        var passwordHash = passwordHasher.Hash(TestPassword);
        var user = new User(" Alex Rivera ", new EmailAddress("Alex@Example.com"), passwordHash);
        user.AssignRole(UserRole.Admin);
        user.Activate();

        await using (var dbContext = database.CreateDbContext())
        {
            await new UserRepository(dbContext).AddAsync(user);
        }

        await using var queryContext = database.CreateDbContext();
        var result = await new UserRepository(queryContext)
            .GetByEmailAsync(new EmailAddress(" alex@example.com "));

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Alex Rivera", result.Name);
        Assert.Equal("alex@example.com", result.Email.Value);
        Assert.True(result.IsActive);
        Assert.Contains(UserRole.User, result.Roles);
        Assert.Contains(UserRole.Admin, result.Roles);
        Assert.Equal(passwordHash.EncodedValue, result.PasswordHash.EncodedValue);
        Assert.DoesNotContain(TestPassword, result.PasswordHash.EncodedValue, StringComparison.Ordinal);
        Assert.True(passwordHasher.Verify(TestPassword, result.PasswordHash));
    }

    [Fact]
    public async Task UserRepository_PersistsInactiveState()
    {
        await database.ClearAsync();
        var user = CreateUser("inactive@example.com");

        await using (var dbContext = database.CreateDbContext())
        {
            await new UserRepository(dbContext).AddAsync(user);
        }

        await using var queryContext = database.CreateDbContext();
        var result = await new UserRepository(queryContext)
            .GetByEmailAsync(new EmailAddress("inactive@example.com"));

        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UserRepository_EmailLookupAndExistsUseNormalizedIdentity()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new UserRepository(dbContext);
        await repository.AddAsync(CreateUser("login@example.com"));

        Assert.True(await repository.EmailExistsAsync(new EmailAddress("LOGIN@example.com")));
        Assert.False(await repository.EmailExistsAsync(new EmailAddress("missing@example.com")));
        Assert.NotNull(await repository.GetByEmailAsync(new EmailAddress("LOGIN@example.com")));
    }

    [Fact]
    public async Task UserEmail_IsUniqueAtDatabaseLevel()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new UserRepository(dbContext);
        await repository.AddAsync(CreateUser("unique@example.com"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(CreateUser("UNIQUE@example.com")));

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            ((PostgresException)exception.InnerException!).SqlState);
    }

    [Fact]
    public void AspNetPasswordHasher_UsesIndependentHashesAndVerifiesCredentials()
    {
        IPasswordHasher passwordHasher = new AspNetPasswordHasher();

        var firstHash = passwordHasher.Hash(TestPassword);
        var secondHash = passwordHasher.Hash(TestPassword);

        Assert.NotEqual(firstHash.EncodedValue, secondHash.EncodedValue);
        Assert.DoesNotContain(TestPassword, firstHash.EncodedValue, StringComparison.Ordinal);
        Assert.True(passwordHasher.Verify(TestPassword, firstHash));
        Assert.False(passwordHasher.Verify("Incorrect password", firstHash));
    }

    [Fact]
    public void PersistenceRegistration_ResolvesUserRepositoryAndPasswordHasher()
    {
        using var dbContext = database.CreateDbContext();
        var services = new ServiceCollection();
        services.AddPersistence(dbContext.Database.GetConnectionString()!);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<UserRepository>(provider.GetRequiredService<IUserRepository>());
        Assert.IsType<AspNetPasswordHasher>(provider.GetRequiredService<IPasswordHasher>());
    }

    private static User CreateUser(string email) =>
        new(
            "Test User",
            new EmailAddress(email),
            new AspNetPasswordHasher().Hash(TestPassword));
}
