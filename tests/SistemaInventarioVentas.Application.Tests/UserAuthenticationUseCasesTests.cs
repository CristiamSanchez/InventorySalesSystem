using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class UserAuthenticationUseCasesTests
{
    private readonly InMemoryUsers _users = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private UserAuthenticationUseCases UseCases => new(_users, _passwordHasher);

    [Fact]
    public async Task RegisterAsync_HashesPasswordAndCreatesInactiveUserWithDefaultRole()
    {
        var result = await UseCases.RegisterAsync(
            "  Alex Rivera ",
            " Alex@Example.com ",
            "Correct Horse Battery Staple");

        Assert.True(result.IsSuccess);
        Assert.Equal("Alex Rivera", result.Value.Name);
        Assert.Equal("alex@example.com", result.Value.Email.Value);
        Assert.False(result.Value.IsActive);
        Assert.Equal([UserRole.User], result.Value.Roles);
        Assert.NotEqual("Correct Horse Battery Staple", result.Value.PasswordHash.EncodedValue);
        Assert.Contains(result.Value, _users.StoredUsers);
        Assert.Equal("Correct Horse Battery Staple", _passwordHasher.LastHashedPassword);
    }

    [Fact]
    public async Task RegisterAsync_AllowsAuthorizedCallerToAssignAdminRole()
    {
        var result = await UseCases.RegisterAsync(
            "Admin",
            "admin@example.com",
            "credential",
            UserRole.Admin);

        Assert.True(result.IsSuccess);
        Assert.Equal([UserRole.Admin], result.Value.Roles);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsConflictForDuplicateNormalizedEmail()
    {
        await UseCases.RegisterAsync("Alex", "alex@example.com", "first");

        var result = await UseCases.RegisterAsync("Alex Again", " ALEX@EXAMPLE.COM ", "second");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.UserEmailConflict, result.Error!.Code);
        Assert.Equal("alex@example.com", Assert.Single(_users.StoredUsers).Email.Value);
        Assert.Equal("first", _passwordHasher.LastHashedPassword);
    }

    [Theory]
    [InlineData("", "user@example.com", "password")]
    [InlineData("Alex", "invalid-email", "password")]
    [InlineData("Alex", "user@example.com", " ")]
    public async Task RegisterAsync_RejectsInvalidRegistration(string name, string email, string password)
    {
        var result = await UseCases.RegisterAsync(name, email, password);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Empty(_users.StoredUsers);
        Assert.Null(_passwordHasher.LastHashedPassword);
    }

    [Fact]
    public async Task RegisterAsync_RejectsUndefinedRole()
    {
        var result = await UseCases.RegisterAsync(
            "Alex",
            "alex@example.com",
            "password",
            (UserRole)99);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Empty(_users.StoredUsers);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsAuthenticatedUserForValidActiveCredentials()
    {
        var user = await CreateActiveUserAsync();

        var result = await UseCases.AuthenticateAsync(" ALEX@EXAMPLE.COM ", "correct-password");

        Assert.Equal(AuthenticationStatus.Authenticated, result.Status);
        Assert.Same(user, result.User);
        Assert.Same(user.PasswordHash, _passwordHasher.LastVerifiedHash);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsInvalidCredentialsForIncorrectPassword()
    {
        await CreateActiveUserAsync();

        var result = await UseCases.AuthenticateAsync("alex@example.com", "incorrect-password");

        Assert.Equal(AuthenticationStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsInvalidCredentialsForMissingUser()
    {
        var result = await UseCases.AuthenticateAsync("missing@example.com", "password");

        Assert.Equal(AuthenticationStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
        Assert.Null(_passwordHasher.LastVerifiedHash);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsInactiveUserOnlyAfterCredentialVerification()
    {
        await UseCases.RegisterAsync("Alex", "alex@example.com", "correct-password");

        var invalidPassword = await UseCases.AuthenticateAsync("alex@example.com", "incorrect-password");
        var inactiveUser = await UseCases.AuthenticateAsync("alex@example.com", "correct-password");

        Assert.Equal(AuthenticationStatus.InvalidCredentials, invalidPassword.Status);
        Assert.Equal(AuthenticationStatus.InactiveUser, inactiveUser.Status);
        Assert.Null(inactiveUser.User);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsInvalidCredentialsForInvalidEmail()
    {
        var result = await UseCases.AuthenticateAsync("invalid-email", "password");

        Assert.Equal(AuthenticationStatus.InvalidCredentials, result.Status);
    }

    private async Task<User> CreateActiveUserAsync()
    {
        var registration = await UseCases.RegisterAsync(
            "Alex",
            "alex@example.com",
            "correct-password");
        var user = registration.Value;
        user.Activate();
        return user;
    }

    private sealed class InMemoryUsers : IUserRepository
    {
        private readonly List<User> _users = [];

        public IReadOnlyList<User> StoredUsers => _users;

        public Task<User?> GetByEmailAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.SingleOrDefault(user => user.Email == email));

        public Task<bool> EmailExistsAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Any(user => user.Email == email));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        private readonly Dictionary<string, string> _passwordByHash = [];
        private int _nextHashId;

        public string? LastHashedPassword { get; private set; }

        public PasswordHash? LastVerifiedHash { get; private set; }

        public PasswordHash Hash(string password)
        {
            LastHashedPassword = password;
            var encodedValue = $"$test$hash-{++_nextHashId}";
            _passwordByHash.Add(encodedValue, password);
            return new PasswordHash(encodedValue);
        }

        public bool Verify(string password, PasswordHash passwordHash)
        {
            LastVerifiedHash = passwordHash;
            return _passwordByHash.TryGetValue(passwordHash.EncodedValue, out var storedPassword) &&
                storedPassword == password;
        }
    }
}
