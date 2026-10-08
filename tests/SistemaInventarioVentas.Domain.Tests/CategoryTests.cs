using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class CategoryTests
{
    [Fact]
    public void Constructor_TrimsNameAndStartsActive()
    {
        var category = new Category("  Beverages  ");

        Assert.Equal("Beverages", category.Name);
        Assert.True(category.IsActive);
        Assert.Empty(category.Products);
        Assert.NotEqual(Guid.Empty, category.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankName(string name)
    {
        Assert.Throws<ArgumentException>(() => new Category(name));
    }

    [Fact]
    public void Rename_RejectsBlankNameWithoutChangingCurrentName()
    {
        var category = new Category("Beverages");

        Assert.Throws<ArgumentException>(() => category.Rename(" "));

        Assert.Equal("Beverages", category.Name);
    }

    [Fact]
    public void EnsureCanBeRemoved_AllowsCategoryWithoutActiveProducts()
    {
        var category = new Category("Beverages");

        var exception = Record.Exception(category.EnsureCanBeRemoved);

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanBeRemoved_RejectsCategoryAssignedToActiveProduct()
    {
        var category = new Category("Beverages");
        _ = new Product(new ProductIdentifier("DRINK-1"), "Water", category);

        Assert.Throws<InvalidOperationException>(category.EnsureCanBeRemoved);
    }

    [Fact]
    public void EnsureCanBeRemoved_AllowsCategoryAfterAssignedProductsAreDeactivated()
    {
        var category = new Category("Beverages");
        var product = new Product(new ProductIdentifier("DRINK-1"), "Water", category);
        product.Deactivate();

        var exception = Record.Exception(category.EnsureCanBeRemoved);

        Assert.Null(exception);
    }

    [Fact]
    public void DeactivateAndActivate_ChangeCategoryState()
    {
        var category = new Category("Beverages");

        category.Deactivate();

        Assert.False(category.IsActive);

        category.Activate();

        Assert.True(category.IsActive);
    }
}
