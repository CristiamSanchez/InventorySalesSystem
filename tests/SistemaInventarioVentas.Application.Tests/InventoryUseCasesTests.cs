using System.Text.Json;
using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class InventoryUseCasesTests
{
    private readonly InMemoryInventoryRepository _inventories = new();
    private readonly InMemoryCatalog _catalog = new();
    private readonly InMemorySupplierRepository _suppliers = new();
    private readonly TestCurrentUser _currentUser = new();

    private InventoryUseCases UseCases => new(_inventories, currentUser: _currentUser);
    private InventoryUseCases UseCasesWithProducts => new(_inventories, _catalog, currentUser: _currentUser);
    private InventoryUseCases UseCasesWithSuppliers => new(_inventories, _catalog, _suppliers, currentUser: _currentUser);

    [Fact]
    public async Task RecordEntryAsync_CreatesInventoryWhenMissing_AndCallsAddAsync()
    {
        var result = await UseCases.RecordEntryAsync("PROD-1", 20, "PO-1001");

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Quantity);
        Assert.Equal("PROD-1", result.Value.ProductIdentifier.Value);
        Assert.Equal(1, _inventories.AddCount);
        Assert.Equal(0, _inventories.SaveCount);
        Assert.Contains(result.Value, _inventories.StoredInventories);
        Assert.Single(result.Value.Movements);
    }

    [Fact]
    public async Task RecordEntryAsync_UpdatesExistingInventory_AndCallsSaveAsync()
    {
        var existing = new Inventory(new ProductIdentifier("PROD-1"));
        existing.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(existing);
        _inventories.ResetCounters();

        var result = await UseCases.RecordEntryAsync("PROD-1", 15, "Restock PO-1002");

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value.Quantity);
        Assert.Equal(0, _inventories.AddCount);
        Assert.Equal(1, _inventories.SaveCount);
        Assert.Equal(2, result.Value.Movements.Count);
    }

    [Fact]
    public async Task RecordEntryAsync_WithProductRepository_ReturnsNotFoundWhenProductDoesNotExist()
    {
        var result = await UseCasesWithProducts.RecordEntryAsync("NONEXISTENT", 10, "PO-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Product was not found.", result.Error!.Message);
        Assert.Equal(0, _inventories.AddCount);
    }

    [Fact]
    public async Task RecordEntryAsync_WithProductRepository_SucceedsWhenProductExists()
    {
        var category = new Category("Electronics");
        var product = new Product(new ProductIdentifier("PROD-1"), "Gadget", category);
        await _catalog.AddAsync(product);

        var result = await UseCasesWithProducts.RecordEntryAsync("PROD-1", 10, "PO-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Quantity);
        Assert.Equal(1, _inventories.AddCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordEntryAsync_RejectsInvalidIdentifier(string? identifier)
    {
        var result = await UseCases.RecordEntryAsync(identifier!, 10, "PO-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _inventories.AddCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-20)]
    public async Task RecordEntryAsync_RejectsNonPositiveQuantity(int quantity)
    {
        var result = await UseCases.RecordEntryAsync("PROD-1", quantity, "PO-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _inventories.AddCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordEntryAsync_RejectsBlankReference(string? reference)
    {
        var result = await UseCases.RecordEntryAsync("PROD-1", 10, reference!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _inventories.AddCount);
    }

    [Fact]
    public async Task RecordExitAsync_DecreasesStockAndCallsSaveAsync()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(50, "Initial");
        await _inventories.AddAsync(inventory);
        _inventories.ResetCounters();

        var result = await UseCases.RecordExitAsync("PROD-1", 15, "Sale #101");

        Assert.True(result.IsSuccess);
        Assert.Equal(35, result.Value.Quantity);
        Assert.Equal(1, _inventories.SaveCount);
        Assert.Equal(2, result.Value.Movements.Count);
        Assert.Equal(MovementType.Exit, result.Value.Movements.Last().MovementType);
    }

    [Fact]
    public async Task RecordExitAsync_ReturnsNotFoundWhenInventoryDoesNotExist()
    {
        var result = await UseCases.RecordExitAsync("PROD-MISSING", 5, "Order");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal(0, _inventories.SaveCount);
    }

    [Fact]
    public async Task RecordExitAsync_WithProductRepository_ReturnsNotFoundWhenProductDoesNotExist()
    {
        var result = await UseCasesWithProducts.RecordExitAsync("NONEXISTENT", 5, "Order");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Product was not found.", result.Error!.Message);
    }

    [Fact]
    public async Task RecordExitAsync_ReturnsInsufficientStockWhenQuantityExceedsAvailable()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);
        _inventories.ResetCounters();

        var result = await UseCases.RecordExitAsync("PROD-1", 11, "Order Overstock");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
        Assert.Equal("Inventory stock cannot become negative.", result.Error!.Message);
        Assert.Equal(10, inventory.Quantity);
        Assert.Equal(0, _inventories.SaveCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task RecordExitAsync_RejectsNonPositiveQuantity(int quantity)
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);

        var result = await UseCases.RecordExitAsync("PROD-1", quantity, "Order");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordExitAsync_RejectsBlankReference(string? reference)
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);

        var result = await UseCases.RecordExitAsync("PROD-1", 5, reference!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordExitAsync_RejectsInvalidIdentifier(string? identifier)
    {
        var result = await UseCases.RecordExitAsync(identifier!, 5, "Order");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task AdjustAsync_PositiveDelta_IncreasesStockAndCallsSaveAsync()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);
        _inventories.ResetCounters();

        var result = await UseCases.AdjustAsync("PROD-1", 4, "Found extra box");

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value.Quantity);
        Assert.Equal(1, _inventories.SaveCount);
        Assert.Equal(MovementType.Adjustment, result.Value.Movements.Last().MovementType);
        Assert.Equal(4, result.Value.Movements.Last().Quantity);
    }

    [Fact]
    public async Task AdjustAsync_NegativeDelta_DecreasesStockAndCallsSaveAsync()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);
        _inventories.ResetCounters();

        var result = await UseCases.AdjustAsync("PROD-1", -3, "Damaged cans");

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Quantity);
        Assert.Equal(1, _inventories.SaveCount);
        Assert.Equal(MovementType.Adjustment, result.Value.Movements.Last().MovementType);
        Assert.Equal(-3, result.Value.Movements.Last().Quantity);
    }

    [Fact]
    public async Task AdjustAsync_ReturnsNotFoundWhenInventoryDoesNotExist()
    {
        var result = await UseCases.AdjustAsync("PROD-MISSING", 5, "Recount");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal(0, _inventories.SaveCount);
    }

    [Fact]
    public async Task AdjustAsync_ReturnsInsufficientStockWhenNegativeDeltaExceedsStock()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);
        _inventories.ResetCounters();

        var result = await UseCases.AdjustAsync("PROD-1", -11, "Major loss");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
        Assert.Equal("Inventory stock cannot become negative.", result.Error!.Message);
        Assert.Equal(10, inventory.Quantity);
        Assert.Equal(0, _inventories.SaveCount);
    }

    [Fact]
    public async Task AdjustAsync_RejectsZeroDelta()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);

        var result = await UseCases.AdjustAsync("PROD-1", 0, "No change");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AdjustAsync_RejectsBlankReason(string? reason)
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(10, "Initial");
        await _inventories.AddAsync(inventory);

        var result = await UseCases.AdjustAsync("PROD-1", 5, reason!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AdjustAsync_RejectsInvalidIdentifier(string? identifier)
    {
        var result = await UseCases.AdjustAsync(identifier!, 5, "Reason");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task GetByProductIdentifierAsync_ReturnsInventoryWhenFound()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(42, "Initial");
        await _inventories.AddAsync(inventory);

        var result = await UseCases.GetByProductIdentifierAsync("PROD-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.Quantity);
        Assert.Equal("PROD-1", result.Value.ProductIdentifier.Value);
    }

    [Fact]
    public async Task GetByProductIdentifierAsync_ReturnsNotFoundWhenMissing()
    {
        var result = await UseCases.GetByProductIdentifierAsync("PROD-MISSING");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByProductIdentifierAsync_RejectsInvalidIdentifier(string? identifier)
    {
        var result = await UseCases.GetByProductIdentifierAsync(identifier!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task GetMovementsAsync_ReturnsAllRecordedMovements()
    {
        var inventory = new Inventory(new ProductIdentifier("PROD-1"));
        inventory.RegisterEntry(30, "PO-1");
        inventory.RegisterExit(5, "Sale-1");
        inventory.Adjust(2, "Recount");
        await _inventories.AddAsync(inventory);

        var result = await UseCases.GetMovementsAsync("PROD-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(MovementType.Entry, result.Value[0].MovementType);
        Assert.Equal(MovementType.Exit, result.Value[1].MovementType);
        Assert.Equal(MovementType.Adjustment, result.Value[2].MovementType);
    }

    [Fact]
    public async Task GetMovementsAsync_ReturnsNotFoundWhenInventoryDoesNotExist()
    {
        var result = await UseCases.GetMovementsAsync("PROD-MISSING");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMovementsAsync_RejectsInvalidIdentifier(string? identifier)
    {
        var result = await UseCases.GetMovementsAsync(identifier!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task Overloads_AcceptingProductIdentifier_ExecuteSuccessfully()
    {
        var identifier = new ProductIdentifier("PROD-TYPED");

        var entryResult = await UseCases.RecordEntryAsync(identifier, 10, "Initial");
        Assert.True(entryResult.IsSuccess);

        var exitResult = await UseCases.RecordExitAsync(identifier, 2, "Sale");
        Assert.True(exitResult.IsSuccess);

        var adjustResult = await UseCases.AdjustAsync(identifier, 1, "Correction");
        Assert.True(adjustResult.IsSuccess);

        var getResult = await UseCases.GetByProductIdentifierAsync(identifier);
        Assert.True(getResult.IsSuccess);
        Assert.Equal(9, getResult.Value.Quantity);

        var movementsResult = await UseCases.GetMovementsAsync(identifier);
        Assert.True(movementsResult.IsSuccess);
        Assert.Equal(3, movementsResult.Value.Count);
    }

    [Fact]
    public async Task CancellationToken_IsPropagatedToRepository()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await UseCases.GetByProductIdentifierAsync("PROD-1", token);

        Assert.Equal(token, _inventories.LastCancellationToken);
    }

    [Fact]
    public async Task RecordEntryAsync_WithActiveSupplier_SnapshotsNameAndSetsSupplierId()
    {
        var category = new Category("Electronics");
        var product = new Product(new ProductIdentifier("PROD-SUP-1"), "Gadget", category);
        await _catalog.AddAsync(product);

        var supplier = new Supplier("Acme Supplies");
        await _suppliers.AddAsync(supplier);

        var result = await UseCasesWithSuppliers.RecordEntryAsync(
            "PROD-SUP-1",
            20,
            "PO-5001",
            occurredAtUtc: DateTimeOffset.UtcNow,
            supplierId: supplier.Id);

        Assert.True(result.IsSuccess);
        var movement = Assert.Single(result.Value.Movements);
        Assert.Equal(supplier.Id, movement.SupplierId);
        Assert.Equal("Acme Supplies", movement.SupplierName);
        Assert.Equal(20, movement.Quantity);
        Assert.Equal(MovementType.Entry, movement.MovementType);
    }

    [Fact]
    public async Task RecordEntryAsync_WithMissingSupplier_ReturnsNotFound()
    {
        var category = new Category("Electronics");
        var product = new Product(new ProductIdentifier("PROD-SUP-2"), "Gadget", category);
        await _catalog.AddAsync(product);

        var nonExistentSupplierId = Guid.NewGuid();
        var result = await UseCasesWithSuppliers.RecordEntryAsync(
            "PROD-SUP-2",
            10,
            "PO-5002",
            occurredAtUtc: DateTimeOffset.UtcNow,
            supplierId: nonExistentSupplierId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Supplier was not found.", result.Error!.Message);
        Assert.Equal(0, _inventories.AddCount);
    }

    [Fact]
    public async Task RecordEntryAsync_WithInactiveSupplier_ReturnsInvalidInput()
    {
        var category = new Category("Electronics");
        var product = new Product(new ProductIdentifier("PROD-SUP-3"), "Gadget", category);
        await _catalog.AddAsync(product);

        var supplier = new Supplier("Inactive Supplier");
        supplier.Deactivate();
        await _suppliers.AddAsync(supplier);

        var result = await UseCasesWithSuppliers.RecordEntryAsync(
            "PROD-SUP-3",
            10,
            "PO-5003",
            occurredAtUtc: DateTimeOffset.UtcNow,
            supplierId: supplier.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Inactive suppliers cannot be used for recording new inventory entries.", result.Error!.Message);
        Assert.Equal(0, _inventories.AddCount);
    }

    [Fact]
    public async Task RecordEntryAsync_WithEmptySupplierId_ReturnsInvalidInput()
    {
        var category = new Category("Electronics");
        var product = new Product(new ProductIdentifier("PROD-SUP-4"), "Gadget", category);
        await _catalog.AddAsync(product);

        var result = await UseCasesWithSuppliers.RecordEntryAsync(
            "PROD-SUP-4",
            10,
            "PO-5004",
            occurredAtUtc: DateTimeOffset.UtcNow,
            supplierId: Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Supplier ID cannot be empty.", result.Error!.Message);
        Assert.Equal(0, _inventories.AddCount);
    }

    [Fact]
    public async Task RecordEntryAsync_WithSupplier_WhenSupplierNameChangesLater_PreservesHistoricalSnapshot()
    {
        var category = new Category("Electronics");
        var product = new Product(new ProductIdentifier("PROD-SUP-5"), "Gadget", category);
        await _catalog.AddAsync(product);

        var supplier = new Supplier("Original Supplier Name");
        await _suppliers.AddAsync(supplier);

        var result = await UseCasesWithSuppliers.RecordEntryAsync(
            "PROD-SUP-5",
            15,
            "PO-5005",
            occurredAtUtc: DateTimeOffset.UtcNow,
            supplierId: supplier.Id);

        Assert.True(result.IsSuccess);

        // Later supplier name is updated
        supplier.UpdateDetails("Renamed Supplier Corp");

        var movement = Assert.Single(result.Value.Movements);
        Assert.Equal("Original Supplier Name", movement.SupplierName);
        Assert.Equal(supplier.Id, movement.SupplierId);
    }

    [Fact]
    public async Task RecordEntryAsync_WithProductIdentifier_Overload_WithSupplier_Succeeds()
    {
        var identifier = new ProductIdentifier("PROD-TYPED-SUP");
        var category = new Category("Electronics");
        var product = new Product(identifier, "Gadget", category);
        await _catalog.AddAsync(product);

        var supplier = new Supplier("Typed Supplier");
        await _suppliers.AddAsync(supplier);

        var result = await UseCasesWithSuppliers.RecordEntryAsync(
            identifier,
            12,
            "PO-TYPED",
            occurredAtUtc: DateTimeOffset.UtcNow,
            supplierId: supplier.Id);

        Assert.True(result.IsSuccess);
        var movement = Assert.Single(result.Value.Movements);
        Assert.Equal(supplier.Id, movement.SupplierId);
        Assert.Equal("Typed Supplier", movement.SupplierName);
    }

    [Fact]
    public async Task RecordEntryAsync_WithSupplierId_WhenSuppliersRepositoryIsNull_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UseCases.RecordEntryAsync(
                "PROD-1",
                10,
                "PO-1",
                occurredAtUtc: DateTimeOffset.UtcNow,
                supplierId: Guid.NewGuid()));
    }

    [Fact]
    public async Task RecordEntryAsync_WithAuthenticatedUser_EmitsInventoryMovementRecordedAuditEventWithUserActor()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = userId;

        var supplier = new Supplier("Audit Supplier");
        await _suppliers.AddAsync(supplier);

        var product = new Product(new ProductIdentifier("AUDIT-PROD-1"), "Gadget", new Category("Default"));
        await _catalog.AddAsync(product);

        var result = await UseCasesWithSuppliers.RecordEntryAsync(
            "AUDIT-PROD-1",
            15,
            "PO-AUDIT-1",
            occurredAtUtc: DateTimeOffset.UtcNow,
            supplierId: supplier.Id);

        Assert.True(result.IsSuccess);
        var audit = _inventories.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.InventoryMovementRecorded, audit.EventType);
        Assert.Equal(AuditEvent.EntityNames.InventoryMovement, audit.EntityName);
        Assert.Equal(result.Value.Movements.Last().Id.ToString(), audit.EntityId);
        Assert.Equal(userId, audit.UserId);
        Assert.True(audit.HasUser);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("AUDIT-PROD-1", doc.RootElement.GetProperty("productIdentifier").GetString());
        Assert.Equal("Entry", doc.RootElement.GetProperty("movementType").GetString());
        Assert.Equal(15, doc.RootElement.GetProperty("quantity").GetInt32());
        Assert.Equal("PO-AUDIT-1", doc.RootElement.GetProperty("reference").GetString());
        Assert.Equal(supplier.Id.ToString(), doc.RootElement.GetProperty("supplierId").GetString());
        Assert.Equal("Audit Supplier", doc.RootElement.GetProperty("supplierName").GetString());
    }

    [Fact]
    public async Task RecordEntryAsync_WithUnauthenticatedUser_EmitsSystemInitiatedAuditEvent()
    {
        _currentUser.IsAuthenticated = false;
        _currentUser.UserId = null;

        var result = await UseCases.RecordEntryAsync("AUDIT-PROD-2", 20, "PO-AUDIT-2");

        Assert.True(result.IsSuccess);
        var audit = _inventories.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.InventoryMovementRecorded, audit.EventType);
        Assert.Null(audit.UserId);
        Assert.False(audit.HasUser);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("system", doc.RootElement.GetProperty("actor").GetString());
        Assert.Equal("Entry", doc.RootElement.GetProperty("movementType").GetString());
        Assert.Equal(20, doc.RootElement.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task RecordExitAsync_EmitsInventoryMovementRecordedAuditEvent()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = userId;

        await UseCases.RecordEntryAsync("AUDIT-PROD-3", 50, "INITIAL");
        _inventories.ResetCounters();

        var result = await UseCases.RecordExitAsync("AUDIT-PROD-3", 10, "DISPATCH-1");

        Assert.True(result.IsSuccess);
        var audit = _inventories.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.InventoryMovementRecorded, audit.EventType);
        Assert.Equal(AuditEvent.EntityNames.InventoryMovement, audit.EntityName);
        Assert.Equal(userId, audit.UserId);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("AUDIT-PROD-3", doc.RootElement.GetProperty("productIdentifier").GetString());
        Assert.Equal("Exit", doc.RootElement.GetProperty("movementType").GetString());
        Assert.Equal(10, doc.RootElement.GetProperty("quantity").GetInt32());
        Assert.Equal("DISPATCH-1", doc.RootElement.GetProperty("reference").GetString());
    }

    [Fact]
    public async Task AdjustAsync_EmitsInventoryMovementRecordedAuditEvent()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = userId;

        await UseCases.RecordEntryAsync("AUDIT-PROD-4", 50, "INITIAL");
        _inventories.ResetCounters();

        var result = await UseCases.AdjustAsync("AUDIT-PROD-4", -5, "Damaged stock in aisle 3");

        Assert.True(result.IsSuccess);
        var audit = _inventories.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.InventoryMovementRecorded, audit.EventType);
        Assert.Equal(AuditEvent.EntityNames.InventoryMovement, audit.EntityName);
        Assert.Equal(userId, audit.UserId);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("AUDIT-PROD-4", doc.RootElement.GetProperty("productIdentifier").GetString());
        Assert.Equal("Adjustment", doc.RootElement.GetProperty("movementType").GetString());
        Assert.Equal(-5, doc.RootElement.GetProperty("quantityDelta").GetInt32());
        Assert.Equal("Damaged stock in aisle 3", doc.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task InventoryOperations_WhenValidationFails_DoNotEmitAuditEvent()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        // 1. Invalid entry quantity
        var entryResult = await UseCases.RecordEntryAsync("AUDIT-FAIL", 0, "PO-FAIL");
        Assert.False(entryResult.IsSuccess);
        Assert.Null(_inventories.LastAuditEvent);

        // 2. Insufficient stock on exit
        await UseCases.RecordEntryAsync("AUDIT-FAIL", 5, "INITIAL");
        _inventories.ResetCounters();

        var exitResult = await UseCases.RecordExitAsync("AUDIT-FAIL", 10, "OVER-DISPATCH");
        Assert.False(exitResult.IsSuccess);
        Assert.Null(_inventories.LastAuditEvent);
        Assert.Empty(_inventories.StoredAuditEvents);
    }

    private sealed class InMemoryInventoryRepository : IInventoryRepository
    {
        private readonly List<Inventory> _items = [];
        private readonly List<AuditEvent> _auditEvents = [];

        public IReadOnlyList<Inventory> StoredInventories => _items;
        public IReadOnlyList<AuditEvent> StoredAuditEvents => _auditEvents;
        public AuditEvent? LastAuditEvent => _auditEvents.LastOrDefault();

        public int AddCount { get; private set; }

        public int SaveCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public void ResetCounters()
        {
            AddCount = 0;
            SaveCount = 0;
            _auditEvents.Clear();
        }

        public Task<Inventory?> GetByProductIdentifierAsync(
            ProductIdentifier productIdentifier,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(i => i.ProductIdentifier == productIdentifier));
        }

        public Task AddAsync(
            Inventory inventory,
            CancellationToken cancellationToken = default) =>
            AddAsync(inventory, auditEvent: null, cancellationToken);

        public Task AddAsync(
            Inventory inventory,
            AuditEvent? auditEvent,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            AddCount++;
            _items.Add(inventory);
            if (auditEvent is not null)
            {
                _auditEvents.Add(auditEvent);
            }

            return Task.CompletedTask;
        }

        public Task SaveAsync(
            Inventory inventory,
            CancellationToken cancellationToken = default) =>
            SaveAsync(inventory, auditEvent: null, cancellationToken);

        public Task SaveAsync(
            Inventory inventory,
            AuditEvent? auditEvent,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            if (auditEvent is not null)
            {
                _auditEvents.Add(auditEvent);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySupplierRepository : ISupplierRepository
    {
        public List<Supplier> Suppliers { get; } = [];
        public int AddCount { get; private set; }
        public int SaveCount { get; private set; }

        public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Suppliers.SingleOrDefault(s => s.Id == id));
        }

        public Task<IReadOnlyList<Supplier>> ListAsync(SupplierQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Supplier>>(Suppliers.ToList());
        }

        public Task<bool> ExistsByIdentificationNumberAsync(
            string normalizedIdentificationNumber,
            Guid? excludingId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var exists = Suppliers.Any(s =>
                s.Id != excludingId &&
                s.IdentificationNumber != null &&
                string.Equals(s.IdentificationNumber, normalizedIdentificationNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddCount++;
            Suppliers.Add(supplier);
            return Task.CompletedTask;
        }

        public Task SaveAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
