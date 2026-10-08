using System.ComponentModel.DataAnnotations;

namespace SistemaInventarioVentas.API.Contracts;

/// <summary>Credentials and profile information required to register a user.</summary>
internal sealed record RegisterRequest
{
    [Required]
    public string? Name { get; init; }

    [Required, EmailAddress, StringLength(320)]
    public string? Email { get; init; }

    [Required]
    public string? Password { get; init; }
}

/// <summary>Result of registering a user account.</summary>
internal sealed record RegisterResponse(Guid UserId, bool IsActive);

/// <summary>Email and password submitted to authenticate a user.</summary>
internal sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(320)]
    public string? Email { get; init; }

    [Required]
    public string? Password { get; init; }
}

/// <summary>Bearer access token returned after successful authentication.</summary>
internal sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc);

/// <summary>Identity and roles associated with the authenticated caller.</summary>
internal sealed record CurrentUserResponse(Guid UserId, IReadOnlyCollection<string> Roles);
