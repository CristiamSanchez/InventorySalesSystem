namespace SistemaInventarioVentas.API.Contracts;

/// <summary>An audit event representation returned by the API.</summary>
public sealed record AuditEventResponse(
    Guid Id,
    string EventType,
    string EntityName,
    string? EntityId,
    Guid? UserId,
    DateTimeOffset OccurredAtUtc,
    string? MetadataJson,
    bool HasUser);
