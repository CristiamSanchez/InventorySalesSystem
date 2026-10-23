using System.Collections.ObjectModel;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class InventoryTests
{
    private readonly ProductIdentifier _testProductIdentifier = new("PROD-TEST-001");

    [Fact]
    public void Constructor_WithValidProductIdentifier_InitializesWithZeroStockAndEmptyMovements()
    {
        var inventory = new Inventory(_testProductIdentifier);

        Assert.NotEqual(Guid.Empty, inventory.Id);
        Assert.Equal(_testProductIdentifier, inventory.ProductIdentifier);
        Assert.Equal(0, inventory.Quantity);
        Assert.Empty(inventory.Movements);
    }

    [Fact]
    public void Constructor_WithInitialStock_RecordsInitialEntryMovement()
    {
        var timestamp = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

        var inventory = new Inventory(_testProductIdentifier, 25, "Initial balance", timestamp);

        Assert.Equal(25, inventory.Quantity);
        Assert.Single(inventory.Movements);

        var movement = inventory.Movements.Single();
        Assert.Equal(_testProductIdentifier, movement.ProductIdentifier);
        Assert.Equal(MovementType.Entry, movement.MovementType);
        Assert.Equal(25, movement.Quantity);
        Assert.Equal("Initial balance", movement.Reference);
        Assert.Equal(timestamp, movement.OccurredAtUtc);
    }

    [Fact]
    public void Constructor_WithZeroInitialStock_InitializesWithZeroStockAndNoMovements()
    {
        var inventory = new Inventory(_testProductIdentifier, 0, "No stock");

        Assert.Equal(0, inventory.Quantity);
        Assert.Empty(inventory.Movements);
    }

    [Fact]
    public void Constructor_ThrowsWhenProductIdentifierIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Inventory(null!));
    }

    [Fact]
    public void Constructor_ThrowsWhenInitialStockIsNegative()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Inventory(_testProductIdentifier, -10, "Opening balance"));

        Assert.Equal("initialStock", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsWhenInitialStockIsPositiveAndReferenceIsBlank(string? reference)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new Inventory(_testProductIdentifier, 10, reference!));
    }

    [Fact]
    public void RegisterEntry_IncreasesStockAndAppendsMovement()
    {
        var inventory = new Inventory(_testProductIdentifier);
        var timestamp = new DateTimeOffset(2026, 10, 18, 14, 0, 0, TimeSpan.Zero);

        inventory.RegisterEntry(15, "PO-1001", timestamp);

        Assert.Equal(15, inventory.Quantity);
        Assert.Single(inventory.Movements);

        var movement = inventory.Movements.Single();
        Assert.Equal(_testProductIdentifier, movement.ProductIdentifier);
        Assert.Equal(MovementType.Entry, movement.MovementType);
        Assert.Equal(15, movement.Quantity);
        Assert.Equal("PO-1001", movement.Reference);
        Assert.Equal(timestamp, movement.OccurredAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void RegisterEntry_ThrowsWhenQuantityIsZeroOrNegative(int quantity)
    {
        var inventory = new Inventory(_testProductIdentifier);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => inventory.RegisterEntry(quantity, "PO-1001"));

        Assert.Equal("quantity", exception.ParamName);
        Assert.Equal(0, inventory.Quantity);
        Assert.Empty(inventory.Movements);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterEntry_ThrowsWhenReferenceIsNullOrWhitespace(string? reference)
    {
        var inventory = new Inventory(_testProductIdentifier);

        Assert.ThrowsAny<ArgumentException>(
            () => inventory.RegisterEntry(10, reference!));

        Assert.Equal(0, inventory.Quantity);
        Assert.Empty(inventory.Movements);
    }

    [Fact]
    public void RegisterExit_DecreasesStockAndAppendsMovement()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(20, "PO-1001");
        var timestamp = new DateTimeOffset(2026, 10, 18, 15, 0, 0, TimeSpan.Zero);

        inventory.RegisterExit(8, "Order-501", timestamp);

        Assert.Equal(12, inventory.Quantity);
        Assert.Equal(2, inventory.Movements.Count);

        var exitMovement = inventory.Movements.Last();
        Assert.Equal(_testProductIdentifier, exitMovement.ProductIdentifier);
        Assert.Equal(MovementType.Exit, exitMovement.MovementType);
        Assert.Equal(8, exitMovement.Quantity);
        Assert.Equal("Order-501", exitMovement.Reference);
        Assert.Equal(timestamp, exitMovement.OccurredAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-20)]
    public void RegisterExit_ThrowsWhenQuantityIsZeroOrNegative(int quantity)
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(20, "PO-1001");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => inventory.RegisterExit(quantity, "Order-501"));

        Assert.Equal("quantity", exception.ParamName);
        Assert.Equal(20, inventory.Quantity);
        Assert.Single(inventory.Movements);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterExit_ThrowsWhenReferenceIsNullOrWhitespace(string? reference)
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(20, "PO-1001");

        Assert.ThrowsAny<ArgumentException>(
            () => inventory.RegisterExit(5, reference!));

        Assert.Equal(20, inventory.Quantity);
        Assert.Single(inventory.Movements);
    }

    [Fact]
    public void RegisterExit_ThrowsWhenQuantityExceedsCurrentStock()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");

        var exception = Assert.Throws<InvalidOperationException>(
            () => inventory.RegisterExit(11, "Order-999"));

        Assert.Equal("Inventory stock cannot become negative.", exception.Message);
        Assert.Equal(10, inventory.Quantity);
        Assert.Single(inventory.Movements);
    }

    [Fact]
    public void RegisterExit_AllowsDepletingStockToZero()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");

        inventory.RegisterExit(10, "Order-FullClear");

        Assert.Equal(0, inventory.Quantity);
        Assert.Equal(2, inventory.Movements.Count);
        Assert.Equal(MovementType.Exit, inventory.Movements.Last().MovementType);
    }

    [Fact]
    public void Adjust_PositiveDelta_IncreasesStockAndAppendsMovement()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");
        var timestamp = new DateTimeOffset(2026, 10, 18, 16, 0, 0, TimeSpan.Zero);

        inventory.Adjust(5, "Found extra item during audit", timestamp);

        Assert.Equal(15, inventory.Quantity);
        Assert.Equal(2, inventory.Movements.Count);

        var movement = inventory.Movements.Last();
        Assert.Equal(_testProductIdentifier, movement.ProductIdentifier);
        Assert.Equal(MovementType.Adjustment, movement.MovementType);
        Assert.Equal(5, movement.Quantity);
        Assert.Equal("Found extra item during audit", movement.Reference);
        Assert.Equal(timestamp, movement.OccurredAtUtc);
    }

    [Fact]
    public void Adjust_NegativeDelta_DecreasesStockAndAppendsMovement()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");
        var timestamp = new DateTimeOffset(2026, 10, 18, 16, 30, 0, TimeSpan.Zero);

        inventory.Adjust(-4, "Damaged in transit", timestamp);

        Assert.Equal(6, inventory.Quantity);
        Assert.Equal(2, inventory.Movements.Count);

        var movement = inventory.Movements.Last();
        Assert.Equal(_testProductIdentifier, movement.ProductIdentifier);
        Assert.Equal(MovementType.Adjustment, movement.MovementType);
        Assert.Equal(-4, movement.Quantity);
        Assert.Equal("Damaged in transit", movement.Reference);
        Assert.Equal(timestamp, movement.OccurredAtUtc);
    }

    [Fact]
    public void Adjust_ThrowsWhenDeltaIsZero()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => inventory.Adjust(0, "No change"));

        Assert.Equal("quantityDelta", exception.ParamName);
        Assert.Equal(10, inventory.Quantity);
        Assert.Single(inventory.Movements);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Adjust_ThrowsWhenReasonIsNullOrWhitespace(string? reason)
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");

        Assert.ThrowsAny<ArgumentException>(
            () => inventory.Adjust(2, reason!));

        Assert.Equal(10, inventory.Quantity);
        Assert.Single(inventory.Movements);
    }

    [Fact]
    public void Adjust_ThrowsWhenNegativeDeltaExceedsAvailableStock()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");

        var exception = Assert.Throws<InvalidOperationException>(
            () => inventory.Adjust(-11, "Missing items"));

        Assert.Equal("Inventory stock cannot become negative.", exception.Message);
        Assert.Equal(10, inventory.Quantity);
        Assert.Single(inventory.Movements);
    }

    [Fact]
    public void Adjust_AllowsAdjustingStockToExactlyZero()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-1001");

        inventory.Adjust(-10, "Inventory write-off");

        Assert.Equal(0, inventory.Quantity);
        Assert.Equal(2, inventory.Movements.Count);
        Assert.Equal(MovementType.Adjustment, inventory.Movements.Last().MovementType);
    }

    [Fact]
    public void ReferencesAndReasons_AreTrimmed()
    {
        var inventory = new Inventory(_testProductIdentifier);

        inventory.RegisterEntry(10, "   Supplier Receipt   ");
        inventory.RegisterExit(3, "   Broken Box   ");
        inventory.Adjust(1, "   Audit recount   ");

        var movements = inventory.Movements.ToList();
        Assert.Equal("Supplier Receipt", movements[0].Reference);
        Assert.Equal("Broken Box", movements[1].Reference);
        Assert.Equal("Audit recount", movements[2].Reference);
    }

    [Fact]
    public void Aliases_RecordAndRegister_ProduceEquivalentBehavior()
    {
        var inventory = new Inventory(_testProductIdentifier);

        inventory.RecordEntry(20, "PO-100");
        inventory.RecordExit(5, "Delivery-1");
        inventory.RecordAdjustment(-2, "Damaged");
        inventory.RegisterAdjustment(1, "Recount");

        Assert.Equal(14, inventory.Quantity);
        Assert.Equal(4, inventory.Movements.Count);
    }

    [Fact]
    public void MovementHistory_PreservesChronologyAndCumulativeBalance()
    {
        var inventory = new Inventory(_testProductIdentifier);
        var baseTime = new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);

        inventory.RegisterEntry(50, "Supplier shipment", baseTime.AddHours(1));
        inventory.RegisterExit(10, "Store sale line", baseTime.AddHours(2));
        inventory.Adjust(-2, "Shrinkage", baseTime.AddHours(3));
        inventory.RegisterEntry(20, "Secondary supply", baseTime.AddHours(4));
        inventory.Adjust(3, "Found misplaced stock", baseTime.AddHours(5));
        inventory.RegisterExit(15, "Store sale line 2", baseTime.AddHours(6));

        // 50 - 10 - 2 + 20 + 3 - 15 = 46
        Assert.Equal(46, inventory.Quantity);
        Assert.Equal(6, inventory.Movements.Count);

        var movements = inventory.Movements.ToList();
        for (var i = 1; i < movements.Count; i++)
        {
            Assert.True(movements[i].OccurredAtUtc >= movements[i - 1].OccurredAtUtc);
        }

        var cumulative = movements.Sum(m => m.MovementType switch
        {
            MovementType.Entry => m.Quantity,
            MovementType.Exit => -m.Quantity,
            MovementType.Adjustment => m.Quantity,
            _ => throw new InvalidOperationException()
        });

        Assert.Equal(inventory.Quantity, cumulative);
    }

    [Fact]
    public void Movements_CollectionIsEncapsulatedAndCannotBeModifiedExternally()
    {
        var inventory = new Inventory(_testProductIdentifier);
        inventory.RegisterEntry(10, "PO-100");

        Assert.IsAssignableFrom<ReadOnlyCollection<InventoryMovement>>(inventory.Movements);
        var collection = inventory.Movements as ICollection<InventoryMovement>;
        Assert.NotNull(collection);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(
            () => collection.Add(new InventoryMovement(
                _testProductIdentifier,
                MovementType.Entry,
                5,
                "Unauthorized inject")));
    }

    [Fact]
    public void InventoryMovement_Constructor_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>(
            () => new InventoryMovement(null!, MovementType.Entry, 10, "Valid"));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new InventoryMovement(_testProductIdentifier, (MovementType)99, 10, "Valid"));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new InventoryMovement(_testProductIdentifier, MovementType.Entry, 0, "Valid"));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new InventoryMovement(_testProductIdentifier, MovementType.Exit, -5, "Valid"));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new InventoryMovement(_testProductIdentifier, MovementType.Adjustment, 0, "Valid"));

        Assert.ThrowsAny<ArgumentException>(
            () => new InventoryMovement(_testProductIdentifier, MovementType.Entry, 10, "   "));

        var validMovement = new InventoryMovement(
            _testProductIdentifier,
            MovementType.Adjustment,
            -3,
            "  Audit discrepancy  ");

        Assert.NotEqual(Guid.Empty, validMovement.Id);
        Assert.Equal(_testProductIdentifier, validMovement.ProductIdentifier);
        Assert.Equal(MovementType.Adjustment, validMovement.MovementType);
        Assert.Equal(-3, validMovement.Quantity);
        Assert.Equal("Audit discrepancy", validMovement.Reference);
        Assert.True(validMovement.OccurredAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterEntry_WithValidSupplier_SetsSupplierIdAndTrimmedSupplierName()
    {
        var inventory = new Inventory(_testProductIdentifier);
        var supplierId = Guid.NewGuid();

        inventory.RegisterEntry(15, "Delivery Note #123", null, supplierId, "  Global Suppliers S.A.  ");

        var movement = Assert.Single(inventory.Movements);
        Assert.Equal(supplierId, movement.SupplierId);
        Assert.Equal("Global Suppliers S.A.", movement.SupplierName);
        Assert.Equal(15, movement.Quantity);
    }

    [Fact]
    public void RegisterEntry_WithoutSupplier_LeavesSupplierIdAndSupplierNameNull()
    {
        var inventory = new Inventory(_testProductIdentifier);

        inventory.RegisterEntry(20, "Manual count receipt");

        var movement = Assert.Single(inventory.Movements);
        Assert.Null(movement.SupplierId);
        Assert.Null(movement.SupplierName);
    }

    [Fact]
    public void RegisterEntry_WithEmptySupplierId_ThrowsArgumentException()
    {
        var inventory = new Inventory(_testProductIdentifier);

        var exception = Assert.Throws<ArgumentException>(
            () => inventory.RegisterEntry(10, "Ref", null, Guid.Empty, "Valid Supplier"));

        Assert.Equal("supplierId", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterEntry_WithSupplierId_AndNullOrWhitespaceName_ThrowsArgumentException(string? invalidName)
    {
        var inventory = new Inventory(_testProductIdentifier);
        var supplierId = Guid.NewGuid();

        var exception = Assert.Throws<ArgumentException>(
            () => inventory.RegisterEntry(10, "Ref", null, supplierId, invalidName));

        Assert.Equal("supplierName", exception.ParamName);
    }

    [Fact]
    public void RegisterEntry_WithSupplierId_AndNameExceeding200Chars_ThrowsArgumentException()
    {
        var inventory = new Inventory(_testProductIdentifier);
        var supplierId = Guid.NewGuid();
        var longName = new string('A', 201);

        var exception = Assert.Throws<ArgumentException>(
            () => inventory.RegisterEntry(10, "Ref", null, supplierId, longName));

        Assert.Equal("supplierName", exception.ParamName);
    }

    [Fact]
    public void InventoryMovement_WithSupplierOnExitOrAdjustment_ThrowsArgumentException()
    {
        var supplierId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new InventoryMovement(
            _testProductIdentifier,
            MovementType.Exit,
            5,
            "Exit ref",
            null,
            supplierId,
            "Supplier Name"));

        Assert.Throws<ArgumentException>(() => new InventoryMovement(
            _testProductIdentifier,
            MovementType.Adjustment,
            5,
            "Adjustment reason",
            null,
            supplierId,
            "Supplier Name"));
    }

    [Fact]
    public void InventoryMovement_WithSupplierNameButNoSupplierId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new InventoryMovement(
            _testProductIdentifier,
            MovementType.Entry,
            10,
            "Entry ref",
            null,
            null,
            "Supplier Without Id"));

        Assert.Equal("supplierName", exception.ParamName);
    }
}
