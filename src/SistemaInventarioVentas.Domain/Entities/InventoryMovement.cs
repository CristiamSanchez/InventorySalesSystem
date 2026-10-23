using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class InventoryMovement
{
    private InventoryMovement()
    {
        ProductIdentifier = null!;
        Reference = string.Empty;
        SupplierId = null;
        SupplierName = null;
    }

    public InventoryMovement(
        ProductIdentifier productIdentifier,
        MovementType movementType,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc = null,
        Guid? supplierId = null,
        string? supplierName = null)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        if (!Enum.IsDefined(movementType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(movementType),
                "A valid movement type must be provided.");
        }

        if (movementType != MovementType.Adjustment && quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (movementType == MovementType.Adjustment && quantity == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Adjustment quantity cannot be zero.");
        }

        if (movementType != MovementType.Entry && (supplierId.HasValue || supplierName is not null))
        {
            throw new ArgumentException(
                "Supplier information can only be associated with stock entries.",
                nameof(supplierId));
        }

        if (supplierId.HasValue)
        {
            if (supplierId.Value == Guid.Empty)
            {
                throw new ArgumentException("Supplier ID cannot be empty.", nameof(supplierId));
            }

            if (string.IsNullOrWhiteSpace(supplierName))
            {
                throw new ArgumentException("Supplier name is required when a supplier ID is provided.", nameof(supplierName));
            }

            var trimmedName = supplierName.Trim();
            if (trimmedName.Length > 200)
            {
                throw new ArgumentException("Supplier name cannot exceed 200 characters.", nameof(supplierName));
            }

            SupplierId = supplierId.Value;
            SupplierName = trimmedName;
        }
        else
        {
            if (supplierName is not null)
            {
                throw new ArgumentException("Supplier name cannot be provided without a supplier ID.", nameof(supplierName));
            }

            SupplierId = null;
            SupplierName = null;
        }

        Id = Guid.NewGuid();
        ProductIdentifier = productIdentifier;
        MovementType = movementType;
        Quantity = quantity;
        Reference = reference.Trim();
        OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public ProductIdentifier ProductIdentifier { get; }

    public MovementType MovementType { get; }

    public int Quantity { get; }

    public string Reference { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public Guid? SupplierId { get; }

    public string? SupplierName { get; }
}
