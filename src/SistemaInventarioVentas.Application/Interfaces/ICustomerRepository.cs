using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListAsync(
        CustomerQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdentificationNumberAsync(
        string normalizedIdentificationNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Customer customer,
        CancellationToken cancellationToken = default);
}
