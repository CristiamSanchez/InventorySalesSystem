using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdentifierAsync(
        ProductIdentifier identifier,
        CancellationToken cancellationToken = default) =>
        dbContext.Products
            .Include(product => product.Category)
            .ThenInclude(category => category.Products)
            .SingleOrDefaultAsync(
                product => product.Identifier.Value == identifier.Value,
                cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Product> products = dbContext.Products.Include(product => product.Category);
        if (query.CategoryId is Guid categoryId)
        {
            products = products.Where(product => product.Category.Id == categoryId);
        }

        if (query.IsActive is bool isActive)
        {
            products = products.Where(product => product.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            products = products.Where(product =>
                EF.Functions.ILike(product.Name, $"%{searchTerm}%") ||
                EF.Functions.ILike(product.Identifier.Value, $"%{searchTerm}%"));
        }

        return await products.ToListAsync(cancellationToken);
    }

    public Task<bool> IdentifierExistsAsync(
        ProductIdentifier identifier,
        CancellationToken cancellationToken = default) =>
        dbContext.Products.AnyAsync(
            product => product.Identifier.Value == identifier.Value,
            cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (dbContext.Entry(product).State == EntityState.Detached)
        {
            dbContext.Products.Update(product);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
