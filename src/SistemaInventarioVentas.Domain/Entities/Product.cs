using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class Product
{
    private Product()
    {
        Identifier = null!;
        Name = string.Empty;
        Category = null!;
    }

    public Product(ProductIdentifier identifier, string name, Category category)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(category);

        Identifier = identifier;
        Name = NormalizeName(name);
        Category = category;
        category.AddProduct(this);
    }

    public ProductIdentifier Identifier { get; }

    public string Name { get; private set; }

    public Category Category { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Update(string name, Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        var normalizedName = NormalizeName(name);

        if (!ReferenceEquals(Category, category))
        {
            var previousCategory = Category;
            category.AddProduct(this);
            Category = category;
            previousCategory.RemoveProduct(this);
        }

        Name = normalizedName;
    }

    public void Activate()
    {
        if (!Category.IsActive)
        {
            throw new InvalidOperationException(
                "A product cannot be activated while its category is inactive.");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
