using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Models;

public enum AuthenticationStatus
{
    InvalidCredentials,
    InactiveUser,
    Authenticated
}

public sealed class AuthenticationResult
{
    private AuthenticationResult(AuthenticationStatus status, User? user)
    {
        Status = status;
        User = user;
    }

    public AuthenticationStatus Status { get; }

    public User? User { get; }

    public static AuthenticationResult InvalidCredentials() =>
        new(AuthenticationStatus.InvalidCredentials, null);

    public static AuthenticationResult InactiveUser() =>
        new(AuthenticationStatus.InactiveUser, null);

    public static AuthenticationResult Authenticated(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new(AuthenticationStatus.Authenticated, user);
    }
}
