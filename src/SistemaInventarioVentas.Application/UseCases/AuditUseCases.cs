using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class AuditUseCases(
    IAuditRepository auditRepository,
    ICurrentUser? currentUser = null)
{
    public async Task<Result<AuditEvent>> RecordAsync(
        string eventType,
        string entityName,
        string? entityId = null,
        string? metadataJson = null,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var isAuthenticated = currentUser?.IsAuthenticated ?? false;
        var userId = currentUser?.UserId;

        if (isAuthenticated && (!userId.HasValue || userId.Value == Guid.Empty))
        {
            return Result<AuditEvent>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Authenticated user must have a valid user ID.");
        }

        AuditEvent auditEvent;
        try
        {
            auditEvent = isAuthenticated && userId.HasValue
                ? AuditEvent.CreateForUser(
                    userId.Value,
                    eventType,
                    entityName,
                    entityId,
                    occurredAtUtc,
                    metadataJson)
                : AuditEvent.CreateForSystem(
                    eventType,
                    entityName,
                    entityId,
                    occurredAtUtc,
                    metadataJson);
        }
        catch (ArgumentException exception)
        {
            return Result<AuditEvent>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await auditRepository.AddAsync(auditEvent, cancellationToken);
        return Result<AuditEvent>.Success(auditEvent);
    }

    public async Task<Result<AuditEvent>> RecordSystemEventAsync(
        string eventType,
        string entityName,
        string? entityId = null,
        string? metadataJson = null,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        AuditEvent auditEvent;
        try
        {
            auditEvent = AuditEvent.CreateForSystem(
                eventType,
                entityName,
                entityId,
                occurredAtUtc,
                metadataJson);
        }
        catch (ArgumentException exception)
        {
            return Result<AuditEvent>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await auditRepository.AddAsync(auditEvent, cancellationToken);
        return Result<AuditEvent>.Success(auditEvent);
    }

    public async Task<Result<AuditEvent>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<AuditEvent>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Audit event ID cannot be empty.");
        }

        var auditEvent = await auditRepository.GetByIdAsync(id, cancellationToken);
        return auditEvent is null
            ? Result<AuditEvent>.Failure(ApplicationErrorCode.NotFound, "Audit event was not found.")
            : Result<AuditEvent>.Success(auditEvent);
    }

    public async Task<Result<IReadOnlyList<AuditEvent>>> ListAsync(
        AuditQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new AuditQuery();

        if (effectiveQuery.FromUtc.HasValue && effectiveQuery.ToUtc.HasValue &&
            effectiveQuery.FromUtc.Value > effectiveQuery.ToUtc.Value)
        {
            return Result<IReadOnlyList<AuditEvent>>.Failure(
                ApplicationErrorCode.InvalidInput,
                "FromUtc must be less than or equal to ToUtc.");
        }

        var normalizedQuery = effectiveQuery with
        {
            EventType = string.IsNullOrWhiteSpace(effectiveQuery.EventType) ? null : effectiveQuery.EventType.Trim(),
            EntityName = string.IsNullOrWhiteSpace(effectiveQuery.EntityName) ? null : effectiveQuery.EntityName.Trim(),
            EntityId = string.IsNullOrWhiteSpace(effectiveQuery.EntityId) ? null : effectiveQuery.EntityId.Trim(),
            FromUtc = effectiveQuery.FromUtc?.ToUniversalTime(),
            ToUtc = effectiveQuery.ToUtc?.ToUniversalTime()
        };

        var result = await auditRepository.ListAsync(normalizedQuery, cancellationToken);
        return Result<IReadOnlyList<AuditEvent>>.Success(result);
    }
}
