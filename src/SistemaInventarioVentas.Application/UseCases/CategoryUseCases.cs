using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class CategoryUseCases(ICategoryRepository categories)
{
    public async Task<Result<Category>> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeName(name, out var normalizedName))
        {
            return Result<Category>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Category name is required.");
        }

        if (await categories.NameExistsAsync(normalizedName, cancellationToken: cancellationToken))
        {
            return Result<Category>.Failure(
                ApplicationErrorCode.CategoryNameConflict,
                "A category with this name already exists.");
        }

        var category = new Category(normalizedName);
        await categories.AddAsync(category, cancellationToken);
        return Result<Category>.Success(category);
    }

    public async Task<Result<Category>> UpdateAsync(
        Guid id,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdWithProductsAsync(id, cancellationToken);
        if (category is null)
        {
            return Result<Category>.Failure(
                ApplicationErrorCode.NotFound,
                "Category was not found.");
        }

        if (!TryNormalizeName(name, out var normalizedName))
        {
            return Result<Category>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Category name is required.");
        }

        if (await categories.NameExistsAsync(normalizedName, id, cancellationToken))
        {
            return Result<Category>.Failure(
                ApplicationErrorCode.CategoryNameConflict,
                "A category with this name already exists.");
        }

        category.Rename(normalizedName);
        SetCategoryStatus(category, isActive);
        await categories.SaveAsync(category, cancellationToken);
        return Result<Category>.Success(category);
    }

    public async Task<Result<Category>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdWithProductsAsync(id, cancellationToken);
        return category is null
            ? Result<Category>.Failure(ApplicationErrorCode.NotFound, "Category was not found.")
            : Result<Category>.Success(category);
    }

    public async Task<Result<IReadOnlyList<Category>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await categories.ListWithProductsAsync(cancellationToken);
        return Result<IReadOnlyList<Category>>.Success(result);
    }

    public async Task<Result<Guid>> RemoveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdWithProductsAsync(id, cancellationToken);
        if (category is null)
        {
            return Result<Guid>.Failure(
                ApplicationErrorCode.NotFound,
                "Category was not found.");
        }

        try
        {
            category.EnsureCanBeRemoved();
        }
        catch (InvalidOperationException exception)
        {
            return Result<Guid>.Failure(ApplicationErrorCode.CategoryInUse, exception.Message);
        }

        await categories.RemoveAsync(category, cancellationToken);
        return Result<Guid>.Success(category.Id);
    }

    private static bool TryNormalizeName(string? name, out string normalizedName)
    {
        normalizedName = name?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(normalizedName);
    }

    private static void SetCategoryStatus(Category category, bool isActive)
    {
        if (isActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }
    }
}
