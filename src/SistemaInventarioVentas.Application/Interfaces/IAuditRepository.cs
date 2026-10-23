using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface IAuditRepository
{
    Task AddAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    Task<AuditEvent?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEvent>> ListAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default);
}
