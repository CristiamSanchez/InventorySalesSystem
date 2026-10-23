namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record ProductSalesReport
{
    public DateTimeOffset FromUtc { get; init; }

    public DateTimeOffset ToUtc { get; init; }

    public int TotalProductsSold { get; init; }

    public int TotalUnitsSold { get; init; }

    public decimal TotalSubtotal { get; init; }

    public decimal TotalTax { get; init; }

    public decimal TotalRevenue { get; init; }

    public IReadOnlyList<ProductSalesItem> Items { get; init; } = [];
}

public sealed record ProductSalesItem
{
    public string ProductIdentifier { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public int UnitsSold { get; init; }

    public decimal Subtotal { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal Total { get; init; }
}
