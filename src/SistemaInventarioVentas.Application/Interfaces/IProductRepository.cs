using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdentifierAsync(
        ProductIdentifier identifier,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> IdentifierExistsAsync(
        ProductIdentifier identifier,
        CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task SaveAsync(Product product, CancellationToken cancellationToken = default);
}
