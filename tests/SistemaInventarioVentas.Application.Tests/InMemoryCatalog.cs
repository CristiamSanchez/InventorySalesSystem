using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Tests;

internal sealed class InMemoryCatalog : ICategoryRepository, IProductRepository
{
    private readonly List<Category> _categories = [];
    private readonly List<Product> _products = [];

    public Task<Category?> GetByIdWithProductsAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_categories.SingleOrDefault(category => category.Id == id));

    public Task<IReadOnlyList<Category>> ListWithProductsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Category>>(_categories.ToArray());

    public Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_categories.Any(category =>
            category.Id != excludingId &&
            string.Equals(category.Name, normalizedName, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _categories.Add(category);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Category category, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(Category category, CancellationToken cancellationToken = default)
    {
        _categories.Remove(category);
        return Task.CompletedTask;
    }

    public Task<Product?> GetByIdentifierAsync(
        ProductIdentifier identifier,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.SingleOrDefault(product => product.Identifier == identifier));

    public Task<IReadOnlyList<Product>> ListAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Product> result = _products;
        if (query.CategoryId is Guid categoryId)
        {
            result = result.Where(product => product.Category.Id == categoryId);
        }

        if (query.IsActive is bool isActive)
        {
            result = result.Where(product => product.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            result = result.Where(product =>
                product.Name.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                product.Identifier.Value.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<Product>>(result.ToArray());
    }

    public Task<bool> IdentifierExistsAsync(
        ProductIdentifier identifier,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Any(product => product.Identifier == identifier));

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Product product, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
