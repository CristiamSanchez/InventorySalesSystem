using Microsoft.AspNetCore.Identity;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Infrastructure.Security;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private static readonly object UserContext = new();
    private readonly PasswordHasher<object> _passwordHasher = new();

    public PasswordHash Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return new PasswordHash(_passwordHasher.HashPassword(UserContext, password));
    }

    public bool Verify(string password, PasswordHash passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        return _passwordHasher.VerifyHashedPassword(
            UserContext,
            passwordHash.EncodedValue,
            password) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
