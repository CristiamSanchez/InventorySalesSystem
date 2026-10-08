using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class User
{
    private HashSet<UserRole> _roles = [];

    private User()
    {
        Name = string.Empty;
        Email = null!;
        PasswordHash = null!;
    }

    public User(
        string name,
        EmailAddress email,
        PasswordHash passwordHash,
        UserRole role = UserRole.User)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);
        ValidateRole(role);

        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        Email = email;
        PasswordHash = passwordHash;
        _roles.Add(role);
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public EmailAddress Email { get; private set; }

    public PasswordHash PasswordHash { get; }

    public bool IsActive { get; private set; }

    public IReadOnlySet<UserRole> Roles => _roles.ToHashSet();

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void AssignRole(UserRole role)
    {
        ValidateRole(role);
        _roles.Add(role);
    }

    public void RemoveRole(UserRole role)
    {
        ValidateRole(role);
        if (!_roles.Contains(role))
        {
            return;
        }

        if (_roles.Count == 1)
        {
            throw new InvalidOperationException("A user must have at least one role.");
        }

        _roles.Remove(role);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }

    private static void ValidateRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "The user role is not supported.");
        }
    }
}
