using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class CatalogUseCasesTests
{
    private readonly InMemoryCatalog _catalog = new();
    private CategoryUseCases CategoryCases => new(_catalog);
    private ProductUseCases ProductCases => new(_catalog, _catalog);

    [Fact]
    public async Task CreateCategory_TrimsNameAndPersistsCategory()
    {
        var result = await CategoryCases.CreateAsync("  Beverages  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Beverages", result.Value.Name);
        Assert.Contains(result.Value, (await _catalog.ListWithProductsAsync()));
    }

    [Fact]
    public async Task CreateCategory_RejectsDuplicateName()
    {
        await CategoryCases.CreateAsync("Beverages");

        var result = await CategoryCases.CreateAsync(" beverages ");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.CategoryNameConflict, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateCategory_ReturnsNotFoundWhenMissing()
    {
        var result = await CategoryCases.UpdateAsync(Guid.NewGuid(), "Beverages", true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateCategory_RejectsDuplicateNameWithoutChangingCategory()
    {
        var existing = await CategoryCases.CreateAsync("Beverages");
        var other = await CategoryCases.CreateAsync("Snacks");

        var result = await CategoryCases.UpdateAsync(other.Value.Id, existing.Value.Name, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.CategoryNameConflict, result.Error!.Code);
        Assert.Equal("Snacks", other.Value.Name);
        Assert.True(other.Value.IsActive);
    }

    [Fact]
    public async Task UpdateCategory_RenamesAndChangesStatus()
    {
        var category = await CategoryCases.CreateAsync("Beverages");

        var result = await CategoryCases.UpdateAsync(category.Value.Id, "Cold Drinks", false);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cold Drinks", result.Value.Name);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task GetCategory_ReturnsCategoryById()
    {
        var category = await CategoryCases.CreateAsync("Beverages");

        var result = await CategoryCases.GetAsync(category.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Same(category.Value, result.Value);
    }

    [Fact]
    public async Task ListCategories_IncludesAssociatedProducts()
    {
        var category = await CategoryCases.CreateAsync("Beverages");
        var product = await ProductCases.CreateAsync("DRINK-1", "Water", category.Value.Id);

        var result = await CategoryCases.ListAsync();

        Assert.True(result.IsSuccess);
        var listedCategory = Assert.Single(result.Value);
        Assert.Contains(product.Value, listedCategory.Products);
    }

    [Fact]
    public async Task RemoveCategory_RejectsCategoryAssignedToActiveProduct()
    {
        var category = await CategoryCases.CreateAsync("Beverages");
        await ProductCases.CreateAsync("DRINK-1", "Water", category.Value.Id);

        var result = await CategoryCases.RemoveAsync(category.Value.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.CategoryInUse, result.Error!.Code);
        Assert.NotNull(await _catalog.GetByIdWithProductsAsync(category.Value.Id));
    }

    [Fact]
    public async Task RemoveCategory_RemovesCategoryWithoutActiveProducts()
    {
        var category = await CategoryCases.CreateAsync("Beverages");

        var result = await CategoryCases.RemoveAsync(category.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(category.Value.Id, result.Value);
        Assert.Null(await _catalog.GetByIdWithProductsAsync(category.Value.Id));
    }

    [Fact]
    public async Task CreateProduct_ReturnsNotFoundWhenCategoryDoesNotExist()
    {
        var result = await ProductCases.CreateAsync("DRINK-1", "Water", Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CreateProduct_RejectsDuplicateIdentifier()
    {
        var category = await CategoryCases.CreateAsync("Beverages");
        await ProductCases.CreateAsync("DRINK-1", "Water", category.Value.Id);

        var result = await ProductCases.CreateAsync("DRINK-1", "Sparkling Water", category.Value.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.ProductIdentifierConflict, result.Error!.Code);
    }

    [Fact]
    public async Task CreateProduct_RejectsInactiveCategory()
    {
        var category = await CategoryCases.CreateAsync("Beverages");
        await CategoryCases.UpdateAsync(category.Value.Id, category.Value.Name, false);

        var result = await ProductCases.CreateAsync("DRINK-1", "Water", category.Value.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InactiveCategory, result.Error!.Code);
    }

    [Fact]
    public async Task CreateProduct_RejectsInvalidInput()
    {
        var category = await CategoryCases.CreateAsync("Beverages");

        var result = await ProductCases.CreateAsync(" ", "Water", category.Value.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateProduct_UpdatesNameCategoryAndStatus()
    {
        var originalCategory = await CategoryCases.CreateAsync("Beverages");
        var newCategory = await CategoryCases.CreateAsync("Cold Drinks");
        var product = await ProductCases.CreateAsync("DRINK-1", "Water", originalCategory.Value.Id);

        var result = await ProductCases.UpdateAsync(
            product.Value.Identifier.Value,
            "Sparkling Water",
            newCategory.Value.Id,
            false);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sparkling Water", result.Value.Name);
        Assert.Same(newCategory.Value, result.Value.Category);
        Assert.False(result.Value.IsActive);
        Assert.DoesNotContain(result.Value, originalCategory.Value.Products);
        Assert.Contains(result.Value, newCategory.Value.Products);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsNotFoundWhenProductDoesNotExist()
    {
        var category = await CategoryCases.CreateAsync("Beverages");

        var result = await ProductCases.UpdateAsync("MISSING", "Water", category.Value.Id, true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task GetProduct_ReturnsProductByIdentifier()
    {
        var category = await CategoryCases.CreateAsync("Beverages");
        var product = await ProductCases.CreateAsync("DRINK-1", "Water", category.Value.Id);

        var result = await ProductCases.GetAsync(" DRINK-1 ");

        Assert.True(result.IsSuccess);
        Assert.Same(product.Value, result.Value);
    }

    [Fact]
    public async Task UpdateProduct_DoesNotMutateProductWhenActivationIsInvalid()
    {
        var category = await CategoryCases.CreateAsync("Beverages");
        var product = await ProductCases.CreateAsync("DRINK-1", "Water", category.Value.Id);
        await CategoryCases.UpdateAsync(category.Value.Id, category.Value.Name, false);

        var result = await ProductCases.UpdateAsync(
            product.Value.Identifier.Value,
            "Sparkling Water",
            category.Value.Id,
            true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InactiveCategory, result.Error!.Code);
        Assert.Equal("Water", product.Value.Name);
        Assert.True(product.Value.IsActive);
    }

    [Fact]
    public async Task ListProducts_PassesTrimmedSearchAndFiltersToRepository()
    {
        var category = await CategoryCases.CreateAsync("Beverages");
        await ProductCases.CreateAsync("DRINK-1", "Water", category.Value.Id);
        await ProductCases.CreateAsync("DRINK-2", "Juice", category.Value.Id);

        var result = await ProductCases.ListAsync(
            new ProductQuery(category.Value.Id, true, "  water  "));

        Assert.True(result.IsSuccess);
        var product = Assert.Single(result.Value);
        Assert.Equal("DRINK-1", product.Identifier.Value);
    }

    [Fact]
    public void FailedResult_DoesNotExposeAValue()
    {
        var result = Result<Category>.Failure(ApplicationErrorCode.NotFound, "Missing");

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
