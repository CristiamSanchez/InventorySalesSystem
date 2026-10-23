using System.Reflection;
using System.Text.Json;
using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class AuditUseCasesTests
{
    private readonly InMemoryAuditRepository _repository = new();
    private readonly TestCurrentUser _currentUser = new();

    private AuditUseCases UseCases => new(_repository, _currentUser);

    [Fact]
    public async Task RecordAsync_WithAuthenticatedUser_CreatesUserInitiatedAuditEventAndCallsAdd()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = userId;
        _currentUser.Roles = ["Admin"];

        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            "sale-101",
            "{\"total\":150.00}");

        Assert.True(result.IsSuccess);
        Assert.Equal(AuditEvent.EventTypes.SaleConfirmed, result.Value.EventType);
        Assert.Equal(AuditEvent.EntityNames.Sale, result.Value.EntityName);
        Assert.Equal("sale-101", result.Value.EntityId);
        Assert.Equal(userId, result.Value.UserId);
        Assert.True(result.Value.HasUser);
        Assert.Equal("{\"total\":150.00}", result.Value.MetadataJson);
        Assert.Equal(1, _repository.AddCount);
        Assert.Contains(result.Value, _repository.AuditEvents);
    }

    [Fact]
    public async Task RecordAsync_WithUnauthenticatedUser_CreatesSystemInitiatedAuditEventWithActorInMetadata()
    {
        _currentUser.IsAuthenticated = false;
        _currentUser.UserId = null;

        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            "mov-202",
            "{\"quantity\":10}");

        Assert.True(result.IsSuccess);
        Assert.Equal(AuditEvent.EventTypes.InventoryMovementRecorded, result.Value.EventType);
        Assert.Equal(AuditEvent.EntityNames.InventoryMovement, result.Value.EntityName);
        Assert.Equal("mov-202", result.Value.EntityId);
        Assert.Null(result.Value.UserId);
        Assert.False(result.Value.HasUser);
        Assert.NotNull(result.Value.MetadataJson);

        using var doc = JsonDocument.Parse(result.Value.MetadataJson);
        Assert.Equal("system", doc.RootElement.GetProperty("actor").GetString());
        Assert.Equal(10, doc.RootElement.GetProperty("quantity").GetInt32());
        Assert.Equal(1, _repository.AddCount);
        Assert.Contains(result.Value, _repository.AuditEvents);
    }

    [Fact]
    public async Task RecordAsync_WithNullCurrentUser_DefaultsToSystemInitiatedEvent()
    {
        var useCases = new AuditUseCases(_repository, currentUser: null);

        var result = await useCases.RecordAsync(
            AuditEvent.EventTypes.ProductActivated,
            AuditEvent.EntityNames.Product,
            "prod-303");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.UserId);
        Assert.False(result.Value.HasUser);
        Assert.Equal("{\"actor\":\"system\"}", result.Value.MetadataJson);
        Assert.Equal(1, _repository.AddCount);
    }

    [Fact]
    public async Task RecordSystemEventAsync_CreatesSystemInitiatedEventEvenIfUserIsAuthenticated()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var result = await UseCases.RecordSystemEventAsync(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            "mov-404",
            "{\"reconciled\":true}");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.UserId);
        Assert.False(result.Value.HasUser);
        Assert.NotNull(result.Value.MetadataJson);

        using var doc = JsonDocument.Parse(result.Value.MetadataJson);
        Assert.Equal("system", doc.RootElement.GetProperty("actor").GetString());
        Assert.True(doc.RootElement.GetProperty("reconciled").GetBoolean());
        Assert.Equal(1, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_WhenAuthenticatedUserHasNullUserId_ReturnsInvalidInput()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = null;

        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Authenticated user must have a valid user ID.", result.Error.Message);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_WhenAuthenticatedUserHasEmptyGuidUserId_ReturnsInvalidInput()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.Empty;

        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Authenticated user must have a valid user ID.", result.Error.Message);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public void RecordAsync_DoesNotAcceptCallerSuppliedActorIdentityParameter()
    {
        var method = typeof(AuditUseCases).GetMethod(nameof(AuditUseCases.RecordAsync));
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.DoesNotContain(parameters, p => p.Name!.Equals("userId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(parameters, p => p.Name!.Equals("actorId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(parameters, p => p.Name!.Equals("actor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RecordAsync_IgnoresCallerAttemptToOverrideActorInsideMetadata()
    {
        var realUserId = Guid.NewGuid();
        var spoofedUserId = Guid.NewGuid();

        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = realUserId;

        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            metadataJson: $"{{\"userId\":\"{spoofedUserId}\",\"total\":100.00}}");

        Assert.True(result.IsSuccess);
        Assert.Equal(realUserId, result.Value.UserId);
        Assert.NotEqual(spoofedUserId, result.Value.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordAsync_RejectsBlankEventType(string? eventType)
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var result = await UseCases.RecordAsync(eventType!, AuditEvent.EntityNames.Sale);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_RejectsEventTypeExceeding100Characters()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var longEventType = new string('E', 101);
        var result = await UseCases.RecordAsync(longEventType, AuditEvent.EntityNames.Sale);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordAsync_RejectsBlankEntityName(string? entityName)
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var result = await UseCases.RecordAsync(AuditEvent.EventTypes.SaleConfirmed, entityName!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_RejectsEntityNameExceeding100Characters()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var longEntityName = new string('N', 101);
        var result = await UseCases.RecordAsync(AuditEvent.EventTypes.SaleConfirmed, longEntityName);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_RejectsEntityIdExceeding100Characters()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var longEntityId = new string('I', 101);
        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            entityId: longEntityId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Theory]
    [InlineData("{\"password\":\"123456\"}")]
    [InlineData("{\"client_secret\":\"super-secret\"}")]
    [InlineData("{\"token\":\"bearer-token-here\"}")]
    [InlineData("{\"refreshToken\":\"refresh-token-here\"}")]
    [InlineData("{\"apiKey\":\"my-api-key\"}")]
    public async Task RecordAsync_RejectsSensitiveDataInMetadata(string sensitiveJson)
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            metadataJson: sensitiveJson);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Contains("sensitive credential", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_RejectsMalformedJsonMetadata()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            metadataJson: "{ not valid json: }");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_RejectsMetadataExceeding4000Characters()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        var hugeJson = "{\"note\":\"" + new string('X', 4000) + "\"}";
        var result = await UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            metadataJson: hugeJson);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task RecordAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RecordAsync_WhenRepositoryFails_PropagatesException()
    {
        _currentUser.IsAuthenticated = true;
        _currentUser.UserId = Guid.NewGuid();
        _repository.SimulateException = new InvalidOperationException("Simulated database failure");

        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCases.RecordAsync(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale));
    }

    [Fact]
    public async Task GetByIdAsync_WhenEmptyGuid_ReturnsInvalidInput()
    {
        var result = await UseCases.GetByIdAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var result = await UseCases.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsSuccess()
    {
        var auditEvent = AuditEvent.CreateForUser(
            Guid.NewGuid(),
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale);
        await _repository.AddAsync(auditEvent);

        var result = await UseCases.GetByIdAsync(auditEvent.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(auditEvent.Id, result.Value.Id);
    }

    [Fact]
    public async Task ListAsync_FiltersCorrectly()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var event1 = AuditEvent.CreateForUser(user1, AuditEvent.EventTypes.SaleCreated, AuditEvent.EntityNames.Sale, "sale-1");
        var event2 = AuditEvent.CreateForUser(user1, AuditEvent.EventTypes.SaleConfirmed, AuditEvent.EntityNames.Sale, "sale-1");
        var event3 = AuditEvent.CreateForUser(user2, AuditEvent.EventTypes.ProductActivated, AuditEvent.EntityNames.Product, "prod-1");
        var event4 = AuditEvent.CreateForSystem(AuditEvent.EventTypes.InventoryMovementRecorded, AuditEvent.EntityNames.InventoryMovement);

        await _repository.AddAsync(event1);
        await _repository.AddAsync(event2);
        await _repository.AddAsync(event3);
        await _repository.AddAsync(event4);

        var allResult = await UseCases.ListAsync();
        Assert.True(allResult.IsSuccess);
        Assert.Equal(4, allResult.Value.Count);

        var saleEvents = await UseCases.ListAsync(new AuditQuery(EntityName: AuditEvent.EntityNames.Sale));
        Assert.True(saleEvents.IsSuccess);
        Assert.Equal(2, saleEvents.Value.Count);

        var user1Events = await UseCases.ListAsync(new AuditQuery(UserId: user1));
        Assert.True(user1Events.IsSuccess);
        Assert.Equal(2, user1Events.Value.Count);

        var confirmedEvents = await UseCases.ListAsync(new AuditQuery(EventType: AuditEvent.EventTypes.SaleConfirmed));
        Assert.True(confirmedEvents.IsSuccess);
        Assert.Single(confirmedEvents.Value);
    }

    [Fact]
    public async Task ListAsync_WhenFromUtcGreaterThanToUtc_ReturnsInvalidInput()
    {
        var from = DateTimeOffset.UtcNow;
        var to = from.AddHours(-1);

        var result = await UseCases.ListAsync(new AuditQuery(FromUtc: from, ToUtc: to));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("FromUtc must be less than or equal to ToUtc.", result.Error.Message);
    }


    private sealed class InMemoryAuditRepository : IAuditRepository
    {
        public List<AuditEvent> AuditEvents { get; } = [];
        public int AddCount { get; private set; }
        public Exception? SimulateException { get; set; }

        public Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SimulateException != null)
            {
                throw SimulateException;
            }

            AuditEvents.Add(auditEvent);
            AddCount++;
            return Task.CompletedTask;
        }

        public Task<AuditEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(AuditEvents.SingleOrDefault(e => e.Id == id));
        }

        public Task<IReadOnlyList<AuditEvent>> ListAsync(AuditQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<AuditEvent> queryable = AuditEvents;

            if (!string.IsNullOrWhiteSpace(query.EventType))
            {
                queryable = queryable.Where(e => string.Equals(e.EventType, query.EventType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.EntityName))
            {
                queryable = queryable.Where(e => string.Equals(e.EntityName, query.EntityName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                queryable = queryable.Where(e => string.Equals(e.EntityId, query.EntityId, StringComparison.OrdinalIgnoreCase));
            }

            if (query.UserId.HasValue)
            {
                queryable = queryable.Where(e => e.UserId == query.UserId.Value);
            }

            if (query.FromUtc.HasValue)
            {
                queryable = queryable.Where(e => e.OccurredAtUtc >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                queryable = queryable.Where(e => e.OccurredAtUtc <= query.ToUtc.Value);
            }

            return Task.FromResult<IReadOnlyList<AuditEvent>>(queryable.ToList());
        }
    }
}
