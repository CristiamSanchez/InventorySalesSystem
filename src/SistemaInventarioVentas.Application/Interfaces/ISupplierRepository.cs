using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Supplier>> ListAsync(
        SupplierQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdentificationNumberAsync(
        string normalizedIdentificationNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Supplier supplier,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Supplier supplier,
        CancellationToken cancellationToken = default);
}
