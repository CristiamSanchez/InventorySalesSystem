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

public sealed class SupplierPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task SupplierRepository_PersistsAndLoadsSupplierWithAllFields()
    {
        await database.ClearAsync();
        var supplierId = Guid.NewGuid();
        var createdAtUtc = DateTimeOffset.UtcNow.AddHours(-2);
        var email = new EmailAddress("orders@megasupplies.com");

        var supplier = new Supplier(
            id: supplierId,
            name: "Mega Supplies Corp",
            contactName: "Robert Johnson",
            identificationNumber: "TAX-SUP-8899",
            email: email,
            phone: "+1-555-4321",
            address: "450 Warehouse Boulevard, Building B",
            isActive: true,
            createdAtUtc: createdAtUtc);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(supplier);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SupplierRepository(queryContext).GetByIdAsync(supplierId);

        Assert.NotNull(loaded);
        Assert.Equal(supplierId, loaded.Id);
        Assert.Equal("Mega Supplies Corp", loaded.Name);
        Assert.Equal("Robert Johnson", loaded.ContactName);
        Assert.Equal("TAX-SUP-8899", loaded.IdentificationNumber);
        Assert.NotNull(loaded.Email);
        Assert.Equal("orders@megasupplies.com", loaded.Email.Value);
        Assert.Equal("+1-555-4321", loaded.Phone);
        Assert.Equal("450 Warehouse Boulevard, Building B", loaded.Address);
        Assert.True(loaded.IsActive);
        Assert.Equal(createdAtUtc.ToUnixTimeSeconds(), loaded.CreatedAtUtc.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task SupplierRepository_PersistsAndLoadsSupplierWithNullOptionalFields()
    {
        await database.ClearAsync();
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier(
            id: supplierId,
            name: "Local Producer",
            contactName: null,
            identificationNumber: null,
            email: null,
            phone: null,
            address: null,
            isActive: true);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(supplier);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new SupplierRepository(queryContext).GetByIdAsync(supplierId);

        Assert.NotNull(loaded);
        Assert.Equal(supplierId, loaded.Id);
        Assert.Equal("Local Producer", loaded.Name);
        Assert.Null(loaded.ContactName);
        Assert.Null(loaded.IdentificationNumber);
        Assert.Null(loaded.Email);
        Assert.Null(loaded.Phone);
        Assert.Null(loaded.Address);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public async Task SupplierRepository_AllowsMultipleSuppliersWithNullIdentificationNumber()
    {
        await database.ClearAsync();

        var supplier1 = new Supplier(Guid.NewGuid(), "Supplier Alpha", identificationNumber: null);
        var supplier2 = new Supplier(Guid.NewGuid(), "Supplier Beta", identificationNumber: null);
        var supplier3 = new Supplier(Guid.NewGuid(), "Supplier Gamma", identificationNumber: null);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(supplier1);
            await repository.AddAsync(supplier2);
            await repository.AddAsync(supplier3);
        }

        await using var queryContext = database.CreateDbContext();
        var list = await new SupplierRepository(queryContext).ListAsync(new SupplierQuery());

        Assert.Equal(3, list.Count);
        Assert.All(list, s => Assert.Null(s.IdentificationNumber));
    }

    [Fact]
    public async Task SupplierRepository_AddAsync_RejectsDuplicateIdentificationNumberCaseInsensitively()
    {
        await database.ClearAsync();

        var supplier1 = new Supplier(Guid.NewGuid(), "Supplier One", identificationNumber: "TAX-UNIQUE");
        var supplier2 = new Supplier(Guid.NewGuid(), "Supplier Two", identificationNumber: "tax-unique");

        await using var dbContext = database.CreateDbContext();
        var repository = new SupplierRepository(dbContext);
        await repository.AddAsync(supplier1);

        var exception = await Assert.ThrowsAsync<DuplicateSupplierIdentificationException>(
            () => repository.AddAsync(supplier2));

        Assert.Contains("identification number already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<DbUpdateException>(exception.InnerException);
    }

    [Fact]
    public async Task SupplierRepository_SaveAsync_RejectsDuplicateIdentificationNumberCaseInsensitively()
    {
        await database.ClearAsync();

        var supplier1 = new Supplier(Guid.NewGuid(), "Supplier One", identificationNumber: "RFC-1111");
        var supplier2 = new Supplier(Guid.NewGuid(), "Supplier Two", identificationNumber: "RFC-2222");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(supplier1);
            await repository.AddAsync(supplier2);
        }

        await using var updateContext = database.CreateDbContext();
        var repo = new SupplierRepository(updateContext);
        var loadedSupplier2 = await repo.GetByIdAsync(supplier2.Id);
        Assert.NotNull(loadedSupplier2);

        loadedSupplier2.UpdateDetails(
            name: loadedSupplier2.Name,
            identificationNumber: "rfc-1111");

        var exception = await Assert.ThrowsAsync<DuplicateSupplierIdentificationException>(
            () => repo.SaveAsync(loadedSupplier2));

        Assert.Contains("identification number already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SupplierRepository_SaveAsync_AllowsUpdatingFieldsWithoutChangingIdentificationNumber()
    {
        await database.ClearAsync();

        var supplier = new Supplier(
            Guid.NewGuid(),
            "Original Supplier",
            contactName: "Old Contact",
            identificationNumber: "TAX-SAME-ID",
            phone: "+1-111");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(supplier);
        }

        await using var updateContext = database.CreateDbContext();
        var repo = new SupplierRepository(updateContext);
        var loaded = await repo.GetByIdAsync(supplier.Id);
        Assert.NotNull(loaded);

        loaded.UpdateDetails(
            name: "Updated Supplier",
            contactName: "New Contact",
            identificationNumber: "tax-same-id",
            phone: "+1-222");

        await repo.SaveAsync(loaded);

        await using var queryContext = database.CreateDbContext();
        var reloaded = await new SupplierRepository(queryContext).GetByIdAsync(supplier.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Supplier", reloaded.Name);
        Assert.Equal("New Contact", reloaded.ContactName);
        Assert.Equal("TAX-SAME-ID", reloaded.IdentificationNumber);
        Assert.Equal("+1-222", reloaded.Phone);
    }

    [Fact]
    public async Task SupplierRepository_UpdateDetailsAndStatus_PersistsCorrectly()
    {
        await database.ClearAsync();

        var supplier = new Supplier(Guid.NewGuid(), "Active Supplier");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(supplier);
        }

        await using (var updateContext = database.CreateDbContext())
        {
            var repo = new SupplierRepository(updateContext);
            var loaded = await repo.GetByIdAsync(supplier.Id);
            Assert.NotNull(loaded);

            loaded.Deactivate();
            await repo.SaveAsync(loaded);
        }

        await using (var queryContext = database.CreateDbContext())
        {
            var loaded = await new SupplierRepository(queryContext).GetByIdAsync(supplier.Id);
            Assert.NotNull(loaded);
            Assert.False(loaded.IsActive);
        }

        await using (var reactivateContext = database.CreateDbContext())
        {
            var repo = new SupplierRepository(reactivateContext);
            var loaded = await repo.GetByIdAsync(supplier.Id);
            Assert.NotNull(loaded);

            loaded.Activate();
            await repo.SaveAsync(loaded);
        }

        await using (var finalQueryContext = database.CreateDbContext())
        {
            var loaded = await new SupplierRepository(finalQueryContext).GetByIdAsync(supplier.Id);
            Assert.NotNull(loaded);
            Assert.True(loaded.IsActive);
        }
    }

    [Fact]
    public async Task SupplierRepository_ExistsByIdentificationNumberAsync_DetectsExistingAndHonorsExclusion()
    {
        await database.ClearAsync();

        var supplier1 = new Supplier(Guid.NewGuid(), "Supplier A", identificationNumber: "RFC-9988");
        var supplier2 = new Supplier(Guid.NewGuid(), "Supplier B", identificationNumber: "RFC-7766");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(supplier1);
            await repository.AddAsync(supplier2);
        }

        await using var queryContext = database.CreateDbContext();
        var repo = new SupplierRepository(queryContext);

        Assert.True(await repo.ExistsByIdentificationNumberAsync("rfc-9988"));
        Assert.True(await repo.ExistsByIdentificationNumberAsync("RFC-9988"));
        Assert.False(await repo.ExistsByIdentificationNumberAsync("RFC-0000"));
        Assert.False(await repo.ExistsByIdentificationNumberAsync("rfc-9988", excludingId: supplier1.Id));
        Assert.True(await repo.ExistsByIdentificationNumberAsync("rfc-9988", excludingId: supplier2.Id));
    }

    [Fact]
    public async Task SupplierRepository_ListAsync_FiltersByActiveStatus()
    {
        await database.ClearAsync();

        var active1 = new Supplier(Guid.NewGuid(), "Alpha Active", isActive: true);
        var active2 = new Supplier(Guid.NewGuid(), "Beta Active", isActive: true);
        var inactive1 = new Supplier(Guid.NewGuid(), "Gamma Inactive", isActive: false);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(active1);
            await repository.AddAsync(active2);
            await repository.AddAsync(inactive1);
        }

        await using var queryContext = database.CreateDbContext();
        var repo = new SupplierRepository(queryContext);

        var all = await repo.ListAsync(new SupplierQuery());
        var activeOnly = await repo.ListAsync(new SupplierQuery(IsActive: true));
        var inactiveOnly = await repo.ListAsync(new SupplierQuery(IsActive: false));

        Assert.Equal(3, all.Count);
        Assert.Equal(2, activeOnly.Count);
        Assert.All(activeOnly, s => Assert.True(s.IsActive));
        Assert.Single(inactiveOnly);
        Assert.False(inactiveOnly[0].IsActive);
    }

    [Fact]
    public async Task SupplierRepository_ListAsync_SearchesByNameContactIdentificationEmailAndPhone()
    {
        await database.ClearAsync();

        var s1 = new Supplier(
            Guid.NewGuid(),
            "Global Importers",
            contactName: "John Connor",
            identificationNumber: "TAX-GLO-1",
            email: new EmailAddress("john@globalimporters.com"),
            phone: "+1-555-1234");

        var s2 = new Supplier(
            Guid.NewGuid(),
            "National Distro",
            contactName: "Sarah Smith",
            identificationNumber: "RFC-NAT-2",
            email: new EmailAddress("sarah@distro.org"),
            phone: "+1-555-5678");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new SupplierRepository(dbContext);
            await repository.AddAsync(s1);
            await repository.AddAsync(s2);
        }

        await using var queryContext = database.CreateDbContext();
        var repo = new SupplierRepository(queryContext);

        // Search by Name
        var byName = await repo.ListAsync(new SupplierQuery(SearchTerm: "importers"));
        Assert.Single(byName);
        Assert.Equal("Global Importers", byName[0].Name);

        // Search by ContactName
        var byContact = await repo.ListAsync(new SupplierQuery(SearchTerm: "sarah"));
        Assert.Single(byContact);
        Assert.Equal("National Distro", byContact[0].Name);

        // Search by IdentificationNumber
        var byId = await repo.ListAsync(new SupplierQuery(SearchTerm: "glo-1"));
        Assert.Single(byId);
        Assert.Equal("Global Importers", byId[0].Name);

        // Search by Email
        var byEmail = await repo.ListAsync(new SupplierQuery(SearchTerm: "distro.org"));
        Assert.Single(byEmail);
        Assert.Equal("National Distro", byEmail[0].Name);

        // Search by Phone
        var byPhone = await repo.ListAsync(new SupplierQuery(SearchTerm: "1234"));
        Assert.Single(byPhone);
        Assert.Equal("Global Importers", byPhone[0].Name);

        // Search with non-matching term
        var empty = await repo.ListAsync(new SupplierQuery(SearchTerm: "non-existent"));
        Assert.Empty(empty);
    }

    [Fact]
    public void AddPersistence_RegistersISupplierRepositoryInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddPersistence("Host=localhost;Database=dummy;Username=postgres;Password=postgres");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var repository = scope.ServiceProvider.GetService<ISupplierRepository>();
        Assert.NotNull(repository);
        Assert.IsType<SupplierRepository>(repository);
    }

    [Fact]
    public async Task SupplierRepository_PropagatesCancellationToken()
    {
        await database.ClearAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;

        await using var dbContext = database.CreateDbContext();
        var repo = new SupplierRepository(dbContext);
        var supplier = new Supplier(Guid.NewGuid(), "Supplier Cancel");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repo.GetByIdAsync(supplier.Id, token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repo.ListAsync(new SupplierQuery(), token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repo.ExistsByIdentificationNumberAsync("TAX-1", cancellationToken: token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repo.AddAsync(supplier, token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repo.SaveAsync(supplier, token));
    }
}
