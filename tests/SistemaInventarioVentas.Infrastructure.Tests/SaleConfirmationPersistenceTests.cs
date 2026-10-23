using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class SaleConfirmationPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task ConfirmSaleAsync_AtomicallyUpdatesSaleStatusDeductsStockAndPersistsExitMovement()
    {
        await database.ClearAsync();
        var prod = new ProductIdentifier("PROD-CONFIRM-1");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        // 1. Seed inventory with 20 units
        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            var inventory = new Inventory(prod, initialStock: 20, initialStockReference: "Initial PO-101");
            await invRepo.AddAsync(inventory);
        }

        // 2. Create draft sale with detail of 5 units
        await using (var draftContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(draftContext);
            var sale = new Sale(saleId, "Customer One", userId);
            sale.AddDetail(prod, quantity: 5, unitPrice: 20.00m, taxRate: 0.16m);
            await saleRepo.AddAsync(sale);
        }

        // 3. Confirm sale via UseCases
        var fixedConfirmedAt = new DateTimeOffset(2026, 10, 19, 15, 0, 0, TimeSpan.Zero);
        await using (var confirmContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(confirmContext);
            var invRepo = new InventoryRepository(confirmContext);
            var useCases = new SaleUseCases(saleRepo, inventories: invRepo);

            var result = await useCases.ConfirmSaleAsync(saleId, fixedConfirmedAt);

            Assert.True(result.IsSuccess);
            Assert.Equal(SaleStatus.Confirmed, result.Value.Status);
            Assert.Equal(fixedConfirmedAt, result.Value.ConfirmedAtUtc);
        }

        // 4. Verify atomic persistence in a fresh context
        await using var verifyContext = database.CreateDbContext();
        var savedSale = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
        Assert.NotNull(savedSale);
        Assert.Equal(SaleStatus.Confirmed, savedSale.Status);
        Assert.Equal(fixedConfirmedAt, savedSale.ConfirmedAtUtc);

        var savedInventory = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(prod);
        Assert.NotNull(savedInventory);
        Assert.Equal(15, savedInventory.Quantity);

        Assert.Equal(2, savedInventory.Movements.Count);
        var exitMovement = savedInventory.Movements.Single(m => m.MovementType == MovementType.Exit);
        Assert.Equal(5, exitMovement.Quantity);
        Assert.Equal($"Sale {saleId}", exitMovement.Reference);
        Assert.Equal(fixedConfirmedAt, exitMovement.OccurredAtUtc);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WithMultipleProducts_AtomicallyDeductsStockAndPersistsExitMovementsForEveryProduct()
    {
        await database.ClearAsync();
        var prod1 = new ProductIdentifier("PROD-MULTI-1");
        var prod2 = new ProductIdentifier("PROD-MULTI-2");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        // 1. Seed two inventories
        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            await invRepo.AddAsync(new Inventory(prod1, initialStock: 10, initialStockReference: "Seed 1"));
            await invRepo.AddAsync(new Inventory(prod2, initialStock: 15, initialStockReference: "Seed 2"));
        }

        // 2. Create draft sale with two lines
        await using (var draftContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(draftContext);
            var sale = new Sale(saleId, "Customer Two", userId);
            sale.AddDetail(prod1, quantity: 4, unitPrice: 10.00m, taxRate: 0m);
            sale.AddDetail(prod2, quantity: 6, unitPrice: 25.00m, taxRate: 0.16m);
            await saleRepo.AddAsync(sale);
        }

        // 3. Confirm sale
        await using (var confirmContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(confirmContext);
            var invRepo = new InventoryRepository(confirmContext);
            var useCases = new SaleUseCases(saleRepo, inventories: invRepo);

            var result = await useCases.ConfirmSaleAsync(saleId);
            Assert.True(result.IsSuccess);
        }

        // 4. Verify in fresh context
        await using var verifyContext = database.CreateDbContext();
        var invRepoQuery = new InventoryRepository(verifyContext);

        var inv1 = await invRepoQuery.GetByProductIdentifierAsync(prod1);
        Assert.NotNull(inv1);
        Assert.Equal(6, inv1.Quantity);
        var exit1 = inv1.Movements.Single(m => m.MovementType == MovementType.Exit);
        Assert.Equal(4, exit1.Quantity);
        Assert.Equal($"Sale {saleId}", exit1.Reference);

        var inv2 = await invRepoQuery.GetByProductIdentifierAsync(prod2);
        Assert.NotNull(inv2);
        Assert.Equal(9, inv2.Quantity);
        var exit2 = inv2.Movements.Single(m => m.MovementType == MovementType.Exit);
        Assert.Equal(6, exit2.Quantity);
        Assert.Equal($"Sale {saleId}", exit2.Reference);

        var savedSale = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
        Assert.NotNull(savedSale);
        Assert.Equal(SaleStatus.Confirmed, savedSale.Status);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenOneProductHasInsufficientStock_RollsBackEntireOperation()
    {
        await database.ClearAsync();
        var prod1 = new ProductIdentifier("PROD-FAIL-1");
        var prod2 = new ProductIdentifier("PROD-FAIL-2");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        // 1. Seed prod1 with enough stock (10), prod2 with insufficient stock (2)
        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            await invRepo.AddAsync(new Inventory(prod1, initialStock: 10, initialStockReference: "Seed 1"));
            await invRepo.AddAsync(new Inventory(prod2, initialStock: 2, initialStockReference: "Seed 2"));
        }

        // 2. Create draft sale requiring 5 of prod1 and 5 of prod2
        await using (var draftContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(draftContext);
            var sale = new Sale(saleId, "Customer Three", userId);
            sale.AddDetail(prod1, quantity: 5, unitPrice: 10.00m, taxRate: 0m);
            sale.AddDetail(prod2, quantity: 5, unitPrice: 20.00m, taxRate: 0m);
            await saleRepo.AddAsync(sale);
        }

        // 3. Attempt confirmation
        await using (var confirmContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(confirmContext);
            var invRepo = new InventoryRepository(confirmContext);
            var useCases = new SaleUseCases(saleRepo, inventories: invRepo);

            var result = await useCases.ConfirmSaleAsync(saleId);

            Assert.False(result.IsSuccess);
            Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
            Assert.Contains("Insufficient stock for product 'PROD-FAIL-2'", result.Error!.Message);
        }

        // 4. Verify rollback: prod1 stock NOT deducted, prod2 stock untouched, sale remains Draft
        await using var verifyContext = database.CreateDbContext();
        var invRepoQuery = new InventoryRepository(verifyContext);

        var inv1 = await invRepoQuery.GetByProductIdentifierAsync(prod1);
        Assert.NotNull(inv1);
        Assert.Equal(10, inv1.Quantity);
        Assert.DoesNotContain(inv1.Movements, m => m.MovementType == MovementType.Exit);

        var inv2 = await invRepoQuery.GetByProductIdentifierAsync(prod2);
        Assert.NotNull(inv2);
        Assert.Equal(2, inv2.Quantity);
        Assert.DoesNotContain(inv2.Movements, m => m.MovementType == MovementType.Exit);

        var savedSale = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
        Assert.NotNull(savedSale);
        Assert.Equal(SaleStatus.Draft, savedSale.Status);
        Assert.Null(savedSale.ConfirmedAtUtc);
    }

    [Fact]
    public async Task ConfirmSaleAsync_TwoConcurrentConfirmationsForLimitedStock_AtMostOneSucceedsAndStockNeverNegative()
    {
        await database.ClearAsync();
        var prod = new ProductIdentifier("PROD-CONCUR-STOCK");
        var userId = Guid.NewGuid();
        var saleId1 = Guid.NewGuid();
        var saleId2 = Guid.NewGuid();

        // 1. Seed product with 10 units
        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            await invRepo.AddAsync(new Inventory(prod, initialStock: 10, initialStockReference: "Initial stock 10"));
        }

        // 2. Create two distinct Draft sales, each requesting 7 units
        await using (var draftContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(draftContext);

            var sale1 = new Sale(saleId1, "Concurrent Cust 1", userId);
            sale1.AddDetail(prod, quantity: 7, unitPrice: 15.00m, taxRate: 0m);
            await saleRepo.AddAsync(sale1);

            var sale2 = new Sale(saleId2, "Concurrent Cust 2", userId);
            sale2.AddDetail(prod, quantity: 7, unitPrice: 15.00m, taxRate: 0m);
            await saleRepo.AddAsync(sale2);
        }

        // 3. Coordinate two concurrent confirmation attempts
        var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var saleRepo = new SaleRepository(context);
            var invRepo = new InventoryRepository(context);
            var useCases = new SaleUseCases(saleRepo, inventories: invRepo);

            barrier.SignalAndWait();
            return await useCases.ConfirmSaleAsync(saleId1);
        });

        var task2 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var saleRepo = new SaleRepository(context);
            var invRepo = new InventoryRepository(context);
            var useCases = new SaleUseCases(saleRepo, inventories: invRepo);

            barrier.SignalAndWait();
            return await useCases.ConfirmSaleAsync(saleId2);
        });

        var results = await Task.WhenAll(task1, task2);
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);

        var failedResult = results.Single(r => !r.IsSuccess);
        Assert.True(
            failedResult.Error!.Code == ApplicationErrorCode.ConcurrencyConflict ||
            failedResult.Error.Code == ApplicationErrorCode.InsufficientStock);

        // 4. Verify database consistency
        await using var verifyContext = database.CreateDbContext();
        var finalInv = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(prod);
        Assert.NotNull(finalInv);
        Assert.Equal(3, finalInv.Quantity); // 10 - 7 = 3, never negative!

        var finalSale1 = await new SaleRepository(verifyContext).GetByIdAsync(saleId1);
        var finalSale2 = await new SaleRepository(verifyContext).GetByIdAsync(saleId2);
        Assert.NotNull(finalSale1);
        Assert.NotNull(finalSale2);

        var statuses = new[] { finalSale1.Status, finalSale2.Status };
        Assert.Contains(SaleStatus.Confirmed, statuses);
        Assert.Contains(SaleStatus.Draft, statuses);

        var exitMovements = finalInv.Movements.Where(m => m.MovementType == MovementType.Exit).ToList();
        Assert.Single(exitMovements);
        Assert.Equal(7, exitMovements[0].Quantity);
    }

    [Fact]
    public async Task ConfirmSaleAsync_CalledTwiceSequentially_SecondCallFailsAndDoesNotDeductStockTwice()
    {
        await database.ClearAsync();
        var prod = new ProductIdentifier("PROD-REPEAT-1");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            await invRepo.AddAsync(new Inventory(prod, initialStock: 10, initialStockReference: "Seed 10"));

            var saleRepo = new SaleRepository(seedContext);
            var sale = new Sale(saleId, "Customer Repeat", userId);
            sale.AddDetail(prod, quantity: 4, unitPrice: 10.00m, taxRate: 0m);
            await saleRepo.AddAsync(sale);
        }

        // First confirmation succeeds
        await using (var firstContext = database.CreateDbContext())
        {
            var useCases = new SaleUseCases(
                new SaleRepository(firstContext),
                inventories: new InventoryRepository(firstContext));

            var result = await useCases.ConfirmSaleAsync(saleId);
            Assert.True(result.IsSuccess);
            Assert.Equal(SaleStatus.Confirmed, result.Value.Status);
        }

        // Second confirmation fails immediately
        await using (var secondContext = database.CreateDbContext())
        {
            var useCases = new SaleUseCases(
                new SaleRepository(secondContext),
                inventories: new InventoryRepository(secondContext));

            var result = await useCases.ConfirmSaleAsync(saleId);
            Assert.False(result.IsSuccess);
            Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
            Assert.Contains("cannot be confirmed", result.Error!.Message);
        }

        // Verify stock deducted only once (10 - 4 = 6)
        await using var verifyContext = database.CreateDbContext();
        var inv = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(prod);
        Assert.NotNull(inv);
        Assert.Equal(6, inv.Quantity);
        Assert.Single(inv.Movements, m => m.MovementType == MovementType.Exit);
    }

    [Fact]
    public async Task ConfirmSaleAsync_TwoConcurrentCallsOnSameSale_AtMostOneSucceedsAndStockOnlyDeductedOnce()
    {
        await database.ClearAsync();
        var prod = new ProductIdentifier("PROD-SAME-SALE");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            await invRepo.AddAsync(new Inventory(prod, initialStock: 10, initialStockReference: "Seed 10"));

            var saleRepo = new SaleRepository(seedContext);
            var sale = new Sale(saleId, "Customer Same Sale", userId);
            sale.AddDetail(prod, quantity: 4, unitPrice: 10.00m, taxRate: 0m);
            await saleRepo.AddAsync(sale);
        }

        var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var useCases = new SaleUseCases(
                new SaleRepository(context),
                inventories: new InventoryRepository(context));

            barrier.SignalAndWait();
            return await useCases.ConfirmSaleAsync(saleId);
        });

        var task2 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var useCases = new SaleUseCases(
                new SaleRepository(context),
                inventories: new InventoryRepository(context));

            barrier.SignalAndWait();
            return await useCases.ConfirmSaleAsync(saleId);
        });

        var results = await Task.WhenAll(task1, task2);
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);

        // Verify stock was deducted only once
        await using var verifyContext = database.CreateDbContext();
        var inv = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(prod);
        Assert.NotNull(inv);
        Assert.Equal(6, inv.Quantity);

        var savedSale = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
        Assert.NotNull(savedSale);
        Assert.Equal(SaleStatus.Confirmed, savedSale.Status);

        var exitMovements = inv.Movements.Where(m => m.MovementType == MovementType.Exit).ToList();
        Assert.Single(exitMovements);
        Assert.Equal(4, exitMovements[0].Quantity);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenInventoryMissing_ReturnsInsufficientStockAndLeavesSaleUnchanged()
    {
        await database.ClearAsync();
        var prod = new ProductIdentifier("PROD-NONEXISTENT");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        await using (var seedContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(seedContext);
            var sale = new Sale(saleId, "Customer No Inv", userId);
            sale.AddDetail(prod, quantity: 3, unitPrice: 10.00m, taxRate: 0m);
            await saleRepo.AddAsync(sale);
        }

        await using (var confirmContext = database.CreateDbContext())
        {
            var useCases = new SaleUseCases(
                new SaleRepository(confirmContext),
                inventories: new InventoryRepository(confirmContext));

            var result = await useCases.ConfirmSaleAsync(saleId);
            Assert.False(result.IsSuccess);
            Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
            Assert.Contains("Insufficient stock for product 'PROD-NONEXISTENT'", result.Error!.Message);
        }

        await using var verifyContext = database.CreateDbContext();
        var savedSale = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
        Assert.NotNull(savedSale);
        Assert.Equal(SaleStatus.Draft, savedSale.Status);
    }

    [Fact]
    public async Task ConfirmSaleAsync_AtomicallyPersistsSaleConfirmedAuditEvent()
    {
        await database.ClearAsync();
        var prod = new ProductIdentifier("PROD-AUDIT-TX");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        // 1. Seed inventory
        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            await invRepo.AddAsync(new Inventory(prod, initialStock: 25, initialStockReference: "Seed Stock"));
        }

        // 2. Create draft sale
        await using (var draftContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(draftContext);
            var sale = new Sale(saleId, "Acme Customer", userId);
            sale.AddDetail(prod, quantity: 5, unitPrice: 30.00m, taxRate: 0.16m);
            await saleRepo.AddAsync(sale);
        }

        // 3. Confirm sale
        var confirmedAt = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        await using (var confirmContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(confirmContext);
            var invRepo = new InventoryRepository(confirmContext);
            var useCases = new SaleUseCases(saleRepo, inventories: invRepo);

            var result = await useCases.ConfirmSaleAsync(saleId, confirmedAt);
            Assert.True(result.IsSuccess);
        }

        // 4. Verify atomic persistence of Sale, Inventory decrement, and AuditEvent
        await using var verifyContext = database.CreateDbContext();
        var savedSale = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
        Assert.NotNull(savedSale);
        Assert.Equal(SaleStatus.Confirmed, savedSale.Status);

        var savedInventory = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(prod);
        Assert.NotNull(savedInventory);
        Assert.Equal(20, savedInventory.Quantity);

        var auditEvent = await verifyContext.AuditEvents
            .SingleOrDefaultAsync(a => a.EntityId == saleId.ToString());
        Assert.NotNull(auditEvent);
        Assert.Equal(AuditEvent.EventTypes.SaleConfirmed, auditEvent.EventType);
        Assert.Equal(AuditEvent.EntityNames.Sale, auditEvent.EntityName);
        Assert.Equal(confirmedAt.ToUniversalTime(), auditEvent.OccurredAtUtc);
        Assert.NotNull(auditEvent.MetadataJson);

        using var doc = System.Text.Json.JsonDocument.Parse(auditEvent.MetadataJson);
        Assert.Equal("Acme Customer", doc.RootElement.GetProperty("customerReference").GetString());
        Assert.Equal("Confirmed", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(150.00m, doc.RootElement.GetProperty("subtotal").GetDecimal());
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenFailsDueToInsufficientStock_RollsBackAndLeavesNoPhantomAuditEvent()
    {
        await database.ClearAsync();
        var prod = new ProductIdentifier("PROD-FAIL-AUDIT");
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        await using (var seedContext = database.CreateDbContext())
        {
            var invRepo = new InventoryRepository(seedContext);
            await invRepo.AddAsync(new Inventory(prod, initialStock: 2, initialStockReference: "Seed Stock"));
        }

        await using (var draftContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(draftContext);
            var sale = new Sale(saleId, "Acme Customer", userId);
            sale.AddDetail(prod, quantity: 10, unitPrice: 30.00m, taxRate: 0m);
            await saleRepo.AddAsync(sale);
        }

        await using (var confirmContext = database.CreateDbContext())
        {
            var saleRepo = new SaleRepository(confirmContext);
            var invRepo = new InventoryRepository(confirmContext);
            var useCases = new SaleUseCases(saleRepo, inventories: invRepo);

            var result = await useCases.ConfirmSaleAsync(saleId);
            Assert.False(result.IsSuccess);
            Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
        }

        await using var verifyContext = database.CreateDbContext();
        var auditEvents = await verifyContext.AuditEvents
            .Where(a => a.EntityId == saleId.ToString())
            .ToListAsync();
        Assert.Empty(auditEvents);
    }
}
