using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models.Reports;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class ReportingUseCases(IReportingRepository reportingRepository)
{
    public async Task<Result<SalesSummaryReport>> GetSalesSummaryReportAsync(
        SalesSummaryReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.FromUtc > query.ToUtc)
        {
            return Result<SalesSummaryReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "FromUtc must be less than or equal to ToUtc.");
        }

        if (query.CustomerId.HasValue && query.CustomerId.Value == Guid.Empty)
        {
            return Result<SalesSummaryReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "CustomerId cannot be empty when provided.");
        }

        if (query.UserId.HasValue && query.UserId.Value == Guid.Empty)
        {
            return Result<SalesSummaryReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "UserId cannot be empty when provided.");
        }

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            return Result<SalesSummaryReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Invalid sale status.");
        }

        var normalizedQuery = query with
        {
            FromUtc = query.FromUtc.ToUniversalTime(),
            ToUtc = query.ToUtc.ToUniversalTime()
        };

        var report = await reportingRepository.GetSalesSummaryReportAsync(normalizedQuery, cancellationToken);
        return Result<SalesSummaryReport>.Success(report);
    }

    public async Task<Result<ProductSalesReport>> GetProductSalesReportAsync(
        ProductSalesReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.FromUtc > query.ToUtc)
        {
            return Result<ProductSalesReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "FromUtc must be less than or equal to ToUtc.");
        }

        if (query.CategoryId.HasValue && query.CategoryId.Value == Guid.Empty)
        {
            return Result<ProductSalesReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "CategoryId cannot be empty when provided.");
        }

        var normalizedQuery = query with
        {
            FromUtc = query.FromUtc.ToUniversalTime(),
            ToUtc = query.ToUtc.ToUniversalTime(),
            ProductIdentifier = string.IsNullOrWhiteSpace(query.ProductIdentifier) ? null : query.ProductIdentifier.Trim()
        };

        var report = await reportingRepository.GetProductSalesReportAsync(normalizedQuery, cancellationToken);
        return Result<ProductSalesReport>.Success(report);
    }

    public async Task<Result<InventoryStatusReport>> GetInventoryStatusReportAsync(
        InventoryStatusReportQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new InventoryStatusReportQuery();

        if (effectiveQuery.CategoryId.HasValue && effectiveQuery.CategoryId.Value == Guid.Empty)
        {
            return Result<InventoryStatusReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "CategoryId cannot be empty when provided.");
        }

        var report = await reportingRepository.GetInventoryStatusReportAsync(effectiveQuery, cancellationToken);
        return Result<InventoryStatusReport>.Success(report);
    }

    public async Task<Result<InventoryMovementReport>> GetInventoryMovementReportAsync(
        InventoryMovementReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.FromUtc > query.ToUtc)
        {
            return Result<InventoryMovementReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "FromUtc must be less than or equal to ToUtc.");
        }

        if (query.MovementType.HasValue && !Enum.IsDefined(query.MovementType.Value))
        {
            return Result<InventoryMovementReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Invalid movement type.");
        }

        if (query.SupplierId.HasValue && query.SupplierId.Value == Guid.Empty)
        {
            return Result<InventoryMovementReport>.Failure(
                ApplicationErrorCode.InvalidInput,
                "SupplierId cannot be empty when provided.");
        }

        var normalizedQuery = query with
        {
            FromUtc = query.FromUtc.ToUniversalTime(),
            ToUtc = query.ToUtc.ToUniversalTime(),
            ProductIdentifier = string.IsNullOrWhiteSpace(query.ProductIdentifier) ? null : query.ProductIdentifier.Trim()
        };

        var report = await reportingRepository.GetInventoryMovementReportAsync(normalizedQuery, cancellationToken);
        return Result<InventoryMovementReport>.Success(report);
    }
}
