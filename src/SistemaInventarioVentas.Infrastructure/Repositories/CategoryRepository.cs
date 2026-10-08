using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class CategoryRepository(CatalogDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> GetByIdWithProductsAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Categories
            .Include(category => category.Products)
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListWithProductsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .Include(category => category.Products)
            .ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var nameKey = normalizedName.Trim().ToLowerInvariant();
        var categories = excludingId is Guid id
            ? dbContext.Categories.Where(category => category.Id != id)
            : dbContext.Categories;

        return categories.AnyAsync(
            category => EF.Property<string>(category, "NameKey") == nameKey,
            cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await dbContext.Categories.AddAsync(category, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(Category category, CancellationToken cancellationToken = default)
    {
        if (dbContext.Entry(category).State == EntityState.Detached)
        {
            dbContext.Categories.Update(category);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Category category, CancellationToken cancellationToken = default)
    {
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
