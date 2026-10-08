using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(
        EmailAddress email,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        EmailAddress email,
        CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
