using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class InventoryPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task InventoryRepository_PersistsAndLoadsInventoryWithInitialStockAndMovement()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-100");
        var initialTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(dbContext);
            var inventory = new Inventory(
                productIdentifier,
                initialStock: 50,
                initialStockReference: "Initial warehouse receipt",
                occurredAtUtc: initialTimestamp);

            await repository.AddAsync(inventory);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new InventoryRepository(queryContext)
            .GetByProductIdentifierAsync(productIdentifier);

        Assert.NotNull(loaded);
        Assert.Equal(productIdentifier.Value, loaded.ProductIdentifier.Value);
        Assert.Equal(50, loaded.Quantity);
        var movement = Assert.Single(loaded.Movements);
        Assert.Equal(productIdentifier.Value, movement.ProductIdentifier.Value);
        Assert.Equal(MovementType.Entry, movement.MovementType);
        Assert.Equal(50, movement.Quantity);
        Assert.Equal("Initial warehouse receipt", movement.Reference);
        Assert.Equal(initialTimestamp.ToUnixTimeSeconds(), movement.OccurredAtUtc.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task InventoryRepository_PersistsSubsequentMovementsAndUpdatesCurrentStock()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-200");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(dbContext);
            var inventory = new Inventory(
                productIdentifier,
                initialStock: 100,
                initialStockReference: "Opening balance");

            await repository.AddAsync(inventory);
        }

        await using (var updateContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(updateContext);
            var inventory = await repository.GetByProductIdentifierAsync(productIdentifier);
            Assert.NotNull(inventory);

            inventory.RegisterEntry(25, "PO-1002 Receipt");
            inventory.RegisterExit(30, "SO-5001 Dispatch");
            inventory.Adjust(-5, "Damaged in transit");

            await repository.SaveAsync(inventory);
        }

        await using var queryContext = database.CreateDbContext();
        var result = await new InventoryRepository(queryContext)
            .GetByProductIdentifierAsync(productIdentifier);

        Assert.NotNull(result);
        Assert.Equal(90, result.Quantity);
        Assert.Equal(4, result.Movements.Count);

        var movements = result.Movements.OrderBy(m => m.OccurredAtUtc).ToList();
        Assert.Equal(MovementType.Entry, movements[0].MovementType);
        Assert.Equal(100, movements[0].Quantity);
        Assert.Equal("Opening balance", movements[0].Reference);

        Assert.Equal(MovementType.Entry, movements[1].MovementType);
        Assert.Equal(25, movements[1].Quantity);
        Assert.Equal("PO-1002 Receipt", movements[1].Reference);

        Assert.Equal(MovementType.Exit, movements[2].MovementType);
        Assert.Equal(30, movements[2].Quantity);
        Assert.Equal("SO-5001 Dispatch", movements[2].Reference);

        Assert.Equal(MovementType.Adjustment, movements[3].MovementType);
        Assert.Equal(-5, movements[3].Quantity);
        Assert.Equal("Damaged in transit", movements[3].Reference);

        var netStock = movements.Sum(m => m.MovementType switch
        {
            MovementType.Entry => m.Quantity,
            MovementType.Exit => -m.Quantity,
            MovementType.Adjustment => m.Quantity,
            _ => throw new InvalidOperationException()
        });

        Assert.Equal(result.Quantity, netStock);
    }

    [Fact]
    public async Task InventoryRepository_HandlesDetachedInventorySave()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-300");
        Inventory detachedInventory;

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(dbContext);
            detachedInventory = new Inventory(
                productIdentifier,
                initialStock: 40,
                initialStockReference: "Initial receipt");

            await repository.AddAsync(detachedInventory);
        }

        // Apply operations on the detached entity
        detachedInventory.RegisterEntry(20, "Detached Entry");

        await using (var saveContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(saveContext);
            await repository.SaveAsync(detachedInventory);
        }

        await using var queryContext = database.CreateDbContext();
        var result = await new InventoryRepository(queryContext)
            .GetByProductIdentifierAsync(productIdentifier);

        Assert.NotNull(result);
        Assert.Equal(60, result.Quantity);
        Assert.Equal(2, result.Movements.Count);
        Assert.Contains(result.Movements, m => m.Reference == "Detached Entry" && m.Quantity == 20);
    }

    [Fact]
    public async Task ProductIdentifier_IsUniqueAtDatabaseLevel()
    {
        await database.ClearAsync();
        var identifier = new ProductIdentifier("UNIQUE-PROD");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(dbContext);
            await repository.AddAsync(new Inventory(identifier));
        }

        await using (var conflictContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(conflictContext);
            var exception = await Assert.ThrowsAsync<ConcurrencyException>(
                () => repository.AddAsync(new Inventory(identifier)));

            Assert.IsType<DbUpdateException>(exception.InnerException);
            var dbUpdate = (DbUpdateException)exception.InnerException!;
            Assert.IsType<PostgresException>(dbUpdate.InnerException);
            Assert.Equal(
                PostgresErrorCodes.UniqueViolation,
                ((PostgresException)dbUpdate.InnerException!).SqlState);
        }
    }

    [Fact]
    public async Task InventoryRepository_ReturnsNullForNonExistentProduct()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new InventoryRepository(dbContext);

        var result = await repository.GetByProductIdentifierAsync(
            new ProductIdentifier("NON-EXISTENT"));

        Assert.Null(result);
    }

    [Fact]
    public async Task InventoryRepository_RespectsCancellationToken()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new InventoryRepository(dbContext);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByProductIdentifierAsync(
                new ProductIdentifier("ANY-PROD"),
                cancellation.Token));
    }

    [Fact]
    public void PersistenceRegistration_ResolvesInventoryRepository()
    {
        using var dbContext = database.CreateDbContext();
        var services = new ServiceCollection();
        services.AddPersistence(dbContext.Database.GetConnectionString()!);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<InventoryRepository>(provider.GetRequiredService<IInventoryRepository>());
    }

    [Fact]
    public async Task InventoryRepository_PersistsAndLoadsEntryMovementWithSupplierDetails()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-SUP-1");
        var supplier = new Supplier("Acme Industrial");

        await using (var dbContext = database.CreateDbContext())
        {
            dbContext.Suppliers.Add(supplier);
            await dbContext.SaveChangesAsync();

            var repository = new InventoryRepository(dbContext);
            var inventory = new Inventory(productIdentifier);
            inventory.RegisterEntry(
                quantity: 25,
                reference: "PO-SUP-101",
                occurredAtUtc: DateTimeOffset.UtcNow,
                supplierId: supplier.Id,
                supplierName: supplier.Name);

            await repository.AddAsync(inventory);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new InventoryRepository(queryContext)
            .GetByProductIdentifierAsync(productIdentifier);

        Assert.NotNull(loaded);
        Assert.Equal(25, loaded.Quantity);
        var movement = Assert.Single(loaded.Movements);
        Assert.Equal(supplier.Id, movement.SupplierId);
        Assert.Equal("Acme Industrial", movement.SupplierName);
        Assert.Equal("PO-SUP-101", movement.Reference);
        Assert.Equal(MovementType.Entry, movement.MovementType);
    }

    [Fact]
    public async Task InventoryRepository_RestrictsDeletionOfSupplierWithAssociatedMovements()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-SUP-2");
        var supplier = new Supplier("Protected Supplier");

        await using (var dbContext = database.CreateDbContext())
        {
            dbContext.Suppliers.Add(supplier);
            await dbContext.SaveChangesAsync();

            var repository = new InventoryRepository(dbContext);
            var inventory = new Inventory(productIdentifier);
            inventory.RegisterEntry(10, "Receipt", supplierId: supplier.Id, supplierName: supplier.Name);
            await repository.AddAsync(inventory);
        }

        await using (var deleteContext = database.CreateDbContext())
        {
            var supplierToDelete = await deleteContext.Suppliers.FindAsync(supplier.Id);
            Assert.NotNull(supplierToDelete);
            deleteContext.Suppliers.Remove(supplierToDelete);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
            Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal(
                PostgresErrorCodes.ForeignKeyViolation,
                ((PostgresException)exception.InnerException!).SqlState);
        }
    }

    [Fact]
    public async Task InventoryRepository_PreservesSupplierNameSnapshot_WhenSupplierIsUpdatedOrDeactivated()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-SUP-3");
        var supplier = new Supplier("Original Supplier Name");

        await using (var dbContext = database.CreateDbContext())
        {
            dbContext.Suppliers.Add(supplier);
            await dbContext.SaveChangesAsync();

            var repository = new InventoryRepository(dbContext);
            var inventory = new Inventory(productIdentifier);
            inventory.RegisterEntry(15, "Receipt", supplierId: supplier.Id, supplierName: supplier.Name);
            await repository.AddAsync(inventory);
        }

        await using (var updateContext = database.CreateDbContext())
        {
            var loadedSupplier = await updateContext.Suppliers.FindAsync(supplier.Id);
            Assert.NotNull(loadedSupplier);
            loadedSupplier.UpdateDetails("Updated Supplier Name");
            loadedSupplier.Deactivate();
            await updateContext.SaveChangesAsync();
        }

        await using var queryContext = database.CreateDbContext();
        var loadedInventory = await new InventoryRepository(queryContext)
            .GetByProductIdentifierAsync(productIdentifier);

        Assert.NotNull(loadedInventory);
        var movement = Assert.Single(loadedInventory.Movements);
        Assert.Equal("Original Supplier Name", movement.SupplierName);
        Assert.Equal(supplier.Id, movement.SupplierId);
    }

    [Fact]
    public async Task InventoryRepository_AtomicallyPersistsEntryMovementAndAuditEvent()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-AUDIT-1");
        var auditId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new InventoryRepository(dbContext);
            var inventory = new Inventory(productIdentifier);
            inventory.RegisterEntry(20, "PO-AUDIT-RECEIPT");
            var movement = inventory.Movements.Last();

            var auditEvent = AuditEvent.CreateForUser(
                userId,
                AuditEvent.EventTypes.InventoryMovementRecorded,
                AuditEvent.EntityNames.InventoryMovement,
                movement.Id.ToString(),
                metadataJson: "{\"quantity\":20}");

            await repository.AddAsync(inventory, auditEvent);
        }

        await using var queryContext = database.CreateDbContext();
        var loadedInventory = await new InventoryRepository(queryContext)
            .GetByProductIdentifierAsync(productIdentifier);
        Assert.NotNull(loadedInventory);
        Assert.Equal(20, loadedInventory.Quantity);

        var loadedAudit = await queryContext.AuditEvents
            .SingleOrDefaultAsync(a => a.EntityId == loadedInventory.Movements.Last().Id.ToString());
        Assert.NotNull(loadedAudit);
        Assert.Equal(AuditEvent.EventTypes.InventoryMovementRecorded, loadedAudit.EventType);
        Assert.Equal(userId, loadedAudit.UserId);
    }

    [Fact]
    public async Task InventoryRepository_SaveAsync_WithConcurrencyConflict_RollsBackAndLeavesNoPhantomAuditEvent()
    {
        await database.ClearAsync();
        var productIdentifier = new ProductIdentifier("ITEM-OCC-AUDIT");
        var userId = Guid.NewGuid();

        await using (var seedContext = database.CreateDbContext())
        {
            var repo = new InventoryRepository(seedContext);
            var inv = new Inventory(productIdentifier, initialStock: 50, initialStockReference: "Seed");
            await repo.AddAsync(inv);
        }

        // Load into two separate contexts to create an OCC conflict
        await using var contextA = database.CreateDbContext();
        await using var contextB = database.CreateDbContext();

        var repoA = new InventoryRepository(contextA);
        var repoB = new InventoryRepository(contextB);

        var invA = await repoA.GetByProductIdentifierAsync(productIdentifier);
        var invB = await repoB.GetByProductIdentifierAsync(productIdentifier);

        Assert.NotNull(invA);
        Assert.NotNull(invB);

        // contextA modifies and saves
        invA.RegisterExit(10, "Exit A");
        await repoA.SaveAsync(invA);

        // contextB attempts to save with an audit event -> should fail with ConcurrencyException
        invB.RegisterExit(10, "Exit B");
        var phantomMovement = invB.Movements.Last();
        var phantomAudit = AuditEvent.CreateForUser(
            userId,
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            phantomMovement.Id.ToString(),
            metadataJson: "{\"quantity\":10}");

        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            repoB.SaveAsync(invB, phantomAudit));

        // In fresh verify context, verify NO phantom audit event exists
        await using var verifyContext = database.CreateDbContext();
        var auditEvents = await verifyContext.AuditEvents
            .Where(a => a.EntityId == phantomMovement.Id.ToString())
            .ToListAsync();
        Assert.Empty(auditEvents);
    }
}
