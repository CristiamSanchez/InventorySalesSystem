using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class InventoryConcurrencyPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task InitialStock10_TwoConcurrentWithdrawalsOf7_AtMostOneSucceedsAndStockNeverNegative()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("CONCUR-WITHDRAW-1");

        // Seed product in inventory with 10 units
        await using (var seedContext = database.CreateDbContext())
        {
            var repo = new InventoryRepository(seedContext);
            var inventory = new Inventory(productIdentifier, initialStock: 10, initialStockReference: "Initial stock 10");
            await repo.AddAsync(inventory);
        }

        // Coordinate two concurrent withdrawal requests of 7 units each
        var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var repo = new InventoryRepository(context);
            var useCases = new InventoryUseCases(repo);

            barrier.SignalAndWait();
            return await useCases.RecordExitAsync(productIdentifier, 7, "Withdrawal Task 1");
        });

        var task2 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var repo = new InventoryRepository(context);
            var useCases = new InventoryUseCases(repo);

            barrier.SignalAndWait();
            return await useCases.RecordExitAsync(productIdentifier, 7, "Withdrawal Task 2");
        });

        var results = await Task.WhenAll(task1, task2);
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        // At most one can succeed
        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);

        var failedResult = results.Single(r => !r.IsSuccess);
        Assert.True(
            failedResult.Error!.Code == ApplicationErrorCode.ConcurrencyConflict ||
            failedResult.Error!.Code == ApplicationErrorCode.InsufficientStock);

        // Verify final state in database
        await using var verifyContext = database.CreateDbContext();
        var finalInventory = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(productIdentifier);

        Assert.NotNull(finalInventory);
        Assert.Equal(3, finalInventory.Quantity);
        Assert.True(finalInventory.Quantity >= 0, "Inventory stock must never become negative.");

        // Verify movements agree with final persisted stock
        var movements = finalInventory.Movements.OrderBy(m => m.OccurredAtUtc).ToList();
        Assert.Equal(2, movements.Count); // Initial 10 + single exit of 7
        Assert.Equal(MovementType.Entry, movements[0].MovementType);
        Assert.Equal(10, movements[0].Quantity);
        Assert.Equal(MovementType.Exit, movements[1].MovementType);
        Assert.Equal(7, movements[1].Quantity);

        var netMovementStock = movements.Sum(m => m.MovementType switch
        {
            MovementType.Entry => m.Quantity,
            MovementType.Exit => -m.Quantity,
            MovementType.Adjustment => m.Quantity,
            _ => throw new InvalidOperationException()
        });

        Assert.Equal(finalInventory.Quantity, netMovementStock);
    }

    [Fact]
    public async Task ConcurrentValidUpdates_DoNotSilentlyLoseSuccessfulChanges_AndMovementsAgreeWithStock()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("CONCUR-ENTRIES-1");

        // Seed product in inventory with 100 units
        await using (var seedContext = database.CreateDbContext())
        {
            var repo = new InventoryRepository(seedContext);
            var inventory = new Inventory(productIdentifier, initialStock: 100, initialStockReference: "Base stock 100");
            await repo.AddAsync(inventory);
        }

        var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var repo = new InventoryRepository(context);
            var useCases = new InventoryUseCases(repo);

            barrier.SignalAndWait();
            return await useCases.RecordEntryAsync(productIdentifier, 15, "Entry Task 1");
        });

        var task2 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var repo = new InventoryRepository(context);
            var useCases = new InventoryUseCases(repo);

            barrier.SignalAndWait();
            return await useCases.RecordEntryAsync(productIdentifier, 25, "Entry Task 2");
        });

        var results = await Task.WhenAll(task1, task2);

        await using var verifyContext = database.CreateDbContext();
        var finalInventory = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(productIdentifier);
        Assert.NotNull(finalInventory);

        // Neither update should have been silently lost:
        // Either one succeeded and the other was reported as ConcurrencyConflict,
        // or both succeeded if committed sequentially.
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        var failedResults = results.Where(r => !r.IsSuccess).ToList();

        if (successfulResults.Count == 1)
        {
            // One succeeded, one got concurrency conflict
            var failed = failedResults.Single();
            Assert.Equal(ApplicationErrorCode.ConcurrencyConflict, failed.Error!.Code);

            // Persisted stock must reflect the single successful update, NOT an overwrite
            var winner = successfulResults.Single();
            Assert.Equal(winner.Value.Quantity, finalInventory.Quantity);

            // Exactly 2 movements (initial + winning entry)
            Assert.Equal(2, finalInventory.Movements.Count);
        }
        else
        {
            // Both succeeded (interleaved/serialized)
            Assert.Equal(140, finalInventory.Quantity);
            Assert.Equal(3, finalInventory.Movements.Count);
        }

        // In all cases, movements strictly agree with current stock
        var netMovementStock = finalInventory.Movements.Sum(m => m.MovementType switch
        {
            MovementType.Entry => m.Quantity,
            MovementType.Exit => -m.Quantity,
            MovementType.Adjustment => m.Quantity,
            _ => throw new InvalidOperationException()
        });

        Assert.Equal(finalInventory.Quantity, netMovementStock);
    }

    [Fact]
    public async Task ConcurrentInitialization_ForSameProduct_DoesNotCreateDuplicateInventoryRecords()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("CONCUR-NEW-PROD");

        var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var repo = new InventoryRepository(context);
            var useCases = new InventoryUseCases(repo);

            barrier.SignalAndWait();
            return await useCases.RecordEntryAsync(productIdentifier, 20, "Init Entry 1");
        });

        var task2 = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            var repo = new InventoryRepository(context);
            var useCases = new InventoryUseCases(repo);

            barrier.SignalAndWait();
            return await useCases.RecordEntryAsync(productIdentifier, 30, "Init Entry 2");
        });

        var results = await Task.WhenAll(task1, task2);
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        // Verify only ONE record in the database
        await using var verifyContext = database.CreateDbContext();
        var count = await verifyContext.Inventories
            .CountAsync(i => i.ProductIdentifier == productIdentifier);

        Assert.Equal(1, count);

        var inventory = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(productIdentifier);
        Assert.NotNull(inventory);

        if (successCount == 1)
        {
            Assert.Equal(1, failureCount);
            var failed = results.Single(r => !r.IsSuccess);
            Assert.Equal(ApplicationErrorCode.ConcurrencyConflict, failed.Error!.Code);
            var winner = results.Single(r => r.IsSuccess).Value;
            Assert.Equal(winner.Quantity, inventory.Quantity);
            Assert.Single(inventory.Movements);
        }
        else
        {
            // If the runner serialized execution, both operations succeeded without duplicating records
            Assert.Equal(2, successCount);
            Assert.Equal(50, inventory.Quantity);
            Assert.Equal(2, inventory.Movements.Count);
        }
    }

    [Fact]
    public async Task DatabaseCheckConstraint_RejectsNegativeQuantityAtDatabaseLevel()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("CHECK-CONSTRAINT-PROD");

        await using (var seedContext = database.CreateDbContext())
        {
            var repo = new InventoryRepository(seedContext);
            var inventory = new Inventory(productIdentifier, initialStock: 10, initialStockReference: "Seed");
            await repo.AddAsync(inventory);
        }

        await using (var directSqlContext = database.CreateDbContext())
        {
            // Attempt to bypass domain logic and set Quantity = -5 via SQL
            var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                directSqlContext.Database.ExecuteSqlRawAsync(
                    "UPDATE inventories SET \"Quantity\" = -5 WHERE \"ProductIdentifier\" = 'CHECK-CONSTRAINT-PROD'"));

            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Contains("ck_inventories_quantity_non_negative", exception.ConstraintName);
        }
    }

    [Fact]
    public async Task SequentialOperations_MaintainInvariantsAndTraceability()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("SEQUENTIAL-PROD");

        await using var context = database.CreateDbContext();
        var repo = new InventoryRepository(context);
        var useCases = new InventoryUseCases(repo);

        // 1. Initial entry
        var entryResult = await useCases.RecordEntryAsync(productIdentifier, 50, "Initial batch");
        Assert.True(entryResult.IsSuccess);
        Assert.Equal(50, entryResult.Value.Quantity);

        // 2. Additional entry
        var entry2Result = await useCases.RecordEntryAsync(productIdentifier, 20, "Second batch");
        Assert.True(entry2Result.IsSuccess);
        Assert.Equal(70, entry2Result.Value.Quantity);

        // 3. Valid exit
        var exitResult = await useCases.RecordExitAsync(productIdentifier, 30, "Sale dispatch");
        Assert.True(exitResult.IsSuccess);
        Assert.Equal(40, exitResult.Value.Quantity);

        // 4. Valid adjustment
        var adjustResult = await useCases.AdjustAsync(productIdentifier, -5, "Damaged stock");
        Assert.True(adjustResult.IsSuccess);
        Assert.Equal(35, adjustResult.Value.Quantity);

        // 5. Invalid exit (exceeds stock 35)
        var invalidExit = await useCases.RecordExitAsync(productIdentifier, 40, "Excessive exit");
        Assert.False(invalidExit.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, invalidExit.Error!.Code);

        // 6. Invalid adjustment (would drop stock below zero)
        var invalidAdjust = await useCases.AdjustAsync(productIdentifier, -40, "Excessive negative adjustment");
        Assert.False(invalidAdjust.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, invalidAdjust.Error!.Code);

        // Verify final persisted state
        await using var verifyContext = database.CreateDbContext();
        var finalInventory = await new InventoryRepository(verifyContext).GetByProductIdentifierAsync(productIdentifier);
        Assert.NotNull(finalInventory);
        Assert.Equal(35, finalInventory.Quantity);
        Assert.Equal(4, finalInventory.Movements.Count); // 2 entries, 1 exit, 1 adjustment (none for the 2 failed attempts)

        var netStock = finalInventory.Movements.Sum(m => m.MovementType switch
        {
            MovementType.Entry => m.Quantity,
            MovementType.Exit => -m.Quantity,
            MovementType.Adjustment => m.Quantity,
            _ => throw new InvalidOperationException()
        });

        Assert.Equal(finalInventory.Quantity, netStock);
    }
}
