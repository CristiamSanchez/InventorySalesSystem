using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class CustomerPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task CustomerRepository_PersistsAndLoadsCustomerWithAllFields()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var createdAtUtc = DateTimeOffset.UtcNow.AddHours(-1);
        var email = new EmailAddress("billing@acme.corp");

        var customer = new Customer(
            id: customerId,
            name: "Acme Corporation",
            identificationNumber: "TAX-998877",
            email: email,
            phone: "+1-555-0100",
            address: "100 Industrial Parkway, Sector 4",
            isActive: true,
            createdAtUtc: createdAtUtc);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(customer);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new CustomerRepository(queryContext).GetByIdAsync(customerId);

        Assert.NotNull(loaded);
        Assert.Equal(customerId, loaded.Id);
        Assert.Equal("Acme Corporation", loaded.Name);
        Assert.Equal("TAX-998877", loaded.IdentificationNumber);
        Assert.NotNull(loaded.Email);
        Assert.Equal("billing@acme.corp", loaded.Email.Value);
        Assert.Equal("+1-555-0100", loaded.Phone);
        Assert.Equal("100 Industrial Parkway, Sector 4", loaded.Address);
        Assert.True(loaded.IsActive);
        Assert.Equal(createdAtUtc.ToUnixTimeSeconds(), loaded.CreatedAtUtc.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task CustomerRepository_PersistsAndLoadsCustomerWithNullOptionalFields()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var customer = new Customer(
            id: customerId,
            name: "Walk-in Buyer",
            identificationNumber: null,
            email: null,
            phone: null,
            address: null,
            isActive: true);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(customer);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new CustomerRepository(queryContext).GetByIdAsync(customerId);

        Assert.NotNull(loaded);
        Assert.Equal(customerId, loaded.Id);
        Assert.Equal("Walk-in Buyer", loaded.Name);
        Assert.Null(loaded.IdentificationNumber);
        Assert.Null(loaded.Email);
        Assert.Null(loaded.Phone);
        Assert.Null(loaded.Address);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public async Task CustomerRepository_AllowsMultipleCustomersWithNullIdentificationNumber()
    {
        await database.ClearAsync();

        var customer1 = new Customer(Guid.NewGuid(), "Customer Alpha", identificationNumber: null);
        var customer2 = new Customer(Guid.NewGuid(), "Customer Beta", identificationNumber: null);
        var customer3 = new Customer(Guid.NewGuid(), "Customer Gamma", identificationNumber: null);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(customer1);
            await repository.AddAsync(customer2);
            await repository.AddAsync(customer3);
        }

        await using var queryContext = database.CreateDbContext();
        var list = await new CustomerRepository(queryContext).ListAsync(new CustomerQuery());

        Assert.Equal(3, list.Count);
        Assert.All(list, c => Assert.Null(c.IdentificationNumber));
    }

    [Fact]
    public async Task CustomerRepository_UpdatesDetailsAndStatus_TrackedAndDetached()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var customer = new Customer(
            id: customerId,
            name: "Original Name",
            identificationNumber: "OLD-ID",
            email: new EmailAddress("old@corp.com"),
            phone: "111-222",
            address: "Old Address",
            isActive: true);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(customer);
        }

        // Update details via tracked entity
        await using (var updateContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(updateContext);
            var tracked = await repository.GetByIdAsync(customerId);
            Assert.NotNull(tracked);

            tracked.UpdateDetails(
                name: "Updated Name",
                identificationNumber: "NEW-ID",
                email: new EmailAddress("new@corp.com"),
                phone: "333-444",
                address: "New Address");
            tracked.Deactivate();

            await repository.SaveAsync(tracked);
        }

        await using (var verifyContext = database.CreateDbContext())
        {
            var loaded = await new CustomerRepository(verifyContext).GetByIdAsync(customerId);
            Assert.NotNull(loaded);
            Assert.Equal("Updated Name", loaded.Name);
            Assert.Equal("NEW-ID", loaded.IdentificationNumber);
            Assert.NotNull(loaded.Email);
            Assert.Equal("new@corp.com", loaded.Email.Value);
            Assert.Equal("333-444", loaded.Phone);
            Assert.Equal("New Address", loaded.Address);
            Assert.False(loaded.IsActive);
        }

        // Update status via detached entity
        var detached = new Customer(
            id: customerId,
            name: "Updated Name",
            identificationNumber: "NEW-ID",
            email: new EmailAddress("new@corp.com"),
            phone: "333-444",
            address: "New Address",
            isActive: true); // Reactivate

        await using (var detachedContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(detachedContext);
            await repository.SaveAsync(detached);
        }

        await using (var finalContext = database.CreateDbContext())
        {
            var loaded = await new CustomerRepository(finalContext).GetByIdAsync(customerId);
            Assert.NotNull(loaded);
            Assert.True(loaded.IsActive);
        }
    }

    [Fact]
    public async Task CustomerRepository_ListAsync_FiltersByActiveStatus()
    {
        await database.ClearAsync();

        var active1 = new Customer(Guid.NewGuid(), "Active Corp 1", isActive: true);
        var active2 = new Customer(Guid.NewGuid(), "Active Corp 2", isActive: true);
        var inactive = new Customer(Guid.NewGuid(), "Inactive Corp", isActive: false);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(active1);
            await repository.AddAsync(active2);
            await repository.AddAsync(inactive);
        }

        await using var queryContext = database.CreateDbContext();
        var repo = new CustomerRepository(queryContext);

        var allCustomers = await repo.ListAsync(new CustomerQuery());
        Assert.Equal(3, allCustomers.Count);

        var activeCustomers = await repo.ListAsync(new CustomerQuery(IsActive: true));
        Assert.Equal(2, activeCustomers.Count);
        Assert.All(activeCustomers, c => Assert.True(c.IsActive));

        var inactiveCustomers = await repo.ListAsync(new CustomerQuery(IsActive: false));
        var singleInactive = Assert.Single(inactiveCustomers);
        Assert.Equal("Inactive Corp", singleInactive.Name);
        Assert.False(singleInactive.IsActive);
    }

    [Fact]
    public async Task CustomerRepository_ListAsync_FiltersBySearchTermCaseInsensitive()
    {
        await database.ClearAsync();

        var c1 = new Customer(Guid.NewGuid(), "Acme Logistics", "RUT-100", new EmailAddress("logistics@acme.com"), "+1-555-1234");
        var c2 = new Customer(Guid.NewGuid(), "Global Retail", "RUT-200", new EmailAddress("contact@global.com"), "+1-555-5678");
        var c3 = new Customer(Guid.NewGuid(), "Pacific Shipping", "RUT-300", new EmailAddress("info@pacific.org"), "+1-555-9012");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(c1);
            await repository.AddAsync(c2);
            await repository.AddAsync(c3);
        }

        await using var queryContext = database.CreateDbContext();
        var repo = new CustomerRepository(queryContext);

        // Search by partial name
        var byName = await repo.ListAsync(new CustomerQuery(SearchTerm: "  loGIstics  "));
        Assert.Single(byName, c => c.Name == "Acme Logistics");

        // Search by identification number
        var byId = await repo.ListAsync(new CustomerQuery(SearchTerm: "rut-200"));
        Assert.Single(byId, c => c.IdentificationNumber == "RUT-200");

        // Search by email
        var byEmail = await repo.ListAsync(new CustomerQuery(SearchTerm: "PACIFIC.ORG"));
        Assert.Single(byEmail, c => c.Email!.Value == "info@pacific.org");

        // Search by phone
        var byPhone = await repo.ListAsync(new CustomerQuery(SearchTerm: "5678"));
        Assert.Single(byPhone, c => c.Phone == "+1-555-5678");
    }

    [Fact]
    public async Task CustomerRepository_ExistsByIdentificationNumberAsync_ChecksUniquenessAccurately()
    {
        await database.ClearAsync();
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId, "Acme Corp", "TAX-445566");

        await using (var dbContext = database.CreateDbContext())
        {
            await new CustomerRepository(dbContext).AddAsync(customer);
        }

        await using var queryContext = database.CreateDbContext();
        var repo = new CustomerRepository(queryContext);

        // Case-insensitive match exists
        Assert.True(await repo.ExistsByIdentificationNumberAsync("  tax-445566  "));
        Assert.True(await repo.ExistsByIdentificationNumberAsync("TAX-445566"));

        // Match exists when excluding other ID
        Assert.True(await repo.ExistsByIdentificationNumberAsync("tax-445566", excludingId: Guid.NewGuid()));

        // Excludes current customer ID
        Assert.False(await repo.ExistsByIdentificationNumberAsync("tax-445566", excludingId: customerId));

        // Non-existent ID returns false
        Assert.False(await repo.ExistsByIdentificationNumberAsync("TAX-000000"));
    }

    [Fact]
    public async Task CustomerRepository_AddAsync_TranslatesDuplicateIdentificationToDuplicateException()
    {
        await database.ClearAsync();

        var existing = new Customer(Guid.NewGuid(), "First Customer", "DUP-12345");
        var duplicate = new Customer(Guid.NewGuid(), "Second Customer", "dup-12345");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(existing);
        }

        await using (var duplicateContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(duplicateContext);
            var exception = await Assert.ThrowsAsync<DuplicateCustomerIdentificationException>(
                () => repository.AddAsync(duplicate));

            Assert.Contains("identification number already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.IsType<DbUpdateException>(exception.InnerException);
            var inner = (DbUpdateException)exception.InnerException;
            Assert.IsType<PostgresException>(inner.InnerException);
            var postgresEx = (PostgresException)inner.InnerException;
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresEx.SqlState);
            Assert.Equal("ux_customers_identification_number_key", postgresEx.ConstraintName);
        }
    }

    [Fact]
    public async Task CustomerRepository_SaveAsync_TranslatesDuplicateIdentificationToDuplicateException()
    {
        await database.ClearAsync();

        var customer1 = new Customer(Guid.NewGuid(), "First Customer", "CODE-1");
        var customer2 = new Customer(Guid.NewGuid(), "Second Customer", "CODE-2");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(dbContext);
            await repository.AddAsync(customer1);
            await repository.AddAsync(customer2);
        }

        await using (var updateContext = database.CreateDbContext())
        {
            var repository = new CustomerRepository(updateContext);
            var loaded = await repository.GetByIdAsync(customer2.Id);
            Assert.NotNull(loaded);

            loaded.UpdateDetails("Second Customer", identificationNumber: "code-1");

            var exception = await Assert.ThrowsAsync<DuplicateCustomerIdentificationException>(
                () => repository.SaveAsync(loaded));

            Assert.Contains("identification number already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.IsType<DbUpdateException>(exception.InnerException);
        }
    }

    [Fact]
    public async Task DirectInsert_ThrowsUniqueConstraintViolationOnDatabaseLevel()
    {
        await database.ClearAsync();

        var customer1 = new Customer(Guid.NewGuid(), "Customer A", "IDENT-X");
        var customer2 = new Customer(Guid.NewGuid(), "Customer B", "ident-x");

        await using (var dbContext = database.CreateDbContext())
        {
            await dbContext.Customers.AddAsync(customer1);
            await dbContext.SaveChangesAsync();
        }

        await using (var duplicateContext = database.CreateDbContext())
        {
            await duplicateContext.Customers.AddAsync(customer2);

            var dbEx = await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicateContext.SaveChangesAsync());

            Assert.IsType<PostgresException>(dbEx.InnerException);
            var pgEx = (PostgresException)dbEx.InnerException;
            Assert.Equal(PostgresErrorCodes.UniqueViolation, pgEx.SqlState);
            Assert.Equal("ux_customers_identification_number_key", pgEx.ConstraintName);
        }
    }

    [Fact]
    public async Task CustomerRepository_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await using var dbContext = database.CreateDbContext();
        var repository = new CustomerRepository(dbContext);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByIdAsync(Guid.NewGuid(), cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.ListAsync(new CustomerQuery(), cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.ExistsByIdentificationNumberAsync("ID-123", cancellationToken: cts.Token));
    }

    [Fact]
    public void PersistenceRegistration_ResolvesCustomerRepository()
    {
        using var dbContext = database.CreateDbContext();
        var services = new ServiceCollection();
        services.AddPersistence(dbContext.Database.GetConnectionString()!);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<CustomerRepository>(provider.GetRequiredService<ICustomerRepository>());
    }
}
