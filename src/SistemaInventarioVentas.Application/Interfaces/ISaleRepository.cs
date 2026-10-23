using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Sale sale,
        CancellationToken cancellationToken = default) =>
        AddAsync(sale, auditEvent: null, cancellationToken);

    Task AddAsync(
        Sale sale,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Sale sale,
        CancellationToken cancellationToken = default) =>
        SaveAsync(sale, auditEvent: null, cancellationToken);

    Task SaveAsync(
        Sale sale,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default);

    Task ConfirmSaleAsync(
        Sale sale,
        IReadOnlyCollection<Inventory> inventories,
        CancellationToken cancellationToken = default) =>
        ConfirmSaleAsync(sale, inventories, auditEvent: null, cancellationToken);

    Task ConfirmSaleAsync(
        Sale sale,
        IReadOnlyCollection<Inventory> inventories,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default);
}
