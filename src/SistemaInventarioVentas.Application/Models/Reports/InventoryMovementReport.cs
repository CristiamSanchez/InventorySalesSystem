using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record InventoryMovementReport
{
    public DateTimeOffset FromUtc { get; init; }

    public DateTimeOffset ToUtc { get; init; }

    public int TotalMovements { get; init; }

    public int EntryCount { get; init; }

    public int ExitCount { get; init; }

    public int AdjustmentCount { get; init; }

    public int TotalUnitsIn { get; init; }

    public int TotalUnitsOut { get; init; }

    public int NetQuantityDelta { get; init; }

    public IReadOnlyList<InventoryMovementReportItem> Movements { get; init; } = [];
}

public sealed record InventoryMovementReportItem
{
    public Guid Id { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public string ProductIdentifier { get; init; } = string.Empty;

    public MovementType MovementType { get; init; }

    public int Quantity { get; init; }

    public string Reference { get; init; } = string.Empty;

    public Guid? SupplierId { get; init; }

    public string? SupplierName { get; init; }

    public Guid? UserId { get; init; }
}
