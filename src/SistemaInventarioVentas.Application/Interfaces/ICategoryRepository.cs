using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdWithProductsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListWithProductsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task SaveAsync(Category category, CancellationToken cancellationToken = default);

    Task RemoveAsync(Category category, CancellationToken cancellationToken = default);
}
