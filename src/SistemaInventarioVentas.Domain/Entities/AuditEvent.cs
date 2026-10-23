using System.Text.Json;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class AuditEvent
{
    public static class EventTypes
    {
        public const string SaleCreated = "SaleCreated";
        public const string SaleConfirmed = "SaleConfirmed";
        public const string SaleCancelled = "SaleCancelled";
        public const string InventoryMovementRecorded = "InventoryMovementRecorded";
        public const string ProductActivated = "ProductActivated";
        public const string ProductDeactivated = "ProductDeactivated";
        public const string SupplierActivated = "SupplierActivated";
        public const string SupplierDeactivated = "SupplierDeactivated";
        public const string CustomerActivated = "CustomerActivated";
        public const string CustomerDeactivated = "CustomerDeactivated";
        public const string UserLoginSucceeded = "UserLoginSucceeded";
        public const string UserLoginFailed = "UserLoginFailed";
        public const string UserActivated = "UserActivated";
        public const string UserDeactivated = "UserDeactivated";
    }

    public static class EntityNames
    {
        public const string Sale = "Sale";
        public const string InventoryMovement = "InventoryMovement";
        public const string Product = "Product";
        public const string Supplier = "Supplier";
        public const string Customer = "Customer";
        public const string User = "User";
    }

    private static readonly string[] ProhibitedSensitiveKeys =
    [
        "password",
        "passwordhash",
        "secret",
        "privatekey",
        "token",
        "accesstoken",
        "refreshtoken",
        "apikey",
        "credential"
    ];

    private AuditEvent()
    {
        EventType = string.Empty;
        EntityName = string.Empty;
    }

    public AuditEvent(
        string eventType,
        string entityName,
        string? entityId = null,
        Guid? userId = null,
        DateTimeOffset? occurredAtUtc = null,
        string? metadataJson = null)
        : this(Guid.NewGuid(), eventType, entityName, entityId, userId, occurredAtUtc, metadataJson)
    {
    }

    public AuditEvent(
        Guid id,
        string eventType,
        string entityName,
        string? entityId = null,
        Guid? userId = null,
        DateTimeOffset? occurredAtUtc = null,
        string? metadataJson = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Audit event ID cannot be empty.", nameof(id));
        }

        Id = id;
        EventType = NormalizeEventType(eventType);
        EntityName = NormalizeEntityName(entityName);
        EntityId = NormalizeEntityId(entityId);
        UserId = ValidateUserId(userId);
        OccurredAtUtc = (occurredAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        MetadataJson = NormalizeMetadata(metadataJson);
    }

    public Guid Id { get; }

    public string EventType { get; }

    public string EntityName { get; }

    public string? EntityId { get; }

    public Guid? UserId { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string? MetadataJson { get; }

    public bool HasUser => UserId.HasValue;

    public static AuditEvent CreateForUser(
        Guid userId,
        string eventType,
        string entityName,
        string? entityId = null,
        DateTimeOffset? occurredAtUtc = null,
        string? metadataJson = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        return new AuditEvent(
            Guid.NewGuid(),
            eventType,
            entityName,
            entityId,
            userId,
            occurredAtUtc,
            metadataJson);
    }

    public static AuditEvent CreateForSystem(
        string eventType,
        string entityName,
        string? entityId = null,
        DateTimeOffset? occurredAtUtc = null,
        string? metadataJson = null)
    {
        var metadata = EnsureSystemActorMetadata(metadataJson);
        return new AuditEvent(
            Guid.NewGuid(),
            eventType,
            entityName,
            entityId,
            userId: null,
            occurredAtUtc,
            metadata);
    }

    private static string NormalizeEventType(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        var trimmed = eventType.Trim();
        if (trimmed.Length > 100)
        {
            throw new ArgumentException("Event type cannot exceed 100 characters.", nameof(eventType));
        }

        return trimmed;
    }

    private static string NormalizeEntityName(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        var trimmed = entityName.Trim();
        if (trimmed.Length > 100)
        {
            throw new ArgumentException("Entity name cannot exceed 100 characters.", nameof(entityName));
        }

        return trimmed;
    }

    private static string? NormalizeEntityId(string? entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return null;
        }

        var trimmed = entityId.Trim();
        if (trimmed.Length > 100)
        {
            throw new ArgumentException("Entity ID cannot exceed 100 characters.", nameof(entityId));
        }

        return trimmed;
    }

    private static Guid? ValidateUserId(Guid? userId)
    {
        if (userId.HasValue && userId.Value == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty when provided.", nameof(userId));
        }

        return userId;
    }

    private static string? NormalizeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        var trimmed = metadataJson.Trim();
        if (trimmed.Length > 4000)
        {
            throw new ArgumentException("Metadata cannot exceed 4000 characters.", nameof(metadataJson));
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(trimmed);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Metadata must be a valid JSON document.", nameof(metadataJson), ex);
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object && doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Metadata must be a valid JSON object or array.", nameof(metadataJson));
            }

            ValidateNoSensitiveData(doc.RootElement);
        }

        return trimmed;
    }

    private static void ValidateNoSensitiveData(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var cleanName = property.Name
                        .Replace("-", string.Empty)
                        .Replace("_", string.Empty);

                    foreach (var prohibited in ProhibitedSensitiveKeys)
                    {
                        if (cleanName.Contains(prohibited, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new ArgumentException(
                                $"Audit metadata must not contain sensitive credential keys ('{property.Name}').",
                                "metadataJson");
                        }
                    }

                    ValidateNoSensitiveData(property.Value);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ValidateNoSensitiveData(item);
                }
                break;

            case JsonValueKind.String:
                var strVal = element.GetString();
                if (!string.IsNullOrEmpty(strVal))
                {
                    if (strVal.StartsWith("Bearer eyJ", StringComparison.OrdinalIgnoreCase) ||
                        (strVal.StartsWith("eyJ", StringComparison.Ordinal) && strVal.Count(c => c == '.') >= 2))
                    {
                        throw new ArgumentException(
                            "Audit metadata must not contain sensitive authentication tokens or JWTs.",
                            "metadataJson");
                    }

                    if (strVal.Contains("password=", StringComparison.OrdinalIgnoreCase) ||
                        strVal.Contains("pwd=", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException(
                            "Audit metadata must not contain connection strings with passwords.",
                            "metadataJson");
                    }
                }
                break;
        }
    }

    private static string EnsureSystemActorMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return "{\"actor\":\"system\"}";
        }

        var trimmed = metadataJson.Trim();
        using var doc = JsonDocument.Parse(trimmed);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return trimmed;
        }

        if (doc.RootElement.TryGetProperty("actor", out _))
        {
            return trimmed;
        }

        var dict = new Dictionary<string, object?>();
        dict["actor"] = "system";
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.Clone();
        }

        return JsonSerializer.Serialize(dict);
    }
}
