using Microsoft.EntityFrameworkCore;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class AuditRepository(CatalogDbContext dbContext) : IAuditRepository
{
    public async Task AddAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<AuditEvent?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.AuditEvents.SingleOrDefaultAsync(
            audit => audit.Id == id,
            cancellationToken);

    public async Task<IReadOnlyList<AuditEvent>> ListAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<AuditEvent> auditEvents = dbContext.AuditEvents;

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            var eventType = query.EventType.Trim();
            auditEvents = auditEvents.Where(audit => audit.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            var entityName = query.EntityName.Trim();
            auditEvents = auditEvents.Where(audit => audit.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var entityId = query.EntityId.Trim();
            auditEvents = auditEvents.Where(audit => audit.EntityId == entityId);
        }

        if (query.UserId.HasValue)
        {
            var userId = query.UserId.Value;
            auditEvents = auditEvents.Where(audit => audit.UserId == userId);
        }

        if (query.FromUtc.HasValue)
        {
            var fromUtc = query.FromUtc.Value.ToUniversalTime();
            auditEvents = auditEvents.Where(audit => audit.OccurredAtUtc >= fromUtc);
        }

        if (query.ToUtc.HasValue)
        {
            var toUtc = query.ToUtc.Value.ToUniversalTime();
            auditEvents = auditEvents.Where(audit => audit.OccurredAtUtc <= toUtc);
        }

        return await auditEvents
            .OrderByDescending(audit => audit.OccurredAtUtc)
            .ThenBy(audit => audit.Id)
            .ToListAsync(cancellationToken);
    }
}
