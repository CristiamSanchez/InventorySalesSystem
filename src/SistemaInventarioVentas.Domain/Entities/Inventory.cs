using System.Collections.ObjectModel;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class Inventory
{
    private readonly List<InventoryMovement> _movements = [];
    private readonly ReadOnlyCollection<InventoryMovement> _movementsView;

    private Inventory()
    {
        ProductIdentifier = null!;
        _movementsView = _movements.AsReadOnly();
    }

    public Inventory(ProductIdentifier productIdentifier)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);

        Id = Guid.NewGuid();
        ProductIdentifier = productIdentifier;
        Quantity = 0;
        _movementsView = _movements.AsReadOnly();
    }

    public Inventory(
        ProductIdentifier productIdentifier,
        int initialStock,
        string initialStockReference,
        DateTimeOffset? occurredAtUtc = null)
        : this(productIdentifier)
    {
        if (initialStock < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialStock),
                "Initial inventory stock cannot be negative.");
        }

        if (initialStock > 0)
        {
            RegisterEntry(initialStock, initialStockReference, occurredAtUtc);
        }
    }

    public Guid Id { get; }

    public ProductIdentifier ProductIdentifier { get; }

    public int Quantity { get; private set; }

    public IReadOnlyCollection<InventoryMovement> Movements => _movementsView;

    public void RegisterEntry(
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc = null,
        Guid? supplierId = null,
        string? supplierName = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Entry quantity must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        checked
        {
            Quantity += quantity;
        }

        var movement = new InventoryMovement(
            ProductIdentifier,
            MovementType.Entry,
            quantity,
            reference,
            occurredAtUtc,
            supplierId,
            supplierName);

        _movements.Add(movement);
    }

    public void RegisterExit(int quantity, string reference, DateTimeOffset? occurredAtUtc = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Exit quantity must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        if (Quantity < quantity)
        {
            throw new InvalidOperationException("Inventory stock cannot become negative.");
        }

        Quantity -= quantity;

        var movement = new InventoryMovement(
            ProductIdentifier,
            MovementType.Exit,
            quantity,
            reference,
            occurredAtUtc);

        _movements.Add(movement);
    }

    public void Adjust(int quantityDelta, string reason, DateTimeOffset? occurredAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityDelta),
                "Adjustment quantity cannot be zero.");
        }

        if (Quantity + quantityDelta < 0)
        {
            throw new InvalidOperationException("Inventory stock cannot become negative.");
        }

        checked
        {
            Quantity += quantityDelta;
        }

        var movement = new InventoryMovement(
            ProductIdentifier,
            MovementType.Adjustment,
            quantityDelta,
            reason,
            occurredAtUtc);

        _movements.Add(movement);
    }

    public void RegisterAdjustment(int quantityDelta, string reason, DateTimeOffset? occurredAtUtc = null) =>
        Adjust(quantityDelta, reason, occurredAtUtc);

    public void RecordEntry(
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc = null,
        Guid? supplierId = null,
        string? supplierName = null) =>
        RegisterEntry(quantity, reference, occurredAtUtc, supplierId, supplierName);

    public void RecordExit(int quantity, string reference, DateTimeOffset? occurredAtUtc = null) =>
        RegisterExit(quantity, reference, occurredAtUtc);

    public void RecordAdjustment(int quantityDelta, string reason, DateTimeOffset? occurredAtUtc = null) =>
        Adjust(quantityDelta, reason, occurredAtUtc);
}
