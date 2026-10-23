using System.ComponentModel.DataAnnotations;

namespace SistemaInventarioVentas.API.Contracts;

/// <summary>Request to record a stock entry.</summary>
public sealed record RecordStockEntryRequest
{
    /// <summary>The product identifier if not supplied in the route path.</summary>
    public string? ProductIdentifier { get; init; }

    /// <summary>The quantity of stock entering inventory.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Entry quantity must be greater than zero.")]
    public int? Quantity { get; init; }

    /// <summary>The business reference or document for the entry.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Reference is required.")]
    public string? Reference { get; init; }

    /// <summary>Optional UTC occurrence timestamp.</summary>
    public DateTimeOffset? OccurredAtUtc { get; init; }

    /// <summary>Optional registered supplier ID.</summary>
    public Guid? SupplierId { get; init; }
}

/// <summary>Request to record a stock exit.</summary>
public sealed record RecordStockExitRequest
{
    /// <summary>The product identifier if not supplied in the route path.</summary>
    public string? ProductIdentifier { get; init; }

    /// <summary>The quantity of stock leaving inventory.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Exit quantity must be greater than zero.")]
    public int? Quantity { get; init; }

    /// <summary>The business reference or document for the exit.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Reference is required.")]
    public string? Reference { get; init; }

    /// <summary>Optional UTC occurrence timestamp.</summary>
    public DateTimeOffset? OccurredAtUtc { get; init; }
}

/// <summary>Request to adjust stock using signed-delta semantics.</summary>
public sealed record AdjustStockRequest
{
    /// <summary>The product identifier if not supplied in the route path.</summary>
    public string? ProductIdentifier { get; init; }

    /// <summary>The signed non-zero quantity change.</summary>
    [Required]
    public int? QuantityDelta { get; init; }

    /// <summary>The business reason for the adjustment.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Reason is required.")]
    public string? Reason { get; init; }

    /// <summary>Optional UTC occurrence timestamp.</summary>
    public DateTimeOffset? OccurredAtUtc { get; init; }
}

/// <summary>Current inventory representation returned by the API.</summary>
public sealed record InventoryResponse(
    string ProductIdentifier,
    int Quantity);

/// <summary>An inventory movement record returned by the API.</summary>
public sealed record InventoryMovementResponse(
    Guid Id,
    string ProductIdentifier,
    string MovementType,
    int Quantity,
    string Reference,
    DateTimeOffset OccurredAtUtc,
    Guid? SupplierId = null,
    string? SupplierName = null);
