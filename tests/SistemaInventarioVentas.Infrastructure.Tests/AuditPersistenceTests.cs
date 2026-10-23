using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Repositories;

namespace SistemaInventarioVentas.Infrastructure.Tests;

public sealed class AuditPersistenceTests(PostgreSqlFixture database)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task AuditRepository_PersistsAndLoads_UserInitiatedAuditEvent_WithAllFields()
    {
        await database.ClearAsync();
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredAtUtc = new DateTimeOffset(2026, 10, 21, 14, 30, 0, TimeSpan.Zero);
        var metadataJson = "{\"action\":\"SaleConfirmed\",\"amount\":1250.50,\"currency\":\"USD\"}";

        var auditEvent = new AuditEvent(
            id: id,
            eventType: AuditEvent.EventTypes.SaleConfirmed,
            entityName: AuditEvent.EntityNames.Sale,
            entityId: "SALE-2026-001",
            userId: userId,
            occurredAtUtc: occurredAtUtc,
            metadataJson: metadataJson);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(auditEvent);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new AuditRepository(queryContext).GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.Id);
        Assert.Equal(AuditEvent.EventTypes.SaleConfirmed, loaded.EventType);
        Assert.Equal(AuditEvent.EntityNames.Sale, loaded.EntityName);
        Assert.Equal("SALE-2026-001", loaded.EntityId);
        Assert.Equal(userId, loaded.UserId);
        Assert.True(loaded.HasUser);
        Assert.Equal(occurredAtUtc.ToUnixTimeSeconds(), loaded.OccurredAtUtc.ToUnixTimeSeconds());
        Assert.Equal(TimeSpan.Zero, loaded.OccurredAtUtc.Offset);
        Assert.Equal(metadataJson, loaded.MetadataJson);
    }

    [Fact]
    public async Task AuditRepository_PersistsAndLoads_SystemInitiatedAuditEvent_WithNullUserId()
    {
        await database.ClearAsync();
        var id = Guid.NewGuid();
        var occurredAtUtc = DateTimeOffset.UtcNow;

        var auditEvent = AuditEvent.CreateForSystem(
            eventType: AuditEvent.EventTypes.UserLoginFailed,
            entityName: AuditEvent.EntityNames.User,
            entityId: "system-job-42",
            occurredAtUtc: occurredAtUtc,
            metadataJson: "{\"attemptedUser\":\"admin@store.com\"}");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(auditEvent);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new AuditRepository(queryContext).GetByIdAsync(auditEvent.Id);

        Assert.NotNull(loaded);
        Assert.Equal(auditEvent.Id, loaded.Id);
        Assert.Equal(AuditEvent.EventTypes.UserLoginFailed, loaded.EventType);
        Assert.Equal(AuditEvent.EntityNames.User, loaded.EntityName);
        Assert.Equal("system-job-42", loaded.EntityId);
        Assert.Null(loaded.UserId);
        Assert.False(loaded.HasUser);
        Assert.NotNull(loaded.MetadataJson);
        Assert.Contains("\"actor\":\"system\"", loaded.MetadataJson);
        Assert.Contains("\"attemptedUser\":\"admin@store.com\"", loaded.MetadataJson);
    }

    [Fact]
    public async Task AuditRepository_PersistsAndLoads_AuditEvent_WithNullOptionalFields()
    {
        await database.ClearAsync();
        var id = Guid.NewGuid();
        var auditEvent = new AuditEvent(
            id: id,
            eventType: AuditEvent.EventTypes.ProductActivated,
            entityName: AuditEvent.EntityNames.Product,
            entityId: null,
            userId: null,
            occurredAtUtc: null,
            metadataJson: null);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(auditEvent);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new AuditRepository(queryContext).GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.Id);
        Assert.Equal(AuditEvent.EventTypes.ProductActivated, loaded.EventType);
        Assert.Equal(AuditEvent.EntityNames.Product, loaded.EntityName);
        Assert.Null(loaded.EntityId);
        Assert.Null(loaded.UserId);
        Assert.False(loaded.HasUser);
        Assert.Null(loaded.MetadataJson);
    }

    [Fact]
    public async Task AuditRepository_GetByIdAsync_ReturnsNull_WhenRecordDoesNotExist()
    {
        await database.ClearAsync();
        await using var dbContext = database.CreateDbContext();
        var repository = new AuditRepository(dbContext);

        var loaded = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    [Fact]
    public async Task AuditRepository_ListAsync_FiltersByEventType()
    {
        await database.ClearAsync();
        var event1 = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale);
        var event2 = new AuditEvent(AuditEvent.EventTypes.SaleConfirmed, AuditEvent.EntityNames.Sale);
        var event3 = new AuditEvent(AuditEvent.EventTypes.SaleCancelled, AuditEvent.EntityNames.Sale);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(event1);
            await repository.AddAsync(event2);
            await repository.AddAsync(event3);
        }

        await using var queryContext = database.CreateDbContext();
        var repositoryQuery = new AuditRepository(queryContext);
        var result = await repositoryQuery.ListAsync(new AuditQuery(EventType: AuditEvent.EventTypes.SaleConfirmed));

        Assert.Single(result);
        Assert.Equal(event2.Id, result[0].Id);
    }

    [Fact]
    public async Task AuditRepository_ListAsync_FiltersByEntityName()
    {
        await database.ClearAsync();
        var event1 = new AuditEvent(AuditEvent.EventTypes.ProductActivated, AuditEvent.EntityNames.Product);
        var event2 = new AuditEvent(AuditEvent.EventTypes.SupplierActivated, AuditEvent.EntityNames.Supplier);
        var event3 = new AuditEvent(AuditEvent.EventTypes.CustomerActivated, AuditEvent.EntityNames.Customer);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(event1);
            await repository.AddAsync(event2);
            await repository.AddAsync(event3);
        }

        await using var queryContext = database.CreateDbContext();
        var repositoryQuery = new AuditRepository(queryContext);
        var result = await repositoryQuery.ListAsync(new AuditQuery(EntityName: AuditEvent.EntityNames.Supplier));

        Assert.Single(result);
        Assert.Equal(event2.Id, result[0].Id);
    }

    [Fact]
    public async Task AuditRepository_ListAsync_FiltersByEntityId()
    {
        await database.ClearAsync();
        var event1 = new AuditEvent(AuditEvent.EventTypes.ProductActivated, AuditEvent.EntityNames.Product, entityId: "PROD-100");
        var event2 = new AuditEvent(AuditEvent.EventTypes.ProductDeactivated, AuditEvent.EntityNames.Product, entityId: "PROD-200");
        var event3 = new AuditEvent(AuditEvent.EventTypes.ProductActivated, AuditEvent.EntityNames.Product, entityId: "PROD-100");

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(event1);
            await repository.AddAsync(event2);
            await repository.AddAsync(event3);
        }

        await using var queryContext = database.CreateDbContext();
        var repositoryQuery = new AuditRepository(queryContext);
        var result = await repositoryQuery.ListAsync(new AuditQuery(EntityId: "PROD-100"));

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("PROD-100", item.EntityId));
    }

    [Fact]
    public async Task AuditRepository_ListAsync_FiltersByUserId()
    {
        await database.ClearAsync();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var event1 = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, userId: user1);
        var event2 = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, userId: user2);
        var event3 = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, userId: null);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(event1);
            await repository.AddAsync(event2);
            await repository.AddAsync(event3);
        }

        await using var queryContext = database.CreateDbContext();
        var repositoryQuery = new AuditRepository(queryContext);
        var result = await repositoryQuery.ListAsync(new AuditQuery(UserId: user1));

        Assert.Single(result);
        Assert.Equal(event1.Id, result[0].Id);
    }

    [Fact]
    public async Task AuditRepository_ListAsync_FiltersByDateRange_InclusiveBoundaries()
    {
        await database.ClearAsync();
        var baseTime = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

        var eventBefore = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime.AddHours(-2));
        var eventAtStart = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime.AddHours(-1));
        var eventInside = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime);
        var eventAtEnd = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime.AddHours(1));
        var eventAfter = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime.AddHours(2));

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(eventBefore);
            await repository.AddAsync(eventAtStart);
            await repository.AddAsync(eventInside);
            await repository.AddAsync(eventAtEnd);
            await repository.AddAsync(eventAfter);
        }

        await using var queryContext = database.CreateDbContext();
        var repositoryQuery = new AuditRepository(queryContext);
        var result = await repositoryQuery.ListAsync(new AuditQuery(
            FromUtc: baseTime.AddHours(-1),
            ToUtc: baseTime.AddHours(1)));

        Assert.Equal(3, result.Count);
        var ids = result.Select(r => r.Id).ToHashSet();
        Assert.Contains(eventAtStart.Id, ids);
        Assert.Contains(eventInside.Id, ids);
        Assert.Contains(eventAtEnd.Id, ids);
        Assert.DoesNotContain(eventBefore.Id, ids);
        Assert.DoesNotContain(eventAfter.Id, ids);
    }

    [Fact]
    public async Task AuditRepository_ListAsync_EnforcesDeterministicOrdering_NewestFirstThenById()
    {
        await database.ClearAsync();
        var baseTime = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);

        var idA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var idB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var idOldest = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var idNewest = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var eventOldest = new AuditEvent(idOldest, AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime);
        var eventSameTimeA = new AuditEvent(idA, AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime.AddHours(1));
        var eventSameTimeB = new AuditEvent(idB, AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime.AddHours(1));
        var eventNewest = new AuditEvent(idNewest, AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, occurredAtUtc: baseTime.AddHours(2));

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(eventOldest);
            await repository.AddAsync(eventSameTimeA);
            await repository.AddAsync(eventSameTimeB);
            await repository.AddAsync(eventNewest);
        }

        await using var queryContext = database.CreateDbContext();
        var repositoryQuery = new AuditRepository(queryContext);
        var result = await repositoryQuery.ListAsync(new AuditQuery());

        Assert.Equal(4, result.Count);
        Assert.Equal(idNewest, result[0].Id);
        Assert.Equal(idA, result[1].Id); // idA < idB because of .ThenBy(audit => audit.Id)
        Assert.Equal(idB, result[2].Id);
        Assert.Equal(idOldest, result[3].Id);
    }

    [Fact]
    public async Task AuditRepository_ListAsync_ReturnsAllEvents_WhenQueryHasNoFilters()
    {
        await database.ClearAsync();
        var event1 = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale);
        var event2 = new AuditEvent(AuditEvent.EventTypes.ProductActivated, AuditEvent.EntityNames.Product);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(event1);
            await repository.AddAsync(event2);
        }

        await using var queryContext = database.CreateDbContext();
        var repositoryQuery = new AuditRepository(queryContext);
        var result = await repositoryQuery.ListAsync(new AuditQuery());

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AuditRepository_AddAsync_PreservesComplexMetadataJson_Roundtrip()
    {
        await database.ClearAsync();
        var id = Guid.NewGuid();
        var complexJson = "{\"items\":[{\"id\":1,\"sku\":\"ITEM-1\",\"qty\":5},{\"id\":2,\"sku\":\"ITEM-2\",\"qty\":10}],\"total\":150.75,\"approved\":true,\"discount\":null}";

        var auditEvent = new AuditEvent(
            id: id,
            eventType: AuditEvent.EventTypes.SaleConfirmed,
            entityName: AuditEvent.EntityNames.Sale,
            metadataJson: complexJson);

        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(auditEvent);
        }

        await using var queryContext = database.CreateDbContext();
        var loaded = await new AuditRepository(queryContext).GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(complexJson, loaded.MetadataJson);
    }

    [Fact]
    public async Task AuditRepository_ThrowsOperationCanceledException_WhenCancellationRequested()
    {
        await database.ClearAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await using var dbContext = database.CreateDbContext();
        var repository = new AuditRepository(dbContext);
        var auditEvent = new AuditEvent(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.AddAsync(auditEvent, cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByIdAsync(Guid.NewGuid(), cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.ListAsync(new AuditQuery(), cts.Token));
    }

    [Fact]
    public async Task AuditRepository_ParticipatesInExistingTransaction_RollbackPreventsPersistence()
    {
        await database.ClearAsync();
        var id = Guid.NewGuid();
        var auditEvent = new AuditEvent(id, AuditEvent.EventTypes.SaleConfirmed, AuditEvent.EntityNames.Sale);

        await using (var dbContext = database.CreateDbContext())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(auditEvent);

            // Rollback business transaction
            await transaction.RollbackAsync();
        }

        await using (var verifyContext = database.CreateDbContext())
        {
            var loaded = await new AuditRepository(verifyContext).GetByIdAsync(id);
            Assert.Null(loaded); // No phantom audit records!
        }

        // Now verify with commit
        await using (var dbContext = database.CreateDbContext())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            var repository = new AuditRepository(dbContext);
            await repository.AddAsync(auditEvent);

            await transaction.CommitAsync();
        }

        await using (var verifyContext = database.CreateDbContext())
        {
            var loaded = await new AuditRepository(verifyContext).GetByIdAsync(id);
            Assert.NotNull(loaded);
            Assert.Equal(id, loaded.Id);
        }
    }

    [Fact]
    public void AuditRepository_IsRegisteredInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddPersistence("Host=localhost;Database=test;Username=postgres;Password=postgres");

        using var provider = services.BuildServiceProvider();
        var repository = provider.GetService<IAuditRepository>();

        Assert.NotNull(repository);
        Assert.IsType<AuditRepository>(repository);
    }
}
