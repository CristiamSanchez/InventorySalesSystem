using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class UserAuthenticationUseCases(
    IUserRepository users,
    IPasswordHasher passwordHasher)
{
    public async Task<Result<User>> RegisterAsync(
        string name,
        string email,
        string password,
        UserRole role = UserRole.User,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            !Enum.IsDefined(role))
        {
            return Result<User>.Failure(
                ApplicationErrorCode.InvalidInput,
                "User registration details are invalid.");
        }

        EmailAddress emailAddress;
        try
        {
            emailAddress = new EmailAddress(email);
        }
        catch (ArgumentException)
        {
            return Result<User>.Failure(
                ApplicationErrorCode.InvalidInput,
                "User registration details are invalid.");
        }

        if (await users.EmailExistsAsync(emailAddress, cancellationToken))
        {
            return Result<User>.Failure(
                ApplicationErrorCode.UserEmailConflict,
                "A user with this email address already exists.");
        }

        var passwordHash = passwordHasher.Hash(password);
        var user = new User(name, emailAddress, passwordHash, role);
        await users.AddAsync(user, cancellationToken);
        return Result<User>.Success(user);
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthenticationResult.InvalidCredentials();
        }

        EmailAddress emailAddress;
        try
        {
            emailAddress = new EmailAddress(email);
        }
        catch (ArgumentException)
        {
            return AuthenticationResult.InvalidCredentials();
        }

        var user = await users.GetByEmailAsync(emailAddress, cancellationToken);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
        {
            return AuthenticationResult.InvalidCredentials();
        }

        return user.IsActive
            ? AuthenticationResult.Authenticated(user)
            : AuthenticationResult.InactiveUser();
    }
}
