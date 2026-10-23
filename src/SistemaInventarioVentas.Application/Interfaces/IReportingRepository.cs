using SistemaInventarioVentas.Application.Models.Reports;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface IReportingRepository
{
    Task<SalesSummaryReport> GetSalesSummaryReportAsync(
        SalesSummaryReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ProductSalesReport> GetProductSalesReportAsync(
        ProductSalesReportQuery query,
        CancellationToken cancellationToken = default);

    Task<InventoryStatusReport> GetInventoryStatusReportAsync(
        InventoryStatusReportQuery query,
        CancellationToken cancellationToken = default);

    Task<InventoryMovementReport> GetInventoryMovementReportAsync(
        InventoryMovementReportQuery query,
        CancellationToken cancellationToken = default);
}
