using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.API.Tests;

public sealed class ReportApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AnonymousAccess_ReturnsUnauthorized401_ForAllEndpoints()
    {
        await fixture.ResetAsync();

        using var salesResponse = await fixture.Client.GetAsync("/api/reports/sales-summary?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.Unauthorized, salesResponse.StatusCode);

        using var prodResponse = await fixture.Client.GetAsync("/api/reports/product-sales?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.Unauthorized, prodResponse.StatusCode);

        using var invStatusResponse = await fixture.Client.GetAsync("/api/reports/inventory-status");
        Assert.Equal(HttpStatusCode.Unauthorized, invStatusResponse.StatusCode);

        using var movResponse = await fixture.Client.GetAsync("/api/reports/inventory-movements?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.Unauthorized, movResponse.StatusCode);
    }

    [Fact]
    public async Task NonAdminUser_Forbidden403_OnFinancialReports_AllowedOnInventoryReports()
    {
        await fixture.ResetAsAsync(UserRole.User);

        // Financial reports require AdminOnly -> 403 Forbidden
        using var salesResponse = await fixture.Client.GetAsync("/api/reports/sales-summary?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.Forbidden, salesResponse.StatusCode);

        using var prodResponse = await fixture.Client.GetAsync("/api/reports/product-sales?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.Forbidden, prodResponse.StatusCode);

        // Operational inventory reports are accessible to authenticated regular User -> 200 OK
        using var invStatusResponse = await fixture.Client.GetAsync("/api/reports/inventory-status");
        Assert.Equal(HttpStatusCode.OK, invStatusResponse.StatusCode);

        using var movResponse = await fixture.Client.GetAsync("/api/reports/inventory-movements?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, movResponse.StatusCode);
    }

    [Fact]
    public async Task Endpoints_WithMissingOrInvalidDates_ReturnBadRequest400ProblemDetails()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        // Missing FromUtc
        using var missingFrom = await fixture.Client.GetAsync("/api/reports/sales-summary?toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, missingFrom.StatusCode);
        var problem1 = await missingFrom.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem1);
        Assert.Contains("FromUtc and ToUtc are required", problem1.Detail);

        // Missing ToUtc
        using var missingTo = await fixture.Client.GetAsync("/api/reports/product-sales?fromUtc=2026-10-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, missingTo.StatusCode);
        var problem2 = await missingTo.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem2);
        Assert.Contains("FromUtc and ToUtc are required", problem2.Detail);

        // FromUtc > ToUtc
        using var invalidRange = await fixture.Client.GetAsync("/api/reports/sales-summary?fromUtc=2026-10-20T00:00:00Z&toUtc=2026-10-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
        var problem3 = await invalidRange.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem3);
        Assert.Contains("FromUtc must be less than or equal to ToUtc", problem3.Detail);

        // Movement report with FromUtc > ToUtc
        using var movInvalid = await fixture.Client.GetAsync("/api/reports/inventory-movements?fromUtc=2026-10-20T00:00:00Z&toUtc=2026-10-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, movInvalid.StatusCode);
    }

    [Fact]
    public async Task GetSalesSummary_ReturnsAccurateMetrics_ExcludingDraftAndCancelledFromRevenue()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 10, 14, 0, 0, TimeSpan.Zero);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var customer = new Customer(customerId, "Mega Store", "TAX-MEGA-01");
            await dbContext.Customers.AddAsync(customer);

            var category = new Category("Appliances");
            await dbContext.Categories.AddAsync(category);

            var prod = new Product(new ProductIdentifier("APP-01"), "Blender", category);
            await dbContext.Products.AddAsync(prod);

            // Confirmed sale: Subtotal 80, Tax 12.80, Total 92.80
            var confirmed = new Sale(Guid.NewGuid(), "Mega Store", userId, baseDate, customerId);
            confirmed.AddDetail(prod.Identifier, 2, 40m, 0.16m);
            confirmed.Confirm(baseDate.AddMinutes(2));
            await dbContext.Sales.AddAsync(confirmed);

            // Draft sale: Subtotal 100, Tax 16, Total 116 (must not contribute to revenue)
            var draft = new Sale(Guid.NewGuid(), "Mega Store", userId, baseDate.AddHours(1), customerId);
            draft.AddDetail(prod.Identifier, 1, 100m, 0.16m);
            await dbContext.Sales.AddAsync(draft);

            // Cancelled sale: Subtotal 40, Tax 6.40, Total 46.40 (must not contribute to revenue)
            var cancelled = new Sale(Guid.NewGuid(), "Mega Store", userId, baseDate.AddHours(2), customerId);
            cancelled.AddDetail(prod.Identifier, 1, 40m, 0.16m);
            cancelled.Cancel();
            await dbContext.Sales.AddAsync(cancelled);

            await dbContext.SaveChangesAsync();
        }

        var url = $"/api/reports/sales-summary?fromUtc={Uri.EscapeDataString(baseDate.AddHours(-1).ToString("O"))}&toUtc={Uri.EscapeDataString(baseDate.AddHours(5).ToString("O"))}";
        using var response = await fixture.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<SalesSummaryReportResponse>(JsonOptions);

        Assert.NotNull(report);
        Assert.Equal(3, report.TotalTransactions);
        Assert.Equal(1, report.ConfirmedTransactions);
        Assert.Equal(1, report.DraftTransactions);
        Assert.Equal(1, report.CancelledTransactions);

        // BR-REP-004: Revenue totals include strictly Confirmed sales
        Assert.Equal(80m, report.TotalSubtotal);
        Assert.Equal(12.80m, report.TotalTax);
        Assert.Equal(92.80m, report.TotalRevenue);

        Assert.Equal(3, report.Sales.Count);
        Assert.Contains(report.Sales, s => s.Status == "Confirmed" && s.Total == 92.80m);
        Assert.Contains(report.Sales, s => s.Status == "Draft" && s.Total == 116m);
        Assert.Contains(report.Sales, s => s.Status == "Cancelled" && s.Total == 46.40m);
    }

    [Fact]
    public async Task GetSalesSummary_WithOptionalFilters_ReturnsMatchingSubset()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var custA = new Customer(customerId, "Target Customer");
            var custB = new Customer(otherCustomerId, "Other Customer");
            await dbContext.Customers.AddRangeAsync(custA, custB);

            var category = new Category("Audio");
            await dbContext.Categories.AddAsync(category);

            var prod = new Product(new ProductIdentifier("AUD-01"), "Headphones", category);
            await dbContext.Products.AddAsync(prod);

            var sale1 = new Sale(Guid.NewGuid(), "Target Customer", userId, baseDate, customerId);
            sale1.AddDetail(prod.Identifier, 1, 50m, 0m);
            sale1.Confirm(baseDate.AddMinutes(1));
            await dbContext.Sales.AddAsync(sale1);

            var sale2 = new Sale(Guid.NewGuid(), "Other Customer", userId, baseDate.AddMinutes(5), otherCustomerId);
            sale2.AddDetail(prod.Identifier, 2, 50m, 0m);
            sale2.Confirm(baseDate.AddMinutes(6));
            await dbContext.Sales.AddAsync(sale2);

            await dbContext.SaveChangesAsync();
        }

        var url = $"/api/reports/sales-summary?fromUtc={Uri.EscapeDataString(baseDate.AddHours(-1).ToString("O"))}&toUtc={Uri.EscapeDataString(baseDate.AddHours(2).ToString("O"))}&customerId={customerId}&status=Confirmed";
        using var response = await fixture.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<SalesSummaryReportResponse>(JsonOptions);

        Assert.NotNull(report);
        Assert.Equal(1, report.TotalTransactions);
        Assert.Equal(50m, report.TotalRevenue);
        Assert.Single(report.Sales);
        Assert.Equal(customerId, report.Sales[0].CustomerId);
    }

    [Fact]
    public async Task GetProductSales_CalculatesVolumeAndAmounts_FromConfirmedSaleDetailsSnapshots()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var category = new Category("Sports");
            await dbContext.Categories.AddAsync(category);

            var p1 = new Product(new ProductIdentifier("BALL-01"), "Soccer Ball", category);
            var p2 = new Product(new ProductIdentifier("CONE-01"), "Training Cone", category);
            await dbContext.Products.AddRangeAsync(p1, p2);

            var sale = new Sale(Guid.NewGuid(), "Club FC", userId, baseDate);
            sale.AddDetail(p1.Identifier, 3, 30m, 0.10m); // Subtotal: 90, Tax: 9, Total: 99
            sale.AddDetail(p2.Identifier, 10, 5m, 0.10m); // Subtotal: 50, Tax: 5, Total: 55
            sale.Confirm(baseDate.AddMinutes(5));
            await dbContext.Sales.AddAsync(sale);

            await dbContext.SaveChangesAsync();
        }

        var url = $"/api/reports/product-sales?fromUtc={Uri.EscapeDataString(baseDate.AddHours(-1).ToString("O"))}&toUtc={Uri.EscapeDataString(baseDate.AddHours(2).ToString("O"))}";
        using var response = await fixture.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<ProductSalesReportResponse>(JsonOptions);

        Assert.NotNull(report);
        Assert.Equal(2, report.TotalProductsSold);
        Assert.Equal(13, report.TotalUnitsSold);
        Assert.Equal(140m, report.TotalSubtotal);
        Assert.Equal(14m, report.TotalTax);
        Assert.Equal(154m, report.TotalRevenue);

        Assert.Equal(2, report.Items.Count);
        // Ranked by Total descending: Ball (99.00) first, Cone (55.00) second
        Assert.Equal("BALL-01", report.Items[0].ProductIdentifier);
        Assert.Equal(3, report.Items[0].UnitsSold);
        Assert.Equal(99m, report.Items[0].Total);

        Assert.Equal("CONE-01", report.Items[1].ProductIdentifier);
        Assert.Equal(10, report.Items[1].UnitsSold);
        Assert.Equal(55m, report.Items[1].Total);
    }

    [Fact]
    public async Task GetInventoryStatus_ReportsInStock_ZeroStock_AndUninitializedCatalogProducts()
    {
        await fixture.ResetAsAsync(UserRole.User);

        Guid catId;
        await using (var dbContext = fixture.CreateDbContext())
        {
            var category = new Category("Office Supplies");
            catId = category.Id;
            await dbContext.Categories.AddAsync(category);

            // In-stock product
            var p1 = new Product(new ProductIdentifier("PAPER-01"), "Printer Paper", category);
            var inv1 = new Inventory(p1.Identifier, 25, "Shipment A");

            // Out-of-stock product with inventory record (qty = 0)
            var p2 = new Product(new ProductIdentifier("STAPLER-01"), "Heavy Stapler", category);
            var inv2 = new Inventory(p2.Identifier);

            // Out-of-stock registered product without inventory record
            var p3 = new Product(new ProductIdentifier("CLIPS-01"), "Paper Clips", category);

            await dbContext.Products.AddRangeAsync(p1, p2, p3);
            await dbContext.Inventories.AddRangeAsync(inv1, inv2);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync("/api/reports/inventory-status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<InventoryStatusReportResponse>(JsonOptions);
        Assert.NotNull(report);

        Assert.Equal(3, report.TotalProducts);
        Assert.Equal(25, report.TotalUnitsOnHand);
        Assert.Equal(1, report.InStockProductsCount);
        Assert.Equal(2, report.OutOfStockProductsCount);

        var paper = report.Items.First(i => i.ProductIdentifier == "PAPER-01");
        Assert.Equal(25, paper.Quantity);
        Assert.True(paper.HasInventoryRecord);
        Assert.Equal("InStock", paper.StockStatus);

        var stapler = report.Items.First(i => i.ProductIdentifier == "STAPLER-01");
        Assert.Equal(0, stapler.Quantity);
        Assert.True(stapler.HasInventoryRecord);
        Assert.Equal("OutOfStock", stapler.StockStatus);

        // BR-REP-006: Uninitialized product reports 0 stock and HasInventoryRecord = false
        var clips = report.Items.First(i => i.ProductIdentifier == "CLIPS-01");
        Assert.Equal(0, clips.Quantity);
        Assert.False(clips.HasInventoryRecord);
        Assert.Equal("OutOfStock", clips.StockStatus);
    }

    [Fact]
    public async Task GetInventoryMovements_PreservesSnapshots_Attribution_AndCalculatesNetDeltas()
    {
        await fixture.ResetAsAsync(UserRole.User);

        var supplierId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2026, 10, 16, 8, 0, 0, TimeSpan.Zero);
        var productIdentifier = new ProductIdentifier("WOOD-PLANK");

        Guid entryMovementId;
        await using (var dbContext = fixture.CreateDbContext())
        {
            var supplier = new Supplier(supplierId, "Forestry Suppliers Inc", "RFC-FOREST-01");
            await dbContext.Suppliers.AddAsync(supplier);

            var category = new Category("Lumber");
            await dbContext.Categories.AddAsync(category);

            var product = new Product(productIdentifier, "Wood Plank", category);
            await dbContext.Products.AddAsync(product);

            var inventory = new Inventory(productIdentifier);
            inventory.RegisterEntry(50, "Truck Delivery", baseDate, supplierId, "Forestry Suppliers Inc");
            entryMovementId = inventory.Movements.Last().Id;

            inventory.RegisterExit(10, "Cut for project", baseDate.AddHours(1));
            inventory.RegisterAdjustment(5, "Inventory recount surplus", baseDate.AddHours(2));

            await dbContext.Inventories.AddAsync(inventory);

            // Audit event linking entry to user
            var auditEvent = AuditEvent.CreateForUser(
                userId,
                AuditEvent.EventTypes.InventoryMovementRecorded,
                AuditEvent.EntityNames.InventoryMovement,
                entryMovementId.ToString(),
                baseDate);
            await dbContext.AuditEvents.AddAsync(auditEvent);

            await dbContext.SaveChangesAsync();
        }

        var url = $"/api/reports/inventory-movements?fromUtc={Uri.EscapeDataString(baseDate.AddHours(-1).ToString("O"))}&toUtc={Uri.EscapeDataString(baseDate.AddHours(4).ToString("O"))}";
        using var response = await fixture.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<InventoryMovementReportResponse>(JsonOptions);

        Assert.NotNull(report);
        Assert.Equal(3, report.TotalMovements);
        Assert.Equal(1, report.EntryCount);
        Assert.Equal(1, report.ExitCount);
        Assert.Equal(1, report.AdjustmentCount);
        Assert.Equal(50, report.TotalUnitsIn);
        Assert.Equal(10, report.TotalUnitsOut);
        // Net delta: (50 - 10) + 5 = 45
        Assert.Equal(45, report.NetQuantityDelta);

        var entryItem = report.Movements.First(m => m.Id == entryMovementId);
        Assert.Equal("WOOD-PLANK", entryItem.ProductIdentifier);
        Assert.Equal("Entry", entryItem.MovementType);
        Assert.Equal(50, entryItem.Quantity);
        Assert.Equal(supplierId, entryItem.SupplierId);
        Assert.Equal("Forestry Suppliers Inc", entryItem.SupplierName);
        Assert.Equal(userId, entryItem.UserId);
    }

    [Fact]
    public async Task ReportQueries_AreStrictlyReadOnly_AndDoNotGenerateAuditEvents()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var baseDate = DateTimeOffset.UtcNow.AddDays(-1);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var category = new Category("Security");
            await dbContext.Categories.AddAsync(category);

            var prod = new Product(new ProductIdentifier("PADLOCK-01"), "Padlock", category);
            await dbContext.Products.AddAsync(prod);

            var inv = new Inventory(prod.Identifier, 10, "Stock delivery", baseDate);
            await dbContext.Inventories.AddAsync(inv);

            var sale = new Sale(Guid.NewGuid(), "Customer", Guid.NewGuid(), baseDate);
            sale.AddDetail(prod.Identifier, 2, 10m, 0m);
            sale.Confirm(baseDate.AddMinutes(1));
            await dbContext.Sales.AddAsync(sale);

            await dbContext.SaveChangesAsync();
        }

        int initialAuditEventsCount;
        int initialSalesCount;
        int initialMovementsCount;

        await using (var countContext = fixture.CreateDbContext())
        {
            initialAuditEventsCount = await countContext.AuditEvents.CountAsync();
            initialSalesCount = await countContext.Sales.CountAsync();
            initialMovementsCount = await countContext.InventoryMovements.CountAsync();
        }

        // Call all four endpoints
        var fromStr = Uri.EscapeDataString(baseDate.AddHours(-1).ToString("O"));
        var toStr = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"));

        using var r1 = await fixture.Client.GetAsync($"/api/reports/sales-summary?fromUtc={fromStr}&toUtc={toStr}");
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        using var r2 = await fixture.Client.GetAsync($"/api/reports/product-sales?fromUtc={fromStr}&toUtc={toStr}");
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        using var r3 = await fixture.Client.GetAsync("/api/reports/inventory-status");
        Assert.Equal(HttpStatusCode.OK, r3.StatusCode);

        using var r4 = await fixture.Client.GetAsync($"/api/reports/inventory-movements?fromUtc={fromStr}&toUtc={toStr}");
        Assert.Equal(HttpStatusCode.OK, r4.StatusCode);

        // Verify that database state was not modified and zero audit events were generated (BR-AUD-004, BR-REP-007)
        await using (var verifyContext = fixture.CreateDbContext())
        {
            var finalAuditEventsCount = await verifyContext.AuditEvents.CountAsync();
            var finalSalesCount = await verifyContext.Sales.CountAsync();
            var finalMovementsCount = await verifyContext.InventoryMovements.CountAsync();

            Assert.Equal(initialAuditEventsCount, finalAuditEventsCount);
            Assert.Equal(initialSalesCount, finalSalesCount);
            Assert.Equal(initialMovementsCount, finalMovementsCount);
        }
    }
}
