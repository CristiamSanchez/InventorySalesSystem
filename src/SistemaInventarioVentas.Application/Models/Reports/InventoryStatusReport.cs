namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record InventoryStatusReport
{
    public int TotalProducts { get; init; }

    public int TotalUnitsOnHand { get; init; }

    public int InStockProductsCount { get; init; }

    public int OutOfStockProductsCount { get; init; }

    public IReadOnlyList<InventoryStatusItem> Items { get; init; } = [];
}

public sealed record InventoryStatusItem
{
    public string ProductIdentifier { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public bool HasInventoryRecord { get; init; }

    public bool IsActive { get; init; }

    public string StockStatus { get; init; } = string.Empty;
}
