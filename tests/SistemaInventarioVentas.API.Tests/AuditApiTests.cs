using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class AuditApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AnonymousAccess_ReturnsUnauthorized401()
    {
        await fixture.ResetAsync();

        using var listResponse = await fixture.Client.GetAsync("/api/audit");
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);

        using var getResponse = await fixture.Client.GetAsync($"/api/audit/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
    }

    [Fact]
    public async Task NonAdminUser_ReturnsForbidden403()
    {
        await fixture.ResetAsAsync(UserRole.User);

        using var listResponse = await fixture.Client.GetAsync("/api/audit");
        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);

        using var getResponse = await fixture.Client.GetAsync($"/api/audit/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
    }

    [Fact]
    public async Task AdminUser_CanListAuditEvents_WithDeterministicOrdering()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var baseTime = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        var event1 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.SaleCreated,
            AuditEvent.EntityNames.Sale,
            "SALE-ORDER-1",
            baseTime.AddMinutes(1),
            "{\"detail\":\"first\"}");

        var event2 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            "SALE-ORDER-1",
            baseTime.AddMinutes(5),
            "{\"detail\":\"second\"}");

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddRangeAsync(event1, event2);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync("/api/audit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        Assert.Equal(2, events.Count);

        // Reverse-chronological order: event2 (12:05) then event1 (12:01)
        Assert.Equal(event2.Id, events[0].Id);
        Assert.Equal(event1.Id, events[1].Id);
    }

    [Fact]
    public async Task AdminUser_CanGetAuditEventById_WithAccurateDtoMapping()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var userId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 10, 22, 14, 30, 0, TimeSpan.Zero);
        var auditEvent = AuditEvent.CreateForUser(
            userId,
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            "MOV-9988",
            occurredAt,
            "{\"quantity\":25,\"movementType\":\"Entry\"}");

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddAsync(auditEvent);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync($"/api/audit/{auditEvent.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<AuditEventResponse>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(auditEvent.Id, dto.Id);
        Assert.Equal(AuditEvent.EventTypes.InventoryMovementRecorded, dto.EventType);
        Assert.Equal(AuditEvent.EntityNames.InventoryMovement, dto.EntityName);
        Assert.Equal("MOV-9988", dto.EntityId);
        Assert.Equal(userId, dto.UserId);
        Assert.True(dto.HasUser);
        Assert.Equal(occurredAt, dto.OccurredAtUtc);
        Assert.Equal("{\"quantity\":25,\"movementType\":\"Entry\"}", dto.MetadataJson);
    }

    [Fact]
    public async Task AdminUser_CanGetSystemInitiatedAuditEvent_WithDtoMapping()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var occurredAt = new DateTimeOffset(2026, 10, 22, 15, 0, 0, TimeSpan.Zero);
        var auditEvent = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            "unknown-user",
            occurredAt,
            "{\"ipAddress\":\"127.0.0.1\"}");

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddAsync(auditEvent);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync($"/api/audit/{auditEvent.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<AuditEventResponse>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(auditEvent.Id, dto.Id);
        Assert.Null(dto.UserId);
        Assert.False(dto.HasUser);
        Assert.Equal(AuditEvent.EventTypes.UserLoginFailed, dto.EventType);
        Assert.Equal(AuditEvent.EntityNames.User, dto.EntityName);
        Assert.Contains("\"actor\":\"system\"", dto.MetadataJson!);
    }

    [Fact]
    public async Task GetAuditEvent_WhenNotFound_ReturnsNotFound404()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var nonExistentId = Guid.NewGuid();
        using var response = await fixture.Client.GetAsync($"/api/audit/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found", problem.Title);
        Assert.Equal("Audit event was not found.", problem.Detail);
    }

    [Fact]
    public async Task GetAuditEvent_WhenEmptyGuid_ReturnsBadRequest400()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.GetAsync($"/api/audit/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Invalid request", problem.Title);
        Assert.Equal("Audit event ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task ListAuditEvents_FilterByEventType_ReturnsMatchingEventsOnly()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var event1 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.SaleCreated,
            AuditEvent.EntityNames.Sale,
            "S-1");
        var event2 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            "M-1");

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddRangeAsync(event1, event2);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync($"/api/audit?eventType={AuditEvent.EventTypes.SaleCreated}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        var matched = Assert.Single(events);
        Assert.Equal(event1.Id, matched.Id);
        Assert.Equal(AuditEvent.EventTypes.SaleCreated, matched.EventType);
    }

    [Fact]
    public async Task ListAuditEvents_FilterByEntityName_ReturnsMatchingEventsOnly()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var event1 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            "S-1");
        var event2 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.ProductActivated,
            AuditEvent.EntityNames.Product,
            "P-1");

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddRangeAsync(event1, event2);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync($"/api/audit?entityName={AuditEvent.EntityNames.Product}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        var matched = Assert.Single(events);
        Assert.Equal(event2.Id, matched.Id);
        Assert.Equal(AuditEvent.EntityNames.Product, matched.EntityName);
    }

    [Fact]
    public async Task ListAuditEvents_FilterByEntityId_ReturnsMatchingEventsOnly()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var event1 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.SaleCreated,
            AuditEvent.EntityNames.Sale,
            "TARGET-ENTITY-ID");
        var event2 = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.SaleCreated,
            AuditEvent.EntityNames.Sale,
            "OTHER-ENTITY-ID");

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddRangeAsync(event1, event2);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync("/api/audit?entityId=TARGET-ENTITY-ID");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        var matched = Assert.Single(events);
        Assert.Equal(event1.Id, matched.Id);
        Assert.Equal("TARGET-ENTITY-ID", matched.EntityId);
    }

    [Fact]
    public async Task ListAuditEvents_FilterByUserId_ReturnsMatchingEventsOnly()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var event1 = AuditEvent.CreateForUser(
            targetUserId,
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            "S-USER-1");
        var event2 = AuditEvent.CreateForUser(
            otherUserId,
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            "S-USER-2");

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddRangeAsync(event1, event2);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync($"/api/audit?userId={targetUserId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        var matched = Assert.Single(events);
        Assert.Equal(event1.Id, matched.Id);
        Assert.Equal(targetUserId, matched.UserId);
    }

    [Fact]
    public async Task ListAuditEvents_FilterByDateRange_ReturnsEventsWithinRangeOnly()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var t1 = new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 10, 30, 10, 0, 0, TimeSpan.Zero);

        var event1 = AuditEvent.CreateForSystem(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, "S-1", t1);
        var event2 = AuditEvent.CreateForSystem(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, "S-2", t2);
        var event3 = AuditEvent.CreateForSystem(AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, "S-3", t3);

        await using (var dbContext = fixture.CreateDbContext())
        {
            await dbContext.AuditEvents.AddRangeAsync(event1, event2, event3);
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.GetAsync("/api/audit?fromUtc=2026-10-10T00:00:00Z&toUtc=2026-10-20T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        var matched = Assert.Single(events);
        Assert.Equal(event2.Id, matched.Id);
    }

    [Fact]
    public async Task ListAuditEvents_WhenFromUtcGreaterThanToUtc_ReturnsBadRequest400()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.GetAsync("/api/audit?fromUtc=2026-10-25T00:00:00Z&toUtc=2026-10-10T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Invalid request", problem.Title);
        Assert.Contains("FromUtc must be less than or equal to ToUtc.", problem.Detail);
    }

    [Fact]
    public async Task EnsuringNoWriteEndpointsAreExposed()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var sampleId = Guid.NewGuid();

        using var postResponse = await fixture.Client.PostAsJsonAsync("/api/audit", new { });
        Assert.True(
            postResponse.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"Expected MethodNotAllowed or NotFound for POST /api/audit, got {postResponse.StatusCode}");

        using var putResponse = await fixture.Client.PutAsJsonAsync($"/api/audit/{sampleId}", new { });
        Assert.True(
            putResponse.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"Expected MethodNotAllowed or NotFound for PUT /api/audit/{{id}}, got {putResponse.StatusCode}");

        using var deleteResponse = await fixture.Client.DeleteAsync($"/api/audit/{sampleId}");
        Assert.True(
            deleteResponse.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"Expected MethodNotAllowed or NotFound for DELETE /api/audit/{{id}}, got {deleteResponse.StatusCode}");

        using var patchResponse = await fixture.Client.PatchAsJsonAsync($"/api/audit/{sampleId}", new { });
        Assert.True(
            patchResponse.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"Expected MethodNotAllowed or NotFound for PATCH /api/audit/{{id}}, got {patchResponse.StatusCode}");
    }
}
