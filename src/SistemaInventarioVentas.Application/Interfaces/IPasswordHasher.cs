using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Interfaces;

/// <summary>Defines the application boundary for secure password hashing and verification.</summary>
/// <remarks>
/// Implementations must use a salted, adaptive password-hashing algorithm and must not implement
/// custom cryptography. Passwords and hashes must not be logged.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>Creates a non-reversible password hash.</summary>
    PasswordHash Hash(string password);

    /// <summary>Verifies a supplied password against its stored hash.</summary>
    bool Verify(string password, PasswordHash passwordHash);
}
