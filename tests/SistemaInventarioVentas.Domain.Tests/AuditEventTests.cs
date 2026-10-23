using System.Reflection;
using System.Text.Json;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class AuditEventTests
{
    [Fact]
    public void Constructor_WithValidUserInitiatedEvent_InitializesAllProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        const string metadata = "{\"total\":150.50,\"itemsCount\":2}";

        var auditEvent = new AuditEvent(
            id,
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            "sale-123",
            userId,
            occurredAt,
            metadata);

        Assert.Equal(id, auditEvent.Id);
        Assert.Equal(AuditEvent.EventTypes.SaleConfirmed, auditEvent.EventType);
        Assert.Equal(AuditEvent.EntityNames.Sale, auditEvent.EntityName);
        Assert.Equal("sale-123", auditEvent.EntityId);
        Assert.Equal(userId, auditEvent.UserId);
        Assert.True(auditEvent.HasUser);
        Assert.Equal(occurredAt.ToUniversalTime(), auditEvent.OccurredAtUtc);
        Assert.Equal(TimeSpan.Zero, auditEvent.OccurredAtUtc.Offset);
        Assert.Equal(metadata, auditEvent.MetadataJson);
    }

    [Fact]
    public void Constructor_GeneratesIdAndUtcTimestampWhenNotProvided()
    {
        var before = DateTimeOffset.UtcNow;

        var auditEvent = new AuditEvent(
            AuditEvent.EventTypes.ProductActivated,
            AuditEvent.EntityNames.Product);

        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal(AuditEvent.EventTypes.ProductActivated, auditEvent.EventType);
        Assert.Equal(AuditEvent.EntityNames.Product, auditEvent.EntityName);
        Assert.Null(auditEvent.EntityId);
        Assert.Null(auditEvent.UserId);
        Assert.False(auditEvent.HasUser);
        Assert.Null(auditEvent.MetadataJson);
        Assert.Equal(TimeSpan.Zero, auditEvent.OccurredAtUtc.Offset);
        Assert.InRange(auditEvent.OccurredAtUtc, before, after);
    }

    [Fact]
    public void Constructor_RejectsEmptyGuidId()
    {
        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            Guid.Empty,
            AuditEvent.EventTypes.ProductDeactivated,
            AuditEvent.EntityNames.Product));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsNullOrWhitespaceEventType(string? eventType)
    {
        Assert.ThrowsAny<ArgumentException>(() => new AuditEvent(
            eventType!,
            AuditEvent.EntityNames.Product));
    }

    [Fact]
    public void Constructor_RejectsEventTypeExceeding100Characters()
    {
        var longEventType = new string('A', 101);

        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            longEventType,
            AuditEvent.EntityNames.Product));

        Assert.Equal("eventType", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsEventTypeUpTo100CharactersAndTrims()
    {
        var eventType = new string('A', 100);
        var auditEvent = new AuditEvent($"  {eventType}  ", AuditEvent.EntityNames.Product);

        Assert.Equal(eventType, auditEvent.EventType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsNullOrWhitespaceEntityName(string? entityName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.SupplierActivated,
            entityName!));
    }

    [Fact]
    public void Constructor_RejectsEntityNameExceeding100Characters()
    {
        var longEntityName = new string('E', 101);

        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.SupplierActivated,
            longEntityName));

        Assert.Equal("entityName", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsEntityNameUpTo100CharactersAndTrims()
    {
        var entityName = new string('E', 100);
        var auditEvent = new AuditEvent(AuditEvent.EventTypes.SupplierActivated, $"  {entityName}  ");

        Assert.Equal(entityName, auditEvent.EntityName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NormalizesEmptyOrWhitespaceEntityIdToNull(string? entityId)
    {
        var auditEvent = new AuditEvent(
            AuditEvent.EventTypes.CustomerActivated,
            AuditEvent.EntityNames.Customer,
            entityId: entityId);

        Assert.Null(auditEvent.EntityId);
    }

    [Fact]
    public void Constructor_RejectsEntityIdExceeding100Characters()
    {
        var longEntityId = new string('I', 101);

        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.CustomerActivated,
            AuditEvent.EntityNames.Customer,
            entityId: longEntityId));

        Assert.Equal("entityId", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsEntityIdUpTo100CharactersAndTrims()
    {
        var entityId = new string('I', 100);
        var auditEvent = new AuditEvent(
            AuditEvent.EventTypes.CustomerActivated,
            AuditEvent.EntityNames.Customer,
            entityId: $"  {entityId}  ");

        Assert.Equal(entityId, auditEvent.EntityId);
    }

    [Fact]
    public void Constructor_RejectsEmptyGuidUserId()
    {
        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.SaleCreated,
            AuditEvent.EntityNames.Sale,
            userId: Guid.Empty));

        Assert.Equal("userId", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NormalizesEmptyOrWhitespaceMetadataToNull(string? metadata)
    {
        var auditEvent = new AuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            metadataJson: metadata);

        Assert.Null(auditEvent.MetadataJson);
    }

    [Fact]
    public void Constructor_RejectsMetadataExceeding4000Characters()
    {
        var hugeJson = "{\"note\":\"" + new string('X', 4000) + "\"}";

        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            metadataJson: hugeJson));

        Assert.Equal("metadataJson", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsMalformedJsonMetadata()
    {
        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            metadataJson: "{ not valid json: }"));

        Assert.Equal("metadataJson", ex.ParamName);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("\"flat string\"")]
    [InlineData("true")]
    public void Constructor_RejectsNonObjectAndNonArrayJson(string nonObjectJson)
    {
        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            metadataJson: nonObjectJson));

        Assert.Equal("metadataJson", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsJsonArrayMetadata()
    {
        const string arrayJson = "[{\"id\":1},{\"id\":2}]";

        var auditEvent = new AuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            metadataJson: arrayJson);

        Assert.Equal(arrayJson, auditEvent.MetadataJson);
    }

    [Fact]
    public void Constructor_ConvertsNonUtcTimestampToUtc()
    {
        var offsetTimestamp = new DateTimeOffset(2026, 10, 20, 15, 30, 0, TimeSpan.FromHours(5));

        var auditEvent = new AuditEvent(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            occurredAtUtc: offsetTimestamp);

        Assert.Equal(TimeSpan.Zero, auditEvent.OccurredAtUtc.Offset);
        Assert.Equal(10, auditEvent.OccurredAtUtc.Hour);
        Assert.Equal(30, auditEvent.OccurredAtUtc.Minute);
    }

    [Fact]
    public void CreateForUser_CreatesUserInitiatedEventSuccessfully()
    {
        var userId = Guid.NewGuid();

        var auditEvent = AuditEvent.CreateForUser(
            userId,
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            "sale-456",
            metadataJson: "{\"total\":200.00}");

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal(userId, auditEvent.UserId);
        Assert.True(auditEvent.HasUser);
        Assert.Equal("sale-456", auditEvent.EntityId);
        Assert.Equal("{\"total\":200.00}", auditEvent.MetadataJson);
    }

    [Fact]
    public void CreateForUser_RejectsEmptyGuidUserId()
    {
        var ex = Assert.Throws<ArgumentException>(() => AuditEvent.CreateForUser(
            Guid.Empty,
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale));

        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public void CreateForSystem_WhenMetadataIsNull_SetsSystemActorMetadata()
    {
        var auditEvent = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            "mov-789");

        Assert.Null(auditEvent.UserId);
        Assert.False(auditEvent.HasUser);
        Assert.Equal("{\"actor\":\"system\"}", auditEvent.MetadataJson);
    }

    [Fact]
    public void CreateForSystem_WhenMetadataIsProvided_MergesSystemActor()
    {
        const string inputMetadata = "{\"reason\":\"Nightly stock reconciliation\",\"itemsReconciled\":42}";

        var auditEvent = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            "mov-789",
            metadataJson: inputMetadata);

        Assert.Null(auditEvent.UserId);
        Assert.False(auditEvent.HasUser);
        Assert.NotNull(auditEvent.MetadataJson);

        using var doc = JsonDocument.Parse(auditEvent.MetadataJson);
        Assert.Equal("system", doc.RootElement.GetProperty("actor").GetString());
        Assert.Equal("Nightly stock reconciliation", doc.RootElement.GetProperty("reason").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("itemsReconciled").GetInt32());
    }

    [Fact]
    public void CreateForSystem_WhenMetadataAlreadyContainsActor_PreservesIt()
    {
        const string inputMetadata = "{\"actor\":\"system\",\"jobId\":\"job-123\"}";

        var auditEvent = AuditEvent.CreateForSystem(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            metadataJson: inputMetadata);

        Assert.Equal(inputMetadata, auditEvent.MetadataJson);
    }

    [Theory]
    [InlineData("{\"password\":\"plaintext123\"}")]
    [InlineData("{\"userPassword\":\"plaintext123\"}")]
    [InlineData("{\"PasswordHash\":\"$hash$encoded\"}")]
    [InlineData("{\"password_hash\":\"$hash$encoded\"}")]
    [InlineData("{\"secret\":\"supersecret\"}")]
    [InlineData("{\"client_secret\":\"supersecret\"}")]
    [InlineData("{\"private_key\":\"key-contents\"}")]
    [InlineData("{\"privateKey\":\"key-contents\"}")]
    [InlineData("{\"token\":\"bearer-token\"}")]
    [InlineData("{\"access_token\":\"eyJhbGciOi...\"}")]
    [InlineData("{\"accessToken\":\"eyJhbGciOi...\"}")]
    [InlineData("{\"refresh_token\":\"refresh-token\"}")]
    [InlineData("{\"refreshToken\":\"refresh-token\"}")]
    [InlineData("{\"apiKey\":\"api-key-value\"}")]
    [InlineData("{\"credentials\":\"user-creds\"}")]
    public void Constructor_RejectsSensitiveCredentialKeysInMetadata(string sensitiveJson)
    {
        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            metadataJson: sensitiveJson));

        Assert.Equal("metadataJson", ex.ParamName);
        Assert.Contains("sensitive credential", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RejectsSensitiveKeysInNestedObjectsAndArrays()
    {
        const string nestedJson = "{\"auth\":{\"nested\":{\"password\":\"12345\"}}}";

        var ex1 = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            metadataJson: nestedJson));

        Assert.Equal("metadataJson", ex1.ParamName);

        const string arrayJson = "[{\"safe\":1},{\"password\":\"12345\"}]";
        var ex2 = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            metadataJson: arrayJson));

        Assert.Equal("metadataJson", ex2.ParamName);
    }

    [Fact]
    public void Constructor_RejectsJwtTokensInMetadataValues()
    {
        const string jwtJson = "{\"receivedData\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozG6P_dcXA\"}";

        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            metadataJson: jwtJson));

        Assert.Equal("metadataJson", ex.ParamName);
        Assert.Contains("tokens", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RejectsBearerPrefixTokensInMetadataValues()
    {
        const string bearerJson = "{\"header\":\"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"}";

        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            metadataJson: bearerJson));

        Assert.Equal("metadataJson", ex.ParamName);
        Assert.Contains("tokens", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{\"conn\":\"Host=db;Password=secret123;\"}")]
    [InlineData("{\"conn\":\"Server=db;Database=test;pwd=mypass;\"}")]
    public void Constructor_RejectsConnectionStringsWithPasswordsInMetadata(string connStringJson)
    {
        var ex = Assert.Throws<ArgumentException>(() => new AuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            metadataJson: connStringJson));

        Assert.Equal("metadataJson", ex.ParamName);
        Assert.Contains("passwords", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_AcceptsLegitimateOperationalMetadata()
    {
        const string legitimateJson = "{\"total\":150.00,\"itemsCount\":3,\"attemptedUsername\":\"admin@example.com\",\"reason\":\"Invalid credentials\"}";

        var auditEvent = new AuditEvent(
            AuditEvent.EventTypes.UserLoginFailed,
            AuditEvent.EntityNames.User,
            metadataJson: legitimateJson);

        Assert.Equal(legitimateJson, auditEvent.MetadataJson);
    }

    [Fact]
    public void AuditEvent_IsAppendOnly_HasNoPublicSettersOrMutators()
    {
        var properties = typeof(AuditEvent).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            Assert.False(property.CanWrite, $"Property '{property.Name}' should not have a public setter.");
        }

        var mutatorMethods = typeof(AuditEvent).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && (
                m.Name.StartsWith("Set", StringComparison.OrdinalIgnoreCase) ||
                m.Name.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
                m.Name.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) ||
                m.Name.StartsWith("Remove", StringComparison.OrdinalIgnoreCase) ||
                m.Name.StartsWith("Modify", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.Empty(mutatorMethods);
    }

    [Fact]
    public void StandardConstants_MatchApprovedOperationalEventTypesAndEntityNames()
    {
        Assert.Equal("SaleCreated", AuditEvent.EventTypes.SaleCreated);
        Assert.Equal("SaleConfirmed", AuditEvent.EventTypes.SaleConfirmed);
        Assert.Equal("SaleCancelled", AuditEvent.EventTypes.SaleCancelled);
        Assert.Equal("InventoryMovementRecorded", AuditEvent.EventTypes.InventoryMovementRecorded);
        Assert.Equal("ProductActivated", AuditEvent.EventTypes.ProductActivated);
        Assert.Equal("ProductDeactivated", AuditEvent.EventTypes.ProductDeactivated);
        Assert.Equal("SupplierActivated", AuditEvent.EventTypes.SupplierActivated);
        Assert.Equal("SupplierDeactivated", AuditEvent.EventTypes.SupplierDeactivated);
        Assert.Equal("CustomerActivated", AuditEvent.EventTypes.CustomerActivated);
        Assert.Equal("CustomerDeactivated", AuditEvent.EventTypes.CustomerDeactivated);
        Assert.Equal("UserLoginSucceeded", AuditEvent.EventTypes.UserLoginSucceeded);
        Assert.Equal("UserLoginFailed", AuditEvent.EventTypes.UserLoginFailed);
        Assert.Equal("UserActivated", AuditEvent.EventTypes.UserActivated);
        Assert.Equal("UserDeactivated", AuditEvent.EventTypes.UserDeactivated);

        Assert.Equal("Sale", AuditEvent.EntityNames.Sale);
        Assert.Equal("InventoryMovement", AuditEvent.EntityNames.InventoryMovement);
        Assert.Equal("Product", AuditEvent.EntityNames.Product);
        Assert.Equal("Supplier", AuditEvent.EntityNames.Supplier);
        Assert.Equal("Customer", AuditEvent.EntityNames.Customer);
        Assert.Equal("User", AuditEvent.EntityNames.User);
    }
}
