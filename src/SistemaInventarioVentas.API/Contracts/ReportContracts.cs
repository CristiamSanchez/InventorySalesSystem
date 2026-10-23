namespace SistemaInventarioVentas.API.Contracts;

/// <summary>Sales summary and transaction history report response.</summary>
public sealed record SalesSummaryReportResponse(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalTransactions,
    int ConfirmedTransactions,
    int DraftTransactions,
    int CancelledTransactions,
    decimal TotalSubtotal,
    decimal TotalTax,
    decimal TotalRevenue,
    IReadOnlyList<SalesSummarySaleItemResponse> Sales);

/// <summary>An individual sale item in the sales summary report.</summary>
public sealed record SalesSummarySaleItemResponse(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    string Status,
    Guid? CustomerId,
    string CustomerReference,
    Guid UserId,
    int TotalUnits,
    int LineCount,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total);

/// <summary>Product sales performance report response.</summary>
public sealed record ProductSalesReportResponse(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalProductsSold,
    int TotalUnitsSold,
    decimal TotalSubtotal,
    decimal TotalTax,
    decimal TotalRevenue,
    IReadOnlyList<ProductSalesItemResponse> Items);

/// <summary>Product contribution item in the product sales performance report.</summary>
public sealed record ProductSalesItemResponse(
    string ProductIdentifier,
    string ProductName,
    Guid CategoryId,
    string CategoryName,
    int UnitsSold,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total);

/// <summary>Inventory stock status report response across catalog products.</summary>
public sealed record InventoryStatusReportResponse(
    int TotalProducts,
    int TotalUnitsOnHand,
    int InStockProductsCount,
    int OutOfStockProductsCount,
    IReadOnlyList<InventoryStatusItemResponse> Items);

/// <summary>Individual product stock status in the inventory status report.</summary>
public sealed record InventoryStatusItemResponse(
    string ProductIdentifier,
    string ProductName,
    Guid CategoryId,
    string CategoryName,
    int Quantity,
    bool HasInventoryRecord,
    bool IsActive,
    string StockStatus);

/// <summary>Inventory movement traceability report response.</summary>
public sealed record InventoryMovementReportResponse(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalMovements,
    int EntryCount,
    int ExitCount,
    int AdjustmentCount,
    int TotalUnitsIn,
    int TotalUnitsOut,
    int NetQuantityDelta,
    IReadOnlyList<InventoryMovementReportItemResponse> Movements);

/// <summary>An individual movement record in the inventory movement traceability report.</summary>
public sealed record InventoryMovementReportItemResponse(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string ProductIdentifier,
    string MovementType,
    int Quantity,
    string Reference,
    Guid? SupplierId,
    string? SupplierName,
    Guid? UserId);
