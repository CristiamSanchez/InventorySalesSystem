using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models.Reports;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class ReportingRepository(CatalogDbContext dbContext) : IReportingRepository
{
    public async Task<SalesSummaryReport> GetSalesSummaryReportAsync(
        SalesSummaryReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var baseQuery = dbContext.Sales.AsNoTracking()
            .Where(s => s.CreatedAtUtc >= query.FromUtc && s.CreatedAtUtc <= query.ToUtc);

        if (query.CustomerId.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.CustomerId == query.CustomerId.Value);
        }

        if (query.UserId.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.UserId == query.UserId.Value);
        }

        if (query.Status.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.Status == query.Status.Value);
        }

        var summary = await baseQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalTransactions = g.Count(),
                ConfirmedTransactions = g.Count(s => s.Status == SaleStatus.Confirmed),
                DraftTransactions = g.Count(s => s.Status == SaleStatus.Draft),
                CancelledTransactions = g.Count(s => s.Status == SaleStatus.Cancelled),
                TotalSubtotal = g.Where(s => s.Status == SaleStatus.Confirmed).Sum(s => (decimal?)s.Subtotal) ?? 0m,
                TotalTax = g.Where(s => s.Status == SaleStatus.Confirmed).Sum(s => (decimal?)s.TaxAmount) ?? 0m,
                TotalRevenue = g.Where(s => s.Status == SaleStatus.Confirmed).Sum(s => (decimal?)s.Total) ?? 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        var sales = await baseQuery
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Select(s => new SalesSummarySaleItem
            {
                Id = s.Id,
                CreatedAtUtc = s.CreatedAtUtc,
                ConfirmedAtUtc = s.ConfirmedAtUtc,
                Status = s.Status,
                CustomerId = s.CustomerId,
                CustomerReference = s.CustomerReference,
                UserId = s.UserId,
                TotalUnits = s.Details.Sum(d => d.Quantity),
                LineCount = s.Details.Count,
                Subtotal = s.Subtotal,
                TaxAmount = s.TaxAmount,
                Total = s.Total
            })
            .ToListAsync(cancellationToken);

        return new SalesSummaryReport
        {
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
            TotalTransactions = summary?.TotalTransactions ?? 0,
            ConfirmedTransactions = summary?.ConfirmedTransactions ?? 0,
            DraftTransactions = summary?.DraftTransactions ?? 0,
            CancelledTransactions = summary?.CancelledTransactions ?? 0,
            TotalSubtotal = summary?.TotalSubtotal ?? 0m,
            TotalTax = summary?.TotalTax ?? 0m,
            TotalRevenue = summary?.TotalRevenue ?? 0m,
            Sales = sales
        };
    }

    public async Task<ProductSalesReport> GetProductSalesReportAsync(
        ProductSalesReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var detailsQuery = from detail in dbContext.SaleDetails.AsNoTracking()
                           join sale in dbContext.Sales.AsNoTracking() on detail.SaleId equals sale.Id
                           where sale.Status == SaleStatus.Confirmed &&
                                 sale.CreatedAtUtc >= query.FromUtc &&
                                 sale.CreatedAtUtc <= query.ToUtc
                           select detail;

        if (!string.IsNullOrWhiteSpace(query.ProductIdentifier))
        {
            var identifier = new ProductIdentifier(query.ProductIdentifier.Trim());
            detailsQuery = detailsQuery.Where(d => d.ProductIdentifier == identifier);
        }

        if (query.CategoryId.HasValue)
        {
            var matchingIdentifiers = await dbContext.Products.AsNoTracking()
                .Where(p => p.Category.Id == query.CategoryId.Value)
                .Select(p => p.Identifier.Value)
                .ToListAsync(cancellationToken);

            var matchingProductIdentifiers = matchingIdentifiers.Select(id => new ProductIdentifier(id)).ToList();
            detailsQuery = detailsQuery.Where(d => matchingProductIdentifiers.Contains(d.ProductIdentifier));
        }

        var grouped = await detailsQuery
            .GroupBy(d => d.ProductIdentifier)
            .Select(g => new
            {
                ProductIdentifier = g.Key,
                UnitsSold = g.Sum(d => d.Quantity),
                Subtotal = g.Sum(d => d.Subtotal),
                TaxAmount = g.Sum(d => d.TaxAmount),
                Total = g.Sum(d => d.Total)
            })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return new ProductSalesReport
            {
                FromUtc = query.FromUtc,
                ToUtc = query.ToUtc,
                TotalProductsSold = 0,
                TotalUnitsSold = 0,
                TotalSubtotal = 0m,
                TotalTax = 0m,
                TotalRevenue = 0m,
                Items = []
            };
        }

        var identifierStrings = grouped.Select(g => g.ProductIdentifier.Value).ToList();
        var products = await dbContext.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => identifierStrings.Contains(p.Identifier.Value))
            .ToListAsync(cancellationToken);

        var productDict = products.ToDictionary(p => p.Identifier.Value);

        var items = grouped
            .Select(g =>
            {
                productDict.TryGetValue(g.ProductIdentifier.Value, out var prod);
                return new ProductSalesItem
                {
                    ProductIdentifier = g.ProductIdentifier.Value,
                    ProductName = prod?.Name ?? g.ProductIdentifier.Value,
                    CategoryId = prod?.Category?.Id ?? Guid.Empty,
                    CategoryName = prod?.Category?.Name ?? string.Empty,
                    UnitsSold = g.UnitsSold,
                    Subtotal = g.Subtotal,
                    TaxAmount = g.TaxAmount,
                    Total = g.Total
                };
            })
            .OrderByDescending(i => i.Total)
            .ThenByDescending(i => i.UnitsSold)
            .ThenBy(i => i.ProductIdentifier)
            .ToList();

        return new ProductSalesReport
        {
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
            TotalProductsSold = items.Count,
            TotalUnitsSold = items.Sum(i => i.UnitsSold),
            TotalSubtotal = items.Sum(i => i.Subtotal),
            TotalTax = items.Sum(i => i.TaxAmount),
            TotalRevenue = items.Sum(i => i.Total),
            Items = items
        };
    }

    public async Task<InventoryStatusReport> GetInventoryStatusReportAsync(
        InventoryStatusReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var productsQuery = dbContext.Products.AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (query.CategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Category.Id == query.CategoryId.Value);
        }

        var products = await productsQuery
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Identifier.Value)
            .ToListAsync(cancellationToken);

        var productIdentifiers = products.Select(p => new ProductIdentifier(p.Identifier.Value)).ToList();
        var inventories = await dbContext.Inventories.AsNoTracking()
            .Where(i => productIdentifiers.Contains(i.ProductIdentifier))
            .ToDictionaryAsync(i => i.ProductIdentifier.Value, i => i.Quantity, cancellationToken);

        var allItems = products.Select(p =>
        {
            var hasRecord = inventories.TryGetValue(p.Identifier.Value, out var qty);
            var quantity = hasRecord ? qty : 0;
            return new InventoryStatusItem
            {
                ProductIdentifier = p.Identifier.Value,
                ProductName = p.Name,
                CategoryId = p.Category.Id,
                CategoryName = p.Category.Name,
                Quantity = quantity,
                HasInventoryRecord = hasRecord,
                IsActive = p.IsActive,
                StockStatus = hasRecord && quantity > 0 ? "InStock" : "OutOfStock"
            };
        }).ToList();

        var totalProducts = allItems.Count;
        var totalUnits = allItems.Sum(i => i.Quantity);
        var inStockCount = allItems.Count(i => i.StockStatus == "InStock");
        var outOfStockCount = totalProducts - inStockCount;

        var resultItems = query.InStockOnly == true
            ? allItems.Where(i => i.StockStatus == "InStock").ToList()
            : allItems;

        return new InventoryStatusReport
        {
            TotalProducts = totalProducts,
            TotalUnitsOnHand = totalUnits,
            InStockProductsCount = inStockCount,
            OutOfStockProductsCount = outOfStockCount,
            Items = resultItems
        };
    }

    public async Task<InventoryMovementReport> GetInventoryMovementReportAsync(
        InventoryMovementReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var baseQuery = dbContext.InventoryMovements.AsNoTracking()
            .Where(m => m.OccurredAtUtc >= query.FromUtc && m.OccurredAtUtc <= query.ToUtc);

        if (!string.IsNullOrWhiteSpace(query.ProductIdentifier))
        {
            var identifier = new ProductIdentifier(query.ProductIdentifier.Trim());
            baseQuery = baseQuery.Where(m => m.ProductIdentifier == identifier);
        }

        if (query.MovementType.HasValue)
        {
            baseQuery = baseQuery.Where(m => m.MovementType == query.MovementType.Value);
        }

        if (query.SupplierId.HasValue)
        {
            baseQuery = baseQuery.Where(m => m.SupplierId == query.SupplierId.Value);
        }

        var summary = await baseQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalMovements = g.Count(),
                EntryCount = g.Count(m => m.MovementType == MovementType.Entry),
                ExitCount = g.Count(m => m.MovementType == MovementType.Exit),
                AdjustmentCount = g.Count(m => m.MovementType == MovementType.Adjustment),
                TotalUnitsIn = g.Where(m => m.MovementType == MovementType.Entry).Sum(m => (int?)m.Quantity) ?? 0,
                TotalUnitsOut = g.Where(m => m.MovementType == MovementType.Exit).Sum(m => (int?)m.Quantity) ?? 0,
                AdjustmentDelta = g.Where(m => m.MovementType == MovementType.Adjustment).Sum(m => (int?)m.Quantity) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        var movements = await baseQuery
            .OrderByDescending(m => m.OccurredAtUtc)
            .ThenByDescending(m => m.Id)
            .Select(m => new
            {
                m.Id,
                m.OccurredAtUtc,
                ProductIdentifier = m.ProductIdentifier.Value,
                m.MovementType,
                m.Quantity,
                m.Reference,
                m.SupplierId,
                m.SupplierName
            })
            .ToListAsync(cancellationToken);

        var movementIds = movements.Select(m => m.Id.ToString()).ToList();
        var auditMap = await dbContext.AuditEvents.AsNoTracking()
            .Where(a => a.EntityName == AuditEvent.EntityNames.InventoryMovement &&
                        a.EntityId != null &&
                        movementIds.Contains(a.EntityId))
            .GroupBy(a => a.EntityId!)
            .ToDictionaryAsync(g => g.Key, g => g.First().UserId, cancellationToken);

        var items = movements.Select(m => new InventoryMovementReportItem
        {
            Id = m.Id,
            OccurredAtUtc = m.OccurredAtUtc,
            ProductIdentifier = m.ProductIdentifier,
            MovementType = m.MovementType,
            Quantity = m.Quantity,
            Reference = m.Reference,
            SupplierId = m.SupplierId,
            SupplierName = m.SupplierName,
            UserId = auditMap.TryGetValue(m.Id.ToString(), out var userId) ? userId : null
        }).ToList();

        var totalUnitsIn = summary?.TotalUnitsIn ?? 0;
        var totalUnitsOut = summary?.TotalUnitsOut ?? 0;
        var adjustmentDelta = summary?.AdjustmentDelta ?? 0;

        return new InventoryMovementReport
        {
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
            TotalMovements = summary?.TotalMovements ?? 0,
            EntryCount = summary?.EntryCount ?? 0,
            ExitCount = summary?.ExitCount ?? 0,
            AdjustmentCount = summary?.AdjustmentCount ?? 0,
            TotalUnitsIn = totalUnitsIn,
            TotalUnitsOut = totalUnitsOut,
            NetQuantityDelta = (totalUnitsIn - totalUnitsOut) + adjustmentDelta,
            Movements = items
        };
    }
}
