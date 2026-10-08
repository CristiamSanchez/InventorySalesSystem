using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class ProductUseCases(
    IProductRepository products,
    ICategoryRepository categories)
{
    public async Task<Result<Product>> CreateAsync(
        string identifier,
        string name,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeName(name, out var normalizedName))
        {
            return Result<Product>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Product name is required.");
        }

        ProductIdentifier productIdentifier;
        try
        {
            productIdentifier = new ProductIdentifier(identifier);
        }
        catch (ArgumentException exception)
        {
            return Result<Product>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        var category = await categories.GetByIdWithProductsAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result<Product>.Failure(
                ApplicationErrorCode.NotFound,
                "Category was not found.");
        }

        if (await products.IdentifierExistsAsync(productIdentifier, cancellationToken))
        {
            return Result<Product>.Failure(
                ApplicationErrorCode.ProductIdentifierConflict,
                "A product with this identifier already exists.");
        }

        Product product;
        try
        {
            product = new Product(productIdentifier, normalizedName, category);
        }
        catch (InvalidOperationException exception)
        {
            return Result<Product>.Failure(ApplicationErrorCode.InactiveCategory, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Result<Product>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await products.AddAsync(product, cancellationToken);
        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> UpdateAsync(
        string identifier,
        string name,
        Guid categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeName(name, out var normalizedName))
        {
            return Result<Product>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Product name is required.");
        }

        ProductIdentifier productIdentifier;
        try
        {
            productIdentifier = new ProductIdentifier(identifier);
        }
        catch (ArgumentException exception)
        {
            return Result<Product>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        var product = await products.GetByIdentifierAsync(productIdentifier, cancellationToken);
        if (product is null)
        {
            return Result<Product>.Failure(ApplicationErrorCode.NotFound, "Product was not found.");
        }

        var category = await categories.GetByIdWithProductsAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result<Product>.Failure(
                ApplicationErrorCode.NotFound,
                "Category was not found.");
        }

        if (isActive && !category.IsActive)
        {
            return Result<Product>.Failure(
                ApplicationErrorCode.InactiveCategory,
                "A product cannot be activated while its category is inactive.");
        }

        try
        {
            product.Update(normalizedName, category);
            SetProductStatus(product, isActive);
        }
        catch (InvalidOperationException exception)
        {
            return Result<Product>.Failure(ApplicationErrorCode.InactiveCategory, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Result<Product>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await products.SaveAsync(product, cancellationToken);
        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> GetAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        ProductIdentifier productIdentifier;
        try
        {
            productIdentifier = new ProductIdentifier(identifier);
        }
        catch (ArgumentException exception)
        {
            return Result<Product>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        var product = await products.GetByIdentifierAsync(productIdentifier, cancellationToken);
        return product is null
            ? Result<Product>.Failure(ApplicationErrorCode.NotFound, "Product was not found.")
            : Result<Product>.Success(product);
    }

    public async Task<Result<IReadOnlyList<Product>>> ListAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedQuery = query with { SearchTerm = query.SearchTerm?.Trim() };
        var result = await products.ListAsync(normalizedQuery, cancellationToken);
        return Result<IReadOnlyList<Product>>.Success(result);
    }

    private static bool TryNormalizeName(string? name, out string normalizedName)
    {
        normalizedName = name?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(normalizedName);
    }

    private static void SetProductStatus(Product product, bool isActive)
    {
        if (isActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }
    }
}
