using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models.Reports;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class ReportingUseCasesTests
{
    private readonly InMemoryReportingRepository _repository = new();
    private ReportingUseCases UseCases => new(_repository);

    [Fact]
    public async Task GetSalesSummaryReportAsync_WithFromUtcGreaterThanToUtc_ReturnsInvalidInput()
    {
        var query = new SalesSummaryReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(1),
            ToUtc = DateTimeOffset.UtcNow
        };

        var result = await UseCases.GetSalesSummaryReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("FromUtc must be less than or equal to ToUtc", result.Error.Message);
        Assert.Null(_repository.LastSalesSummaryQuery);
    }

    [Fact]
    public async Task GetSalesSummaryReportAsync_WithEmptyCustomerId_ReturnsInvalidInput()
    {
        var query = new SalesSummaryReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ToUtc = DateTimeOffset.UtcNow,
            CustomerId = Guid.Empty
        };

        var result = await UseCases.GetSalesSummaryReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("CustomerId cannot be empty", result.Error.Message);
    }

    [Fact]
    public async Task GetSalesSummaryReportAsync_WithEmptyUserId_ReturnsInvalidInput()
    {
        var query = new SalesSummaryReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ToUtc = DateTimeOffset.UtcNow,
            UserId = Guid.Empty
        };

        var result = await UseCases.GetSalesSummaryReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("UserId cannot be empty", result.Error.Message);
    }

    [Fact]
    public async Task GetSalesSummaryReportAsync_WithInvalidStatus_ReturnsInvalidInput()
    {
        var query = new SalesSummaryReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ToUtc = DateTimeOffset.UtcNow,
            Status = (SaleStatus)999
        };

        var result = await UseCases.GetSalesSummaryReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("Invalid sale status", result.Error.Message);
    }

    [Fact]
    public async Task GetSalesSummaryReportAsync_WithValidQuery_NormalizesUtcAndCallsRepository()
    {
        var localOffset = TimeSpan.FromHours(-5);
        var from = new DateTimeOffset(2026, 10, 1, 10, 0, 0, localOffset);
        var to = new DateTimeOffset(2026, 10, 15, 18, 0, 0, localOffset);
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var query = new SalesSummaryReportQuery
        {
            FromUtc = from,
            ToUtc = to,
            CustomerId = customerId,
            UserId = userId,
            Status = SaleStatus.Confirmed
        };

        _repository.SalesSummaryReportResult = new SalesSummaryReport
        {
            FromUtc = from.ToUniversalTime(),
            ToUtc = to.ToUniversalTime(),
            TotalTransactions = 5,
            ConfirmedTransactions = 5,
            TotalRevenue = 1500m
        };

        var result = await UseCases.GetSalesSummaryReportAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalTransactions);
        Assert.Equal(1500m, result.Value.TotalRevenue);

        var executedQuery = _repository.LastSalesSummaryQuery;
        Assert.NotNull(executedQuery);
        Assert.Equal(TimeSpan.Zero, executedQuery.FromUtc.Offset);
        Assert.Equal(TimeSpan.Zero, executedQuery.ToUtc.Offset);
        Assert.Equal(from.ToUniversalTime(), executedQuery.FromUtc);
        Assert.Equal(to.ToUniversalTime(), executedQuery.ToUtc);
        Assert.Equal(customerId, executedQuery.CustomerId);
        Assert.Equal(userId, executedQuery.UserId);
        Assert.Equal(SaleStatus.Confirmed, executedQuery.Status);
    }

    [Fact]
    public async Task GetProductSalesReportAsync_WithFromUtcGreaterThanToUtc_ReturnsInvalidInput()
    {
        var query = new ProductSalesReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(1),
            ToUtc = DateTimeOffset.UtcNow
        };

        var result = await UseCases.GetProductSalesReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task GetProductSalesReportAsync_WithEmptyCategoryId_ReturnsInvalidInput()
    {
        var query = new ProductSalesReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ToUtc = DateTimeOffset.UtcNow,
            CategoryId = Guid.Empty
        };

        var result = await UseCases.GetProductSalesReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("CategoryId cannot be empty", result.Error.Message);
    }

    [Fact]
    public async Task GetProductSalesReportAsync_WithValidQuery_TrimsProductIdentifierAndNormalizesDates()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var categoryId = Guid.NewGuid();

        var query = new ProductSalesReportQuery
        {
            FromUtc = from,
            ToUtc = to,
            ProductIdentifier = "  PROD-001  ",
            CategoryId = categoryId
        };

        _repository.ProductSalesReportResult = new ProductSalesReport
        {
            FromUtc = from.ToUniversalTime(),
            ToUtc = to.ToUniversalTime(),
            TotalProductsSold = 1,
            TotalUnitsSold = 10,
            TotalRevenue = 250m
        };

        var result = await UseCases.GetProductSalesReportAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalProductsSold);
        Assert.Equal(10, result.Value.TotalUnitsSold);

        var executed = _repository.LastProductSalesQuery;
        Assert.NotNull(executed);
        Assert.Equal("PROD-001", executed.ProductIdentifier);
        Assert.Equal(categoryId, executed.CategoryId);
    }

    [Fact]
    public async Task GetProductSalesReportAsync_WithWhitespaceProductIdentifier_NormalizesToNull()
    {
        var query = new ProductSalesReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ToUtc = DateTimeOffset.UtcNow,
            ProductIdentifier = "    "
        };

        var result = await UseCases.GetProductSalesReportAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Null(_repository.LastProductSalesQuery!.ProductIdentifier);
    }

    [Fact]
    public async Task GetInventoryStatusReportAsync_WithEmptyCategoryId_ReturnsInvalidInput()
    {
        var query = new InventoryStatusReportQuery { CategoryId = Guid.Empty };

        var result = await UseCases.GetInventoryStatusReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("CategoryId cannot be empty", result.Error.Message);
    }

    [Fact]
    public async Task GetInventoryStatusReportAsync_WithNullQuery_DefaultsToEmptyQueryAndCallsRepository()
    {
        _repository.InventoryStatusReportResult = new InventoryStatusReport
        {
            TotalProducts = 12,
            TotalUnitsOnHand = 150,
            InStockProductsCount = 10,
            OutOfStockProductsCount = 2
        };

        var result = await UseCases.GetInventoryStatusReportAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value.TotalProducts);
        Assert.Equal(150, result.Value.TotalUnitsOnHand);
        Assert.NotNull(_repository.LastInventoryStatusQuery);
        Assert.Null(_repository.LastInventoryStatusQuery.CategoryId);
        Assert.Null(_repository.LastInventoryStatusQuery.InStockOnly);
    }

    [Fact]
    public async Task GetInventoryMovementReportAsync_WithFromUtcGreaterThanToUtc_ReturnsInvalidInput()
    {
        var query = new InventoryMovementReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(1),
            ToUtc = DateTimeOffset.UtcNow
        };

        var result = await UseCases.GetInventoryMovementReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task GetInventoryMovementReportAsync_WithInvalidMovementType_ReturnsInvalidInput()
    {
        var query = new InventoryMovementReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ToUtc = DateTimeOffset.UtcNow,
            MovementType = (MovementType)999
        };

        var result = await UseCases.GetInventoryMovementReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("Invalid movement type", result.Error.Message);
    }

    [Fact]
    public async Task GetInventoryMovementReportAsync_WithEmptySupplierId_ReturnsInvalidInput()
    {
        var query = new InventoryMovementReportQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ToUtc = DateTimeOffset.UtcNow,
            SupplierId = Guid.Empty
        };

        var result = await UseCases.GetInventoryMovementReportAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("SupplierId cannot be empty", result.Error.Message);
    }

    [Fact]
    public async Task GetInventoryMovementReportAsync_WithValidQuery_NormalizesAndCallsRepository()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-3);
        var to = DateTimeOffset.UtcNow;
        var supplierId = Guid.NewGuid();

        var query = new InventoryMovementReportQuery
        {
            FromUtc = from,
            ToUtc = to,
            ProductIdentifier = "  WIDGET-01  ",
            MovementType = MovementType.Entry,
            SupplierId = supplierId
        };

        _repository.InventoryMovementReportResult = new InventoryMovementReport
        {
            FromUtc = from.ToUniversalTime(),
            ToUtc = to.ToUniversalTime(),
            TotalMovements = 2,
            EntryCount = 2,
            TotalUnitsIn = 50,
            NetQuantityDelta = 50
        };

        var result = await UseCases.GetInventoryMovementReportAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalMovements);
        Assert.Equal(50, result.Value.TotalUnitsIn);

        var executed = _repository.LastInventoryMovementQuery;
        Assert.NotNull(executed);
        Assert.Equal("WIDGET-01", executed.ProductIdentifier);
        Assert.Equal(MovementType.Entry, executed.MovementType);
        Assert.Equal(supplierId, executed.SupplierId);
    }

    private sealed class InMemoryReportingRepository : IReportingRepository
    {
        public SalesSummaryReportQuery? LastSalesSummaryQuery { get; private set; }
        public SalesSummaryReport SalesSummaryReportResult { get; set; } = new();

        public ProductSalesReportQuery? LastProductSalesQuery { get; private set; }
        public ProductSalesReport ProductSalesReportResult { get; set; } = new();

        public InventoryStatusReportQuery? LastInventoryStatusQuery { get; private set; }
        public InventoryStatusReport InventoryStatusReportResult { get; set; } = new();

        public InventoryMovementReportQuery? LastInventoryMovementQuery { get; private set; }
        public InventoryMovementReport InventoryMovementReportResult { get; set; } = new();

        public Task<SalesSummaryReport> GetSalesSummaryReportAsync(
            SalesSummaryReportQuery query,
            CancellationToken cancellationToken = default)
        {
            LastSalesSummaryQuery = query;
            return Task.FromResult(SalesSummaryReportResult);
        }

        public Task<ProductSalesReport> GetProductSalesReportAsync(
            ProductSalesReportQuery query,
            CancellationToken cancellationToken = default)
        {
            LastProductSalesQuery = query;
            return Task.FromResult(ProductSalesReportResult);
        }

        public Task<InventoryStatusReport> GetInventoryStatusReportAsync(
            InventoryStatusReportQuery query,
            CancellationToken cancellationToken = default)
        {
            LastInventoryStatusQuery = query;
            return Task.FromResult(InventoryStatusReportResult);
        }

        public Task<InventoryMovementReport> GetInventoryMovementReportAsync(
            InventoryMovementReportQuery query,
            CancellationToken cancellationToken = default)
        {
            LastInventoryMovementQuery = query;
            return Task.FromResult(InventoryMovementReportResult);
        }
    }
}
