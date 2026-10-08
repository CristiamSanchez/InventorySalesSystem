using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class UserTests
{
    private static PasswordHash TestHash => new("$test$encoded-password-hash");

    [Fact]
    public void Constructor_NormalizesProfileAndDefaultsToInactiveUserRole()
    {
        var passwordHash = TestHash;
        var user = new User(
            "  Alex Rivera  ",
            new EmailAddress(" Alex.Rivera@Example.com "),
            passwordHash);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Alex Rivera", user.Name);
        Assert.Equal("alex.rivera@example.com", user.Email.Value);
        Assert.False(user.IsActive);
        Assert.Equal([UserRole.User], user.Roles);
        Assert.Same(passwordHash, user.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankName(string name)
    {
        Assert.Throws<ArgumentException>(
            () => new User(name, new EmailAddress("user@example.com"), TestHash));
    }

    [Fact]
    public void Constructor_RejectsNullEmailOrPasswordHash()
    {
        Assert.Throws<ArgumentNullException>(
            () => new User("Alex", null!, TestHash));
        Assert.Throws<ArgumentNullException>(
            () => new User("Alex", new EmailAddress("user@example.com"), null!));
    }

    [Fact]
    public void Constructor_RejectsUndefinedRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new User("Alex", new EmailAddress("user@example.com"), TestHash, (UserRole)99));
    }

    [Fact]
    public void Rename_RejectsBlankNameWithoutChangingCurrentName()
    {
        var user = CreateUser();

        Assert.Throws<ArgumentException>(() => user.Rename(" "));

        Assert.Equal("Alex", user.Name);
    }

    [Fact]
    public void ActivateAndDeactivate_ChangeUserStatus()
    {
        var user = CreateUser();

        user.Activate();
        Assert.True(user.IsActive);

        user.Deactivate();
        Assert.False(user.IsActive);
    }

    [Fact]
    public void AssignRole_AddsAdminRoleAndRemoveRolePreservesUserRole()
    {
        var user = CreateUser();

        user.AssignRole(UserRole.Admin);

        Assert.Contains(UserRole.Admin, user.Roles);
        Assert.Contains(UserRole.User, user.Roles);

        user.RemoveRole(UserRole.Admin);

        Assert.Equal([UserRole.User], user.Roles);
    }

    [Fact]
    public void RemoveRole_RejectsRemovingTheLastRole()
    {
        var user = CreateUser();

        Assert.Throws<InvalidOperationException>(() => user.RemoveRole(UserRole.User));

        Assert.Equal([UserRole.User], user.Roles);
    }

    [Fact]
    public void AssignRole_RejectsUndefinedRole()
    {
        var user = CreateUser();

        Assert.Throws<ArgumentOutOfRangeException>(() => user.AssignRole((UserRole)99));

        Assert.Equal([UserRole.User], user.Roles);
    }

    [Fact]
    public void PasswordHash_RequiresValueAndDoesNotRevealItInStringRepresentation()
    {
        var hash = TestHash;

        Assert.Equal("$test$encoded-password-hash", hash.EncodedValue);
        Assert.Equal("[REDACTED]", hash.ToString());
        Assert.Throws<ArgumentException>(() => new PasswordHash(" "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("user@@example.com")]
    [InlineData("user name@example.com")]
    public void EmailAddress_RejectsInvalidValue(string email)
    {
        Assert.Throws<ArgumentException>(() => new EmailAddress(email));
    }

    private static User CreateUser() =>
        new("Alex", new EmailAddress("alex@example.com"), TestHash);
}
