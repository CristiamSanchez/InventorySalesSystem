using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdentifierAsync(
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Inventory inventory,
        CancellationToken cancellationToken = default) =>
        AddAsync(inventory, auditEvent: null, cancellationToken);

    Task AddAsync(
        Inventory inventory,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Inventory inventory,
        CancellationToken cancellationToken = default) =>
        SaveAsync(inventory, auditEvent: null, cancellationToken);

    Task SaveAsync(
        Inventory inventory,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default);
}
