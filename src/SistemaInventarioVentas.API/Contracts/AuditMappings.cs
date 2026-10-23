using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Contracts;

internal static class AuditMappings
{
    public static AuditEventResponse ToResponse(this AuditEvent auditEvent) =>
        new(
            auditEvent.Id,
            auditEvent.EventType,
            auditEvent.EntityName,
            auditEvent.EntityId,
            auditEvent.UserId,
            auditEvent.OccurredAtUtc,
            auditEvent.MetadataJson,
            auditEvent.HasUser);
}
