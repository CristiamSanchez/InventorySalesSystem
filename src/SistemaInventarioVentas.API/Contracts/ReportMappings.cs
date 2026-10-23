using SistemaInventarioVentas.Application.Models.Reports;

namespace SistemaInventarioVentas.API.Contracts;

internal static class ReportMappings
{
    public static SalesSummaryReportResponse ToResponse(this SalesSummaryReport report) =>
        new(
            report.FromUtc,
            report.ToUtc,
            report.TotalTransactions,
            report.ConfirmedTransactions,
            report.DraftTransactions,
            report.CancelledTransactions,
            report.TotalSubtotal,
            report.TotalTax,
            report.TotalRevenue,
            report.Sales.Select(ToResponse).ToList());

    public static SalesSummarySaleItemResponse ToResponse(this SalesSummarySaleItem item) =>
        new(
            item.Id,
            item.CreatedAtUtc,
            item.ConfirmedAtUtc,
            item.Status.ToString(),
            item.CustomerId,
            item.CustomerReference,
            item.UserId,
            item.TotalUnits,
            item.LineCount,
            item.Subtotal,
            item.TaxAmount,
            item.Total);

    public static ProductSalesReportResponse ToResponse(this ProductSalesReport report) =>
        new(
            report.FromUtc,
            report.ToUtc,
            report.TotalProductsSold,
            report.TotalUnitsSold,
            report.TotalSubtotal,
            report.TotalTax,
            report.TotalRevenue,
            report.Items.Select(ToResponse).ToList());

    public static ProductSalesItemResponse ToResponse(this ProductSalesItem item) =>
        new(
            item.ProductIdentifier,
            item.ProductName,
            item.CategoryId,
            item.CategoryName,
            item.UnitsSold,
            item.Subtotal,
            item.TaxAmount,
            item.Total);

    public static InventoryStatusReportResponse ToResponse(this InventoryStatusReport report) =>
        new(
            report.TotalProducts,
            report.TotalUnitsOnHand,
            report.InStockProductsCount,
            report.OutOfStockProductsCount,
            report.Items.Select(ToResponse).ToList());

    public static InventoryStatusItemResponse ToResponse(this InventoryStatusItem item) =>
        new(
            item.ProductIdentifier,
            item.ProductName,
            item.CategoryId,
            item.CategoryName,
            item.Quantity,
            item.HasInventoryRecord,
            item.IsActive,
            item.StockStatus);

    public static InventoryMovementReportResponse ToResponse(this InventoryMovementReport report) =>
        new(
            report.FromUtc,
            report.ToUtc,
            report.TotalMovements,
            report.EntryCount,
            report.ExitCount,
            report.AdjustmentCount,
            report.TotalUnitsIn,
            report.TotalUnitsOut,
            report.NetQuantityDelta,
            report.Movements.Select(ToResponse).ToList());

    public static InventoryMovementReportItemResponse ToResponse(this InventoryMovementReportItem item) =>
        new(
            item.Id,
            item.OccurredAtUtc,
            item.ProductIdentifier,
            item.MovementType.ToString(),
            item.Quantity,
            item.Reference,
            item.SupplierId,
            item.SupplierName,
            item.UserId);
}
