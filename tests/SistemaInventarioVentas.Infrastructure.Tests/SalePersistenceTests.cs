using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class SalePersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task SaleRepository_PersistsAndLoadsDraftSaleWithoutDetails()
    {
        await database.ClearAsync();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(dbContext);
            var sale = new Sale(saleId, "Acme Corporation", userId, createdAtUtc);

            await repository.AddAsync(sale);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SaleRepository(queryContext).GetByIdAsync(saleId);

        Assert.NotNull(loaded);
        Assert.Equal(saleId, loaded.Id);
        Assert.Equal("Acme Corporation", loaded.CustomerReference);
        Assert.Equal(userId, loaded.UserId);
        Assert.Equal(createdAtUtc.ToUnixTimeSeconds(), loaded.CreatedAtUtc.ToUnixTimeSeconds());
        Assert.Null(loaded.ConfirmedAtUtc);
        Assert.Equal(SaleStatus.Draft, loaded.Status);
        Assert.Equal(0m, loaded.Subtotal);
        Assert.Equal(0m, loaded.TaxAmount);
        Assert.Equal(0m, loaded.Total);
        Assert.Empty(loaded.Details);
    }

    [Fact]
    public async Task SaleRepository_PersistsAndLoadsSaleWithDetailsAndPreservesMonetaryValues()
    {
        await database.ClearAsync();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var prod1 = new ProductIdentifier("PROD-001");
        var prod2 = new ProductIdentifier("PROD-002");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(dbContext);
            var sale = new Sale(saleId, "Customer Alpha", userId);

            // Item 1: 2 * 10.50 = 21.00 subtotal, 16% tax = 3.36, total = 24.36
            sale.AddDetail(prod1, quantity: 2, unitPrice: 10.50m, taxRate: 0.16m);
            // Item 2: 3 * 5.00 = 15.00 subtotal, 0% tax = 0.00, total = 15.00
            sale.AddDetail(prod2, quantity: 3, unitPrice: 5.00m, taxRate: 0.00m);

            await repository.AddAsync(sale);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SaleRepository(queryContext).GetByIdAsync(saleId);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Details.Count);

        var detail1 = loaded.Details.Single(d => d.ProductIdentifier == prod1);
        Assert.Equal(saleId, detail1.SaleId);
        Assert.Equal(2, detail1.Quantity);
        Assert.Equal(10.50m, detail1.UnitPrice);
        Assert.Equal(0.1600m, detail1.TaxRate);
        Assert.Equal(21.00m, detail1.Subtotal);
        Assert.Equal(3.36m, detail1.TaxAmount);
        Assert.Equal(24.36m, detail1.Total);

        var detail2 = loaded.Details.Single(d => d.ProductIdentifier == prod2);
        Assert.Equal(saleId, detail2.SaleId);
        Assert.Equal(3, detail2.Quantity);
        Assert.Equal(5.00m, detail2.UnitPrice);
        Assert.Equal(0.0000m, detail2.TaxRate);
        Assert.Equal(15.00m, detail2.Subtotal);
        Assert.Equal(0.00m, detail2.TaxAmount);
        Assert.Equal(15.00m, detail2.Total);

        Assert.Equal(36.00m, loaded.Subtotal);
        Assert.Equal(3.36m, loaded.TaxAmount);
        Assert.Equal(39.36m, loaded.Total);
    }

    [Fact]
    public async Task SaleRepository_UpdatesDraftSaleDetailsAndRecalculatesTotals()
    {
        await database.ClearAsync();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var prod1 = new ProductIdentifier("PROD-001");
        var prod2 = new ProductIdentifier("PROD-002");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(dbContext);
            var sale = new Sale(saleId, "Original Customer", userId);
            sale.AddDetail(prod1, quantity: 1, unitPrice: 10.00m, taxRate: 0.16m);
            await repository.AddAsync(sale);
        }

        await using (var updateContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(updateContext);
            var sale = await repository.GetByIdAsync(saleId);
            Assert.NotNull(sale);

            sale.UpdateCustomerReference("Updated Customer");
            sale.UpdateDetail(prod1, newQuantity: 3, newUnitPrice: 12.00m, newTaxRate: 0.10m);
            sale.AddDetail(prod2, quantity: 2, unitPrice: 20.00m, taxRate: 0.16m);

            await repository.SaveAsync(sale);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SaleRepository(queryContext).GetByIdAsync(saleId);

        Assert.NotNull(loaded);
        Assert.Equal("Updated Customer", loaded.CustomerReference);
        Assert.Equal(2, loaded.Details.Count);

        // prod1: 3 * 12.00 = 36.00, tax 10% = 3.60, total = 39.60
        var detail1 = loaded.Details.Single(d => d.ProductIdentifier == prod1);
        Assert.Equal(3, detail1.Quantity);
        Assert.Equal(12.00m, detail1.UnitPrice);
        Assert.Equal(0.1000m, detail1.TaxRate);
        Assert.Equal(36.00m, detail1.Subtotal);
        Assert.Equal(3.60m, detail1.TaxAmount);
        Assert.Equal(39.60m, detail1.Total);

        // prod2: 2 * 20.00 = 40.00, tax 16% = 6.40, total = 46.40
        var detail2 = loaded.Details.Single(d => d.ProductIdentifier == prod2);
        Assert.Equal(2, detail2.Quantity);
        Assert.Equal(20.00m, detail2.UnitPrice);
        Assert.Equal(40.00m, detail2.Subtotal);
        Assert.Equal(6.40m, detail2.TaxAmount);
        Assert.Equal(46.40m, detail2.Total);

        Assert.Equal(76.00m, loaded.Subtotal);
        Assert.Equal(10.00m, loaded.TaxAmount);
        Assert.Equal(86.00m, loaded.Total);
    }

    [Fact]
    public async Task SaleRepository_RemovesSaleDetailAndPersistsDeletion()
    {
        await database.ClearAsync();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var prod1 = new ProductIdentifier("PROD-001");
        var prod2 = new ProductIdentifier("PROD-002");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(dbContext);
            var sale = new Sale(saleId, "Customer Alpha", userId);
            sale.AddDetail(prod1, quantity: 2, unitPrice: 15.00m, taxRate: 0.16m);
            sale.AddDetail(prod2, quantity: 1, unitPrice: 50.00m, taxRate: 0.00m);
            await repository.AddAsync(sale);
        }

        await using (var updateContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(updateContext);
            var sale = await repository.GetByIdAsync(saleId);
            Assert.NotNull(sale);

            var removed = sale.RemoveDetail(prod1);
            Assert.True(removed);

            await repository.SaveAsync(sale);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SaleRepository(queryContext).GetByIdAsync(saleId);

        Assert.NotNull(loaded);
        var detail = Assert.Single(loaded.Details);
        Assert.Equal(prod2, detail.ProductIdentifier);
        Assert.Equal(50.00m, loaded.Subtotal);
        Assert.Equal(0.00m, loaded.TaxAmount);
        Assert.Equal(50.00m, loaded.Total);

        var directDetailCount = await queryContext.SaleDetails
            .CountAsync(d => d.SaleId == saleId);
        Assert.Equal(1, directDetailCount);
    }

    [Fact]
    public async Task SaleRepository_HandlesDetachedSaleSave()
    {
        await database.ClearAsync();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var prod1 = new ProductIdentifier("PROD-001");
        var prod2 = new ProductIdentifier("PROD-002");
        Sale detachedSale;

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(dbContext);
            detachedSale = new Sale(saleId, "Initial Customer", userId);
            detachedSale.AddDetail(prod1, quantity: 2, unitPrice: 10.00m, taxRate: 0.16m);
            await repository.AddAsync(detachedSale);
        }

        // Apply changes to the detached instance
        detachedSale.UpdateCustomerReference("Detached Customer");
        detachedSale.AddDetail(prod2, quantity: 1, unitPrice: 30.00m, taxRate: 0.16m);
        detachedSale.UpdateDetail(prod1, newQuantity: 4);

        await using (var saveContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(saveContext);
            await repository.SaveAsync(detachedSale);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SaleRepository(queryContext).GetByIdAsync(saleId);

        Assert.NotNull(loaded);
        Assert.Equal("Detached Customer", loaded.CustomerReference);
        Assert.Equal(2, loaded.Details.Count);

        // prod1: 4 * 10 = 40, tax = 6.40, total = 46.40
        var detail1 = loaded.Details.Single(d => d.ProductIdentifier == prod1);
        Assert.Equal(4, detail1.Quantity);
        Assert.Equal(40.00m, detail1.Subtotal);

        // prod2: 1 * 30 = 30, tax = 4.80, total = 34.80
        var detail2 = loaded.Details.Single(d => d.ProductIdentifier == prod2);
        Assert.Equal(1, detail2.Quantity);
        Assert.Equal(30.00m, detail2.Subtotal);

        Assert.Equal(70.00m, loaded.Subtotal);
        Assert.Equal(11.20m, loaded.TaxAmount);
        Assert.Equal(81.20m, loaded.Total);
    }

    [Fact]
    public async Task SaleRepository_PersistsConfirmedAndCancelledStatusTransitions()
    {
        await database.ClearAsync();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var prod = new ProductIdentifier("PROD-CONFIRM");
        var confirmedTimestamp = DateTimeOffset.UtcNow.AddMinutes(1);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(dbContext);
            var sale = new Sale(saleId, "Customer Confirm", userId);
            sale.AddDetail(prod, quantity: 1, unitPrice: 100.00m, taxRate: 0.16m);
            await repository.AddAsync(sale);
        }

        // Confirm
        await using (var confirmContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(confirmContext);
            var sale = await repository.GetByIdAsync(saleId);
            Assert.NotNull(sale);

            sale.Confirm(confirmedTimestamp);
            await repository.SaveAsync(sale);
        }

        await using (var verifyContext = database.CreateDbContext())
        {
            var loaded = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
            Assert.NotNull(loaded);
            Assert.Equal(SaleStatus.Confirmed, loaded.Status);
            Assert.NotNull(loaded.ConfirmedAtUtc);
            Assert.Equal(confirmedTimestamp.ToUnixTimeSeconds(), loaded.ConfirmedAtUtc.Value.ToUnixTimeSeconds());
        }

        // Cancel
        await using (var cancelContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(cancelContext);
            var sale = await repository.GetByIdAsync(saleId);
            Assert.NotNull(sale);

            sale.Cancel();
            await repository.SaveAsync(sale);
        }

        await using (var verifyContext = database.CreateDbContext())
        {
            var loaded = await new SaleRepository(verifyContext).GetByIdAsync(saleId);
            Assert.NotNull(loaded);
            Assert.Equal(SaleStatus.Cancelled, loaded.Status);
        }
    }

    [Fact]
    public async Task SaleRepository_ReturnsNullForNonExistentSale()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new SaleRepository(dbContext);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SaleRepository_RespectsCancellationToken()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new SaleRepository(dbContext);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(Guid.NewGuid(), cancellation.Token));
    }

    [Fact]
    public async Task SaleDetails_EnforcesUniqueProductPerSaleAtDatabaseLevel()
    {
        await database.ClearAsync();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var prod = new ProductIdentifier("PROD-DUP");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SaleRepository(dbContext);
            var sale = new Sale(saleId, "Customer Dup", userId);
            sale.AddDetail(prod, quantity: 1, unitPrice: 10.00m, taxRate: 0.16m);
            await repository.AddAsync(sale);
        }

        // Attempt direct insert of duplicate (SaleId, ProductIdentifier) bypassing domain aggregate
        await using (var duplicateContext = database.CreateDbContext())
        {
            var duplicateDetail = new SaleDetail(
                Guid.NewGuid(),
                saleId,
                prod,
                quantity: 2,
                unitPrice: 15.00m,
                taxRate: 0.16m);

            await duplicateContext.SaleDetails.AddAsync(duplicateDetail);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicateContext.SaveChangesAsync());

            Assert.IsType<PostgresException>(exception.InnerException);
            var postgresEx = (PostgresException)exception.InnerException;
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresEx.SqlState);
            Assert.Equal("ux_sale_details_sale_id_product_identifier", postgresEx.ConstraintName);
        }
    }

    [Fact]
    public async Task SaleRepository_PersistsAndLoadsSaleWithCustomerIdAndSnapshottedReference()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using (var dbContext = database.CreateDbContext())
        {
            var customer = new Customer(customerId, "Corporate Client", "CORP-9999");
            await dbContext.Customers.AddAsync(customer);

            var sale = new Sale(saleId, customer.Name, userId, customerId: customer.Id);
            await new SaleRepository(dbContext).AddAsync(sale);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SaleRepository(queryContext).GetByIdAsync(saleId);

        Assert.NotNull(loaded);
        Assert.Equal(saleId, loaded.Id);
        Assert.Equal(customerId, loaded.CustomerId);
        Assert.Equal("Corporate Client", loaded.CustomerReference);
    }

    [Fact]
    public async Task SaleRepository_AllowsNullCustomerIdForWalkInSalesAlongsideCustomerSales()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var registeredSaleId = Guid.NewGuid();
        var walkInSaleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using (var dbContext = database.CreateDbContext())
        {
            var customer = new Customer(customerId, "Registered Buyer");
            await dbContext.Customers.AddAsync(customer);

            var saleRepo = new SaleRepository(dbContext);
            var registeredSale = new Sale(registeredSaleId, customer.Name, userId, customerId: customer.Id);
            var walkInSale = new Sale(walkInSaleId, "Walk-in Buyer", userId, customerId: null);

            await saleRepo.AddAsync(registeredSale);
            await saleRepo.AddAsync(walkInSale);
        }

        await using var queryContext = database.CreateDbContext();
        var saleRepoQuery = new SaleRepository(queryContext);

        var loadedRegistered = await saleRepoQuery.GetByIdAsync(registeredSaleId);
        var loadedWalkIn = await saleRepoQuery.GetByIdAsync(walkInSaleId);

        Assert.NotNull(loadedRegistered);
        Assert.Equal(customerId, loadedRegistered.CustomerId);
        Assert.Equal("Registered Buyer", loadedRegistered.CustomerReference);

        Assert.NotNull(loadedWalkIn);
        Assert.Null(loadedWalkIn.CustomerId);
        Assert.Equal("Walk-in Buyer", loadedWalkIn.CustomerReference);
    }

    [Fact]
    public async Task SaleRepository_PreservesCustomerReferenceSnapshotWhenCustomerIsModifiedOrDeactivated()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using (var dbContext = database.CreateDbContext())
        {
            var customer = new Customer(customerId, "Original Legal Name");
            await dbContext.Customers.AddAsync(customer);

            var sale = new Sale(saleId, customer.Name, userId, customerId: customer.Id);
            await new SaleRepository(dbContext).AddAsync(sale);
        }

        // Customer modifies name and deactivates later in catalog
        await using (var updateContext = database.CreateDbContext())
        {
            var customer = await updateContext.Customers.FindAsync(customerId);
            Assert.NotNull(customer);
            customer.UpdateDetails("Renamed Company Inc.");
            customer.Deactivate();
            await updateContext.SaveChangesAsync();
        }

        // Sale must preserve original snapshot and retain foreign key link
        await using var queryContext = database.CreateDbContext();
        var loadedSale = await new SaleRepository(queryContext).GetByIdAsync(saleId);

        Assert.NotNull(loadedSale);
        Assert.Equal(customerId, loadedSale.CustomerId);
        Assert.Equal("Original Legal Name", loadedSale.CustomerReference);
    }

    [Fact]
    public async Task SaleRepository_RestrictsCustomerDeletionWhenAssociatedSalesExist()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using (var dbContext = database.CreateDbContext())
        {
            var customer = new Customer(customerId, "Permanent Customer");
            await dbContext.Customers.AddAsync(customer);

            var sale = new Sale(saleId, customer.Name, userId, customerId: customer.Id);
            await new SaleRepository(dbContext).AddAsync(sale);
        }

        // Attempting to delete the customer should fail with FK restriction
        await using (var deleteContext = database.CreateDbContext())
        {
            var customer = await deleteContext.Customers.FindAsync(customerId);
            Assert.NotNull(customer);
            deleteContext.Customers.Remove(customer);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
            Assert.IsType<PostgresException>(exception.InnerException);
            var pgEx = (PostgresException)exception.InnerException;
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, pgEx.SqlState);
        }
    }

    [Fact]
    public void PersistenceRegistration_ResolvesSaleRepository()
    {
        using var dbContext = database.CreateDbContext();
        var services = new ServiceCollection();
        services.AddPersistence(dbContext.Database.GetConnectionString()!);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<SaleRepository>(provider.GetRequiredService<ISaleRepository>());
    }
}
