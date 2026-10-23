using System.Text.Json;
using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class SaleUseCasesTests
{
    private readonly InMemorySaleRepository _sales = new();
    private readonly InMemoryCatalog _catalog = new();
    private readonly InMemoryInventoryRepository _inventories = new();
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly TestCurrentUser _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();

    private SaleUseCases UseCases => new(_sales, currentUser: _currentUser);
    private SaleUseCases UseCasesWithProducts => new(_sales, _catalog, currentUser: _currentUser);
    private SaleUseCases UseCasesWithInventory => new(_sales, _catalog, _inventories, currentUser: _currentUser);
    private SaleUseCases UseCasesWithCustomers => new(_sales, _catalog, _inventories, _customers, currentUser: _currentUser);

    [Fact]
    public async Task CreateDraftAsync_WithValidData_CreatesSaleAndCallsAddAsync()
    {
        var createdAt = new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

        var result = await UseCases.CreateDraftAsync("CUST-101", _userId, createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("CUST-101", result.Value.CustomerReference);
        Assert.Equal(_userId, result.Value.UserId);
        Assert.Equal(SaleStatus.Draft, result.Value.Status);
        Assert.Equal(createdAt, result.Value.CreatedAtUtc);
        Assert.Equal(1, _sales.AddCount);
        Assert.Contains(result.Value, _sales.StoredSales);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDraftAsync_ThrowsOrReturnsInvalidInputWhenCustomerReferenceBlank(string? reference)
    {
        var result = await UseCases.CreateDraftAsync(reference!, _userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _sales.AddCount);
    }

    [Fact]
    public async Task CreateDraftAsync_ReturnsInvalidInputWhenUserIdIsEmpty()
    {
        var result = await UseCases.CreateDraftAsync("CUST-101", Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _sales.AddCount);
    }

    [Fact]
    public async Task CreateDraftAsync_WithActiveCustomerId_SnapshotsCustomerNameAndSetsCustomerId()
    {
        var customer = new Customer("Alice Johnson");
        await _customers.AddAsync(customer);

        var result = await UseCasesWithCustomers.CreateDraftAsync(
            customerReference: null,
            userId: _userId,
            customerId: customer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.CustomerId);
        Assert.Equal("Alice Johnson", result.Value.CustomerReference);
        Assert.Equal(SaleStatus.Draft, result.Value.Status);
        Assert.Equal(1, _sales.AddCount);
    }

    [Fact]
    public async Task CreateDraftAsync_WithNonexistentCustomerId_ReturnsNotFound()
    {
        var nonexistentCustomerId = Guid.NewGuid();

        var result = await UseCasesWithCustomers.CreateDraftAsync(
            customerReference: null,
            userId: _userId,
            customerId: nonexistentCustomerId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Customer was not found.", result.Error.Message);
        Assert.Equal(0, _sales.AddCount);
    }

    [Fact]
    public async Task CreateDraftAsync_WithInactiveCustomerId_ReturnsInvalidInput()
    {
        var customer = new Customer("Inactive Customer");
        customer.Deactivate();
        await _customers.AddAsync(customer);

        var result = await UseCasesWithCustomers.CreateDraftAsync(
            customerReference: null,
            userId: _userId,
            customerId: customer.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Cannot create a sale for an inactive customer.", result.Error.Message);
        Assert.Equal(0, _sales.AddCount);
    }

    [Fact]
    public async Task CreateDraftAsync_WithEmptyGuidCustomerId_ReturnsInvalidInput()
    {
        var result = await UseCasesWithCustomers.CreateDraftAsync(
            customerReference: null,
            userId: _userId,
            customerId: Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _sales.AddCount);
    }

    [Fact]
    public async Task CreateDraftAsync_PreservesHistoricalSnapshot_WhenCustomerRenamedLater()
    {
        var customer = new Customer("Original Customer Name");
        await _customers.AddAsync(customer);

        var result = await UseCasesWithCustomers.CreateDraftAsync(
            customerReference: null,
            userId: _userId,
            customerId: customer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Original Customer Name", result.Value.CustomerReference);

        // Rename customer in catalog
        customer.UpdateDetails("Updated Customer Name");
        await _customers.SaveAsync(customer);

        // Existing sale snapshot remains intact
        Assert.Equal("Original Customer Name", result.Value.CustomerReference);
    }

    [Fact]
    public async Task UpdateCustomerAsync_AssignsActiveCustomer_SnapshotsName()
    {
        var sale = new Sale("Walk-in", _userId);
        await _sales.AddAsync(sale);

        var customer = new Customer("Bob Builder");
        await _customers.AddAsync(customer);

        var result = await UseCasesWithCustomers.UpdateCustomerAsync(
            sale.Id,
            customerId: customer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.CustomerId);
        Assert.Equal("Bob Builder", result.Value.CustomerReference);
        Assert.Equal(1, _sales.SaveCount);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ClearsCustomer_UpdatesReferenceToNewWalkIn()
    {
        var customer = new Customer("Bob Builder");
        await _customers.AddAsync(customer);

        var sale = new Sale("Bob Builder", _userId, customerId: customer.Id);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithCustomers.UpdateCustomerAsync(
            sale.Id,
            customerId: null,
            customerReference: "Anonymous Walk-in");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CustomerId);
        Assert.Equal("Anonymous Walk-in", result.Value.CustomerReference);
        Assert.Equal(1, _sales.SaveCount);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ReturnsNotFound_WhenSaleDoesNotExist()
    {
        var result = await UseCasesWithCustomers.UpdateCustomerAsync(
            Guid.NewGuid(),
            customerId: Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        var sale = new Sale("Walk-in", _userId);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithCustomers.UpdateCustomerAsync(
            sale.Id,
            customerId: Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ReturnsInvalidInput_WhenCustomerIsInactive()
    {
        var sale = new Sale("Walk-in", _userId);
        await _sales.AddAsync(sale);

        var customer = new Customer("Deactivated Customer");
        customer.Deactivate();
        await _customers.AddAsync(customer);

        var result = await UseCasesWithCustomers.UpdateCustomerAsync(
            sale.Id,
            customerId: customer.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Cannot assign an inactive customer to a sale.", result.Error.Message);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ReturnsInvalidInput_WhenSaleNotDraft()
    {
        var sale = new Sale("Walk-in", _userId);
        sale.AddDetail(new ProductIdentifier("P-1"), 1, 10m, 0m);
        sale.Confirm();
        await _sales.AddAsync(sale);

        var customer = new Customer("Any Customer");
        await _customers.AddAsync(customer);

        var result = await UseCasesWithCustomers.UpdateCustomerAsync(
            sale.Id,
            customerId: customer.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("Cannot modify customer of a sale in 'Confirmed' status.", result.Error.Message);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ReturnsInvalidInput_WhenClearingWithoutCustomerReference()
    {
        var customerId = Guid.NewGuid();
        var sale = new Sale("Existing Customer", _userId, customerId: customerId);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithCustomers.UpdateCustomerAsync(
            sale.Id,
            customerId: null,
            customerReference: "   ");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSaleWhenFound()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var result = await UseCases.GetByIdAsync(sale.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(sale.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundWhenSaleDoesNotExist()
    {
        var result = await UseCases.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInvalidInputWhenIdIsEmpty()
    {
        var result = await UseCases.GetByIdAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task AddDetailAsync_AddsDetailAndCallsSaveAsync()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);
        _sales.ResetCounters();

        var result = await UseCases.AddDetailAsync(sale.Id, "PROD-10", 3, 15.00m, 0.16m);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Details);
        Assert.Equal(1, _sales.SaveCount);

        var detail = result.Value.Details.Single();
        Assert.Equal("PROD-10", detail.ProductIdentifier.Value);
        Assert.Equal(3, detail.Quantity);
        Assert.Equal(15.00m, detail.UnitPrice);
        Assert.Equal(45.00m, detail.Subtotal);
        Assert.Equal(7.20m, detail.TaxAmount);
        Assert.Equal(52.20m, detail.Total);
    }

    [Fact]
    public async Task AddDetailAsync_WithExistingProduct_IncreasesQuantity()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);
        _sales.ResetCounters();

        await UseCases.AddDetailAsync(sale.Id, "PROD-10", 2, 10.00m, 0.10m);
        var result = await UseCases.AddDetailAsync(sale.Id, "PROD-10", 3, 10.00m, 0.10m);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Details);
        var detail = result.Value.Details.Single();
        Assert.Equal(5, detail.Quantity);
        Assert.Equal(50.00m, detail.Subtotal);
        Assert.Equal(5.00m, detail.TaxAmount);
        Assert.Equal(55.00m, detail.Total);
        Assert.Equal(2, _sales.SaveCount);
    }

    [Fact]
    public async Task AddDetailAsync_ReturnsNotFoundWhenSaleDoesNotExist()
    {
        var result = await UseCases.AddDetailAsync(Guid.NewGuid(), "PROD-10", 1, 10m, 0m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task AddDetailAsync_ReturnsInvalidInputWhenSaleIdIsEmpty()
    {
        var result = await UseCases.AddDetailAsync(Guid.Empty, "PROD-10", 1, 10m, 0m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddDetailAsync_ReturnsInvalidInputWhenProductIdentifierBlank(string? identifier)
    {
        var result = await UseCases.AddDetailAsync(Guid.NewGuid(), identifier!, 1, 10m, 0m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddDetailAsync_ReturnsInvalidInputWhenQuantityIsNotPositive(int quantity)
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var result = await UseCases.AddDetailAsync(sale.Id, "PROD-10", quantity, 10m, 0m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task AddDetailAsync_ReturnsInvalidInputWhenUnitPriceOrTaxRateNegative()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var priceResult = await UseCases.AddDetailAsync(sale.Id, "PROD-10", 1, -5m, 0m);
        Assert.False(priceResult.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, priceResult.Error!.Code);

        var taxResult = await UseCases.AddDetailAsync(sale.Id, "PROD-10", 1, 5m, -0.1m);
        Assert.False(taxResult.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, taxResult.Error!.Code);
    }

    [Fact]
    public async Task AddDetailAsync_WithProductRepository_ReturnsNotFoundWhenProductNotInCatalog()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithProducts.AddDetailAsync(sale.Id, "MISSING-PROD", 1, 10m, 0m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Product was not found.", result.Error!.Message);
    }

    [Fact]
    public async Task AddDetailAsync_WithProductRepository_SucceedsWhenProductInCatalog()
    {
        var category = new Category("Category 1");
        var product = new Product(new ProductIdentifier("PROD-10"), "Existing Product", category);
        await _catalog.AddAsync(product);

        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithProducts.AddDetailAsync(sale.Id, "PROD-10", 2, 25.00m, 0.16m);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Details);
    }

    [Fact]
    public async Task AddDetailAsync_ReturnsInvalidInputWhenSaleNotDraft()
    {
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(new ProductIdentifier("PROD-10"), 1, 10m, 0m);
        sale.Confirm();
        await _sales.AddAsync(sale);

        var result = await UseCases.AddDetailAsync(sale.Id, "PROD-20", 1, 10m, 0m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateDetailAsync_ByProductIdentifier_UpdatesAndCallsSaveAsync()
    {
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(new ProductIdentifier("PROD-10"), 2, 10.00m, 0.10m);
        await _sales.AddAsync(sale);
        _sales.ResetCounters();

        var result = await UseCases.UpdateDetailAsync(sale.Id, "PROD-10", 4, 12.00m, 0.10m);

        Assert.True(result.IsSuccess);
        var detail = result.Value.Details.Single();
        Assert.Equal(4, detail.Quantity);
        Assert.Equal(12.00m, detail.UnitPrice);
        Assert.Equal(48.00m, detail.Subtotal);
        Assert.Equal(1, _sales.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailAsync_ByDetailId_UpdatesAndCallsSaveAsync()
    {
        var sale = new Sale("CUST-101", _userId);
        var original = sale.AddDetail(new ProductIdentifier("PROD-10"), 2, 10.00m, 0.10m);
        await _sales.AddAsync(sale);
        _sales.ResetCounters();

        var result = await UseCases.UpdateDetailAsync(sale.Id, original.Id, 5, 10.00m, 0.10m);

        Assert.True(result.IsSuccess);
        var detail = result.Value.Details.Single();
        Assert.Equal(5, detail.Quantity);
        Assert.Equal(50.00m, detail.Subtotal);
        Assert.Equal(1, _sales.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailAsync_ReturnsNotFoundWhenDetailNotInSale()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var resultProduct = await UseCases.UpdateDetailAsync(sale.Id, "NONEXISTENT", 2);
        Assert.False(resultProduct.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, resultProduct.Error!.Code);

        var resultId = await UseCases.UpdateDetailAsync(sale.Id, Guid.NewGuid(), 2);
        Assert.False(resultId.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, resultId.Error!.Code);
    }

    [Fact]
    public async Task UpdateDetailAsync_ReturnsInvalidInputWhenSaleNotDraft()
    {
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(new ProductIdentifier("PROD-10"), 1, 10m, 0m);
        sale.Confirm();
        await _sales.AddAsync(sale);

        var result = await UseCases.UpdateDetailAsync(sale.Id, "PROD-10", 3);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task RemoveDetailAsync_ByProductIdentifier_RemovesLineAndCallsSaveAsync()
    {
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(new ProductIdentifier("PROD-10"), 2, 10.00m, 0.10m);
        sale.AddDetail(new ProductIdentifier("PROD-20"), 1, 20.00m, 0.00m);
        await _sales.AddAsync(sale);
        _sales.ResetCounters();

        var result = await UseCases.RemoveDetailAsync(sale.Id, "PROD-10");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Details);
        Assert.Equal("PROD-20", result.Value.Details.Single().ProductIdentifier.Value);
        Assert.Equal(1, _sales.SaveCount);
    }

    [Fact]
    public async Task RemoveDetailAsync_ByDetailId_RemovesLineAndCallsSaveAsync()
    {
        var sale = new Sale("CUST-101", _userId);
        var detail = sale.AddDetail(new ProductIdentifier("PROD-10"), 2, 10.00m, 0.10m);
        await _sales.AddAsync(sale);
        _sales.ResetCounters();

        var result = await UseCases.RemoveDetailAsync(sale.Id, detail.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Details);
        Assert.Equal(1, _sales.SaveCount);
    }

    [Fact]
    public async Task RemoveDetailAsync_ReturnsNotFoundWhenDetailNotInSale()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var resultProd = await UseCases.RemoveDetailAsync(sale.Id, "MISSING");
        Assert.False(resultProd.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, resultProd.Error!.Code);

        var resultId = await UseCases.RemoveDetailAsync(sale.Id, Guid.NewGuid());
        Assert.False(resultId.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, resultId.Error!.Code);
    }

    [Fact]
    public async Task RemoveDetailAsync_ReturnsInvalidInputWhenSaleNotDraft()
    {
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(new ProductIdentifier("PROD-10"), 1, 10m, 0m);
        sale.Confirm();
        await _sales.AddAsync(sale);

        var result = await UseCases.RemoveDetailAsync(sale.Id, "PROD-10");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateCustomerReferenceAsync_UpdatesAndCallsSaveAsync()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);
        _sales.ResetCounters();

        var result = await UseCases.UpdateCustomerReferenceAsync(sale.Id, "NEW-CUST-200");

        Assert.True(result.IsSuccess);
        Assert.Equal("NEW-CUST-200", result.Value.CustomerReference);
        Assert.Equal(1, _sales.SaveCount);
    }

    [Fact]
    public async Task UpdateCustomerReferenceAsync_ReturnsInvalidInputWhenBlank()
    {
        var result = await UseCases.UpdateCustomerReferenceAsync(Guid.NewGuid(), "   ");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateCustomerReferenceAsync_ReturnsNotFoundWhenSaleDoesNotExist()
    {
        var result = await UseCases.UpdateCustomerReferenceAsync(Guid.NewGuid(), "NEW-CUST");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CancellationToken_IsPropagatedToRepository()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        await UseCases.GetByIdAsync(sale.Id, token);
        Assert.Equal(token, _sales.LastCancellationToken);

        await UseCases.CreateDraftAsync("CUST-102", _userId, cancellationToken: token);
        Assert.Equal(token, _sales.LastCancellationToken);

        await UseCases.AddDetailAsync(sale.Id, "PROD-10", 1, 10m, 0m, cancellationToken: token);
        Assert.Equal(token, _sales.LastCancellationToken);

        await UseCases.UpdateDetailAsync(sale.Id, "PROD-10", 2, cancellationToken: token);
        Assert.Equal(token, _sales.LastCancellationToken);

        await UseCases.RemoveDetailAsync(sale.Id, "PROD-10", cancellationToken: token);
        Assert.Equal(token, _sales.LastCancellationToken);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenSaleIdIsEmpty_ReturnsInvalidInput()
    {
        var result = await UseCasesWithInventory.ConfirmSaleAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Sale ID cannot be empty.", result.Error!.Message);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenInventoryRepositoryNotProvided_ThrowsInvalidOperationException()
    {
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(new ProductIdentifier("PROD-1"), 2, 10m, 0.16m);
        await _sales.AddAsync(sale);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => UseCases.ConfirmSaleAsync(sale.Id));
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenSaleNotFound_ReturnsNotFound()
    {
        var result = await UseCasesWithInventory.ConfirmSaleAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Sale was not found.", result.Error!.Message);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenSaleNotInDraftStatus_ReturnsInvalidInput()
    {
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(new ProductIdentifier("PROD-1"), 2, 10m, 0.16m);
        sale.Confirm();
        await _sales.AddAsync(sale);

        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("cannot be confirmed", result.Error!.Message);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenSaleHasNoDetails_ReturnsInvalidInput()
    {
        var sale = new Sale("CUST-101", _userId);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("A sale must contain at least one detail line before it can be confirmed.", result.Error!.Message);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenInventoryRecordMissing_ReturnsInsufficientStock()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 3, 10m, 0.16m);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
        Assert.Contains("Insufficient stock for product 'PROD-1'", result.Error!.Message);
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Equal(0, _sales.ConfirmCount);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenInventoryHasInsufficientStock_ReturnsInsufficientStock()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var inventory = new Inventory(prod1);
        inventory.RegisterEntry(5, "Initial stock");
        await _inventories.AddAsync(inventory);

        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 8, 10m, 0.16m);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
        Assert.Contains("Insufficient stock for product 'PROD-1'", result.Error!.Message);
        Assert.Equal(5, inventory.Quantity);
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Equal(0, _sales.ConfirmCount);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenOneOfMultipleProductsHasInsufficientStock_ReturnsInsufficientStockAndDoesNotDeductAnyStock()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var prod2 = new ProductIdentifier("PROD-2");

        var inv1 = new Inventory(prod1);
        inv1.RegisterEntry(10, "Initial 1");
        await _inventories.AddAsync(inv1);

        var inv2 = new Inventory(prod2);
        inv2.RegisterEntry(2, "Initial 2");
        await _inventories.AddAsync(inv2);

        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 4, 10m, 0m);
        sale.AddDetail(prod2, 5, 20m, 0m);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, result.Error!.Code);
        Assert.Contains("Insufficient stock for product 'PROD-2'", result.Error!.Message);

        Assert.Equal(10, inv1.Quantity);
        Assert.Equal(2, inv2.Quantity);
        Assert.Single(inv1.Movements);
        Assert.Single(inv2.Movements);
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Equal(0, _sales.ConfirmCount);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenSuccessful_ConfirmsSaleDeductsStockRegistersExitMovementsAndCallsConfirmSaleAsync()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var prod2 = new ProductIdentifier("PROD-2");

        var inv1 = new Inventory(prod1);
        inv1.RegisterEntry(20, "Initial stock 1");
        await _inventories.AddAsync(inv1);

        var inv2 = new Inventory(prod2);
        inv2.RegisterEntry(15, "Initial stock 2");
        await _inventories.AddAsync(inv2);

        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 6, 12.50m, 0.16m);
        sale.AddDetail(prod2, 4, 25.00m, 0.16m);
        await _sales.AddAsync(sale);

        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleStatus.Confirmed, result.Value.Status);
        Assert.NotNull(result.Value.ConfirmedAtUtc);
        Assert.Equal(14, inv1.Quantity);
        Assert.Equal(11, inv2.Quantity);
        Assert.Equal(1, _sales.ConfirmCount);

        var exit1 = inv1.Movements.Last();
        Assert.Equal(MovementType.Exit, exit1.MovementType);
        Assert.Equal(6, exit1.Quantity);
        Assert.Equal($"Sale {sale.Id}", exit1.Reference);

        var exit2 = inv2.Movements.Last();
        Assert.Equal(MovementType.Exit, exit2.MovementType);
        Assert.Equal(4, exit2.Quantity);
        Assert.Equal($"Sale {sale.Id}", exit2.Reference);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WithExplicitConfirmedAtUtc_AppliesSameTimestampToSaleAndMovements()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var inv1 = new Inventory(prod1);
        inv1.RegisterEntry(10, "Initial stock");
        await _inventories.AddAsync(inv1);

        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 3, 10m, 0m);
        await _sales.AddAsync(sale);

        var confirmedAt = new DateTimeOffset(2026, 10, 19, 14, 30, 0, TimeSpan.Zero);
        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id, confirmedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(confirmedAt, result.Value.ConfirmedAtUtc);
        Assert.Equal(confirmedAt, inv1.Movements.Last().OccurredAtUtc);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenRepositoryThrowsConcurrencyException_ReturnsConcurrencyConflict()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var inv1 = new Inventory(prod1);
        inv1.RegisterEntry(10, "Initial stock");
        await _inventories.AddAsync(inv1);

        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 3, 10m, 0m);
        await _sales.AddAsync(sale);

        _sales.OnConfirmSaleAsync = (_, _, _) =>
            throw new SistemaInventarioVentas.Application.Exceptions.ConcurrencyException("OCC Conflict");

        var result = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.ConcurrencyConflict, result.Error!.Code);
        Assert.Equal("OCC Conflict", result.Error!.Message);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenCalledAgainOnConfirmedSale_ReturnsInvalidInputAndDoesNotDeductTwice()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var inv1 = new Inventory(prod1);
        inv1.RegisterEntry(10, "Initial stock");
        await _inventories.AddAsync(inv1);

        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 3, 10m, 0m);
        await _sales.AddAsync(sale);

        var firstResult = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);
        Assert.True(firstResult.IsSuccess);
        Assert.Equal(7, inv1.Quantity);
        Assert.Equal(1, _sales.ConfirmCount);

        var secondResult = await UseCasesWithInventory.ConfirmSaleAsync(sale.Id);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, secondResult.Error!.Code);
        Assert.Contains("cannot be confirmed", secondResult.Error!.Message);
        Assert.Equal(7, inv1.Quantity);
        Assert.Equal(1, _sales.ConfirmCount);
    }

    [Fact]
    public async Task ConfirmSaleAsync_PropagatesCancellationToken()
    {
        var prod1 = new ProductIdentifier("PROD-1");
        var inv1 = new Inventory(prod1);
        inv1.RegisterEntry(10, "Initial stock");
        await _inventories.AddAsync(inv1);

        var sale = new Sale("CUST-101", _userId);
        sale.AddDetail(prod1, 3, 10m, 0m);
        await _sales.AddAsync(sale);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await UseCasesWithInventory.ConfirmSaleAsync(sale.Id, cancellationToken: token);

        Assert.Equal(token, _sales.LastCancellationToken);
        Assert.Equal(token, _inventories.LastCancellationToken);
    }

    [Fact]
    public async Task CreateDraftAsync_WithAuthenticatedUser_EmitsSaleCreatedAuditEvent()
    {
        var authUserId = Guid.NewGuid();
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = authUserId;

        var result = await UseCases.CreateDraftAsync("ACME CORP", _userId);

        Assert.True(result.IsSuccess);
        var audit = _sales.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.SaleCreated, audit.EventType);
        Assert.Equal(AuditEvent.EntityNames.Sale, audit.EntityName);
        Assert.Equal(result.Value.Id.ToString(), audit.EntityId);
        Assert.Equal(authUserId, audit.UserId);
        Assert.True(audit.HasUser);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("ACME CORP", doc.RootElement.GetProperty("customerReference").GetString());
        Assert.Equal("Draft", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateDraftAsync_WithUnauthenticatedUser_EmitsSystemInitiatedAuditEvent()
    {
        _currentUser.IsAuthenticated = false;
        _currentUser.UserId = null;

        var result = await UseCases.CreateDraftAsync("ANONYMOUS SALE", _userId);

        Assert.True(result.IsSuccess);
        var audit = _sales.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.SaleCreated, audit.EventType);
        Assert.Null(audit.UserId);
        Assert.False(audit.HasUser);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("system", doc.RootElement.GetProperty("actor").GetString());
        Assert.Equal("ANONYMOUS SALE", doc.RootElement.GetProperty("customerReference").GetString());
    }

    [Fact]
    public async Task ConfirmSaleAsync_EmitsSaleConfirmedAuditEvent()
    {
        var authUserId = Guid.NewGuid();
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = authUserId;

        var prod = new ProductIdentifier("PROD-CONFIRM-AUD");
        var category = new Category("Default");
        await _catalog.AddAsync(new Product(prod, "Widget", category));
        await _inventories.AddAsync(new Inventory(prod, initialStock: 50, initialStockReference: "Seed"));

        var saleResult = await UseCases.CreateDraftAsync("CONFIRM AUDIT", _userId);
        var saleId = saleResult.Value.Id;
        await UseCasesWithProducts.AddDetailAsync(saleId, prod, 5, 20.00m, 0.10m);
        _sales.ResetCounters();

        var confirmResult = await UseCasesWithInventory.ConfirmSaleAsync(saleId);

        Assert.True(confirmResult.IsSuccess);
        var audit = _sales.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.SaleConfirmed, audit.EventType);
        Assert.Equal(AuditEvent.EntityNames.Sale, audit.EntityName);
        Assert.Equal(saleId.ToString(), audit.EntityId);
        Assert.Equal(authUserId, audit.UserId);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("CONFIRM AUDIT", doc.RootElement.GetProperty("customerReference").GetString());
        Assert.Equal(100.00m, doc.RootElement.GetProperty("subtotal").GetDecimal());
        Assert.Equal(10.00m, doc.RootElement.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(110.00m, doc.RootElement.GetProperty("total").GetDecimal());
        Assert.Equal(1, doc.RootElement.GetProperty("itemCount").GetInt32());
        Assert.Equal("Confirmed", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelSaleAsync_TransitionsDraftSaleToCancelledAndEmitsSaleCancelledAuditEvent()
    {
        var authUserId = Guid.NewGuid();
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = authUserId;

        var saleResult = await UseCases.CreateDraftAsync("CANCEL AUDIT", _userId);
        var saleId = saleResult.Value.Id;
        _sales.ResetCounters();

        var cancelResult = await UseCases.CancelSaleAsync(saleId, "Customer requested void");

        Assert.True(cancelResult.IsSuccess);
        Assert.Equal(SaleStatus.Cancelled, cancelResult.Value.Status);

        var audit = _sales.LastAuditEvent;
        Assert.NotNull(audit);
        Assert.Equal(AuditEvent.EventTypes.SaleCancelled, audit.EventType);
        Assert.Equal(AuditEvent.EntityNames.Sale, audit.EntityName);
        Assert.Equal(saleId.ToString(), audit.EntityId);
        Assert.Equal(authUserId, audit.UserId);

        using var doc = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal("CANCEL AUDIT", doc.RootElement.GetProperty("customerReference").GetString());
        Assert.Equal("Cancelled", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Customer requested void", doc.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task CancelSaleAsync_WhenAlreadyCancelled_ReturnsInvalidInputAndDoesNotEmitAuditEvent()
    {
        var saleResult = await UseCases.CreateDraftAsync("DOUBLE CANCEL", _userId);
        var saleId = saleResult.Value.Id;
        await UseCases.CancelSaleAsync(saleId);
        _sales.ResetCounters();

        var secondCancelResult = await UseCases.CancelSaleAsync(saleId);

        Assert.False(secondCancelResult.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, secondCancelResult.Error!.Code);
        Assert.Equal("Sale is already cancelled.", secondCancelResult.Error.Message);
        Assert.Null(_sales.LastAuditEvent);
        Assert.Empty(_sales.StoredAuditEvents);
    }

    [Fact]
    public async Task CancelSaleAsync_WhenConfirmed_ReturnsInvalidInputAndDoesNotEmitAuditEvent()
    {
        var prod = new ProductIdentifier("PROD-CONFIRMED-CANCEL");
        var category = new Category("Default");
        await _catalog.AddAsync(new Product(prod, "Widget", category));
        await _inventories.AddAsync(new Inventory(prod, initialStock: 50, initialStockReference: "Seed"));

        var saleResult = await UseCases.CreateDraftAsync("CONFIRMED SALE", _userId);
        var saleId = saleResult.Value.Id;
        await UseCasesWithProducts.AddDetailAsync(saleId, prod, 2, 10.00m, 0m);
        await UseCasesWithInventory.ConfirmSaleAsync(saleId);
        _sales.ResetCounters();

        var cancelResult = await UseCases.CancelSaleAsync(saleId);

        Assert.False(cancelResult.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, cancelResult.Error!.Code);
        Assert.Contains("without inventory reversal", cancelResult.Error.Message);
        Assert.Null(_sales.LastAuditEvent);
        Assert.Empty(_sales.StoredAuditEvents);
    }

    [Fact]
    public async Task ConfirmSaleAsync_WhenInsufficientStock_ReturnsInsufficientStockAndDoesNotEmitAuditEvent()
    {
        var prod = new ProductIdentifier("PROD-OUT-OF-STOCK");
        var category = new Category("Default");
        await _catalog.AddAsync(new Product(prod, "Widget", category));
        await _inventories.AddAsync(new Inventory(prod, initialStock: 2, initialStockReference: "Seed"));

        var saleResult = await UseCases.CreateDraftAsync("FAIL CONFIRM", _userId);
        var saleId = saleResult.Value.Id;
        await UseCasesWithProducts.AddDetailAsync(saleId, prod, 10, 10.00m, 0m);
        _sales.ResetCounters();

        var confirmResult = await UseCasesWithInventory.ConfirmSaleAsync(saleId);

        Assert.False(confirmResult.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InsufficientStock, confirmResult.Error!.Code);
        Assert.Null(_sales.LastAuditEvent);
        Assert.Empty(_sales.StoredAuditEvents);
    }

    private sealed class InMemorySaleRepository : ISaleRepository
    {
        private readonly List<Sale> _items = [];
        private readonly List<AuditEvent> _auditEvents = [];

        public IReadOnlyList<Sale> StoredSales => _items;
        public IReadOnlyList<AuditEvent> StoredAuditEvents => _auditEvents;
        public AuditEvent? LastAuditEvent => _auditEvents.LastOrDefault();

        public int AddCount { get; private set; }

        public int SaveCount { get; private set; }

        public int ConfirmCount { get; private set; }

        public Func<Sale, IReadOnlyCollection<Inventory>, CancellationToken, Task>? OnConfirmSaleAsync { get; set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public void ResetCounters()
        {
            AddCount = 0;
            SaveCount = 0;
            ConfirmCount = 0;
            _auditEvents.Clear();
        }

        public Task<Sale?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(s => s.Id == id));
        }

        public Task AddAsync(
            Sale sale,
            CancellationToken cancellationToken = default) =>
            AddAsync(sale, auditEvent: null, cancellationToken);

        public Task AddAsync(
            Sale sale,
            AuditEvent? auditEvent,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            AddCount++;
            _items.Add(sale);
            if (auditEvent is not null)
            {
                _auditEvents.Add(auditEvent);
            }

            return Task.CompletedTask;
        }

        public Task SaveAsync(
            Sale sale,
            CancellationToken cancellationToken = default) =>
            SaveAsync(sale, auditEvent: null, cancellationToken);

        public Task SaveAsync(
            Sale sale,
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

        public Task ConfirmSaleAsync(
            Sale sale,
            IReadOnlyCollection<Inventory> inventories,
            CancellationToken cancellationToken = default) =>
            ConfirmSaleAsync(sale, inventories, auditEvent: null, cancellationToken);

        public async Task ConfirmSaleAsync(
            Sale sale,
            IReadOnlyCollection<Inventory> inventories,
            AuditEvent? auditEvent,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            ConfirmCount++;
            if (auditEvent is not null)
            {
                _auditEvents.Add(auditEvent);
            }

            if (OnConfirmSaleAsync is not null)
            {
                await OnConfirmSaleAsync(sale, inventories, cancellationToken);
            }
        }
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

    private sealed class InMemoryCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = [];
        public int AddCount { get; private set; }
        public int SaveCount { get; private set; }

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Customers.SingleOrDefault(c => c.Id == id));
        }

        public Task<IReadOnlyList<Customer>> ListAsync(CustomerQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Customer>>(Customers.ToList());
        }

        public Task<bool> ExistsByIdentificationNumberAsync(
            string normalizedIdentificationNumber,
            Guid? excludingId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var exists = Customers.Any(c =>
                c.Id != excludingId &&
                c.IdentificationNumber != null &&
                string.Equals(c.IdentificationNumber, normalizedIdentificationNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddCount++;
            Customers.Add(customer);
            return Task.CompletedTask;
        }

        public Task SaveAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
