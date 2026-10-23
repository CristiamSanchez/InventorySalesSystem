using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class SaleTests
{
    private readonly ProductIdentifier _productA = new("PROD-001");
    private readonly ProductIdentifier _productB = new("PROD-002");
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_InitializesDraftWithZeroTotalsAndEmptyDetails()
    {
        var createdAt = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

        var sale = new Sale("CUST-100", _userId, createdAt);

        Assert.NotEqual(Guid.Empty, sale.Id);
        Assert.Equal("CUST-100", sale.CustomerReference);
        Assert.Equal(_userId, sale.UserId);
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Equal(createdAt, sale.CreatedAtUtc);
        Assert.Null(sale.ConfirmedAtUtc);
        Assert.Equal(0m, sale.Subtotal);
        Assert.Equal(0m, sale.TaxAmount);
        Assert.Equal(0m, sale.Total);
        Assert.Empty(sale.Details);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsWhenCustomerReferenceIsBlank(string? customerReference)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Sale(customerReference!, _userId));
    }

    [Fact]
    public void Constructor_ThrowsWhenUserIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Sale("CUST-100", Guid.Empty));
        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithCustomerId_InitializesDraftWithCustomerIdAndReference()
    {
        var customerId = Guid.NewGuid();
        var sale = new Sale("John Doe", _userId, customerId: customerId);

        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal("John Doe", sale.CustomerReference);
    }

    [Fact]
    public void Constructor_WithoutCustomerId_LeavesCustomerIdNull()
    {
        var sale = new Sale("Walk-in Customer", _userId);

        Assert.Null(sale.CustomerId);
        Assert.Equal("Walk-in Customer", sale.CustomerReference);
    }

    [Fact]
    public void Constructor_ThrowsWhenCustomerIdIsEmptyGuid()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Sale("John Doe", _userId, customerId: Guid.Empty));
        Assert.Equal("customerId", exception.ParamName);
    }

    [Fact]
    public void AssignCustomer_WhenDraft_UpdatesCustomerIdAndSnapshotsName()
    {
        var sale = new Sale("Walk-in", _userId);
        var customerId = Guid.NewGuid();

        sale.AssignCustomer(customerId, "  Jane Doe  ");

        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal("Jane Doe", sale.CustomerReference);
    }

    [Fact]
    public void AssignCustomer_ThrowsWhenCustomerIdIsEmptyGuid()
    {
        var sale = new Sale("Walk-in", _userId);

        var exception = Assert.Throws<ArgumentException>(
            () => sale.AssignCustomer(Guid.Empty, "Jane Doe"));
        Assert.Equal("customerId", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AssignCustomer_ThrowsWhenCustomerNameIsBlank(string? name)
    {
        var sale = new Sale("Walk-in", _userId);

        Assert.ThrowsAny<ArgumentException>(
            () => sale.AssignCustomer(Guid.NewGuid(), name!));
    }

    [Fact]
    public void AssignCustomer_ThrowsWhenNotDraft()
    {
        var sale = new Sale("Walk-in", _userId);
        sale.AddDetail(_productA, 1, 10m, 0m);
        sale.Confirm();

        Assert.Throws<InvalidOperationException>(
            () => sale.AssignCustomer(Guid.NewGuid(), "Jane Doe"));
    }

    [Fact]
    public void ClearCustomer_WhenDraft_ClearsCustomerIdAndUpdatesReference()
    {
        var customerId = Guid.NewGuid();
        var sale = new Sale("Jane Doe", _userId, customerId: customerId);

        sale.ClearCustomer("  Walk-in Customer  ");

        Assert.Null(sale.CustomerId);
        Assert.Equal("Walk-in Customer", sale.CustomerReference);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ClearCustomer_ThrowsWhenCustomerReferenceIsBlank(string? reference)
    {
        var sale = new Sale("Jane Doe", _userId, customerId: Guid.NewGuid());

        Assert.ThrowsAny<ArgumentException>(
            () => sale.ClearCustomer(reference!));
    }

    [Fact]
    public void ClearCustomer_ThrowsWhenNotDraft()
    {
        var sale = new Sale("Jane Doe", _userId, customerId: Guid.NewGuid());
        sale.AddDetail(_productA, 1, 10m, 0m);
        sale.Confirm();

        Assert.Throws<InvalidOperationException>(
            () => sale.ClearCustomer("Walk-in"));
    }

    [Fact]
    public void UpdateCustomerReference_UpdatesTrimmedReferenceWhenDraft()
    {
        var sale = new Sale("CUST-100", _userId);

        sale.UpdateCustomerReference("  CUST-200  ");

        Assert.Equal("CUST-200", sale.CustomerReference);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateCustomerReference_ThrowsWhenCustomerReferenceIsBlank(string? reference)
    {
        var sale = new Sale("CUST-100", _userId);

        Assert.ThrowsAny<ArgumentException>(() => sale.UpdateCustomerReference(reference!));
    }

    [Fact]
    public void UpdateCustomerReference_ThrowsWhenNotDraft()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 1, 10m, 0m);
        sale.Confirm();

        Assert.Throws<InvalidOperationException>(() => sale.UpdateCustomerReference("CUST-200"));
    }

    [Fact]
    public void SaleDetail_Constructor_CalculatesSubtotalTaxAndTotalAccurately()
    {
        var detail = new SaleDetail(_productA, 3, 10.50m, 0.16m);

        Assert.Equal(_productA, detail.ProductIdentifier);
        Assert.Equal(3, detail.Quantity);
        Assert.Equal(10.50m, detail.UnitPrice);
        Assert.Equal(0.16m, detail.TaxRate);
        Assert.Equal(31.50m, detail.Subtotal);
        Assert.Equal(5.04m, detail.TaxAmount);
        Assert.Equal(36.54m, detail.Total);
    }

    [Fact]
    public void SaleDetail_Constructor_ThrowsWhenProductIdentifierIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SaleDetail(null!, 1, 10m, 0m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void SaleDetail_Constructor_ThrowsWhenQuantityIsNotPositive(int quantity)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SaleDetail(_productA, quantity, 10m, 0m));
        Assert.Equal("quantity", exception.ParamName);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-50)]
    public void SaleDetail_Constructor_ThrowsWhenUnitPriceIsNegative(decimal unitPrice)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SaleDetail(_productA, 1, unitPrice, 0m));
        Assert.Equal("unitPrice", exception.ParamName);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-0.16)]
    public void SaleDetail_Constructor_ThrowsWhenTaxRateIsNegative(decimal taxRate)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SaleDetail(_productA, 1, 10m, taxRate));
        Assert.Equal("taxRate", exception.ParamName);
    }

    [Fact]
    public void MonetaryCalculations_UsesMidpointRoundingAwayFromZeroConsistently()
    {
        // 15.00 * 0.075 = 1.125m.
        // MidpointRounding.AwayFromZero rounds 1.125 to 1.13 (whereas ToEven would round to 1.12)
        var detail = new SaleDetail(_productA, 1, 15.00m, 0.075m);

        Assert.Equal(15.00m, detail.Subtotal);
        Assert.Equal(1.13m, detail.TaxAmount);
        Assert.Equal(16.13m, detail.Total);
    }

    [Fact]
    public void MonetaryCalculations_SupportsZeroTaxRateAndZeroUnitPrice()
    {
        var zeroTaxDetail = new SaleDetail(_productA, 2, 20.00m, 0.00m);
        Assert.Equal(40.00m, zeroTaxDetail.Subtotal);
        Assert.Equal(0.00m, zeroTaxDetail.TaxAmount);
        Assert.Equal(40.00m, zeroTaxDetail.Total);

        var freeDetail = new SaleDetail(_productB, 5, 0.00m, 0.16m);
        Assert.Equal(0.00m, freeDetail.Subtotal);
        Assert.Equal(0.00m, freeDetail.TaxAmount);
        Assert.Equal(0.00m, freeDetail.Total);
    }

    [Fact]
    public void AddDetail_AddsDistinctLinesAndRecalculatesTotals()
    {
        var sale = new Sale("CUST-100", _userId);

        sale.AddDetail(_productA, 2, 10.00m, 0.16m); // Subtotal: 20.00, Tax: 3.20, Total: 23.20
        sale.AddDetail(_productB, 1, 50.00m, 0.08m); // Subtotal: 50.00, Tax: 4.00, Total: 54.00

        Assert.Equal(2, sale.Details.Count);
        Assert.Equal(70.00m, sale.Subtotal);
        Assert.Equal(7.20m, sale.TaxAmount);
        Assert.Equal(77.20m, sale.Total);
    }

    [Fact]
    public void AddDetail_WithExistingProduct_IncreasesQuantityAndRecalculatesTotals()
    {
        var sale = new Sale("CUST-100", _userId);

        sale.AddDetail(_productA, 2, 10.00m, 0.16m);
        var detail = sale.AddDetail(_productA, 3, 10.00m, 0.16m);

        Assert.Single(sale.Details);
        Assert.Equal(5, detail.Quantity);
        Assert.Equal(10.00m, detail.UnitPrice);
        Assert.Equal(50.00m, detail.Subtotal);
        Assert.Equal(8.00m, detail.TaxAmount);
        Assert.Equal(58.00m, detail.Total);
        Assert.Equal(50.00m, sale.Subtotal);
        Assert.Equal(8.00m, sale.TaxAmount);
        Assert.Equal(58.00m, sale.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddDetail_ThrowsWhenQuantityIsNotPositive(int quantity)
    {
        var sale = new Sale("CUST-100", _userId);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sale.AddDetail(_productA, quantity, 10m, 0.16m));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-10)]
    public void AddDetail_ThrowsWhenUnitPriceIsNegative(decimal unitPrice)
    {
        var sale = new Sale("CUST-100", _userId);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sale.AddDetail(_productA, 1, unitPrice, 0.16m));
    }

    [Theory]
    [InlineData(-0.01)]
    public void AddDetail_ThrowsWhenTaxRateIsNegative(decimal taxRate)
    {
        var sale = new Sale("CUST-100", _userId);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sale.AddDetail(_productA, 1, 10m, taxRate));
    }

    [Fact]
    public void UpdateDetail_ByProductIdentifier_UpdatesQuantityAndTotals()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 2, 10.00m, 0.10m);

        sale.UpdateDetail(_productA, 4);

        var detail = sale.Details.Single();
        Assert.Equal(4, detail.Quantity);
        Assert.Equal(40.00m, detail.Subtotal);
        Assert.Equal(4.00m, detail.TaxAmount);
        Assert.Equal(44.00m, detail.Total);
        Assert.Equal(40.00m, sale.Subtotal);
        Assert.Equal(4.00m, sale.TaxAmount);
        Assert.Equal(44.00m, sale.Total);
    }

    [Fact]
    public void UpdateDetail_ByDetailId_UpdatesQuantityPriceTaxRateAndTotals()
    {
        var sale = new Sale("CUST-100", _userId);
        var originalDetail = sale.AddDetail(_productA, 2, 10.00m, 0.10m);

        sale.UpdateDetail(originalDetail.Id, 5, 20.00m, 0.16m);

        var updatedDetail = sale.Details.Single();
        Assert.Equal(5, updatedDetail.Quantity);
        Assert.Equal(20.00m, updatedDetail.UnitPrice);
        Assert.Equal(0.16m, updatedDetail.TaxRate);
        Assert.Equal(100.00m, updatedDetail.Subtotal);
        Assert.Equal(16.00m, updatedDetail.TaxAmount);
        Assert.Equal(116.00m, updatedDetail.Total);
        Assert.Equal(100.00m, sale.Subtotal);
        Assert.Equal(16.00m, sale.TaxAmount);
        Assert.Equal(116.00m, sale.Total);
    }

    [Fact]
    public void UpdateDetail_ThrowsWhenProductNotFound()
    {
        var sale = new Sale("CUST-100", _userId);

        Assert.Throws<InvalidOperationException>(() => sale.UpdateDetail(_productA, 2));
        Assert.Throws<InvalidOperationException>(() => sale.UpdateDetail(Guid.NewGuid(), 2));
    }

    [Fact]
    public void UpdateDetailQuantity_HelperMethod_UpdatesQuantityCorrectly()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 1, 15.00m, 0.10m);

        sale.UpdateDetailQuantity(_productA, 3);

        Assert.Equal(3, sale.Details.Single().Quantity);
        Assert.Equal(45.00m, sale.Subtotal);
    }

    [Fact]
    public void RemoveDetail_ByProductIdentifier_RemovesLineAndRecalculatesTotals()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 2, 10.00m, 0.16m);
        sale.AddDetail(_productB, 1, 50.00m, 0.00m);

        var removed = sale.RemoveDetail(_productA);

        Assert.True(removed);
        Assert.Single(sale.Details);
        Assert.Equal(50.00m, sale.Subtotal);
        Assert.Equal(0.00m, sale.TaxAmount);
        Assert.Equal(50.00m, sale.Total);
    }

    [Fact]
    public void RemoveDetail_ByDetailId_RemovesLineAndRecalculatesTotals()
    {
        var sale = new Sale("CUST-100", _userId);
        var detailA = sale.AddDetail(_productA, 2, 10.00m, 0.16m);

        var removed = sale.RemoveDetail(detailA.Id);

        Assert.True(removed);
        Assert.Empty(sale.Details);
        Assert.Equal(0.00m, sale.Subtotal);
        Assert.Equal(0.00m, sale.TaxAmount);
        Assert.Equal(0.00m, sale.Total);
    }

    [Fact]
    public void RemoveDetail_ReturnsFalseWhenNotFound()
    {
        var sale = new Sale("CUST-100", _userId);

        Assert.False(sale.RemoveDetail(_productA));
        Assert.False(sale.RemoveDetail(Guid.NewGuid()));
    }

    [Fact]
    public void DraftOnlyModifications_ThrowsWhenSaleIsConfirmed()
    {
        var sale = new Sale("CUST-100", _userId);
        var detail = sale.AddDetail(_productA, 1, 10.00m, 0.16m);
        sale.Confirm();

        Assert.Throws<InvalidOperationException>(
            () => sale.AddDetail(_productB, 1, 20m, 0m));
        Assert.Throws<InvalidOperationException>(
            () => sale.UpdateDetail(_productA, 2));
        Assert.Throws<InvalidOperationException>(
            () => sale.UpdateDetail(detail.Id, 2));
        Assert.Throws<InvalidOperationException>(
            () => sale.RemoveDetail(_productA));
        Assert.Throws<InvalidOperationException>(
            () => sale.RemoveDetail(detail.Id));
    }

    [Fact]
    public void DraftOnlyModifications_ThrowsWhenSaleIsCancelled()
    {
        var sale = new Sale("CUST-100", _userId);
        var detail = sale.AddDetail(_productA, 1, 10.00m, 0.16m);
        sale.Cancel();

        Assert.Throws<InvalidOperationException>(
            () => sale.AddDetail(_productB, 1, 20m, 0m));
        Assert.Throws<InvalidOperationException>(
            () => sale.UpdateDetail(_productA, 2));
        Assert.Throws<InvalidOperationException>(
            () => sale.UpdateDetail(detail.Id, 2));
        Assert.Throws<InvalidOperationException>(
            () => sale.RemoveDetail(_productA));
        Assert.Throws<InvalidOperationException>(
            () => sale.RemoveDetail(detail.Id));
    }

    [Fact]
    public void Confirm_TransitionsFromDraftToConfirmed_SetsConfirmedAtUtc()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 2, 25.00m, 0.16m);
        var confirmationTime = new DateTimeOffset(2026, 10, 19, 15, 30, 0, TimeSpan.Zero);

        sale.Confirm(confirmationTime);

        Assert.Equal(SaleStatus.Confirmed, sale.Status);
        Assert.Equal(confirmationTime, sale.ConfirmedAtUtc);
    }

    [Fact]
    public void Confirm_ThrowsWhenSaleHasNoDetails()
    {
        var sale = new Sale("CUST-100", _userId);

        var exception = Assert.Throws<InvalidOperationException>(() => sale.Confirm());
        Assert.Contains("at least one detail line", exception.Message);
    }

    [Fact]
    public void Confirm_ThrowsWhenAlreadyConfirmed()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 1, 10m, 0m);
        sale.Confirm();

        Assert.Throws<InvalidOperationException>(() => sale.Confirm());
    }

    [Fact]
    public void Confirm_ThrowsWhenCancelled()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 1, 10m, 0m);
        sale.Cancel();

        Assert.Throws<InvalidOperationException>(() => sale.Confirm());
    }

    [Fact]
    public void Cancel_TransitionsFromDraftToCancelled()
    {
        var sale = new Sale("CUST-100", _userId);

        sale.Cancel();

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
    }

    [Fact]
    public void Cancel_TransitionsFromConfirmedToCancelled()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 1, 10m, 0m);
        sale.Confirm();

        sale.Cancel();

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
    }

    [Fact]
    public void Cancel_ThrowsWhenAlreadyCancelled()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.Cancel();

        Assert.Throws<InvalidOperationException>(() => sale.Cancel());
    }

    [Fact]
    public void Details_CollectionIsEncapsulatedAndReadOnly()
    {
        var sale = new Sale("CUST-100", _userId);
        sale.AddDetail(_productA, 2, 10.00m, 0.16m);

        Assert.IsAssignableFrom<IReadOnlyCollection<SaleDetail>>(sale.Details);
        Assert.Throws<NotSupportedException>(() => ((ICollection<SaleDetail>)sale.Details).Add(
            new SaleDetail(_productB, 1, 5m, 0m)));
    }
}
