using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class CatalogPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task CategoryRepository_PersistsAndLoadsCategoryWithProducts()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var categories = new CategoryRepository(dbContext);
        var products = new ProductRepository(dbContext);
        var category = new Category("Beverages");
        await categories.AddAsync(category);
        await products.AddAsync(new Product(new ProductIdentifier("DRINK-1"), "Water", category));

        await using var queryContext = database.CreateDbContext();
        var result = await new CategoryRepository(queryContext).GetByIdWithProductsAsync(category.Id);

        Assert.NotNull(result);
        Assert.Equal("Beverages", result.Name);
        Assert.Contains(result.Products, product => product.Identifier.Value == "DRINK-1");
    }

    [Fact]
    public async Task ProductRepository_PersistsProductIdentifierAndCategoryRelationship()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var category = new Category("Beverages");
        await new CategoryRepository(dbContext).AddAsync(category);
        var repository = new ProductRepository(dbContext);
        await repository.AddAsync(new Product(
            new ProductIdentifier("DRINK-1"),
            "Water",
            category));

        await using var queryContext = database.CreateDbContext();
        var result = await new ProductRepository(queryContext)
            .GetByIdentifierAsync(new ProductIdentifier("DRINK-1"));

        Assert.NotNull(result);
        Assert.Equal("DRINK-1", result.Identifier.Value);
        Assert.Equal("Water", result.Name);
        Assert.Equal(category.Id, result.Category.Id);
    }

    [Fact]
    public async Task ProductRepository_ReassignmentUpdatesCategoryNavigation()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var categories = new CategoryRepository(dbContext);
        var oldCategory = new Category("Beverages");
        var newCategory = new Category("Cold Drinks");
        await categories.AddAsync(oldCategory);
        await categories.AddAsync(newCategory);
        var productRepository = new ProductRepository(dbContext);
        await productRepository.AddAsync(
            new Product(new ProductIdentifier("DRINK-1"), "Water", oldCategory));

        var product = await productRepository.GetByIdentifierAsync(new ProductIdentifier("DRINK-1"));
        var targetCategory = await categories.GetByIdWithProductsAsync(newCategory.Id);
        Assert.NotNull(product);
        Assert.NotNull(targetCategory);
        product.Update("Sparkling Water", targetCategory);
        await productRepository.SaveAsync(product);

        await using var queryContext = database.CreateDbContext();
        var updated = await new ProductRepository(queryContext)
            .GetByIdentifierAsync(new ProductIdentifier("DRINK-1"));
        var loadedOldCategory = await new CategoryRepository(queryContext)
            .GetByIdWithProductsAsync(oldCategory.Id);
        var loadedNewCategory = await new CategoryRepository(queryContext)
            .GetByIdWithProductsAsync(newCategory.Id);

        Assert.NotNull(updated);
        Assert.Equal("Sparkling Water", updated.Name);
        Assert.Equal(newCategory.Id, updated.Category.Id);
        Assert.Empty(loadedOldCategory!.Products);
        Assert.Contains(loadedNewCategory!.Products, item => item.Identifier.Value == "DRINK-1");
    }

    [Fact]
    public async Task CategoryName_IsUniqueIgnoringCaseAtDatabaseLevel()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new CategoryRepository(dbContext);
        await repository.AddAsync(new Category("Beverages"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(new Category("beverages")));

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation,
            ((PostgresException)exception.InnerException!).SqlState);
    }

    [Fact]
    public async Task CategoryRepository_NameExistsSupportsCaseInsensitiveLookupAndExclusion()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new CategoryRepository(dbContext);
        var category = new Category("Beverages");
        await repository.AddAsync(category);

        Assert.True(await repository.NameExistsAsync("beverages"));
        Assert.False(await repository.NameExistsAsync("beverages", category.Id));
        Assert.False(await repository.NameExistsAsync("Snacks"));
    }

    [Fact]
    public async Task ProductIdentifier_IsUniqueAtDatabaseLevel()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var category = new Category("Beverages");
        await new CategoryRepository(dbContext).AddAsync(category);
        var repository = new ProductRepository(dbContext);
        await repository.AddAsync(new Product(new ProductIdentifier("DRINK-1"), "Water", category));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(
                new Product(new ProductIdentifier("DRINK-1"), "Juice", category)));

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation,
            ((PostgresException)exception.InnerException!).SqlState);
    }

    [Fact]
    public async Task CategoryRepository_RemovesUnusedCategory()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new CategoryRepository(dbContext);
        var category = new Category("Beverages");
        await repository.AddAsync(category);

        await repository.RemoveAsync(category);

        Assert.False(await dbContext.Categories.AnyAsync(item => item.Id == category.Id));
    }

    [Fact]
    public async Task CategoryDelete_IsRestrictedWhileAnyProductReferencesIt()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var categories = new CategoryRepository(dbContext);
        var category = new Category("Beverages");
        await categories.AddAsync(category);
        var product = new Product(new ProductIdentifier("DRINK-1"), "Water", category);
        product.Deactivate();
        await new ProductRepository(dbContext).AddAsync(product);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM categories WHERE \"Id\" = {category.Id}"));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.NotNull(await categories.GetByIdWithProductsAsync(category.Id));
    }

    [Fact]
    public async Task ProductRepository_FiltersByCategoryStatusAndSearchTerm()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var category = new Category("Beverages");
        await new CategoryRepository(dbContext).AddAsync(category);
        var products = new ProductRepository(dbContext);
        await products.AddAsync(new Product(new ProductIdentifier("DRINK-1"), "Water", category));
        var inactiveProduct = new Product(new ProductIdentifier("DRINK-2"), "Juice", category);
        inactiveProduct.Deactivate();
        await products.AddAsync(inactiveProduct);

        var result = await products.ListAsync(
            new ProductQuery(category.Id, true, "drink-1"));

        var product = Assert.Single(result);
        Assert.Equal("DRINK-1", product.Identifier.Value);
    }

    [Fact]
    public async Task ProductRepository_IdentifierExistsChecksPersistedIdentifiers()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var category = new Category("Beverages");
        await new CategoryRepository(dbContext).AddAsync(category);
        var repository = new ProductRepository(dbContext);
        await repository.AddAsync(new Product(new ProductIdentifier("DRINK-1"), "Water", category));

        Assert.True(await repository.IdentifierExistsAsync(new ProductIdentifier("DRINK-1")));
        Assert.False(await repository.IdentifierExistsAsync(new ProductIdentifier("DRINK-2")));
    }

    [Fact]
    public async Task Repositories_RespectCancellationToken()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new CategoryRepository(dbContext).ListWithProductsAsync(cancellation.Token));
    }
}
