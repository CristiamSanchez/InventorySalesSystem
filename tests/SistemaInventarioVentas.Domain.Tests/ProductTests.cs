using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_TrimsNameAndRegistersProductWithActiveCategory()
    {
        var category = new Category("Beverages");

        var product = new Product(new ProductIdentifier("DRINK-1"), "  Water  ", category);

        Assert.Equal("Water", product.Name);
        Assert.Equal("DRINK-1", product.Identifier.Value);
        Assert.Same(category, product.Category);
        Assert.True(product.IsActive);
        Assert.Contains(product, category.Products);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankName(string name)
    {
        var category = new Category("Beverages");

        Assert.Throws<ArgumentException>(
            () => new Product(new ProductIdentifier("DRINK-1"), name, category));
        Assert.Empty(category.Products);
    }

    [Fact]
    public void Constructor_RejectsNullCategory()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Product(new ProductIdentifier("DRINK-1"), "Water", null!));
    }

    [Fact]
    public void Constructor_RejectsNullIdentifier()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Product(null!, "Water", new Category("Beverages")));
    }

    [Fact]
    public void Constructor_RejectsInactiveCategory()
    {
        var category = new Category("Beverages");
        category.Deactivate();

        Assert.Throws<InvalidOperationException>(
            () => new Product(new ProductIdentifier("DRINK-1"), "Water", category));
        Assert.Empty(category.Products);
    }

    [Fact]
    public void Update_ReassignsCategoryAndMaintainsBothAssociations()
    {
        var originalCategory = new Category("Beverages");
        var newCategory = new Category("Cold Drinks");
        var product = new Product(new ProductIdentifier("DRINK-1"), "Water", originalCategory);

        product.Update("Sparkling Water", newCategory);

        Assert.Equal("Sparkling Water", product.Name);
        Assert.Same(newCategory, product.Category);
        Assert.DoesNotContain(product, originalCategory.Products);
        Assert.Contains(product, newCategory.Products);
    }

    [Fact]
    public void Update_RejectsInactiveCategoryWithoutChangingProduct()
    {
        var originalCategory = new Category("Beverages");
        var inactiveCategory = new Category("Archived");
        inactiveCategory.Deactivate();
        var product = new Product(new ProductIdentifier("DRINK-1"), "Water", originalCategory);

        Assert.Throws<InvalidOperationException>(() => product.Update("Sparkling Water", inactiveCategory));

        Assert.Equal("Water", product.Name);
        Assert.Same(originalCategory, product.Category);
        Assert.Contains(product, originalCategory.Products);
        Assert.Empty(inactiveCategory.Products);
    }

    [Fact]
    public void Update_RejectsBlankNameWithoutChangingProduct()
    {
        var originalCategory = new Category("Beverages");
        var newCategory = new Category("Cold Drinks");
        var product = new Product(new ProductIdentifier("DRINK-1"), "Water", originalCategory);

        Assert.Throws<ArgumentException>(() => product.Update(" ", newCategory));

        Assert.Equal("Water", product.Name);
        Assert.Same(originalCategory, product.Category);
        Assert.Contains(product, originalCategory.Products);
        Assert.Empty(newCategory.Products);
    }

    [Fact]
    public void DeactivateAndActivate_ChangeProductState()
    {
        var product = new Product(
            new ProductIdentifier("DRINK-1"),
            "Water",
            new Category("Beverages"));

        product.Deactivate();

        Assert.False(product.IsActive);

        product.Activate();

        Assert.True(product.IsActive);
    }

    [Fact]
    public void Activate_RejectsInactiveCategory()
    {
        var category = new Category("Beverages");
        var product = new Product(new ProductIdentifier("DRINK-1"), "Water", category);
        product.Deactivate();
        category.Deactivate();

        Assert.Throws<InvalidOperationException>(product.Activate);

        Assert.False(product.IsActive);
    }
}
