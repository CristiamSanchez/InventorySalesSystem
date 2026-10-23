namespace SistemaInventarioVentas.Application.Models.Reports;

public sealed record InventoryStatusReportQuery
{
    public Guid? CategoryId { get; init; }

    public bool? InStockOnly { get; init; }
}
