using System.Collections.ObjectModel;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class Category
{
    private readonly List<Product> _products = [];
    private readonly ReadOnlyCollection<Product> _productsView;

    private Category()
    {
        Name = string.Empty;
        _productsView = _products.AsReadOnly();
    }

    public Category(string name)
    {
        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        _productsView = _products.AsReadOnly();
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Product> Products => _productsView;

    public bool CanBeRemoved => _products.All(product => !product.IsActive);

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void EnsureCanBeRemoved()
    {
        if (!CanBeRemoved)
        {
            throw new InvalidOperationException(
                "A category cannot be removed while it is assigned to active products.");
        }
    }

    internal void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!IsActive)
        {
            throw new InvalidOperationException(
                "An inactive category cannot be assigned to a product.");
        }

        if (!_products.Contains(product))
        {
            _products.Add(product);
        }
    }

    internal void RemoveProduct(Product product)
    {
        _products.Remove(product);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
