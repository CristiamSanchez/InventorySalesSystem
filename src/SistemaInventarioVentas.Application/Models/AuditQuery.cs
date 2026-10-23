namespace SistemaInventarioVentas.Application.Models;

public sealed record AuditQuery(
    string? EventType = null,
    string? EntityName = null,
    string? EntityId = null,
    Guid? UserId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null);
