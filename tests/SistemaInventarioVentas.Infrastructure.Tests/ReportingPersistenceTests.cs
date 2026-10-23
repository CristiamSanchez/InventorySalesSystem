using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.Application.Models.Reports;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class ReportingPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task GetSalesSummaryReportAsync_AggregatesRevenueOnlyForConfirmedSales_AndRespectsFilters()
    {
        await database.ClearAsync();

        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        await using (var context = database.CreateDbContext())
        {
            var customer = new Customer(customerId, "Acme Corp", "RFC-ACME-01");
            await context.Customers.AddAsync(customer);

            var category = new Category("Electronics");
            await context.Categories.AddAsync(category);

            var prodIdentifier = new ProductIdentifier("PROD-01");
            var product = new Product(prodIdentifier, "Laptop", category);
            await context.Products.AddAsync(product);

            // Confirmed sale: Subtotal 100, Tax 16, Total 116
            var confirmedSale = new Sale(Guid.NewGuid(), "Acme Corp", userId, baseDate, customerId);
            confirmedSale.AddDetail(prodIdentifier, 1, 100m, 0.16m);
            confirmedSale.Confirm(baseDate.AddMinutes(5));
            await context.Sales.AddAsync(confirmedSale);

            // Draft sale: Subtotal 200, Tax 32, Total 232
            var draftSale = new Sale(Guid.NewGuid(), "Acme Corp", userId, baseDate.AddHours(1), customerId);
            draftSale.AddDetail(prodIdentifier, 2, 100m, 0.16m);
            await context.Sales.AddAsync(draftSale);

            // Cancelled sale: Subtotal 50, Tax 8, Total 58
            var cancelledSale = new Sale(Guid.NewGuid(), "Acme Corp", userId, baseDate.AddHours(2), customerId);
            cancelledSale.AddDetail(prodIdentifier, 1, 50m, 0.16m);
            cancelledSale.Cancel();
            await context.Sales.AddAsync(cancelledSale);

            await context.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var repository = new ReportingRepository(queryContext);

        var query = new SalesSummaryReportQuery
        {
            FromUtc = baseDate.AddHours(-1),
            ToUtc = baseDate.AddHours(5)
        };

        var report = await repository.GetSalesSummaryReportAsync(query);

        Assert.Equal(3, report.TotalTransactions);
        Assert.Equal(1, report.ConfirmedTransactions);
        Assert.Equal(1, report.DraftTransactions);
        Assert.Equal(1, report.CancelledTransactions);

        // BR-REP-004: Only confirmed sales contribute to financial totals
        Assert.Equal(100m, report.TotalSubtotal);
        Assert.Equal(16m, report.TotalTax);
        Assert.Equal(116m, report.TotalRevenue);

        // Itemized list includes all 3 sales in descending date order
        Assert.Equal(3, report.Sales.Count);
        Assert.Equal(SaleStatus.Cancelled, report.Sales[0].Status);
        Assert.Equal(SaleStatus.Draft, report.Sales[1].Status);
        Assert.Equal(SaleStatus.Confirmed, report.Sales[2].Status);
        Assert.Equal(1, report.Sales[2].TotalUnits);
        Assert.Equal(116m, report.Sales[2].Total);
    }

    [Fact]
    public async Task GetSalesSummaryReportAsync_WithStatusAndCustomerFilter_ReturnsFilteredSubset()
    {
        await database.ClearAsync();

        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        await using (var context = database.CreateDbContext())
        {
            var category = new Category("Hardware");
            await context.Categories.AddAsync(category);

            var custA = new Customer(customerA, "Customer A");
            var custB = new Customer(customerB, "Customer B");
            await context.Customers.AddRangeAsync(custA, custB);

            var p1 = new Product(new ProductIdentifier("P-1"), "Hammer", category);
            await context.Products.AddAsync(p1);

            var saleA = new Sale(Guid.NewGuid(), "Customer A", userId, baseDate, customerA);
            saleA.AddDetail(p1.Identifier, 1, 20m, 0m);
            saleA.Confirm(baseDate.AddMinutes(1));
            await context.Sales.AddAsync(saleA);

            var saleB = new Sale(Guid.NewGuid(), "Customer B", userId, baseDate.AddMinutes(5), customerB);
            saleB.AddDetail(p1.Identifier, 2, 20m, 0m);
            saleB.Confirm(baseDate.AddMinutes(6));
            await context.Sales.AddAsync(saleB);

            await context.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var repository = new ReportingRepository(queryContext);

        var query = new SalesSummaryReportQuery
        {
            FromUtc = baseDate.AddMinutes(-10),
            ToUtc = baseDate.AddHours(1),
            CustomerId = customerA,
            Status = SaleStatus.Confirmed
        };

        var report = await repository.GetSalesSummaryReportAsync(query);

        Assert.Equal(1, report.TotalTransactions);
        Assert.Equal(1, report.ConfirmedTransactions);
        Assert.Equal(20m, report.TotalRevenue);
        Assert.Single(report.Sales);
        Assert.Equal(customerA, report.Sales[0].CustomerId);
    }

    [Fact]
    public async Task GetProductSalesReportAsync_CalculatesMetricsFromConfirmedSaleDetails_AndPreservesSnapshots()
    {
        await database.ClearAsync();

        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

        Guid catId;
        await using (var context = database.CreateDbContext())
        {
            var category = new Category("Beverages");
            catId = category.Id;
            await context.Categories.AddAsync(category);

            var prod1 = new Product(new ProductIdentifier("COFFEE-01"), "Espresso Beans", category);
            var prod2 = new Product(new ProductIdentifier("TEA-01"), "Green Tea Leaves", category);
            await context.Products.AddRangeAsync(prod1, prod2);

            // Sale 1 (Confirmed): 2 Coffee @ 15.00 (tax 10% = 3.00), total 33.00
            var sale1 = new Sale(Guid.NewGuid(), "Walk-in", userId, baseDate);
            sale1.AddDetail(prod1.Identifier, 2, 15m, 0.10m);
            sale1.Confirm(baseDate.AddMinutes(1));
            await context.Sales.AddAsync(sale1);

            // Sale 2 (Confirmed): 3 Coffee @ 15.00 + 1 Tea @ 10.00
            var sale2 = new Sale(Guid.NewGuid(), "Walk-in", userId, baseDate.AddHours(1));
            sale2.AddDetail(prod1.Identifier, 3, 15m, 0.10m);
            sale2.AddDetail(prod2.Identifier, 1, 10m, 0.10m);
            sale2.Confirm(baseDate.AddHours(1).AddMinutes(1));
            await context.Sales.AddAsync(sale2);

            // Sale 3 (Draft - should be excluded): 5 Coffee
            var sale3 = new Sale(Guid.NewGuid(), "Draft Customer", userId, baseDate.AddHours(2));
            sale3.AddDetail(prod1.Identifier, 5, 15m, 0.10m);
            await context.Sales.AddAsync(sale3);

            await context.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var repository = new ReportingRepository(queryContext);

        var report = await repository.GetProductSalesReportAsync(new ProductSalesReportQuery
        {
            FromUtc = baseDate.AddHours(-1),
            ToUtc = baseDate.AddHours(5)
        });

        // 2 distinct products sold across confirmed sales
        Assert.Equal(2, report.TotalProductsSold);
        // Total units: 5 coffee + 1 tea = 6 units
        Assert.Equal(6, report.TotalUnitsSold);
        // Total subtotal: (5 * 15) + (1 * 10) = 75 + 10 = 85m
        Assert.Equal(85m, report.TotalSubtotal);
        // Total tax: (75 * 0.10) + (10 * 0.10) = 7.50 + 1.00 = 8.50m
        Assert.Equal(8.50m, report.TotalTax);
        Assert.Equal(93.50m, report.TotalRevenue);

        Assert.Equal(2, report.Items.Count);

        // Ranked by Total descending: Coffee first, then Tea
        var coffeeItem = report.Items[0];
        Assert.Equal("COFFEE-01", coffeeItem.ProductIdentifier);
        Assert.Equal("Espresso Beans", coffeeItem.ProductName);
        Assert.Equal("Beverages", coffeeItem.CategoryName);
        Assert.Equal(5, coffeeItem.UnitsSold);
        Assert.Equal(75m, coffeeItem.Subtotal);
        Assert.Equal(7.50m, coffeeItem.TaxAmount);
        Assert.Equal(82.50m, coffeeItem.Total);

        var teaItem = report.Items[1];
        Assert.Equal("TEA-01", teaItem.ProductIdentifier);
        Assert.Equal("Green Tea Leaves", teaItem.ProductName);
        Assert.Equal(1, teaItem.UnitsSold);
        Assert.Equal(10m, teaItem.Subtotal);
        Assert.Equal(1m, teaItem.TaxAmount);
        Assert.Equal(11m, teaItem.Total);
    }

    [Fact]
    public async Task GetProductSalesReportAsync_WithCategoryFilter_FiltersToMatchingCategoryOnly()
    {
        await database.ClearAsync();

        var userId = Guid.NewGuid();
        var baseDate = DateTimeOffset.UtcNow.AddDays(-1);

        Guid catAId;
        Guid catBId;

        await using (var context = database.CreateDbContext())
        {
            var catA = new Category("Category A");
            var catB = new Category("Category B");
            catAId = catA.Id;
            catBId = catB.Id;
            await context.Categories.AddRangeAsync(catA, catB);

            var pA = new Product(new ProductIdentifier("P-A"), "Item A", catA);
            var pB = new Product(new ProductIdentifier("P-B"), "Item B", catB);
            await context.Products.AddRangeAsync(pA, pB);

            var sale = new Sale(Guid.NewGuid(), "Customer", userId, baseDate);
            sale.AddDetail(pA.Identifier, 2, 25m, 0m);
            sale.AddDetail(pB.Identifier, 4, 10m, 0m);
            sale.Confirm(baseDate.AddMinutes(1));
            await context.Sales.AddAsync(sale);

            await context.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var repository = new ReportingRepository(queryContext);

        var report = await repository.GetProductSalesReportAsync(new ProductSalesReportQuery
        {
            FromUtc = baseDate.AddHours(-1),
            ToUtc = baseDate.AddHours(2),
            CategoryId = catAId
        });

        Assert.Equal(1, report.TotalProductsSold);
        Assert.Equal(2, report.TotalUnitsSold);
        Assert.Equal(50m, report.TotalRevenue);
        Assert.Single(report.Items);
        Assert.Equal("P-A", report.Items[0].ProductIdentifier);
    }

    [Fact]
    public async Task GetInventoryStatusReportAsync_HandlesInStock_ZeroStock_AndUninitializedRecords()
    {
        await database.ClearAsync();

        Guid catId;
        await using (var context = database.CreateDbContext())
        {
            var category = new Category("Tools");
            catId = category.Id;
            await context.Categories.AddAsync(category);

            // Product 1: Has positive stock (15 units)
            var p1 = new Product(new ProductIdentifier("DRILL-01"), "Cordless Drill", category);
            var inv1 = new Inventory(p1.Identifier, 15, "Initial delivery");

            // Product 2: Has inventory record with 0 stock
            var p2 = new Product(new ProductIdentifier("SAW-01"), "Hand Saw", category);
            var inv2 = new Inventory(p2.Identifier);

            // Product 3: Registered catalog product with NO inventory record
            var p3 = new Product(new ProductIdentifier("WRENCH-01"), "Wrench Set", category);

            await context.Products.AddRangeAsync(p1, p2, p3);
            await context.Inventories.AddRangeAsync(inv1, inv2);
            await context.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var repository = new ReportingRepository(queryContext);

        // Full report
        var report = await repository.GetInventoryStatusReportAsync(new InventoryStatusReportQuery());

        Assert.Equal(3, report.TotalProducts);
        Assert.Equal(15, report.TotalUnitsOnHand);
        Assert.Equal(1, report.InStockProductsCount);
        Assert.Equal(2, report.OutOfStockProductsCount);
        Assert.Equal(3, report.Items.Count);

        var drillItem = report.Items.First(i => i.ProductIdentifier == "DRILL-01");
        Assert.Equal(15, drillItem.Quantity);
        Assert.True(drillItem.HasInventoryRecord);
        Assert.Equal("InStock", drillItem.StockStatus);

        var sawItem = report.Items.First(i => i.ProductIdentifier == "SAW-01");
        Assert.Equal(0, sawItem.Quantity);
        Assert.True(sawItem.HasInventoryRecord);
        Assert.Equal("OutOfStock", sawItem.StockStatus);

        // BR-REP-006: Uninitialized products report 0 stock and HasInventoryRecord = false
        var wrenchItem = report.Items.First(i => i.ProductIdentifier == "WRENCH-01");
        Assert.Equal(0, wrenchItem.Quantity);
        Assert.False(wrenchItem.HasInventoryRecord);
        Assert.Equal("OutOfStock", wrenchItem.StockStatus);

        // Filter InStockOnly = true: items contains only in-stock products
        var inStockReport = await repository.GetInventoryStatusReportAsync(new InventoryStatusReportQuery
        {
            InStockOnly = true
        });

        Assert.Equal(3, inStockReport.TotalProducts);
        Assert.Equal(1, inStockReport.InStockProductsCount);
        Assert.Single(inStockReport.Items);
        Assert.Equal("DRILL-01", inStockReport.Items[0].ProductIdentifier);
    }

    [Fact]
    public async Task GetInventoryMovementReportAsync_CalculatesNetDeltas_AndPreservesSnapshotsAndUserAttribution()
    {
        await database.ClearAsync();

        var supplierId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);
        var productIdentifier = new ProductIdentifier("STEEL-BAR");

        Guid movement1Id;
        Guid movement2Id;
        Guid movement3Id;

        await using (var context = database.CreateDbContext())
        {
            var supplier = new Supplier(supplierId, "Global Metals Ltd", "TAX-METALS-01");
            await context.Suppliers.AddAsync(supplier);

            var category = new Category("Raw Materials");
            await context.Categories.AddAsync(category);

            var product = new Product(productIdentifier, "Steel Bar", category);
            await context.Products.AddAsync(product);

            var inventory = new Inventory(productIdentifier);

            // Entry 1: +100 units with Supplier snapshot at 8:00
            inventory.RegisterEntry(100, "PO-9001", baseDate, supplierId, "Global Metals Ltd");
            movement1Id = inventory.Movements.Last().Id;

            // Exit 2: -30 units at 9:00
            inventory.RegisterExit(30, "Job-42", baseDate.AddHours(1));
            movement2Id = inventory.Movements.Last().Id;

            // Adjustment 3: -5 units at 10:00 (damaged stock)
            inventory.RegisterAdjustment(-5, "Damaged in transit", baseDate.AddHours(2));
            movement3Id = inventory.Movements.Last().Id;

            await context.Inventories.AddAsync(inventory);

            // Audit event for Movement 1 (attributing user)
            var auditEvent = AuditEvent.CreateForUser(
                userId,
                AuditEvent.EventTypes.InventoryMovementRecorded,
                AuditEvent.EntityNames.InventoryMovement,
                movement1Id.ToString(),
                baseDate);
            await context.AuditEvents.AddAsync(auditEvent);

            await context.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var repository = new ReportingRepository(queryContext);

        var report = await repository.GetInventoryMovementReportAsync(new InventoryMovementReportQuery
        {
            FromUtc = baseDate.AddHours(-1),
            ToUtc = baseDate.AddHours(5)
        });

        Assert.Equal(3, report.TotalMovements);
        Assert.Equal(1, report.EntryCount);
        Assert.Equal(1, report.ExitCount);
        Assert.Equal(1, report.AdjustmentCount);
        Assert.Equal(100, report.TotalUnitsIn);
        Assert.Equal(30, report.TotalUnitsOut);
        // Net delta: (100 - 30) + (-5) = 65
        Assert.Equal(65, report.NetQuantityDelta);

        Assert.Equal(3, report.Movements.Count);

        // Ordered by OccurredAtUtc descending (Adjustment -> Exit -> Entry)
        var item1 = report.Movements[0];
        Assert.Equal(movement3Id, item1.Id);
        Assert.Equal(MovementType.Adjustment, item1.MovementType);
        Assert.Equal(-5, item1.Quantity);
        Assert.Equal("Damaged in transit", item1.Reference);

        var item2 = report.Movements[1];
        Assert.Equal(movement2Id, item2.Id);
        Assert.Equal(MovementType.Exit, item2.MovementType);
        Assert.Equal(30, item2.Quantity);
        Assert.Equal("Job-42", item2.Reference);

        var item3 = report.Movements[2];
        Assert.Equal(movement1Id, item3.Id);
        Assert.Equal(MovementType.Entry, item3.MovementType);
        Assert.Equal(100, item3.Quantity);
        Assert.Equal("PO-9001", item3.Reference);
        Assert.Equal(supplierId, item3.SupplierId);
        Assert.Equal("Global Metals Ltd", item3.SupplierName);
        // Preserves user attribution via AuditEvent
        Assert.Equal(userId, item3.UserId);
    }

    [Fact]
    public async Task Reports_AreStrictlyReadOnly_AndDoNotCreateAuditEvents()
    {
        await database.ClearAsync();

        var baseDate = DateTimeOffset.UtcNow.AddDays(-2);
        var userId = Guid.NewGuid();

        await using (var context = database.CreateDbContext())
        {
            var category = new Category("Stationery");
            await context.Categories.AddAsync(category);

            var prod = new Product(new ProductIdentifier("PEN-01"), "Blue Pen", category);
            await context.Products.AddAsync(prod);

            var sale = new Sale(Guid.NewGuid(), "Customer", userId, baseDate);
            sale.AddDetail(prod.Identifier, 5, 2m, 0m);
            sale.Confirm(baseDate.AddMinutes(1));
            await context.Sales.AddAsync(sale);

            await context.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var initialAuditCount = await queryContext.AuditEvents.CountAsync();
        var initialSalesCount = await queryContext.Sales.CountAsync();
        var initialProductsCount = await queryContext.Products.CountAsync();

        var repository = new ReportingRepository(queryContext);

        // Execute all 4 reports
        await repository.GetSalesSummaryReportAsync(new SalesSummaryReportQuery
        {
            FromUtc = baseDate.AddHours(-1),
            ToUtc = DateTimeOffset.UtcNow
        });

        await repository.GetProductSalesReportAsync(new ProductSalesReportQuery
        {
            FromUtc = baseDate.AddHours(-1),
            ToUtc = DateTimeOffset.UtcNow
        });

        await repository.GetInventoryStatusReportAsync(new InventoryStatusReportQuery());

        await repository.GetInventoryMovementReportAsync(new InventoryMovementReportQuery
        {
            FromUtc = baseDate.AddHours(-1),
            ToUtc = DateTimeOffset.UtcNow
        });

        // Verify zero side effects and zero audit events created (BR-AUD-004, BR-REP-007)
        var finalAuditCount = await queryContext.AuditEvents.CountAsync();
        var finalSalesCount = await queryContext.Sales.CountAsync();
        var finalProductsCount = await queryContext.Products.CountAsync();

        Assert.Equal(initialAuditCount, finalAuditCount);
        Assert.Equal(initialSalesCount, finalSalesCount);
        Assert.Equal(initialProductsCount, finalProductsCount);
    }
}
